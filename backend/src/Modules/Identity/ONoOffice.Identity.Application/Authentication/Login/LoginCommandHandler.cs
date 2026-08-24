using Luong.Kernel.Abstractions;
using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Domain;
using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.Application.Authentication.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<LoginResponse>;

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    LoggedInUser User);

/// <summary>
/// <c>MustChangePassword</c> có trong THÂN phản hồi chứ không trong access token.
///
/// Nó không phục vụ quyết định bảo mật nào ở phía server — server không chặn gì dựa vào
/// nó. Nó chỉ để giao diện biết mà đưa người dùng thẳng tới màn đổi mật khẩu. Nhét vào
/// token thì mọi request đều mang theo nó, và nó chỉ đúng tại thời điểm phát token.
/// </summary>
public sealed record LoggedInUser(
    Guid Id,
    Guid TenantId,
    string Email,
    string FullName,
    bool MustChangePassword);

/// <summary>
/// Điều phối một lần đăng nhập.
///
/// Handler này KHÔNG chứa luật nghiệp vụ — luật nằm ở <c>Domain</c>. Việc của nó là
/// sắp xếp thứ tự: tra cứu → kiểm mật khẩu → kiểm trạng thái → phát token → lưu vé.
///
/// Nó cũng KHÔNG kiểm dữ liệu gửi lên (<c>ValidationBehavior</c> lo) và KHÔNG gọi
/// <c>SaveChanges</c> (<c>TransactionBehavior</c> lo). Cả hai đến từ
/// <c>Luong.Kernel.Application</c>.
/// </summary>
internal sealed class LoginCommandHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<LoginCommand, LoginResponse>
{
    /// <summary>Khớp với hạn ghi trong ADR-0002.</summary>
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    /// <summary>
    /// Chuỗi băm giả, dùng khi email không tồn tại — xem <see cref="Handle"/>.
    /// Đây là một chuỗi Argon2id hợp lệ nhưng không ứng với mật khẩu nào.
    /// </summary>
    private const string DummyHash =
        "$argon2id$v=19$m=19456,t=2,p=1$c29tZXNhbHR2YWx1ZQ$0000000000000000000000000000000000000000000";

    public async Task<Result<LoginResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        string email = command.Email.Trim().ToLowerInvariant();

        var data = await users.GetForLoginAsync(email, cancellationToken);

        // Không tìm thấy email thì VẪN chạy Verify một lần.
        //
        // Bỏ qua bước băm ở đây làm request trả về nhanh hơn hẳn, vì Argon2id CỐ Ý chậm
        // (~100ms). Kẻ tấn công chỉ cần đo thời gian phản hồi là biết email nào có thật —
        // dò được tài khoản mà thậm chí không cần đọc nội dung thông báo lỗi.
        bool matKhauDung = passwordHasher.Verify(command.Password, data?.PasswordHash ?? DummyHash);

        if (data is null || !matKhauDung)
        {
            // MỘT thông báo duy nhất cho cả hai ca. Tách bạch "email không tồn tại" và
            // "sai mật khẩu" là tặng công cụ dò tài khoản: gõ 10.000 email, cái nào báo
            // "sai mật khẩu" nghĩa là email đó CÓ THẬT.
            return IdentityErrors.Auth.InvalidCredentials;
        }

        // Kiểm trạng thái SAU khi mật khẩu đã đúng. Đảo ngược thứ tự là để lộ
        // "tài khoản này tồn tại nhưng đang bị khoá" cho người chưa chứng minh được
        // mình là chủ tài khoản.
        if (!data.IsTenantActive)
        {
            return IdentityErrors.Auth.WorkspaceDisabled;
        }

        if (!data.IsUserActive)
        {
            return IdentityErrors.Auth.AccountDisabled;
        }

        var accessToken = tokenService.IssueAccessToken(data.UserId, data.TenantId, data.Permissions);
        var refreshPair = tokenService.IssueRefreshToken();

        var refreshToken = RefreshToken.Create(
            data.UserId,
            data.TenantId,
            refreshPair.Hash,          // ← lưu BĂM, không bao giờ lưu chuỗi thô
            dateTimeProvider.UtcNow,
            RefreshTokenLifetime);

        if (refreshToken.IsFailure)
        {
            return refreshToken.Error;
        }

        refreshTokens.Add(refreshToken.Value);

        return new LoginResponse(
            accessToken.Value,
            refreshPair.Raw,           // ← chuỗi thô chỉ đi ra ngoài, không đọng lại đâu cả
            (int)accessToken.Lifetime.TotalSeconds,
            new LoggedInUser(data.UserId, data.TenantId, data.Email, data.FullName, data.MustChangePassword));
    }
}
