using Luong.Kernel.Abstractions;
using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Domain;

namespace ONoOffice.Identity.Application.Me.UpdateProfile;

public sealed record UpdateMyProfileCommand(string FullName) : ICommand;

/// <summary>
/// Người dùng tự sửa hồ sơ của mình.
///
/// <b>Chỉ có họ tên.</b> Email là định danh đăng nhập và phải qua quản trị viên; chức danh
/// và phòng ban do phòng Nhân sự đặt; vai trò thì đương nhiên không ai tự nâng cho mình
/// được. Giao diện khoá sẵn những ô đó kèm một câu giải thích — xem bản dựng
/// <c>identity/tai-khoan.html</c>.
///
/// Nhận một trường mà vẫn là một command riêng chứ không nhét chung vào
/// <c>UpdateUserCommand</c>: hai use case này khác nhau ở chỗ căn bản là AI được gọi.
/// Gộp lại thì phép kiểm "có phải chính mình không" và "có quyền user.manage không" nằm
/// chung một chỗ, và sớm muộn một nhánh sẽ thiếu.
/// </summary>
internal sealed class UpdateMyProfileCommandHandler(
    IUserRepository users,
    ICurrentUser currentUser) : ICommandHandler<UpdateMyProfileCommand>
{
    public async Task<Result> Handle(UpdateMyProfileCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return IdentityErrors.Users.NotFound;
        }

        var user = await users.GetForUpdateAsync(userId, cancellationToken);

        return user is null ? IdentityErrors.Users.NotFound : user.Rename(command.FullName);
    }
}
