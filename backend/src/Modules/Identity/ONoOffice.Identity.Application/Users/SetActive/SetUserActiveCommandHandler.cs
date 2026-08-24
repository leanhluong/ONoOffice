using Luong.Kernel.Abstractions;
using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Domain;

namespace ONoOffice.Identity.Application.Users.SetActive;

public sealed record SetUserActiveCommand(Guid UserId, bool IsActive) : ICommand;

/// <summary>
/// Vô hiệu hoá hoặc bật lại một tài khoản.
///
/// <b>Vô hiệu hoá, KHÔNG phải xoá.</b> Người nghỉ việc vẫn còn tin nhắn, còn tên trên
/// những bản ghi cũ, còn là người phê duyệt của một đơn từ năm ngoái. Xoá họ đi thì mọi
/// chỗ đó thành khoảng trống, và không ai khôi phục lại được ngữ cảnh.
///
/// Hai cửa chặn ở đây đều là chặn <b>workspace tự khoá chính mình ra ngoài</b>. Cả hai
/// đều rẻ hơn nhiều so với việc đi sửa tay trong database khi đã xảy ra.
/// </summary>
internal sealed class SetUserActiveCommandHandler(
    IUserRepository users,
    ITenantRepository tenants,
    ICurrentUser currentUser) : ICommandHandler<SetUserActiveCommand>
{
    public async Task<Result> Handle(SetUserActiveCommand command, CancellationToken cancellationToken)
    {
        // ⭐ Cách nhanh nhất để một workspace mất hết quản trị viên: người cuối cùng tự
        // khoá mình. Chỉ chặn chiều KHOÁ — bật lại chính mình thì vô hại, và trên thực tế
        // không xảy ra vì người đang bị khoá thì không đăng nhập được để mà bật.
        if (!command.IsActive && currentUser.UserId == command.UserId)
        {
            return IdentityErrors.Users.CannotDisableSelf;
        }

        var user = await users.GetForUpdateAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return IdentityErrors.Users.NotFound;
        }

        if (!command.IsActive)
        {
            var ownerUserId = await tenants.GetOwnerUserIdAsync(cancellationToken);

            // Chủ sở hữu là người DUY NHẤT chuyển nhượng được workspace. Khoá họ lại thì
            // không còn ai làm được việc đó.
            if (ownerUserId == user.Id)
            {
                return IdentityErrors.Users.CannotDisableOwner;
            }
        }

        // `Deactivate` phát sự kiện để thu hồi mọi refresh token đang sống. Thiếu bước đó
        // thì người vừa bị khoá vẫn dùng được phiên cũ suốt 30 ngày.
        return command.IsActive ? user.Activate() : user.Deactivate();
    }
}
