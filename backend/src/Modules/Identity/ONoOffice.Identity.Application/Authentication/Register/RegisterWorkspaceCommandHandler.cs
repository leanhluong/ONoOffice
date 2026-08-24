using Luong.Kernel.Abstractions;
using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Application.Authentication.Login;
using ONoOffice.Identity.Domain;
using ONoOffice.Identity.Domain.Entities;
using ONoOffice.Identity.Domain.ValueObjects;

namespace ONoOffice.Identity.Application.Authentication.Register;

public sealed record RegisterWorkspaceCommand(
    string CompanyName,
    string WorkspaceCode,
    string FullName,
    string Email,
    string Password) : ICommand<RegisterWorkspaceResponse>;

public sealed record RegisteredWorkspace(Guid Id, string Code, string Name);

public sealed record RegisterWorkspaceResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    LoggedInUser User,
    RegisteredWorkspace Workspace);

/// <summary>
/// Dựng một workspace mới cùng người chủ của nó.
///
/// Đây là use case tạo <b>ba thứ một lúc</b>: công ty, bốn vai trò hệ thống, và tài khoản
/// chủ sở hữu. Chính là việc mà <c>IdentityDataSeeder</c> làm bằng tay ở môi trường phát
/// triển — khác biệt duy nhất là ở đây người lạ trên Internet gọi vào, nên mọi thứ phải
/// được kiểm.
///
/// <b>Thứ tự bốn bước không đảo được:</b> phải có <c>Tenant</c> trước (mọi thứ khác mang
/// <c>tenant_id</c> của nó), rồi <c>Role</c> (để có cái mà gán), rồi <c>User</c>, rồi mới
/// gán chủ. Đảo lại thì hoặc vi phạm ràng buộc, hoặc tệ hơn — tạo ra một workspace không
/// ai vào được, mà cũng không có lỗi nào báo.
///
/// Cả bốn bước nằm trong MỘT transaction, do <c>TransactionBehavior</c> lo. Không có nó
/// thì một lần hỏng giữa chừng để lại một công ty không có chủ, và không đường nào sửa
/// ngoài can thiệp thẳng vào database.
/// </summary>
internal sealed class RegisterWorkspaceCommandHandler(
    ITenantRepository tenants,
    IRoleRepository roles,
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RegisterWorkspaceCommand, RegisterWorkspaceResponse>
{
    /// <summary>Khớp với hạn ghi trong ADR-0002.</summary>
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public async Task<Result<RegisterWorkspaceResponse>> Handle(
        RegisterWorkspaceCommand command,
        CancellationToken cancellationToken)
    {
        // ── Kiểm định dạng trước khi hỏi database ────────────────────────
        //
        // Mã sai định dạng thì không thể trùng với ai — hỏi database là một vòng đi về
        // thừa, và tệ hơn: nó cho kẻ đang dò một cách đo xem mã nào tồn tại.
        var code = TenantCode.Create(command.WorkspaceCode);

        if (code.IsFailure)
        {
            return code.Error;
        }

        var email = Email.Create(command.Email);

        if (email.IsFailure)
        {
            return email.Error;
        }

        // ── Kiểm trùng ───────────────────────────────────────────────────
        //
        // Mã workspace TRƯỚC email, và đây không phải chuyện thẩm mỹ: người dùng sửa mã
        // workspace dễ hơn nhiều so với đổi email công ty. Báo cái dễ sửa trước thì họ đi
        // tiếp được ngay thay vì phải quay lại hai lần.
        if (await tenants.IsCodeTakenAsync(code.Value.Value, cancellationToken))
        {
            return IdentityErrors.TenantCodes.Taken;
        }

        if (await users.IsEmailTakenAsync(email.Value.Value, cancellationToken))
        {
            return IdentityErrors.Emails.Taken;
        }

        // ── ① Workspace ──────────────────────────────────────────────────
        var tenant = Tenant.Create(command.WorkspaceCode, command.CompanyName);

        if (tenant.IsFailure)
        {
            return tenant.Error;
        }

        // ── ② Bốn vai trò hệ thống ───────────────────────────────────────
        var systemRoles = new List<Role>(SystemRoles.All.Count);

        foreach (var definition in SystemRoles.All)
        {
            var role = definition.CreateFor(tenant.Value.Id);

            if (role.IsFailure)
            {
                return role.Error;
            }

            systemRoles.Add(role.Value);
        }

        var owner = systemRoles.Single(role =>
            string.Equals(role.Name, SystemRoles.Owner.Name, StringComparison.OrdinalIgnoreCase));

        // ── ③ Tài khoản chủ sở hữu ───────────────────────────────────────
        //
        // Băm ở đây, không bao giờ lưu mật khẩu nguyên văn. Argon2id cố ý chậm (~100ms) —
        // một lần lúc đăng ký là hoàn toàn chấp nhận được.
        var user = User.Create(
            tenant.Value.Id,
            command.Email,
            passwordHasher.Hash(command.Password),
            command.FullName);

        if (user.IsFailure)
        {
            return user.Error;
        }

        var assigned = user.Value.AssignRole(owner.Id);

        if (assigned.IsFailure)
        {
            return assigned.Error;
        }

        // ── ④ Gán chủ ────────────────────────────────────────────────────
        var assignedOwner = tenant.Value.AssignOwner(user.Value.Id);

        if (assignedOwner.IsFailure)
        {
            return assignedOwner.Error;
        }

        // Chỉ ghi vào bộ theo dõi thay đổi SAU KHI mọi phép kiểm đã qua. Thêm rồi mới
        // thất bại thì chỉ cần một ngày ai đó gọi SaveChanges sớm hơn là có workspace ma.
        tenants.Add(tenant.Value);
        roles.AddRange(systemRoles);
        users.Add(user.Value);

        // ── Đăng nhập luôn ───────────────────────────────────────────────
        //
        // Bắt gõ lại mật khẩu vừa đặt cách đây một giây là một bước thừa hoàn toàn.
        var accessToken = tokenService.IssueAccessToken(user.Value.Id, tenant.Value.Id, owner.Permissions);
        var refreshPair = tokenService.IssueRefreshToken();

        var refreshToken = RefreshToken.Create(
            user.Value.Id,
            tenant.Value.Id,
            refreshPair.Hash,          // ← lưu BĂM, không bao giờ lưu chuỗi thô
            dateTimeProvider.UtcNow,
            RefreshTokenLifetime);

        if (refreshToken.IsFailure)
        {
            return refreshToken.Error;
        }

        refreshTokens.Add(refreshToken.Value);

        return new RegisterWorkspaceResponse(
            accessToken.Value,
            refreshPair.Raw,
            (int)accessToken.Lifetime.TotalSeconds,
            new LoggedInUser(
                user.Value.Id,
                tenant.Value.Id,
                user.Value.Email.Value,
                user.Value.FullName),
            new RegisteredWorkspace(tenant.Value.Id, tenant.Value.Code.Value, tenant.Value.Name));
    }
}
