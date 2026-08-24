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
    IReadOnlySet<string> Permissions);

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
}
