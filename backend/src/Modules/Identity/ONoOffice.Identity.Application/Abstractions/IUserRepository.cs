using Luong.Kernel.Pagination;
using Luong.Kernel.Primitives;
using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.Application.Abstractions;

/// <summary>
/// Dữ liệu vừa đủ để xử lý một lần đăng nhập.
///
/// CỐ Ý không trả về thực thể <c>User</c>. Đăng nhập là đường ĐỌC — nó không sửa gì trên
/// tài khoản, nên nạp cả gốc tổng hợp là thừa. Bản chiếu này gom mọi thứ cần thiết vào
/// MỘT truy vấn, thay vì ba lượt hỏi database (tài khoản → workspace → quyền).
///
/// Nó cũng là ví dụ cho luật đã bàn: repository trả về thứ USE CASE cần, không phải thứ
/// bảng dữ liệu có.
/// </summary>
public sealed record AuthUserData(
    Guid UserId,
    Guid TenantId,
    string PasswordHash,
    string Email,
    string FullName,
    bool IsUserActive,
    bool IsTenantActive,
    bool MustChangePassword,
    IReadOnlySet<string> Permissions);

/// <summary>Lọc theo trạng thái ở màn Nhân sự.</summary>
public enum UserStatusFilter
{
    Any = 0,

    Active = 1,

    /// <summary>Đã tạo tài khoản nhưng chưa từng đăng nhập — vẫn còn mật khẩu tạm.</summary>
    PendingFirstLogin = 2,

    Disabled = 3,
}

/// <summary>Điều kiện lọc, đã được handler làm sạch. Repository tin những con số này.</summary>
public sealed record UserSearch(
    string? Search,
    UserStatusFilter Status,
    Guid? RoleId,
    int Page,
    int PageSize);

/// <summary>
/// Một dòng trên bảng Nhân sự.
///
/// <c>RoleName</c> chứ không phải <c>RoleId</c>: màn hình hiện tên, và bắt nó đi tra tên
/// cho từng dòng là kiểu truy vấn N+1 kinh điển.
/// </summary>
public sealed record UserListItem(
    Guid Id,
    string Email,
    string FullName,
    bool IsActive,
    bool MustChangePassword,
    string RoleName,
    DateTimeOffset CreatedAtUtc);

public interface IUserRepository
{
    /// <summary>Chỉ ghi vào bộ theo dõi thay đổi — xem <see cref="ITenantRepository.Add"/>.</summary>
    void Add(User user);

    /// <summary>
    /// Email unique TOÀN hệ thống, không phải unique trong một workspace (ADR-0002).
    ///
    /// Cùng cảnh báo với <see cref="ITenantRepository.IsCodeTakenAsync"/>: đây là phép
    /// kiểm để BÁO LỖI CHO ĐẸP, còn lớp chặn thật là chỉ mục UNIQUE ở database.
    /// </summary>
    Task<bool> IsEmailTakenAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Trả <c>null</c> khi không có tài khoản nào mang email này.</summary>
    Task<AuthUserData?> GetForLoginAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Dùng khi gia hạn phiên — lúc đó đã biết là ai, chỉ cần nạp lại quyền và trạng thái.</summary>
    Task<AuthUserData?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Danh sách nhân sự đã lọc và phân trang.
    ///
    /// Trả bản chiếu chứ không trả thực thể <c>User</c>: đây là đường ĐỌC thuần, không ai
    /// sửa gì. Nạp cả gốc tổng hợp cho 20 dòng là 20 lần theo dõi thay đổi vô ích.
    /// </summary>
    Task<PagedList<UserListItem>> SearchAsync(UserSearch criteria, CancellationToken cancellationToken = default);
}
