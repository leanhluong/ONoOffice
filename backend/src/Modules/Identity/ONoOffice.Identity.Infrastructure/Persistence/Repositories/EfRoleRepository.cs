using Microsoft.EntityFrameworkCore;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.Infrastructure.Persistence.Repositories;

internal sealed class EfRoleRepository(IdentityDbContext context) : IRoleRepository
{
    public void AddRange(IEnumerable<Role> roles) => context.Roles.AddRange(roles);

    /// <summary>
    /// Có theo dõi thay đổi (không <c>AsNoTracking</c>): vai trò nạp về đây là để GÁN, và
    /// những lời gọi sau sẽ sửa nó. Nạp không theo dõi thì mọi thay đổi rơi vào hư không
    /// mà không có lỗi nào báo.
    ///
    /// Bộ lọc theo tenant tự áp — xem <c>IdentityDbContext</c>. Nơi gọi vẫn kiểm lại
    /// <c>TenantId</c> một lần nữa, có chủ ý.
    /// </summary>
    public Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        context.Roles.FirstOrDefaultAsync(role => role.Id == roleId, cancellationToken);

    /// <summary>
    /// Tra vai theo TÊN — chỉ dùng cho bốn vai hệ thống, xem chú thích ở cổng.
    ///
    /// Bộ lọc tenant của EF đã giới hạn về workspace hiện tại, nên tên là duy nhất trong
    /// phạm vi truy vấn này. Có theo dõi thay đổi vì nơi gọi sẽ GÁN vai đó cho ai đó.
    /// </summary>
    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        context.Roles.FirstOrDefaultAsync(role => role.Name == name, cancellationToken);

    /// <summary>
    /// Mọi vai trò của workspace, kèm số người đang giữ.
    ///
    /// Đếm người bằng MỘT câu riêng thay vì một truy vấn con cho từng vai: cột
    /// <c>role_ids</c> là mảng <c>uuid[]</c> đọc lên qua phép chuyển đổi giá trị, nên EF
    /// không dàn phẳng nó bằng SQL được. Nạp về danh sách mảng rồi đếm ở C# là chấp nhận
    /// được — một workspace vài trăm người là vài trăm mảng ngắn, không phải vài trăm nghìn.
    /// </summary>
    public async Task<IReadOnlyList<RoleListItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var roles = await context.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .Select(role => new
            {
                role.Id,
                role.Name,
                role.IsSystem,
                Permissions = EF.Property<HashSet<string>>(role, "_permissions"),
            })
            .ToListAsync(cancellationToken);

        var roleIdsPerUser = await context.Users
            .AsNoTracking()
            .Select(user => EF.Property<List<Guid>>(user, "_roleIds"))
            .ToListAsync(cancellationToken);

        var counts = roleIdsPerUser
            .SelectMany(ids => ids)
            .GroupBy(id => id)
            .ToDictionary(group => group.Key, group => group.Count());

        return
        [
            .. roles.Select(role => new RoleListItem(
                role.Id,
                role.Name,
                role.IsSystem,
                [.. role.Permissions.OrderBy(p => p, StringComparer.Ordinal)],
                counts.GetValueOrDefault(role.Id))),
        ];
    }
}
