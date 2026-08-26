using Luong.Kernel.AspNetCore.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Application.Users.Create;
using ONoOffice.Identity.Application.Users.GetList;
using ONoOffice.Identity.Application.Users.ResetPassword;
using ONoOffice.Identity.Application.Users.SetActive;
using ONoOffice.Identity.Application.Users.Update;
using ONoOffice.Identity.Domain;

namespace ONoOffice.Api.Controllers.Identity;

/// <summary>
/// Quản lý tài khoản người dùng trong một workspace — màn <b>Nhân sự</b> của giao diện.
///
/// Khác <c>AuthController</c> ở chỗ căn bản: mọi endpoint ở đây đòi token và đòi quyền.
/// Người gọi là quản trị viên đang thao tác trên tài khoản của NGƯỜI KHÁC.
///
/// <b>Tách quyền đọc và quyền sửa:</b> <c>user.read</c> cho xem danh sách, <c>user.manage</c>
/// cho tạo và sửa. Gộp làm một thì mọi người xem được danh bạ cũng tạo được tài khoản.
///
/// Luật một dòng cho mỗi action vẫn áp dụng — xem <c>AuthController</c> và
/// <c>ControllerRuleTests</c>.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Danh sách nhân sự, đã lọc và phân trang.
    ///
    /// Nhận tham số qua query string bằng <c>[FromQuery]</c> chứ không qua thân request:
    /// đây là <c>GET</c>, và một bộ lọc nằm trên URL thì <b>dán link cho đồng nghiệp
    /// được</b> — "xem giúp danh sách phòng Kế toán đang chờ nhận tài khoản".
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Permissions.Users.Read)]
    public async Task<IActionResult> GetList([FromQuery] GetUsersQuery query, CancellationToken cancellationToken)
        => (await sender.Send(query, cancellationToken)).ToActionResult();

    /// <summary>
    /// Tạo tài khoản hộ một đồng nghiệp.
    ///
    /// Trả <c>200</c> kèm <b>mật khẩu tạm</b> — lần duy nhất chuỗi thô đó tồn tại ngoài
    /// đầu người tạo. Không có endpoint nào đọc lại được nó; quên thì phải đặt lại mật khẩu.
    ///
    /// Cùng lý do với <c>register-workspace</c>, đây là <c>200</c> chứ không phải
    /// <c>201</c>: chưa có <c>GET /api/users/{id}</c> để header <c>Location</c> trỏ tới.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.Users.Manage)]
    public async Task<IActionResult> Create(CreateUserCommand command, CancellationToken cancellationToken)
        => (await sender.Send(command, cancellationToken)).ToActionResult();

    /// <summary>Đổi họ tên và vai trò của một tài khoản.</summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Policy = Permissions.Users.Manage)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateUserBody body,
        CancellationToken cancellationToken)
        => (await sender.Send(new UpdateUserCommand(id, body.FullName, body.RoleId), cancellationToken))
            .ToActionResult();

    /// <summary>
    /// Đổi <b>chỉ vai trò</b>, không đụng tới tên.
    ///
    /// Đường riêng chứ không gộp vào <c>PATCH</c> ở trên, vì thao tác HÀNG LOẠT ở màn
    /// Thành viên chỉ định đổi vai. Bắt nó gửi kèm tên thì nó phải gửi lại cái tên đã tải
    /// về vài giây trước — và nếu trong khoảng đó có người khác vừa đổi tên, thao tác hàng
    /// loạt sẽ ghi đè tên mới bằng tên cũ. Mất một thay đổi mà không có gì báo.
    ///
    /// Cùng luật với <c>PATCH</c>: vẫn không hạ được vai của chủ sở hữu.
    /// </summary>
    [HttpPost("{id:guid}/role")]
    [Authorize(Policy = Permissions.Users.Manage)]
    public async Task<IActionResult> ChangeRole(
        Guid id,
        ChangeUserRoleBody body,
        CancellationToken cancellationToken)
        => (await sender.Send(
                new UpdateUserCommand(id, FullName: null, body.RoleId),
                cancellationToken))
            .ToActionResult();

    /// <summary>
    /// Đặt lại mật khẩu HỘ một đồng nghiệp — trả về mật khẩu tạm <b>đúng một lần</b>.
    ///
    /// Cần vì chưa có dịch vụ gửi email, nên "Quên mật khẩu" ở màn đăng nhập chưa chạy —
    /// người quên mật khẩu hiện không có đường nào quay lại.
    ///
    /// <c>POST</c> chứ không <c>PATCH</c>: đây là một HÀNH ĐỘNG có hậu quả (thu hồi mọi
    /// phiên đang sống của người đó), không phải một phép sửa trường dữ liệu.
    ///
    /// Hai cửa chặn nằm ở handler, và cả hai đều là chuyện an toàn chứ không phải tiện
    /// dụng — xem <c>ResetUserPasswordCommandHandler</c>.
    /// </summary>
    [HttpPost("{id:guid}/reset-password")]
    [Authorize(Policy = Permissions.Users.Manage)]
    public async Task<IActionResult> ResetPassword(Guid id, CancellationToken cancellationToken)
        => (await sender.Send(new ResetUserPasswordCommand(id), cancellationToken)).ToActionResult();

    /// <summary>
    /// Vô hiệu hoá một tài khoản — <b>không phải xoá</b>.
    ///
    /// Người nghỉ việc vẫn còn tin nhắn, còn tên trên bản ghi cũ, còn là người duyệt của
    /// một đơn từ năm ngoái. Xoá đi thì mọi chỗ đó thành khoảng trống.
    /// </summary>
    [HttpPost("{id:guid}/disable")]
    [Authorize(Policy = Permissions.Users.Manage)]
    public async Task<IActionResult> Disable(Guid id, CancellationToken cancellationToken)
        => (await sender.Send(new SetUserActiveCommand(id, IsActive: false), cancellationToken)).ToActionResult();

    /// <summary>Bật lại một tài khoản đã vô hiệu hoá.</summary>
    [HttpPost("{id:guid}/enable")]
    [Authorize(Policy = Permissions.Users.Manage)]
    public async Task<IActionResult> Enable(Guid id, CancellationToken cancellationToken)
        => (await sender.Send(new SetUserActiveCommand(id, IsActive: true), cancellationToken)).ToActionResult();
}

/// <summary>
/// Thân của <c>PATCH /api/users/{id}</c>.
///
/// Mã tài khoản nằm trên ĐƯỜNG DẪN, không nằm trong thân. Để cả hai chỗ thì sớm muộn có
/// request gửi hai mã khác nhau, và không có câu trả lời đúng cho việc nên tin cái nào.
/// </summary>
public sealed record UpdateUserBody(string FullName, Guid RoleId);

/// <summary>Thân của <c>POST /api/users/{id}/role</c> — cố ý KHÔNG có trường tên.</summary>
public sealed record ChangeUserRoleBody(Guid RoleId);
