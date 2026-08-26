using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Domain;

namespace ONoOffice.Identity.Application.Roles.Delete;

public sealed record DeleteRoleCommand(Guid RoleId) : ICommand;

/// <summary>
/// Xoá một vai tự đặt.
///
/// Hai cửa chặn, và cả hai đều chặn <b>mất quyền im lặng</b>:
///
/// <list type="bullet">
/// <item><b>Vai hệ thống thì không xoá.</b> Bốn vai đó dựng lại từ hằng số trong mã nguồn
/// ở mọi workspace; xoá một cái đi thì lần khởi động sau dựng lại nó với mã KHÁC, và mọi
/// người đang giữ vai cũ trỏ vào hư không.</item>
/// <item><b>Vai còn người giữ thì không xoá.</b> Họ sẽ mang một mã vai không còn tồn tại —
/// mất SẠCH quyền ngay lập tức, và màn Thành viên hiện một ô vai trống. Bắt điều chuyển
/// trước thì người quản trị buộc phải quyết định họ thành vai gì.</item>
/// </list>
///
/// Xoá CỨNG chứ không xoá mềm, khác hẳn hồ sơ nhân sự. Vai trò không phải dữ liệu người ta
/// tra lại sau nhiều năm; nó là cấu hình. Và một vai đã xoá mềm vẫn chiếm tên, nên tạo lại
/// đúng tên đó sẽ báo trùng với một thứ không ai nhìn thấy.
/// </summary>
internal sealed class DeleteRoleCommandHandler(IRoleRepository roles, IUserRepository users)
    : ICommandHandler<DeleteRoleCommand>
{
    public async Task<Result> Handle(DeleteRoleCommand command, CancellationToken cancellationToken)
    {
        var role = await roles.GetByIdAsync(command.RoleId, cancellationToken);

        if (role is null)
        {
            return IdentityErrors.Roles.NotFound;
        }

        if (role.IsSystem)
        {
            return IdentityErrors.Roles.SystemRoleIsImmutable;
        }

        if (await users.CountByRoleAsync(role.Id, cancellationToken) > 0)
        {
            return IdentityErrors.Roles.StillInUse;
        }

        roles.Remove(role);

        return Result.Success();
    }
}
