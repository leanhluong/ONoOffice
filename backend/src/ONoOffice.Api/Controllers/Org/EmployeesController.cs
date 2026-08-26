using Luong.Kernel.AspNetCore.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ONoOffice.Identity.Domain;
using ONoOffice.Org.Application.Employees.Create;
using ONoOffice.Org.Application.Employees.Leave;
using ONoOffice.Org.Application.Employees.LinkAccount;
using ONoOffice.Org.Application.Employees.Transfer;
using ONoOffice.Org.Application.Employees.Update;

namespace ONoOffice.Api.Controllers.Org;

/// <summary>
/// Hồ sơ nhân sự — <b>quản lý</b>, không phải tra cứu.
///
/// ═══════════════════════════════════════════════════════════════════════
///  BA ĐƯỜNG DẪN NÓI VỀ CON NGƯỜI, VÀ CHÚNG KHÔNG THAY THẾ NHAU
/// ═══════════════════════════════════════════════════════════════════════
///
/// <code>
///   /api/contacts   employee.read    TRA CỨU đồng nghiệp — ai cũng vào được
///   /api/employees  employee.write   SỬA hồ sơ nhân sự   — quản trị
///   /api/users      user.manage      SỬA tài khoản đăng nhập (module Identity)
/// </code>
///
/// Hai cái đầu cùng đọc một bảng nhưng khác quyền và khác mục đích, nên tách đường dẫn:
/// gộp lại thì hoặc nhân viên sửa được hồ sơ của nhau, hoặc họ không tra được danh bạ.
///
/// Cái thứ ba là một khái niệm KHÁC HẲN. Một người có thể có hồ sơ mà chưa có tài khoản
/// (nhân viên mới), hoặc có tài khoản mà không phải nhân viên (tài khoản bot chạy sao
/// lưu). Xem chú thích đầu <c>Employee.cs</c>.
///
/// Nối hai khái niệm đó lại là <c>link-account</c> ở cuối file. Nó hỏi được module Identity
/// nhờ <c>Identity.Contracts.IUserDirectory</c> — cổng liên module đầu tiên của dự án.
/// </summary>
[ApiController]
[Route("api/employees")]
[Authorize]
public sealed class EmployeesController(ISender sender) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = Permissions.Employees.Write)]
    public async Task<IActionResult> Create(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken)
        => (await sender.Send(
                new CreateEmployeeCommand(
                    request.Code,
                    request.FullName,
                    request.JobTitle,
                    request.WorkEmail,
                    request.Phone,
                    request.DepartmentId),
                cancellationToken))
            .ToActionResult();

    /// <summary>
    /// Sửa thông tin trên hồ sơ.
    ///
    /// KHÔNG đổi được mã nhân viên (nó nằm trên hợp đồng và thẻ) và KHÔNG đổi được phòng
    /// ban — điều chuyển có đường riêng, vì nó có hậu quả riêng.
    /// </summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Policy = Permissions.Employees.Write)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken)
        => (await sender.Send(
                new UpdateEmployeeCommand(
                    id,
                    request.FullName,
                    request.JobTitle,
                    request.WorkEmail,
                    request.Phone),
                cancellationToken))
            .ToActionResult();

    /// <summary><c>departmentId = null</c> nghĩa là rút khỏi mọi phòng.</summary>
    [HttpPost("{id:guid}/transfer")]
    [Authorize(Policy = Permissions.Employees.Write)]
    public async Task<IActionResult> Transfer(
        Guid id,
        TransferEmployeeRequest request,
        CancellationToken cancellationToken)
        => (await sender.Send(
                new TransferEmployeeCommand(id, request.DepartmentId),
                cancellationToken))
            .ToActionResult();

    /// <summary>
    /// Đóng hồ sơ khi một người nghỉ việc — <b>không xoá</b>.
    ///
    /// Đường dẫn là <c>leave</c> chứ không phải <c>DELETE</c>, và đó là chủ ý: hồ sơ nhân
    /// sự là thứ người ta tra lại sau nhiều năm khi có tranh chấp hợp đồng hay bảo hiểm.
    /// Một endpoint tên <c>DELETE</c> mời gọi đúng cái hiểu nhầm ta muốn tránh.
    /// </summary>
    [HttpPost("{id:guid}/leave")]
    [Authorize(Policy = Permissions.Employees.Write)]
    public async Task<IActionResult> Leave(
        Guid id,
        LeaveEmployeeRequest request,
        CancellationToken cancellationToken)
        => (await sender.Send(new LeaveEmployeeCommand(id, request.LeftOn), cancellationToken))
            .ToActionResult();

    [HttpPost("{id:guid}/reinstate")]
    [Authorize(Policy = Permissions.Employees.Write)]
    public async Task<IActionResult> Reinstate(Guid id, CancellationToken cancellationToken)
        => (await sender.Send(new ReinstateEmployeeCommand(id), cancellationToken)).ToActionResult();

    /// <summary>
    /// Nối hồ sơ với một tài khoản đăng nhập.
    ///
    /// Đòi <c>user.manage</c> chứ không phải <c>employee.write</c>: nối là quyết định về
    /// việc AI ĐĂNG NHẬP ĐƯỢC dưới danh nghĩa hồ sơ nào, tức là một quyết định về tài
    /// khoản. Người chỉ được sửa hồ sơ nhân sự không nên tự trao cho mình một danh tính.
    /// </summary>
    [HttpPost("{id:guid}/link-account")]
    [Authorize(Policy = Permissions.Users.Manage)]
    public async Task<IActionResult> LinkAccount(
        Guid id,
        LinkAccountRequest request,
        CancellationToken cancellationToken)
        => (await sender.Send(new LinkAccountCommand(id, request.UserId), cancellationToken))
            .ToActionResult();

    [HttpPost("{id:guid}/unlink-account")]
    [Authorize(Policy = Permissions.Users.Manage)]
    public async Task<IActionResult> UnlinkAccount(Guid id, CancellationToken cancellationToken)
        => (await sender.Send(new UnlinkAccountCommand(id), cancellationToken)).ToActionResult();
}

public sealed record CreateEmployeeRequest(
    string Code,
    string FullName,
    string? JobTitle,
    string? WorkEmail,
    string? Phone,
    Guid? DepartmentId);

public sealed record UpdateEmployeeRequest(
    string FullName,
    string? JobTitle,
    string? WorkEmail,
    string? Phone);

public sealed record TransferEmployeeRequest(Guid? DepartmentId);

public sealed record LeaveEmployeeRequest(DateOnly LeftOn);

public sealed record LinkAccountRequest(Guid UserId);
