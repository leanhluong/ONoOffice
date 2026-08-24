using Luong.Kernel.AspNetCore.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ONoOffice.Identity.Application.Roles.GetList;
using ONoOffice.Identity.Domain;

namespace ONoOffice.Api.Controllers.Identity;

/// <summary>
/// Vai trò và bộ quyền của chúng — màn <b>Vai trò &amp; quyền</b>, và danh sách xổ chọn
/// vai trò ở hộp thoại thêm người.
/// </summary>
[ApiController]
[Route("api/roles")]
[Authorize]
public sealed class RolesController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Mọi vai trò của workspace, kèm quyền và số người đang giữ.
    ///
    /// Không phân trang: bốn vai hệ thống cộng vài vai tự tạo. Phân trang một danh sách
    /// năm dòng là thêm phức tạp cho cả hai phía mà không đổi được gì.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Permissions.Roles.Read)]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
        => (await sender.Send(new GetRolesQuery(), cancellationToken)).ToActionResult();
}
