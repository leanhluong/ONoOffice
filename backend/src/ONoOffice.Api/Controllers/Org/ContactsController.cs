using Luong.Kernel.AspNetCore.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ONoOffice.Identity.Domain;
using ONoOffice.Org.Application.Contacts.GetList;

namespace ONoOffice.Api.Controllers.Org;

/// <summary>
/// Danh bạ nội bộ — màn <b>Danh bạ</b> trong khung app.
///
/// <b>Ai cũng xem được</b>, chỉ cần <c>employee.read</c> — quyền mà cả bốn vai hệ thống
/// đều có. Đó là chủ ý: danh bạ là thứ nhân viên mở hằng ngày để tra số điện thoại của
/// đồng nghiệp, không phải công cụ quản trị.
///
/// Đừng nhầm với <c>/api/users</c>: ở đó quản trị viên sửa TÀI KHOẢN ĐĂNG NHẬP của người
/// khác (module Identity, quyền `user.read`). Cùng nói về con người, hai khái niệm khác
/// nhau, và chúng đổi vì những lý do khác nhau — xem chú thích đầu <c>Employee.cs</c>.
/// </summary>
[ApiController]
[Route("api/contacts")]
[Authorize]
public sealed class ContactsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.Employees.Read)]
    public async Task<IActionResult> GetList(
        [FromQuery] GetContactsQuery query,
        CancellationToken cancellationToken)
        => (await sender.Send(query, cancellationToken)).ToActionResult();
}
