using Luong.Kernel.AspNetCore.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ONoOffice.Identity.Domain;
using ONoOffice.Org.Application.Members.GetList;

namespace ONoOffice.Api.Controllers.Org;

/// <summary>
/// <b>Một</b> danh sách người của workspace — gộp tài khoản đăng nhập và hồ sơ nhân sự.
///
/// Thay cho <c>GET /api/users</c> ở màn Thành viên. Ba loại dòng, và cả ba đều có thật:
/// có cả hai · chỉ hồ sơ (nhân viên mới chưa được cấp tài khoản) · chỉ tài khoản (bot
/// chạy sao lưu).
///
/// <b>Chỉ ĐỌC.</b> Mọi thao tác sửa vẫn đi về đúng module sở hữu dữ liệu đó:
/// <c>/api/users</c> cho tài khoản, <c>/api/employees</c> cho hồ sơ. Gộp cả phần ghi vào
/// đây thì endpoint này phải biết luật của cả hai module, và nó trở thành chỗ hai bộ luật
/// lệch nhau.
///
/// Đòi CẢ HAI quyền: người chỉ có <c>user.read</c> sẽ thấy hồ sơ nhân sự mà đáng ra họ
/// không được thấy, và ngược lại.
/// </summary>
[ApiController]
[Route("api/members")]
[Authorize]
public sealed class MembersController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.Users.Read)]
    [Authorize(Policy = Permissions.Employees.Read)]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
        => (await sender.Send(new GetMembersQuery(), cancellationToken)).ToActionResult();
}
