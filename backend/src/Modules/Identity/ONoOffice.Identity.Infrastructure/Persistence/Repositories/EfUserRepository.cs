using Microsoft.EntityFrameworkCore;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Domain.ValueObjects;

namespace ONoOffice.Identity.Infrastructure.Persistence.Repositories;

internal sealed class EfUserRepository(IdentityDbContext context) : IUserRepository
{
    public async Task<AuthUserData?> GetForLoginAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailResult = Email.Create(email);

        if (emailResult.IsFailure)
        {
            // Email sai định dạng thì không thể có ai mang nó — khỏi phải hỏi database.
            // Handler vẫn chạy Verify với chuỗi băm giả nên thời gian phản hồi không lệch.
            return null;
        }

        // ⚠️ IgnoreQueryFilters — NGOẠI LỆ CÓ CHỦ ĐÍCH, một trong đúng HAI chỗ toàn hệ thống.
        //
        // Bộ lọc tenant so users.tenant_id với tenant của PHIÊN hiện tại. Nhưng lúc đang
        // đăng nhập thì phiên chưa có tenant — người này đang đi XIN token. Để bộ lọc chạy
        // thì điều kiện thành `WHERE tenant_id = <rỗng>`, không khớp hàng nào, và không ai
        // đăng nhập được — lỗi lại trông y hệt lỗi sai mật khẩu, rất khó lần.
        //
        // Chỗ ngoại lệ thứ hai là tra cứu lúc gia hạn phiên, cùng một lý do.
        var row = await context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.Email == emailResult.Value)
            .Join(
                context.Tenants.IgnoreQueryFilters().AsNoTracking(),
                u => u.TenantId,
                t => t.Id,
                (u, t) => new
                {
                    u.Id,
                    u.TenantId,
                    u.PasswordHash,
                    u.FullName,
                    IsUserActive = u.IsActive,
                    IsTenantActive = t.IsActive,
                    RoleIds = EF.Property<List<Guid>>(u, "_roleIds"),
                })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        // Truy vấn thứ hai: gom quyền từ các vai trò. Tách riêng vì nó là quan hệ một-nhiều
        // sang bảng khác; nhét chung vào câu trên sẽ nhân bản hàng người dùng theo số vai trò.
        var permissions = row.RoleIds.Count == 0
            ? []
            : await context.Roles
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(r => row.RoleIds.Contains(r.Id))
                .SelectMany(r => r.Permissions)
                .Distinct()
                .ToListAsync(cancellationToken);

        return new AuthUserData(
            row.Id,
            row.TenantId,
            row.PasswordHash,
            emailResult.Value.Value,
            row.FullName,
            row.IsUserActive,
            row.IsTenantActive,
            permissions.ToHashSet(StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Nạp lại quyền và trạng thái khi gia hạn phiên.
    ///
    /// Nạp LẠI chứ không tin token cũ là có chủ ý: giữa hai lần gia hạn, người này có thể
    /// đã bị khoá tài khoản hoặc bị thu hồi quyền. Đây chính là chỗ những thay đổi đó có
    /// hiệu lực — và cũng là lý do access token cố tình chỉ sống 15 phút.
    /// </summary>
    public async Task<AuthUserData?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var row = await context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Join(
                context.Tenants.IgnoreQueryFilters().AsNoTracking(),
                u => u.TenantId,
                t => t.Id,
                (u, t) => new
                {
                    u.Id,
                    u.TenantId,
                    u.PasswordHash,
                    u.FullName,
                    Email = u.Email,
                    IsUserActive = u.IsActive,
                    IsTenantActive = t.IsActive,
                    RoleIds = EF.Property<List<Guid>>(u, "_roleIds"),
                })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var permissions = row.RoleIds.Count == 0
            ? []
            : await context.Roles
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(r => row.RoleIds.Contains(r.Id))
                .SelectMany(r => r.Permissions)
                .Distinct()
                .ToListAsync(cancellationToken);

        return new AuthUserData(
            row.Id,
            row.TenantId,
            row.PasswordHash,
            row.Email.Value,
            row.FullName,
            row.IsUserActive,
            row.IsTenantActive,
            permissions.ToHashSet(StringComparer.OrdinalIgnoreCase));
    }
}
