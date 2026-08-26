using Luong.Kernel.Abstractions;
using Luong.Kernel.Pagination;
using ONoOffice.Identity.Contracts;
using ONoOffice.Org.Application.Abstractions;
using ONoOffice.Org.Domain.Entities;

namespace ONoOffice.Org.UnitTests.Fakes;

/// <summary>
/// Bản giả của các cổng, gom về một chỗ.
///
/// Cùng lý do với <c>tests/Fakes/RepositoryFakes.cs</c> bên Identity: nếu mỗi test tự dựng
/// một lớp giả riêng thì thêm một phương thức vào cổng là hàng loạt file test đỏ vì thiếu
/// thành viên, dù phần lớn chẳng dùng tới nó. Đó không phải test bắt lỗi, đó là việc tay
/// chân — và đủ nhàm để người ta bắt đầu dán bừa cho hết đỏ.
///
/// Mọi thành viên đều <c>virtual</c>, mặc định trả về "không có gì". Test chỉ ghi đè đúng
/// thứ nó cần, nên <b>những gì được ghi đè chính là những gì handler đó dùng tới</b>.
/// </summary>
internal class FakeDepartmentRepository : IDepartmentRepository
{
    public List<Department> Added { get; } = [];

    public List<Department> Removed { get; } = [];

    public virtual void Add(Department department) => Added.Add(department);

    public virtual void Remove(Department department) => Removed.Add(department);

    public virtual Task<Department?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult<Department?>(null);

    public virtual Task<IReadOnlyList<DepartmentNode>> GetTreeAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DepartmentNode>>([]);

    public virtual Task<IReadOnlyList<Guid>> GetAncestorIdsAsync(
        Guid id,
        CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Guid>>([]);

    public virtual Task<bool> NameTakenAsync(
        string name,
        Guid? exceptId,
        CancellationToken cancellationToken) => Task.FromResult(false);

    public virtual Task<bool> HasChildrenAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public virtual Task<bool> HasEmployeesAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(false);
}

internal class FakeEmployeeRepository : IEmployeeRepository
{
    public List<Employee> Added { get; } = [];

    public virtual void Add(Employee employee) => Added.Add(employee);

    public virtual Task<Employee?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult<Employee?>(null);

    public virtual Task<PagedList<ContactListItem>> SearchAsync(
        ContactSearch criteria,
        CancellationToken cancellationToken) =>
        Task.FromResult(PagedList<ContactListItem>.Create([], criteria.Page, criteria.PageSize, 0));

    public virtual Task<bool> CodeTakenAsync(
        string code,
        Guid? exceptId,
        CancellationToken cancellationToken) => Task.FromResult(false);

    public virtual Task<bool> UserLinkedAsync(
        Guid userId,
        Guid? exceptId,
        CancellationToken cancellationToken) => Task.FromResult(false);

    public virtual Task<IReadOnlyList<EmployeeAccountLink>> LinkedUserIdsAsync(
        CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<EmployeeAccountLink>>([]);
}

/// <summary>Danh bạ tài khoản của module Identity — cổng liên module, nhìn từ phía Org.</summary>
internal class FakeUserDirectory(params UserSummary[] users) : IUserDirectory
{
    public virtual Task<IReadOnlyList<UserSummary>> GetAllAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<UserSummary>>(users);

    public virtual Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken)
        => Task.FromResult(users.Any(u => u.Id == userId));
}

/// <summary>Phiên đang đứng trong một workspace cố định.</summary>
internal sealed class FakeCurrentTenant(Guid? tenantId) : ICurrentTenant
{
    public Guid? TenantId { get; } = tenantId;
}
