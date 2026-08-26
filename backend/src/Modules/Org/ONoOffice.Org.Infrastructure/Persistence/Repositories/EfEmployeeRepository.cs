using Luong.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;
using ONoOffice.Org.Application.Abstractions;
using ONoOffice.Org.Domain.Entities;

namespace ONoOffice.Org.Infrastructure.Persistence.Repositories;

internal sealed class EfEmployeeRepository(OrgDbContext context) : IEmployeeRepository
{
    public void Add(Employee employee) => context.Employees.Add(employee);

    public Task<Employee?> GetAsync(Guid id, CancellationToken cancellationToken)
        => context.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    /// <summary>
    /// Màn <b>Danh bạ</b>: lọc, sắp xếp và phân trang đều ở DATABASE.
    ///
    /// Kéo hết lên rồi lọc bằng LINQ-to-Objects thì với 38 người vẫn chạy, và với 3.800
    /// người thì sập — mà không có gì trong mã báo trước điều đó.
    /// </summary>
    public async Task<PagedList<ContactListItem>> SearchAsync(
        ContactSearch criteria,
        CancellationToken cancellationToken)
    {
        var query = context.Employees.AsNoTracking();

        if (!criteria.IncludeInactive)
        {
            // Mặc định danh bạ chỉ hiện người ĐANG làm. Người đã nghỉ vẫn còn hồ sơ, nhưng
            // hiện họ trong danh bạ thì đồng nghiệp gọi điện cho một người không còn ở đây.
            query = query.Where(e => e.IsActive);
        }

        if (criteria.DepartmentId is { } phong)
        {
            query = query.Where(e => e.DepartmentId == phong);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            string tim = criteria.Search.ToLower();

            // Tìm theo tên, MÃ và email. Mã nhân viên là thứ người ta gõ khi tra cứu nhanh,
            // và bỏ nó ra khỏi ô tìm thì phải có thêm một ô nữa chỉ để tìm mã.
            query = query.Where(e =>
                e.FullName.ToLower().Contains(tim)
                || e.Code.ToLower().Contains(tim)
                || (e.WorkEmail != null && e.WorkEmail.Value.ToLower().Contains(tim)));
        }

        // Đếm TRƯỚC khi cắt trang. Đếm sau thì con số luôn bằng số dòng của trang, và
        // thanh phân trang nói dối.
        int total = await query.CountAsync(cancellationToken);

        var rows = await query
            // Sắp xếp phải ỔN ĐỊNH, nếu không hai trang liên tiếp có thể trả về cùng một
            // người và bỏ sót một người khác. `Id` là chốt chặn cho những tên trùng nhau.
            .OrderBy(e => e.FullName)
            .ThenBy(e => e.Id)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(e => new ContactListItem(
                e.Id,
                e.Code,
                e.FullName,
                e.JobTitle,
                e.WorkEmail == null ? null : e.WorkEmail.Value,
                e.Phone,
                e.DepartmentId,
                context.Departments
                    .Where(d => d.Id == e.DepartmentId)
                    .Select(d => d.Name)
                    .FirstOrDefault(),
                e.IsActive))
            .ToListAsync(cancellationToken);

        return PagedList<ContactListItem>.Create(rows, criteria.Page, criteria.PageSize, total);
    }

    public async Task<IReadOnlyList<EmployeeAccountLink>> LinkedUserIdsAsync(
        CancellationToken cancellationToken)
        => await context.Employees
            .AsNoTracking()
            .Where(e => e.UserId != null)
            .Select(e => new EmployeeAccountLink(e.Id, e.UserId!.Value))
            .ToListAsync(cancellationToken);

    public Task<bool> CodeTakenAsync(string code, Guid? exceptId, CancellationToken cancellationToken)
        => context.Employees
            .AsNoTracking()
            .AnyAsync(e => e.Code == code && (exceptId == null || e.Id != exceptId), cancellationToken);
}
