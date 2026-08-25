using Luong.Kernel.AspNetCore.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ONoOffice.Identity.Domain;
using ONoOffice.Org.Application.Departments.Create;
using ONoOffice.Org.Application.Departments.Delete;
using ONoOffice.Org.Application.Departments.GetTree;
using ONoOffice.Org.Application.Departments.Move;
using ONoOffice.Org.Application.Departments.Rename;

namespace ONoOffice.Api.Controllers.Org;

/// <summary>
/// Cây phòng ban — màn <b>Phòng ban</b> trong vùng quản trị.
///
/// <b>Đọc và SỬA tách bằng hai quyền khác nhau.</b> `department.read` là quyền ai cũng
/// cần (danh bạ lọc theo phòng ban), còn `department.manage` chỉ quản trị viên. Gộp làm
/// một thì hoặc nhân viên sửa được cây tổ chức, hoặc họ không lọc được danh bạ.
/// </summary>
[ApiController]
[Route("api/departments")]
[Authorize]
public sealed class DepartmentsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Toàn bộ cây, đã nối sẵn cha con.
    ///
    /// Không phân trang, cố ý: một cây bị cắt làm nhiều trang thì không còn là cây.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Permissions.Departments.Read)]
    public async Task<IActionResult> GetTree(CancellationToken cancellationToken)
        => (await sender.Send(new GetDepartmentTreeQuery(), cancellationToken)).ToActionResult();

    [HttpPost]
    [Authorize(Policy = Permissions.Departments.Manage)]
    public async Task<IActionResult> Create(
        CreateDepartmentRequest request,
        CancellationToken cancellationToken)
        => (await sender.Send(
                new CreateDepartmentCommand(request.Name, request.ParentId),
                cancellationToken))
            .ToActionResult();

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = Permissions.Departments.Manage)]
    public async Task<IActionResult> Rename(
        Guid id,
        RenameDepartmentRequest request,
        CancellationToken cancellationToken)
        => (await sender.Send(new RenameDepartmentCommand(id, request.Name), cancellationToken))
            .ToActionResult();

    /// <summary>
    /// Điều chuyển sang phòng cha khác, hoặc nâng lên làm gốc (<c>parentId = null</c>).
    ///
    /// Đường dẫn riêng chứ không gộp vào <c>PATCH</c>: đổi tên và điều chuyển là hai hành
    /// động có hậu quả khác hẳn nhau — cái sau có thể tạo vòng lặp và làm cả một nhánh
    /// biến mất khỏi cây. Một endpoint nhận cả hai thì luật chống vòng lặp nằm lẫn với
    /// luật kiểm tên, và dễ thiếu một nhánh.
    /// </summary>
    [HttpPost("{id:guid}/move")]
    [Authorize(Policy = Permissions.Departments.Manage)]
    public async Task<IActionResult> Move(
        Guid id,
        MoveDepartmentRequest request,
        CancellationToken cancellationToken)
        => (await sender.Send(new MoveDepartmentCommand(id, request.ParentId), cancellationToken))
            .ToActionResult();

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.Departments.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => (await sender.Send(new DeleteDepartmentCommand(id), cancellationToken)).ToActionResult();
}

public sealed record CreateDepartmentRequest(string Name, Guid? ParentId);

public sealed record RenameDepartmentRequest(string Name);

public sealed record MoveDepartmentRequest(Guid? ParentId);
