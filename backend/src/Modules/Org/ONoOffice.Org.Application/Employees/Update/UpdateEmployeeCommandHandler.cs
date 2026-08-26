using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Org.Application.Abstractions;
using ONoOffice.Org.Domain;

namespace ONoOffice.Org.Application.Employees.Update;

public sealed record UpdateEmployeeCommand(
    Guid Id,
    string FullName,
    string? JobTitle,
    string? WorkEmail,
    string? Phone) : ICommand;

/// <summary>
/// Sửa thông tin trên hồ sơ.
///
/// <b>Cố ý KHÔNG cho đổi MÃ nhân viên.</b> Mã là thứ người ta gõ để tra cứu, in lên thẻ
/// và ghi trong hợp đồng; đổi nó thì mọi giấy tờ cũ trỏ vào một mã không còn ai mang. Sai
/// mã lúc tạo thì đóng hồ sơ đó và mở hồ sơ mới — hiếm, và đó là chủ ý.
///
/// <b>Cũng KHÔNG cho đổi phòng ban ở đây</b> — điều chuyển là một hành động riêng với
/// hậu quả riêng (đổi số người của hai phòng, và sau này là đổi cả kênh trao đổi mà người
/// đó thuộc về). Gộp vào một lệnh sửa thì hai luật nằm lẫn nhau. Xem
/// <c>TransferEmployeeCommand</c>.
/// </summary>
internal sealed class UpdateEmployeeCommandHandler(IEmployeeRepository employees)
    : ICommandHandler<UpdateEmployeeCommand>
{
    public async Task<Result> Handle(
        UpdateEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        var nguoi = await employees.GetAsync(command.Id, cancellationToken);

        if (nguoi is null)
        {
            return OrgErrors.Employees.NotFound;
        }

        var doiTen = nguoi.Rename(command.FullName);

        if (doiTen.IsFailure)
        {
            return doiTen.Error;
        }

        var chucDanh = nguoi.ChangeJobTitle(command.JobTitle);

        if (chucDanh.IsFailure)
        {
            return chucDanh.Error;
        }

        // `UpdateContact` phân biệt RỖNG với SAI ĐỊNH DẠNG: để trống là xoá giá trị cũ và
        // thành công. Nhập nhằng hai ca đó thì người dùng xoá email đi lại nhận thông báo
        // "email không hợp lệ", và không có cách nào xoá được.
        return nguoi.UpdateContact(command.WorkEmail, command.Phone);
    }
}
