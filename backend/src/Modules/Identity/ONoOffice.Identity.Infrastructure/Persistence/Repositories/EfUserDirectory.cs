using Microsoft.EntityFrameworkCore;
using ONoOffice.Identity.Contracts;

namespace ONoOffice.Identity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Bản cài <see cref="IUserDirectory"/> — cổng để module Org hỏi về tài khoản.
///
/// <b>KHÔNG có <c>IgnoreQueryFilters</c> ở đây, và đó là điều quan trọng nhất của tệp
/// này.</b> Toàn hệ thống chỉ có ĐÚNG HAI chỗ được phép bỏ bộ lọc tenant: tra cứu lúc
/// đăng nhập và lúc gia hạn phiên — cả hai chạy khi phiên CHƯA có tenant, tức là người ta
/// đang đi *xin* token. Cổng này thì luôn được gọi từ trong một phiên đã có tenant, nên
/// thấy <c>IgnoreQueryFilters</c> xuất hiện ở đây là một lỗ rò: tài khoản của công ty khác
/// sẽ lọt vào danh sách nhân sự của công ty này, im lặng và đúng cú pháp.
/// </summary>
internal sealed class EfUserDirectory(IdentityDbContext context) : IUserDirectory
{
    /// <summary>
    /// Hai lượt đọc, KHÔNG phải một truy vấn con — cùng cách <c>EfUserRepository</c> làm,
    /// và cùng lý do.
    ///
    /// <c>_roleIds</c> là <b>primitive collection</b> (mảng <c>uuid</c> trong Postgres),
    /// không phải bảng quan hệ. Viết <c>context.Roles.Where(r =&gt; u.RoleIds.Contains(r.Id))</c>
    /// trong phần chiếu nghe rất tự nhiên và <b>dịch được hoặc không tuỳ phiên bản</b> —
    /// hỏng thì nó hỏng lúc CHẠY, không phải lúc biên dịch. Dự án này đã bị cắn đúng kiểu
    /// đó một lần: mô hình EF dựng lên được, test đơn vị xanh, và truy vấn thật đầu tiên
    /// trên Postgres mới nổ.
    ///
    /// Hai lượt đọc thì luôn dịch được, và ở quy mô vài trăm tài khoản thì rẻ hơn nhiều so
    /// với rủi ro đó.
    /// </summary>
    public async Task<IReadOnlyList<UserSummary>> GetAllAsync(CancellationToken cancellationToken)
    {
        var rows = await context.Users
            .AsNoTracking()
            .OrderBy(u => u.FullName)
            .ThenBy(u => u.Id)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FullName,
                u.IsActive,
                u.MustChangePassword,
                u.CreatedAtUtc,
                RoleIds = EF.Property<List<Guid>>(u, "_roleIds"),
            })
            .ToListAsync(cancellationToken);

        var roleIds = rows.SelectMany(r => r.RoleIds).Distinct().ToList();

        var roleNames = roleIds.Count == 0
            ? []
            : await context.Roles
                .AsNoTracking()
                .Where(r => roleIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

        return rows
            .Select(r => new UserSummary(
                r.Id,
                r.Email.Value,
                r.FullName,

                // Người chưa được gán vai trò nào là dữ liệu hỏng, nhưng danh sách vẫn
                // phải vẽ ra được — ném ở đây thì cả màn Thành viên trắng vì đúng MỘT hàng
                // lỗi, và quản trị viên không có cách nào tìm ra hàng đó để sửa.
                string.Join(", ", r.RoleIds.Select(id => roleNames.GetValueOrDefault(id, "—"))),
                r.IsActive,
                r.MustChangePassword,
                r.CreatedAtUtc))
            .ToList();
    }

    public Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken)
        => context.Users.AsNoTracking().AnyAsync(u => u.Id == userId, cancellationToken);
}
