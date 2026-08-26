using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Org.Application.Abstractions;
using ONoOffice.Org.Domain;

namespace ONoOffice.Org.Application.Employees.Transfer;

/// <summary><c>DepartmentId = null</c> nghĩa là rút khỏi mọi phòng.</summary>
public sealed record TransferEmployeeCommand(Guid Id, Guid? DepartmentId) : ICommand;

/// <summary>
/// Điều chuyển một người sang phòng khác.
///
/// Lệnh RIÊNG, không gộp vào <c>UpdateEmployeeCommand</c>: điều chuyển đổi số người của
/// hai phòng ban, và sau này sẽ đổi cả kênh trao đổi mà người đó thuộc về. Gộp vào một
/// lệnh sửa thông tin thì luật của hai việc nằm lẫn nhau, và dễ thiếu một nhánh.
///
/// <c>Employee.TransferTo</c> từ chối ca chuyển vào đúng phòng đang ở — không phải để
/// khó tính, mà để nhật ký thay đổi (lát sau) không đầy những dòng "điều chuyển" mà chẳng
/// có gì đổi.
/// </summary>
internal sealed class TransferEmployeeCommandHandler(
    IEmployeeRepository employees,
    IDepartmentRepository departments) : ICommandHandler<TransferEmployeeCommand>
{
    public async Task<Result> Handle(
        TransferEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        var nguoi = await employees.GetAsync(command.Id, cancellationToken);

        if (nguoi is null)
        {
            return OrgErrors.Employees.NotFound;
        }

        // Rút khỏi mọi phòng (`null`) thì không có phòng nào để kiểm tồn tại. Kiểm thừa ở
        // đây sẽ là một truy vấn cho `Guid?` rỗng — luôn không tìm thấy, và luôn từ chối
        // một thao tác hoàn toàn hợp lệ.
        if (command.DepartmentId is { } phongId
            && await departments.GetAsync(phongId, cancellationToken) is null)
        {
            return OrgErrors.Departments.NotFound;
        }

        return nguoi.TransferTo(command.DepartmentId);
    }
}
