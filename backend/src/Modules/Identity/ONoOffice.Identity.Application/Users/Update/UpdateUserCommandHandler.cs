using Luong.Kernel.Abstractions;
using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Domain;

namespace ONoOffice.Identity.Application.Users.Update;

/// <param name="FullName">
/// <c>null</c> = <b>đừng động vào tên</b>, chỉ đổi vai trò.
///
/// Có mặt vì thao tác HÀNG LOẠT ở màn Thành viên chỉ định đổi vai. Bắt nó gửi kèm tên thì
/// nó phải gửi lại cái tên đã tải về vài giây trước — và nếu trong khoảng đó có người khác
/// vừa đổi tên, thao tác hàng loạt sẽ <b>ghi đè tên mới bằng tên cũ</b>. Mất một thay đổi
/// mà không có gì báo, chỉ vì một trường mà người bấm nút còn không biết là mình đang gửi.
///
/// Chuỗi RỖNG thì vẫn bị từ chối như cũ: rỗng là "tôi muốn đặt tên thành rỗng", khác hẳn
/// "tôi không sửa tên". Nhập nhằng hai ca đó là cách xoá sạch tên người ta mà không ai thấy.
/// </param>
public sealed record UpdateUserCommand(Guid UserId, string? FullName, Guid RoleId) : ICommand;

/// <summary>
/// Sửa họ tên và vai trò của một tài khoản.
///
/// <b>Đổi vai trò là THAY, không phải THÊM.</b> Mô hình của app là một người một vai
/// (ADR-0002). Thêm mà không gỡ thì quyền chỉ có tăng — hạ một người từ Admin xuống
/// Member sẽ không lấy lại được quyền nào, và không có gì trên màn hình lộ ra điều đó.
///
/// <b>Vì sao không cho đổi vai trò của chủ sở hữu:</b> chủ sở hữu là người DUY NHẤT
/// chuyển nhượng được workspace. Hạ vai họ xuống thì không còn ai làm được việc đó, và
/// workspace kẹt vĩnh viễn — không có đường sửa nào ngoài can thiệp thẳng vào database.
/// </summary>
internal sealed class UpdateUserCommandHandler(
    IUserRepository users,
    IRoleRepository roles,
    ITenantRepository tenants,
    ICurrentTenant currentTenant) : ICommandHandler<UpdateUserCommand>
{
    public async Task<Result> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        if (currentTenant.TenantId is not { } tenantId)
        {
            return IdentityErrors.Users.TenantRequired;
        }

        var user = await users.GetForUpdateAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return IdentityErrors.Users.NotFound;
        }

        var role = await roles.GetByIdAsync(command.RoleId, cancellationToken);

        // So cả TenantId chứ không chỉ tin bộ lọc của EF — cùng lý do với CreateUser.
        if (role is null || role.TenantId != tenantId)
        {
            return IdentityErrors.Roles.NotFound;
        }

        // `null` là "không sửa tên", KHÔNG phải "đặt tên thành rỗng" — xem chú thích ở
        // `UpdateUserCommand.FullName`. Chuỗi rỗng vẫn đi qua `Rename` và vẫn bị từ chối.
        if (command.FullName is not null)
        {
            var renamed = user.Rename(command.FullName);

            if (renamed.IsFailure)
            {
                return renamed.Error;
            }
        }

        if (user.RoleIds.Contains(command.RoleId))
        {
            // Đã đúng vai rồi thì chỉ đổi tên. Gỡ ra gán lại cũng cho kết quả y hệt, nhưng
            // nó sinh ra một câu UPDATE thừa và một dòng lịch sử sai.
            return Result.Success();
        }

        var ownerUserId = await tenants.GetOwnerUserIdAsync(cancellationToken);

        if (ownerUserId == user.Id)
        {
            return IdentityErrors.Users.CannotChangeOwnerRole;
        }

        foreach (var current in user.RoleIds.ToList())
        {
            var removed = user.RemoveRole(current);

            if (removed.IsFailure)
            {
                return removed.Error;
            }
        }

        return user.AssignRole(command.RoleId);
    }
}
