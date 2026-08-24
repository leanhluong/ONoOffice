using Luong.Kernel.Abstractions;
using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Domain;

namespace ONoOffice.Identity.Application.Me.ChangePassword;

public sealed record ChangeMyPasswordCommand(string CurrentPassword, string NewPassword) : ICommand;

/// <summary>
/// Người dùng tự đổi mật khẩu của mình.
///
/// <b>Hỏi lại mật khẩu hiện tại không phải thủ tục cho có.</b> Nó chặn kiểu chiếm tài
/// khoản rẻ nhất: ngồi vào máy đang mở sẵn của đồng nghiệp và đổi mật khẩu.
///
/// <b>Thu hồi mọi phiên là phần quan trọng nhất.</b> Lý do người ta đổi mật khẩu gần như
/// luôn là "tôi nghĩ nó bị lộ". Không thu hồi thì kẻ trộm vẫn ngồi trong phiên cũ suốt
/// 30 ngày, và việc đổi mật khẩu chỉ là một động tác cho yên tâm.
///
/// ⚠️ <b>Vì sao thu hồi ở ĐÂY chứ không ở nơi lắng nghe sự kiện:</b> <c>User.ChangePassword</c>
/// có phát <c>UserPasswordChanged</c>, và bình luận trong đó từng nói rằng "nơi khác lắng
/// nghe nó để thu hồi". Thực tế <b>không có nơi nào lắng nghe cả</b> — sự kiện được phát
/// ra rồi rơi vào hư không. Làm thẳng ở đây thì chỉ cần đọc handler là thấy, thay vì phải
/// tin vào một đường dây không tồn tại.
/// </summary>
internal sealed class ChangeMyPasswordCommandHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ChangeMyPasswordCommand>
{
    public async Task<Result> Handle(ChangeMyPasswordCommand command, CancellationToken cancellationToken)
    {
        // Không thể xảy ra sau khi qua xác thực. Nhưng nếu xảy ra thì một Guid rỗng sẽ đi
        // tra một tài khoản không tồn tại, và người dùng nhận về một lỗi khó hiểu.
        if (currentUser.UserId is not { } userId)
        {
            return IdentityErrors.Users.NotFound;
        }

        var user = await users.GetForUpdateAsync(userId, cancellationToken);

        if (user is null)
        {
            return IdentityErrors.Users.NotFound;
        }

        if (!passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
        {
            // Thoát SỚM, trước khi đụng tới bất cứ thứ gì. Thu hồi phiên rồi mới báo sai
            // thì bất kỳ ai ngồi vào máy cũng đá được người dùng ra khỏi mọi thiết bị chỉ
            // bằng cách gõ bừa — một kiểu quấy rối rất rẻ.
            return IdentityErrors.Users.WrongCurrentPassword;
        }

        // Đổi sang chính nó thì không đổi gì, nhưng vẫn thu hồi hết phiên và làm người
        // dùng tin rằng họ vừa xử lý xong một vụ lộ mật khẩu.
        if (passwordHasher.Verify(command.NewPassword, user.PasswordHash))
        {
            return IdentityErrors.Users.NewPasswordSameAsCurrent;
        }

        var changed = user.ChangePassword(passwordHasher.Hash(command.NewPassword));

        if (changed.IsFailure)
        {
            return changed.Error;
        }

        await refreshTokens.RevokeAllForUserAsync(user.Id, dateTimeProvider.UtcNow, cancellationToken);

        return Result.Success();
    }
}
