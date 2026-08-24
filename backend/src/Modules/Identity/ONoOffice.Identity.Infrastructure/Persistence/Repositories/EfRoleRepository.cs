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
}
