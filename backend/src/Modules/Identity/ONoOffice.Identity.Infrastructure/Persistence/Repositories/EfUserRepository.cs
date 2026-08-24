using Luong.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Domain.ValueObjects;

namespace ONoOffice.Identity.Infrastructure.Persistence.Repositories;

internal sealed class EfUserRepository(IdentityDbContext context) : IUserRepository
{
    public void Add(Domain.Entities.User user) => context.Users.Add(user);

    /// <summary>
    /// ⚠️ IgnoreQueryFilters — ngoại lệ có chủ đích, cùng lý do với hai chỗ kia: đăng ký
    /// chạy khi CHƯA có workspace nào, nên bộ lọc tenant sẽ không khớp hàng nào và mọi
    /// email đều trông như còn trống. Để lọt thì ràng buộc UNIQUE ở database chặn, nhưng
    /// người dùng nhận một lỗi 500 thay vì một câu tiếng Việt.
    ///
    /// Và email vốn unique TOÀN hệ thống (ADR-0002) — hỏi theo phạm vi workspace là hỏi sai.
    /// </summary>
    public async Task<bool> IsEmailTakenAsync(string email, CancellationToken cancellationToken = default)
    {
        var parsed = Email.Create(email);

        if (parsed.IsFailure)
        {
            return false;
        }

        return await context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == parsed.Value, cancellationToken);
    }

    /// <summary>
    /// Danh sách nhân sự cho màn quản trị: lọc, sắp xếp, phân trang — <b>tất cả ở database</b>.
    ///
    /// Hai truy vấn, không phải N+1:
    ///
    /// <list type="number">
    /// <item>Một câu lấy đúng một trang người dùng, kèm mảng <c>role_ids</c> của họ.</item>
    /// <item>Một câu lấy tên của những vai trò xuất hiện trên trang đó.</item>
    /// </list>
    ///
    /// Gộp làm một câu thì phải dàn phẳng cột mảng <c>uuid[]</c>, mà EF không sinh được
    /// SQL cho việc đó qua phép chuyển đổi giá trị. Hai câu cho tối đa 100 dòng là rẻ;
    /// một câu cho mỗi dòng thì không.
    ///
    /// <b>Về ô tìm kiếm:</b> tên khớp một phần, email chỉ khớp CHÍNH XÁC. Lý do rất cụ
    /// thể: cột <c>email</c> ánh xạ qua một phép chuyển đổi giá trị (<c>Email</c> ↔
    /// <c>text</c>), nên EF không dịch nổi <c>Contains</c> trên nó. Đổi sang kiểu sở hữu
    /// để tìm được một phần email là một thay đổi riêng — ghi ở mục "chưa làm".
    /// </summary>
    public async Task<PagedList<UserListItem>> SearchAsync(
        UserSearch criteria,
        CancellationToken cancellationToken = default)
    {
        // KHÔNG có IgnoreQueryFilters ở đây, khác hẳn ba truy vấn phía trên. Đây là màn
        // quản trị của một workspace cụ thể, và bộ lọc tenant chính là thứ giữ cho quản
        // trị viên công ty A không nhìn thấy nhân sự công ty B.
        var query = context.Users.AsNoTracking();

        if (criteria.Search is { } term)
        {
            var parsed = Email.Create(term);

            query = parsed.IsSuccess
                ? query.Where(u => u.Email == parsed.Value)
                : query.Where(u => EF.Functions.ILike(u.FullName, $"%{term}%"));
        }

        query = criteria.Status switch
        {
            UserStatusFilter.Active => query.Where(u => u.IsActive && !u.MustChangePassword),
            UserStatusFilter.PendingFirstLogin => query.Where(u => u.IsActive && u.MustChangePassword),
            UserStatusFilter.Disabled => query.Where(u => !u.IsActive),
            _ => query,
        };

        if (criteria.RoleId is { } roleId)
        {
            query = query.Where(u => EF.Property<List<Guid>>(u, "_roleIds").Contains(roleId));
        }

        // Đếm TRƯỚC khi phân trang, và đếm bằng một câu riêng. Đếm sau khi đã `Skip/Take`
        // thì con số luôn bằng số dòng của trang — thanh phân trang nói dối.
        var total = await query.CountAsync(cancellationToken);

        var rows = await query
            // Sắp xếp phải ỔN ĐỊNH, nếu không hai trang liên tiếp có thể trả về cùng một
            // người và bỏ sót một người khác. `Id` là chốt chặn cho những tên trùng nhau.
            .OrderBy(u => u.FullName)
            .ThenBy(u => u.Id)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
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

        var items = rows
            .Select(r => new UserListItem(
                r.Id,
                r.Email.Value,
                r.FullName,
                r.IsActive,
                r.MustChangePassword,

                // Người chưa được gán vai trò nào là dữ liệu hỏng, nhưng bảng vẫn phải vẽ
                // ra được — ném ở đây thì cả màn Nhân sự trắng vì đúng MỘT hàng lỗi, và
                // quản trị viên không có cách nào tìm ra hàng đó để sửa.
                string.Join(", ", r.RoleIds.Select(id => roleNames.GetValueOrDefault(id, "—"))),
                r.CreatedAtUtc))
            .ToList();

        return PagedList<UserListItem>.Create(items, criteria.Page, criteria.PageSize, total);
    }

    /// <summary>
    /// Gom quyền của nhiều vai trò thành một tập.
    ///
    /// Tách riêng khỏi truy vấn người dùng vì đó là quan hệ một-nhiều sang bảng khác;
    /// nhét chung vào một câu sẽ nhân bản hàng người dùng theo số vai trò.
    ///
    /// <b>Gộp mảng ở phía C# chứ không ở phía Postgres</b> — cố ý, chứ không phải lười.
    /// Cột <c>permissions</c> là <c>text[]</c> đọc lên qua một phép chuyển đổi giá trị,
    /// nên EF không dàn phẳng nó bằng SQL được. Đổi lại thì cũng chẳng mất gì: một người
    /// có vài vai trò, mỗi vai vài chục quyền — đây là vài trăm chuỗi, không phải vài
    /// trăm nghìn. Ngưỡng phải xem lại là khi một người có hàng trăm vai trò, mà lúc đó
    /// vấn đề nằm ở chỗ khác rồi.
    /// </summary>
    private async Task<List<string>> GomQuyenAsync(List<Guid> roleIds, CancellationToken cancellationToken)
    {
        if (roleIds.Count == 0)
        {
            return [];
        }

        var tungVai = await context.Roles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => EF.Property<HashSet<string>>(r, "_permissions"))
            .ToListAsync(cancellationToken);

        return [.. tungVai.SelectMany(tap => tap).Distinct(StringComparer.OrdinalIgnoreCase)];
    }

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
                    u.MustChangePassword,
                    RoleIds = EF.Property<List<Guid>>(u, "_roleIds"),
                })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        // Truy vấn thứ hai: gom quyền từ các vai trò. Tách riêng vì nó là quan hệ một-nhiều
        // sang bảng khác; nhét chung vào câu trên sẽ nhân bản hàng người dùng theo số vai trò.
        var permissions = await GomQuyenAsync(row.RoleIds, cancellationToken);

        return new AuthUserData(
            row.Id,
            row.TenantId,
            row.PasswordHash,
            emailResult.Value.Value,
            row.FullName,
            row.IsUserActive,
            row.IsTenantActive,
            row.MustChangePassword,
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
                    u.MustChangePassword,
                    RoleIds = EF.Property<List<Guid>>(u, "_roleIds"),
                })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var permissions = await GomQuyenAsync(row.RoleIds, cancellationToken);

        return new AuthUserData(
            row.Id,
            row.TenantId,
            row.PasswordHash,
            row.Email.Value,
            row.FullName,
            row.IsUserActive,
            row.IsTenantActive,
            row.MustChangePassword,
            permissions.ToHashSet(StringComparer.OrdinalIgnoreCase));
    }
}
