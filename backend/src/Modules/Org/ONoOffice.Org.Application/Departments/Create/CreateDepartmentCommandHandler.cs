using Luong.Kernel.Abstractions;
using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Org.Application.Abstractions;
using ONoOffice.Org.Domain;
using ONoOffice.Org.Domain.Entities;

namespace ONoOffice.Org.Application.Departments.Create;

public sealed record CreateDepartmentCommand(string Name, Guid? ParentId)
    : ICommand<CreateDepartmentResponse>;

public sealed record CreateDepartmentResponse(Guid Id, string Name, Guid? ParentId);

/// <summary>
/// Thêm một phòng ban.
///
/// Ba phép kiểm, và <b>thứ tự của chúng là thứ tự người dùng sửa được</b>: workspace →
/// tên → phòng cha. Kiểm tên trước khi hỏi database về phòng cha vì tên sai thì không cần
/// biết phòng cha có tồn tại hay không, và mỗi lần hỏi database là một vòng mạng.
/// </summary>
internal sealed class CreateDepartmentCommandHandler(
    IDepartmentRepository departments,
    ICurrentTenant currentTenant) : ICommandHandler<CreateDepartmentCommand, CreateDepartmentResponse>
{
    public async Task<Result<CreateDepartmentResponse>> Handle(
        CreateDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        // Tenant lấy từ PHIÊN, không bao giờ từ thân request — nhận từ ngoài vào thì gõ
        // tay mã workspace của công ty khác là tạo được phòng ban trong công ty đó.
        if (currentTenant.TenantId is not { } tenantId)
        {
            return OrgErrors.Departments.TenantRequired;
        }

        var phong = Department.Create(tenantId, command.Name, command.ParentId);

        if (phong.IsFailure)
        {
            return phong.Error;
        }

        if (await departments.NameTakenAsync(phong.Value.Name, null, cancellationToken))
        {
            return OrgErrors.Departments.NameTaken;
        }

        // Phòng cha phải TỒN TẠI. Không kiểm thì sinh ra một nhánh mồ côi: `ParentId` trỏ
        // vào hư không, nên phòng đó biến mất khỏi cây trong khi vẫn nằm trong bảng.
        if (command.ParentId is { } chaId
            && await departments.GetAsync(chaId, cancellationToken) is null)
        {
            return OrgErrors.Departments.NotFound;
        }

        departments.Add(phong.Value);

        return new CreateDepartmentResponse(phong.Value.Id, phong.Value.Name, phong.Value.ParentId);
    }
}
