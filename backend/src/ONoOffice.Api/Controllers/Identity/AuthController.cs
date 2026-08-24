using Luong.Kernel.AspNetCore.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ONoOffice.Identity.Application.Authentication.Login;
using ONoOffice.Identity.Application.Authentication.Logout;
using ONoOffice.Identity.Application.Authentication.Refresh;

namespace ONoOffice.Api.Controllers.Identity;

/// <summary>
/// Ba cửa của một phiên đăng nhập: mở, gia hạn, đóng.
///
/// <b>Cả ba đều <c>[AllowAnonymous]</c>, và đó là điều bắt buộc chứ không phải sơ suất:</b>
/// người gọi ba endpoint này chính là người CHƯA có (hoặc không còn) access token dùng
/// được. Đòi token ở đây là khoá cửa rồi để chìa khoá bên trong.
///
/// <b>Luật của mọi action trong dự án này: đúng MỘT dòng.</b> Không <c>if</c>, không
/// <c>try/catch</c>, không gọi repository. Cái bẫy của Controller không phải hiệu năng —
/// mà là nó quá tiện để nhét logic vào. Nhét một lần thì "chỉ một chỗ thôi mà"; sáu tháng
/// sau nghiệp vụ nằm rải ở hai mươi controller, không test nào chạm tới được vì muốn chạy
/// chúng thì phải dựng cả một máy chủ HTTP.
///
/// Luật này có test kiến trúc canh — xem <c>ControllerRuleTests</c>.
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController(ISender sender) : ControllerBase
{
    /// <summary>Đổi email + mật khẩu lấy cặp access token và refresh token.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken)
        => (await sender.Send(command, cancellationToken)).ToActionResult();

    /// <summary>
    /// Đổi refresh token còn sống lấy cặp mới. Vé cũ bị thu hồi ngay trong cùng thao tác
    /// — xem luật xoay vòng và phát hiện trộm ở <c>RefreshTokenCommandHandler</c>.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenCommand command, CancellationToken cancellationToken)
        => (await sender.Send(command, cancellationToken)).ToActionResult();

    /// <summary>
    /// Thu hồi vé gia hạn. Luôn trả <c>204</c>, kể cả khi vé không tồn tại — báo "vé này
    /// không tồn tại" là tiết lộ vé nào từng tồn tại.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutCommand command, CancellationToken cancellationToken)
        => (await sender.Send(command, cancellationToken)).ToActionResult();
}
