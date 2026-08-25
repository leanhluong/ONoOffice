using Luong.Kernel.Pagination;
using ONoOffice.Org.Domain.Entities;

namespace ONoOffice.Org.Application.Abstractions;

/// <summary>Một dòng trên màn <b>Danh bạ</b>.</summary>
public sealed record ContactListItem(
    Guid Id,
    string Code,
    string FullName,
    string? JobTitle,
    string? WorkEmail,
    string? Phone,
    Guid? DepartmentId,
    string? DepartmentName,
    bool IsActive);

/// <summary>Điều kiện lọc của màn Danh bạ. Đã được handler làm sạch.</summary>
public sealed record ContactSearch(
    string? Search,
    Guid? DepartmentId,
    bool IncludeInactive,
    int Page,
    int PageSize);

public interface IEmployeeRepository
{
    void Add(Employee employee);

    Task<Employee?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedList<ContactListItem>> SearchAsync(
        ContactSearch criteria,
        CancellationToken cancellationToken);

    Task<bool> CodeTakenAsync(string code, Guid? exceptId, CancellationToken cancellationToken);
}
