using Luong.Kernel.Abstractions;
using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Domain;

namespace ONoOffice.Identity.Application.Users.ResetPassword;

public sealed record ResetUserPasswordCommand(Guid UserId) : ICommand<ResetUserPasswordResponse>;

/// <summary>
/// <c>TemporaryPassword</c> là lần DUY NHẤT mật khẩu thô tồn tại ngoài đầu người đặt lại.
/// Nó không được ghi log, không được lưu, và không endpoint nào đọc lại được.
/// </summary>
public sealed record ResetUserPasswordResponse(
    Guid Id,
    string Email,
    string FullName,
    string TemporaryPassword);

/// <summary>
/// Quản trị viên đặt lại mật khẩu HỘ một đồng nghiệp.
///
/// ═══════════════════════════════════════════════════════════════════════
///  VÌ SAO CẦN, DÙ NGHE NHƯ MỘT TIỆN ÍCH
/// ═══════════════════════════════════════════════════════════════════════
///
/// Chưa nối dịch vụ gửi email, nên "Quên mật khẩu" ở màn đăng nhập vẫn hiện *đang phát
/// triển*. Người quên mật khẩu <b>không có đường nào quay lại</b> — mất hẳn quyền truy
/// cập, không phải bất tiện. Đây là lối thoát duy nhất cho tới khi có mail.
///
/// ═══════════════════════════════════════════════════════════════════════
///  BA VIỆC, VÀ THIẾU MỘT LÀ HỎNG
/// ═══════════════════════════════════════════════════════════════════════
///
/// <list type="number">
/// <item>Đặt mật khẩu tạm mới (lưu BĂM, trả về bản thô đúng một lần).</item>
/// <item>Bật cờ buộc đổi — mật khẩu tạm đi qua Zalo và lời nói, nó phải chết khi dùng xong.</item>
/// <item><b>Thu hồi mọi phiên</b> của người đó. Ca dùng thật hay gặp nhất là NGHI BỊ CHIẾM
/// TÀI KHOẢN; không thu hồi thì kẻ đang ngồi trong phiên cũ vẫn ở đó thêm 30 ngày, và thao
/// tác này trông như đã cứu mà không cứu gì.</item>
/// </list>
///
/// ═══════════════════════════════════════════════════════════════════════
///  HAI CỬA CHẶN — CẢ HAI ĐỀU LÀ CHUYỆN AN TOÀN
/// ═══════════════════════════════════════════════════════════════════════
///
/// Đặt lại mật khẩu của ai đó = <b>đăng nhập được dưới danh nghĩa người đó</b>. Nên:
///
/// <list type="bullet">
/// <item><b>Không đụng vào chủ sở hữu</b>, trừ khi chính họ làm. Admin có 11/12 quyền,
/// thiếu đúng quyền chuyển nhượng workspace — cho họ đặt lại mật khẩu của chủ sở hữu thì
/// họ đăng nhập thành chủ sở hữu rồi tự chuyển nhượng. Ranh giới Admin ↔ Owner biến mất,
/// mà bảng phân quyền vẫn trông đúng.</item>
/// <item><b>Không tự đặt lại của chính mình.</b> Đổi mật khẩu của mình đã có
/// <c>POST /api/me/password</c>, và đường đó đòi mật khẩu HIỆN TẠI. Đi vòng qua đây là bỏ
/// hẳn phép kiểm ấy: một máy bỏ quên lúc đang đăng nhập là đủ để bị chiếm tài khoản.
/// Ngoại lệ đúng một ca — chủ sở hữu, vì họ không còn đường nào khác.</item>
/// </list>
/// </summary>
internal sealed class ResetUserPasswordCommandHandler(
    IUserRepository users,
    ITenantRepository tenants,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    ITemporaryPasswordGenerator passwordGenerator,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ResetUserPasswordCommand, ResetUserPasswordResponse>
{
    public async Task<Result<ResetUserPasswordResponse>> Handle(
        ResetUserPasswordCommand command,
        CancellationToken cancellationToken)
    {
        var user = await users.GetForUpdateAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return IdentityErrors.Users.NotFound;
        }

        var ownerUserId = await tenants.GetOwnerUserIdAsync(cancellationToken);

        bool laChinhMinh = currentUser.UserId == user.Id;
        bool laChuSoHuu = ownerUserId == user.Id;

        if (laChuSoHuu && !laChinhMinh)
        {
            return IdentityErrors.Users.CannotResetOwnerPassword;
        }

        // Chủ sở hữu được tự đặt lại vì không còn đường nào khác cho họ — chặn nốt thì
        // quên mật khẩu là workspace kẹt vĩnh viễn, sửa được chỉ bằng cách vào thẳng DB.
        if (laChinhMinh && !laChuSoHuu)
        {
            return IdentityErrors.Users.CannotResetOwnPassword;
        }

        string temporaryPassword = passwordGenerator.Generate();

        var changed = user.ChangePassword(passwordHasher.Hash(temporaryPassword));

        if (changed.IsFailure)
        {
            return changed.Error;
        }

        // `ChangePassword` TẮT cờ buộc đổi (nó dùng cho ca người dùng tự đặt mật khẩu
        // thật). Ở đây phải bật lại NGAY SAU — thứ vừa đặt là mật khẩu tạm, không phải
        // mật khẩu của họ.
        user.RequirePasswordChange();

        await refreshTokens.RevokeAllForUserAsync(user.Id, dateTimeProvider.UtcNow, cancellationToken);

        return new ResetUserPasswordResponse(
            user.Id,
            user.Email.Value,
            user.FullName,
            temporaryPassword);
    }
}
