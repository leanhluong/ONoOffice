using Luong.Kernel.Abstractions;
using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Domain;
using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.Application.Roles.Create;

public sealed record CreateRoleCommand(string Name, IReadOnlyList<string> Permissions)
    : ICommand<Guid>;

/// <summary>
/// Tạo một vai trò TỰ ĐẶT.
///
/// ═══════════════════════════════════════════════════════════════════════
///  VÌ SAO CẦN
/// ═══════════════════════════════════════════════════════════════════════
///
/// Màn Vai trò đang nói với người dùng: <i>"Quyền đến TỪ vai trò, không gán lẻ cho từng
/// người. Muốn khác đi thì tạo một vai trò mới."</i> — rồi không cho tạo. Bốn vai hệ thống
/// bất biến, nên trước lệnh này câu đó là một ngõ cụt.
///
/// Nhu cầu rất cụ thể: <c>Manager</c> và <c>Member</c> hiện trùng khít nhau (cùng đúng một
/// quyền <c>employee.read</c>), nên công ty muốn một vai "kế toán xem được danh bạ nhưng
/// không sửa phòng ban" thì không có cách nào.
///
/// ═══════════════════════════════════════════════════════════════════════
///  MỘT QUYỀN KHÔNG BAO GIỜ RỜI KHỎI VAI OWNER
/// ═══════════════════════════════════════════════════════════════════════
///
/// <c>workspace.transfer-ownership</c> là TOÀN BỘ ranh giới giữa Admin và Owner (xem
/// <c>SystemRoles.cs</c>). Cho nó rơi vào một vai tự đặt thì màn Vai trò hiện một dòng
/// quyền <b>không bao giờ làm được gì</b> — <c>TransferOwnershipCommandHandler</c> vẫn đọc
/// <c>Tenant.OwnerUserId</c> từ database và từ chối — và người quản trị tin rằng họ vừa
/// trao đi thứ mình không trao.
/// </summary>
internal sealed class CreateRoleCommandHandler(
    IRoleRepository roles,
    ICurrentTenant currentTenant) : ICommandHandler<CreateRoleCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateRoleCommand command,
        CancellationToken cancellationToken)
    {
        // Tenant lấy từ PHIÊN, không bao giờ từ thân request — cùng lý do với CreateUser.
        if (currentTenant.TenantId is not { } tenantId)
        {
            return IdentityErrors.Roles.TenantRequired;
        }

        var chan = ChanQuyenChuSoHuu(command.Permissions);

        if (chan.IsFailure)
        {
            return chan.Error;
        }

        var role = Role.Create(tenantId, command.Name);

        if (role.IsFailure)
        {
            return role.Error;
        }

        // Hỏi trùng tên bằng tên ĐÃ CHUẨN HOÁ, không bằng chuỗi thô. `Role.Create` cắt
        // khoảng trắng, nên hỏi bằng chuỗi thô là kiểm trên một giá trị KHÁC giá trị sắp
        // lưu — tạo được cả hai, rồi ràng buộc ở database mới nổ bằng một lỗi 500. Đúng
        // cái bẫy đã gặp với mã nhân viên.
        if (await roles.NameTakenAsync(role.Value.Name, null, cancellationToken))
        {
            return IdentityErrors.Roles.NameTaken;
        }

        foreach (var permission in command.Permissions)
        {
            var granted = role.Value.Grant(permission);

            if (granted.IsFailure)
            {
                return granted.Error;
            }
        }

        roles.Add(role.Value);

        return role.Value.Id;
    }

    /// <summary>Dùng chung với lệnh sửa — cùng một luật, và nó phải giống nhau ở cả hai.</summary>
    internal static Result ChanQuyenChuSoHuu(IEnumerable<string> permissions) =>
        permissions.Any(p => p?.Trim() == Permissions.Workspace.TransferOwnership)
            ? IdentityErrors.Roles.PermissionIsOwnerOnly
            : Result.Success();
}
