using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Org.Application.Abstractions;
using ONoOffice.Org.Domain;

namespace ONoOffice.Org.Application.Departments.Delete;

public sealed record DeleteDepartmentCommand(Guid Id) : ICommand;

/// <summary>
/// Xoá một phòng ban — chỉ khi nó RỖNG.
///
/// Hai chốt chặn, và <b>thứ tự báo lỗi có nghĩa</b>: còn cả phòng con lẫn nhân viên thì
/// báo phòng con trước. Không phải chuyện thẩm mỹ — phải chuyển phòng con đi trước rồi
/// mới điều chuyển được người, nên báo lỗi kia trước sẽ khiến người dùng làm xong một
/// việc rồi vẫn bị chặn, và không hiểu vì sao.
///
/// <b>Vì sao không xoá theo tầng (cascade):</b> xoá một phòng ban là thao tác hiếm và
/// khó đảo. Xoá theo tầng thì một cú bấm có thể cuốn theo mười phòng và ba trăm hồ sơ
/// nhân sự, mà người bấm chỉ nhìn thấy đúng một dòng biến mất.
///
/// Xoá ở đây là xoá MỀM cho <c>Employee</c> nhưng <c>Department</c> thì không cài
/// <c>ISoftDeletable</c> — một phòng ban rỗng không mang lịch sử gì đáng giữ, và giữ lại
/// thì cây phải lọc hàng đã xoá ở mọi truy vấn.
/// </summary>
internal sealed class DeleteDepartmentCommandHandler(IDepartmentRepository departments)
    : ICommandHandler<DeleteDepartmentCommand>
{
    public async Task<Result> Handle(
        DeleteDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        var phong = await departments.GetAsync(command.Id, cancellationToken);

        if (phong is null)
        {
            return OrgErrors.Departments.NotFound;
        }

        if (await departments.HasChildrenAsync(command.Id, cancellationToken))
        {
            return OrgErrors.Departments.HasChildren;
        }

        if (await departments.HasEmployeesAsync(command.Id, cancellationToken))
        {
            return OrgErrors.Departments.HasEmployees;
        }

        departments.Remove(phong);

        return Result.Success();
    }
}
