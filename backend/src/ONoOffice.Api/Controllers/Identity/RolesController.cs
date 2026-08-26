using Luong.Kernel.AspNetCore.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ONoOffice.Identity.Application.Roles.Create;
using ONoOffice.Identity.Application.Roles.Delete;
using ONoOffice.Identity.Application.Roles.GetList;
using ONoOffice.Identity.Application.Roles.Update;
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

    /// <summary>
    /// Tạo một vai trò TỰ ĐẶT.
    ///
    /// Bốn vai hệ thống bất biến, nên đây là đường DUY NHẤT để một workspace có bộ quyền
    /// khác bốn bộ dựng sẵn. Màn Vai trò vẫn luôn nói "muốn khác đi thì tạo một vai trò
    /// mới" — trước endpoint này thì câu đó là một ngõ cụt.
    ///
    /// ⚠️ <c>workspace.transfer-ownership</c> bị TỪ CHỐI ở đây: nó là toàn bộ ranh giới
    /// giữa Admin và Owner. Xem <c>CreateRoleCommandHandler</c>.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.Roles.Manage)]
    public async Task<IActionResult> Create(RoleBody body, CancellationToken cancellationToken)
        => (await sender.Send(
                new CreateRoleCommand(body.Name, body.Permissions),
                cancellationToken))
            .ToActionResult();

    /// <summary>
    /// Đổi tên và ĐẶT LẠI bộ quyền — đặt lại cả bộ, không cộng thêm.
    ///
    /// Màn hình gửi lên đúng những ô đang tick, nên thân request là trạng thái mong muốn.
    /// Hiểu thành "thêm" thì bỏ tick chẳng gỡ được gì, và quyền chỉ có tăng.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.Roles.Manage)]
    public async Task<IActionResult> Update(
        Guid id,
        RoleBody body,
        CancellationToken cancellationToken)
        => (await sender.Send(
                new UpdateRoleCommand(id, body.Name, body.Permissions),
                cancellationToken))
            .ToActionResult();

    /// <summary>
    /// Xoá một vai tự đặt — <b>xoá cứng</b>, khác hồ sơ nhân sự.
    ///
    /// Vai trò là cấu hình, không phải dữ liệu tra lại sau nhiều năm. Và một vai xoá mềm
    /// vẫn chiếm tên, nên tạo lại đúng tên đó sẽ báo trùng với thứ không ai nhìn thấy.
    ///
    /// Vai còn người giữ thì bị từ chối: xoá đi thì họ mang một mã vai không còn tồn tại
    /// và mất sạch quyền ngay lập tức.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.Roles.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => (await sender.Send(new DeleteRoleCommand(id), cancellationToken)).ToActionResult();
}

/// <summary>
/// Thân của <c>POST /api/roles</c> và <c>PUT /api/roles/{id}</c> — cùng một hình dạng.
///
/// Dùng chung một record vì hai lệnh nhận đúng cùng bộ trường. Tách làm hai chỉ để tên
/// khác nhau thì sớm muộn một bên thêm trường mà bên kia quên.
/// </summary>
public sealed record RoleBody(string Name, IReadOnlyList<string> Permissions);
