using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Identity.Contracts;
using ONoOffice.Org.Application.Abstractions;
using ONoOffice.Org.Domain;

namespace ONoOffice.Org.Application.Employees.LinkAccount;

public sealed record LinkAccountCommand(Guid EmployeeId, Guid UserId) : ICommand;

public sealed record UnlinkAccountCommand(Guid EmployeeId) : ICommand;

/// <summary>
/// Nối một hồ sơ nhân sự với một tài khoản đăng nhập.
///
/// <b>Đây là chỗ hai module gặp nhau, và cũng là chỗ dễ hỏng nhất.</b>
/// <c>Employee.UserId</c> cố ý KHÔNG phải khoá ngoại — Luật 3 cấm ràng buộc xuyên schema —
/// nên database <b>không</b> canh giúp. Tin thẳng con số client gửi lên thì nối được hồ sơ
/// vào một tài khoản không tồn tại, hoặc vào tài khoản của công ty khác, và không lớp nào
/// phía dưới bắt được.
///
/// Vì vậy phép kiểm phải nằm ở đây, gọi qua <see cref="IUserDirectory"/>. Bản cài của cổng
/// đọc qua <c>IdentityDbContext</c> nên bộ lọc tenant tự áp — tài khoản của workspace khác
/// đơn giản là "không tồn tại".
///
/// <c>Employee.LinkAccount</c> từ chối nếu hồ sơ đã nối rồi, thay vì gán đè: gán đè im
/// lặng nghĩa là một lỗi lập trình có thể nối hồ sơ người này sang tài khoản người khác,
/// và từ đó mọi thao tác của họ bị ghi tên nhầm người. Muốn đổi thì phải gỡ trước — một
/// bước cố ý.
/// </summary>
internal sealed class LinkAccountCommandHandler(
    IEmployeeRepository employees,
    IUserDirectory users) : ICommandHandler<LinkAccountCommand>
{
    public async Task<Result> Handle(
        LinkAccountCommand command,
        CancellationToken cancellationToken)
    {
        var nguoi = await employees.GetAsync(command.EmployeeId, cancellationToken);

        if (nguoi is null)
        {
            return OrgErrors.Employees.NotFound;
        }

        if (!await users.ExistsAsync(command.UserId, cancellationToken))
        {
            // Cố ý dùng mã lỗi của Identity: người dùng đang chọn một TÀI KHOẢN, nên câu
            // trả lời phải nói về tài khoản. Trả `Employee.NotFound` ở đây thì họ đi tìm
            // xem hồ sơ nào biến mất.
            return Error.NotFound("User.NotFound", "Không tìm thấy tài khoản.");
        }

        return nguoi.LinkAccount(command.UserId);
    }
}

/// <summary>
/// Gỡ liên kết — dùng khi nối nhầm, hoặc khi một người đổi sang tài khoản khác.
///
/// Không cần hỏi Identity: gỡ chỉ xoá một giá trị trong hồ sơ, và tài khoản kia còn hay
/// mất cũng không đổi kết quả.
/// </summary>
internal sealed class UnlinkAccountCommandHandler(IEmployeeRepository employees)
    : ICommandHandler<UnlinkAccountCommand>
{
    public async Task<Result> Handle(
        UnlinkAccountCommand command,
        CancellationToken cancellationToken)
    {
        var nguoi = await employees.GetAsync(command.EmployeeId, cancellationToken);

        return nguoi is null ? OrgErrors.Employees.NotFound : nguoi.UnlinkAccount();
    }
}
