using Luong.Kernel.AspNetCore.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Application.Users.Create;
using ONoOffice.Identity.Application.Users.GetList;
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
}
