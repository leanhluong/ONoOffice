using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Identity.Application.Abstractions;

namespace ONoOffice.Identity.Application.Roles.GetList;

public sealed record GetRolesQuery : IQuery<IReadOnlyList<RoleListItem>>;

/// <summary>
/// Mọi vai trò của workspace, kèm quyền và số người đang giữ.
///
/// Không phân trang: một workspace có bốn vai hệ thống cộng vài vai tự tạo. Phân trang
/// một danh sách năm dòng là thêm phức tạp cho cả hai phía mà không đổi được gì.
///
/// Phục vụ hai màn: danh sách xổ chọn vai trò ở hộp thoại thêm người, và màn
/// <b>Vai trò &amp; quyền</b>.
/// </summary>
internal sealed class GetRolesQueryHandler(IRoleRepository roles)
    : IQueryHandler<GetRolesQuery, IReadOnlyList<RoleListItem>>
{
    public async Task<Result<IReadOnlyList<RoleListItem>>> Handle(
        GetRolesQuery query,
        CancellationToken cancellationToken) =>
        Result.Success(await roles.GetAllAsync(cancellationToken));
}
