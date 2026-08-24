using Luong.Kernel.AspNetCore.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ONoOffice.Identity.Application.Me.ChangePassword;
using ONoOffice.Identity.Application.Me.GetProfile;
using ONoOffice.Identity.Application.Me.UpdateProfile;

namespace ONoOffice.Api.Controllers.Identity;

/// <summary>
/// Hồ sơ và bảo mật của <b>chính người đang đăng nhập</b> — màn "Hồ sơ &amp; cài đặt".
///
/// Tách hẳn khỏi <c>UsersController</c>, dù cả hai đều đụng vào bảng <c>users</c>. Lý do
/// không phải là gọn gàng, mà là <b>phân quyền</b>: ở đây không cần quyền gì ngoài việc đã
/// đăng nhập, vì ai cũng được sửa hồ sơ của chính mình. Gộp chung thì hai bộ luật rất khác
/// nhau nằm cạnh nhau trong một file, và sớm muộn một action sẽ mang nhầm thuộc tính.
///
/// Mã người dùng KHÔNG bao giờ nhận từ ngoài vào ở đây — nó lấy từ token. Nhận từ ngoài
/// thì <c>/api/me</c> trở thành cửa sửa hồ sơ bất kỳ ai.
/// </summary>
[ApiController]
[Route("api/me")]
[Authorize]
public sealed class MeController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Hồ sơ của tôi — nguồn sự thật để làm tươi lại bản ghi trong <c>localStorage</c>.
    ///
    /// Frontend ghi tên và email xuống đĩa để mở lại tab là hiện ngay, nhưng bản ghi đó có
    /// thể cũ hàng tuần: phòng Nhân sự đổi chức danh, quản trị viên đổi vai trò.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
        => (await sender.Send(new GetMyProfileQuery(), cancellationToken)).ToActionResult();

    /// <summary>
    /// Sửa hồ sơ của tôi. <b>Chỉ có họ tên.</b>
    ///
    /// Email là định danh đăng nhập nên phải qua quản trị viên; chức danh và phòng ban do
    /// phòng Nhân sự đặt; vai trò thì đương nhiên không ai tự nâng cho mình được.
    /// </summary>
    [HttpPatch]
    public async Task<IActionResult> UpdateProfile(
        UpdateMyProfileCommand command,
        CancellationToken cancellationToken)
        => (await sender.Send(command, cancellationToken)).ToActionResult();

    /// <summary>
    /// Đổi mật khẩu của tôi. Thành công thì <b>mọi phiên khác bị thu hồi</b>.
    ///
    /// Người ta đổi mật khẩu gần như luôn vì nghĩ nó bị lộ. Không thu hồi thì kẻ trộm vẫn
    /// ngồi trong phiên cũ suốt 30 ngày.
    /// </summary>
    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword(
        ChangeMyPasswordCommand command,
        CancellationToken cancellationToken)
        => (await sender.Send(command, cancellationToken)).ToActionResult();
}
