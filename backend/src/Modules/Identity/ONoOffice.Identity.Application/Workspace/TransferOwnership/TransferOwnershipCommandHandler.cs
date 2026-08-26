using Luong.Kernel.Abstractions;
using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Domain;

namespace ONoOffice.Identity.Application.Workspace.TransferOwnership;

public sealed record TransferOwnershipCommand(Guid NewOwnerUserId, string CurrentPassword)
    : ICommand;

/// <summary>
/// Chuyển quyền sở hữu workspace cho người khác.
///
/// ═══════════════════════════════════════════════════════════════════════
///  VÌ SAO PHẢI CÓ
/// ═══════════════════════════════════════════════════════════════════════
///
/// Bốn thông báo lỗi trong hệ thống bảo người dùng <i>"hãy chuyển nhượng quyền sở hữu
/// trước"</i> — vô hiệu hoá chủ sở hữu, hạ vai họ, đặt lại mật khẩu của họ, và chính họ
/// rời workspace. Không có lệnh này thì cả bốn là <b>ngõ cụt</b>: hệ thống chỉ vào một
/// cánh cửa không tồn tại.
///
/// ═══════════════════════════════════════════════════════════════════════
///  THAO TÁC KHÔNG HOÀN TÁC ĐƯỢC — VÀ HAI LỚP CHỈ NÓ MỚI CÓ
/// ═══════════════════════════════════════════════════════════════════════
///
/// Xong lệnh này, người cũ mất đúng cái quyền cần để lấy lại. Mọi thao tác khác trong app
/// đều có đường lùi; cái này thì không. Nên:
///
/// <list type="number">
/// <item><b>Phải LÀ chủ sở hữu, đọc từ DATABASE.</b> Quyền
/// <c>workspace.transfer-ownership</c> chỉ Owner có, nên tầng HTTP đã chặn gần hết — nhưng
/// access token sống 15 phút, và người vừa mất quyền sở hữu vẫn cầm một token mang claim
/// đó thêm một lúc. Không đọc lại từ database thì họ chuyển ngược lại được trong khoảng
/// thời gian ấy.</item>
/// <item><b>Phải gõ lại MẬT KHẨU HIỆN TẠI.</b> Ca nó chặn rất cụ thể: một cái máy bỏ quên
/// lúc đang đăng nhập, và người ngồi xuống sau đó.</item>
/// </list>
///
/// ═══════════════════════════════════════════════════════════════════════
///  VAI TRÒ ĐỔI THEO, KHÔNG CHỈ MỖI CỜ CHỦ SỞ HỮU
/// ═══════════════════════════════════════════════════════════════════════
///
/// Người mới lên vai <c>Owner</c>; người cũ xuống <c>Admin</c>.
///
/// Không đổi vai thì màn Thành viên hiện người mới với huy hiệu "Admin" trong khi hệ thống
/// đối xử như chủ sở hữu — hai câu trả lời khác nhau cho cùng một câu hỏi, và người quản
/// trị tin vào cái nhìn thấy.
///
/// Người cũ xuống <c>Admin</c> chứ không xuống <c>Member</c>: họ vừa là chủ công ty, và hạ
/// thẳng xuống vai hẹp nhất là lấy mất khả năng làm việc của họ trong một cú bấm. Admin
/// thiếu đúng một quyền so với Owner — chính quyền vừa chuyển đi.
/// </summary>
internal sealed class TransferOwnershipCommandHandler(
    IUserRepository users,
    IRoleRepository roles,
    ITenantRepository tenants,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser) : ICommandHandler<TransferOwnershipCommand>
{
    public async Task<Result> Handle(
        TransferOwnershipCommand command,
        CancellationToken cancellationToken)
    {
        var tenant = await tenants.GetCurrentForUpdateAsync(cancellationToken);

        if (tenant is null)
        {
            return IdentityErrors.Users.TenantRequired;
        }

        if (currentUser.UserId is not { } actorId || tenant.OwnerUserId != actorId)
        {
            return IdentityErrors.Tenants.OnlyOwnerCanTransfer;
        }

        // Chặn ca "chuyển cho chính mình" TRƯỚC khi hỏi mật khẩu: nó không đổi gì cả, nên
        // bắt gõ mật khẩu rồi mới báo "người này đã là chủ" là làm phiền không lý do.
        if (tenant.OwnerUserId == command.NewOwnerUserId)
        {
            return IdentityErrors.Tenants.AlreadyTheOwner;
        }

        var actor = await users.GetForUpdateAsync(actorId, cancellationToken);

        if (actor is null)
        {
            return IdentityErrors.Users.NotFound;
        }

        if (!passwordHasher.Verify(command.CurrentPassword, actor.PasswordHash))
        {
            return IdentityErrors.Users.WrongCurrentPassword;
        }

        var newOwner = await users.GetForUpdateAsync(command.NewOwnerUserId, cancellationToken);

        if (newOwner is null || newOwner.TenantId != tenant.Id)
        {
            return IdentityErrors.Users.NotFound;
        }

        if (!newOwner.IsActive)
        {
            return IdentityErrors.Tenants.NewOwnerMustBeActive;
        }

        var ownerRole = await roles.GetByNameAsync(SystemRoles.Owner.Name, cancellationToken);
        var adminRole = await roles.GetByNameAsync(SystemRoles.Admin.Name, cancellationToken);

        if (ownerRole is null || adminRole is null)
        {
            return IdentityErrors.Roles.NotFound;
        }

        // Đổi cờ TRƯỚC: `Tenant.TransferOwnership` là chỗ duy nhất phát sự kiện, và nếu
        // nó từ chối (workspace chưa có chủ) thì chưa có vai trò nào bị động vào.
        var transferred = tenant.TransferOwnership(command.NewOwnerUserId);

        if (transferred.IsFailure)
        {
            return transferred.Error;
        }

        var lenVai = DatVaiDuyNhat(newOwner, ownerRole.Id);

        if (lenVai.IsFailure)
        {
            return lenVai.Error;
        }

        return DatVaiDuyNhat(actor, adminRole.Id);
    }

    /// <summary>
    /// Gỡ hết vai cũ rồi gán đúng một vai — mô hình của app là MỘT người MỘT vai (ADR-0002).
    ///
    /// Gán thêm mà không gỡ thì quyền chỉ có tăng: người cũ vẫn giữ vai <c>Owner</c> cạnh
    /// vai <c>Admin</c> mới, và họ vẫn chuyển nhượng được workspace — tức là lệnh này
    /// không lấy đi gì cả.
    /// </summary>
    private static Result DatVaiDuyNhat(Domain.Entities.User user, Guid roleId)
    {
        foreach (var current in user.RoleIds.ToList())
        {
            var removed = user.RemoveRole(current);

            if (removed.IsFailure)
            {
                return removed.Error;
            }
        }

        return user.AssignRole(roleId);
    }
}
