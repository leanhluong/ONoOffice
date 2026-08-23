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
public sealed record LoginUserData(
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
    /// <summary>Trả <c>null</c> khi không có tài khoản nào mang email này.</summary>
    Task<LoginUserData?> GetForLoginAsync(string email, CancellationToken cancellationToken = default);
}
