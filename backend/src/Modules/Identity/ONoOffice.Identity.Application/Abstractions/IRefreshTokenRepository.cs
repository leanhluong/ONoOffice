using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.Application.Abstractions;

public interface IRefreshTokenRepository
{
    /// <summary>
    /// Chỉ ghi vào bộ theo dõi thay đổi. Việc chốt xuống database là của
    /// <c>TransactionBehavior</c> — handler không tự gọi <c>SaveChanges</c>.
    /// </summary>
    void Add(RefreshToken token);

    /// <summary>Tra theo chuỗi BĂM, vì server không bao giờ giữ chuỗi thô.</summary>
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Thu hồi MỌI vé còn sống của một người.
    ///
    /// Gọi khi phát hiện một refresh token bị dùng lại — dấu hiệu có hai bên cùng giữ
    /// nó. Lúc đó không tin bên nào cả: huỷ sạch, bắt đăng nhập lại bằng mật khẩu.
    /// </summary>
    Task<int> RevokeAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default);
}
