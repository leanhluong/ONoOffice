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

    /// <summary>
    /// Cặp <c>(hồ sơ, tài khoản)</c> của những hồ sơ ĐÃ nối tài khoản.
    ///
    /// Trả riêng thay vì nhét <c>UserId</c> vào <see cref="ContactListItem"/>: danh bạ
    /// không cần biết ai có tài khoản, và lộ khoá của module khác ra một DTO mà cả màn
    /// Danh bạ dùng chung là mở đường cho người sau ghép hai module ở chỗ không nên ghép.
    /// </summary>
    Task<IReadOnlyList<EmployeeAccountLink>> LinkedUserIdsAsync(CancellationToken cancellationToken);
}

/// <summary>Một hồ sơ nhân sự đã nối với một tài khoản đăng nhập.</summary>
public sealed record EmployeeAccountLink(Guid EmployeeId, Guid UserId);
