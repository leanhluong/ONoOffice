using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Application.Roles.Create;
using ONoOffice.Identity.Domain;

namespace ONoOffice.Identity.Application.Roles.Update;

public sealed record UpdateRoleCommand(Guid RoleId, string Name, IReadOnlyList<string> Permissions)
    : ICommand;

/// <summary>
/// Đổi tên và ĐẶT LẠI bộ quyền của một vai tự đặt.
///
/// <b>Đặt lại cả bộ, không cộng thêm.</b> Màn hình gửi lên đúng những ô đang tick, nên thân
/// request là <i>trạng thái mong muốn</i>. Hiểu nó thành "thêm" thì bỏ tick một quyền chẳng
/// gỡ được gì — quyền chỉ có tăng, và không chỗ nào trên giao diện lộ ra điều đó.
///
/// Vai HỆ THỐNG bị <c>Role.Rename</c> và <c>Role.Grant</c> từ chối ngay ở Domain: sửa được
/// chúng thì một cú bấm nhầm có thể thu hết quyền của Owner, và lúc đó không còn ai trong
/// workspace cấp lại được cho ai cả.
/// </summary>
internal sealed class UpdateRoleCommandHandler(IRoleRepository roles)
    : ICommandHandler<UpdateRoleCommand>
{
    public async Task<Result> Handle(UpdateRoleCommand command, CancellationToken cancellationToken)
    {
        var role = await roles.GetByIdAsync(command.RoleId, cancellationToken);

        if (role is null)
        {
            return IdentityErrors.Roles.NotFound;
        }

        // Chặn TRƯỚC khi đụng vào gì: `Rename` cũng từ chối vai hệ thống, nhưng nếu để nó
        // báo thì thứ tự phép kiểm quyết định mã lỗi trả về, và người dùng nhận một câu
        // nói về tên trong khi vấn đề là vai này không sửa được.
        if (role.IsSystem)
        {
            return IdentityErrors.Roles.SystemRoleIsImmutable;
        }

        var chan = CreateRoleCommandHandler.ChanQuyenChuSoHuu(command.Permissions);

        if (chan.IsFailure)
        {
            return chan.Error;
        }

        var renamed = role.Rename(command.Name);

        if (renamed.IsFailure)
        {
            return renamed.Error;
        }

        if (await roles.NameTakenAsync(role.Name, role.Id, cancellationToken))
        {
            return IdentityErrors.Roles.NameTaken;
        }

        // Gỡ hết rồi gán lại. Tính hiệu hai tập rồi chỉ đụng phần khác nhau thì ít câu
        // UPDATE hơn, nhưng bộ quyền là một mảng lưu trong MỘT cột — EF ghi lại cả cột dù
        // đổi một phần tử hay tất cả. Phức tạp thêm mà không mua được gì.
        foreach (var current in role.Permissions.ToList())
        {
            var revoked = role.Revoke(current);

            if (revoked.IsFailure)
            {
                return revoked.Error;
            }
        }

        foreach (var permission in command.Permissions)
        {
            var granted = role.Grant(permission);

            if (granted.IsFailure)
            {
                return granted.Error;
            }
        }

        return Result.Success();
    }
}
