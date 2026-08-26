using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Org.Application.Abstractions;
using ONoOffice.Org.Domain;

namespace ONoOffice.Org.Application.Employees.Leave;

public sealed record LeaveEmployeeCommand(Guid Id, DateOnly LeftOn) : ICommand;

public sealed record ReinstateEmployeeCommand(Guid Id) : ICommand;

/// <summary>
/// Đóng hồ sơ khi một người nghỉ việc.
///
/// <b>KHÔNG xoá hồ sơ.</b> Hồ sơ nhân sự là dữ liệu người ta còn phải tra lại sau nhiều
/// năm — hợp đồng, bảo hiểm, tranh chấp. Xoá một hàng ở đây là mất một mảnh lịch sử công
/// ty. Người đã nghỉ biến mất khỏi danh bạ mặc định, nhưng vẫn tra được bằng công tắc
/// "hiện cả người đã nghỉ".
///
/// Cũng vì thế mà phòng ban còn người đã nghỉ thì <b>không xoá được</b>: hồ sơ của họ vẫn
/// trỏ vào phòng đó, và xoá phòng đi là mất luôn thông tin "từng làm ở đâu".
/// </summary>
internal sealed class LeaveEmployeeCommandHandler(IEmployeeRepository employees)
    : ICommandHandler<LeaveEmployeeCommand>
{
    public async Task<Result> Handle(
        LeaveEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        var nguoi = await employees.GetAsync(command.Id, cancellationToken);

        return nguoi is null ? OrgErrors.Employees.NotFound : nguoi.Leave(command.LeftOn);
    }
}

/// <summary>Nhận lại một người đã nghỉ — họ quay lại công ty, dùng lại đúng hồ sơ cũ.</summary>
internal sealed class ReinstateEmployeeCommandHandler(IEmployeeRepository employees)
    : ICommandHandler<ReinstateEmployeeCommand>
{
    public async Task<Result> Handle(
        ReinstateEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        var nguoi = await employees.GetAsync(command.Id, cancellationToken);

        return nguoi is null ? OrgErrors.Employees.NotFound : nguoi.Reinstate();
    }
}
