using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.Application.Abstractions;

public interface IRefreshTokenRepository
{
    /// <summary>
    /// Chỉ ghi vào bộ theo dõi thay đổi. Việc chốt xuống database là của
    /// <c>TransactionBehavior</c> — handler không tự gọi <c>SaveChanges</c>.
    /// </summary>
    void Add(RefreshToken token);
}
