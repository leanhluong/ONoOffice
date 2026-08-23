using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.Infrastructure.Persistence.Repositories;

internal sealed class EfRefreshTokenRepository(IdentityDbContext context) : IRefreshTokenRepository
{
    /// <summary>
    /// Chỉ ghi vào bộ theo dõi thay đổi, KHÔNG gọi <c>SaveChanges</c>.
    ///
    /// Chốt xuống database là việc của <c>TransactionBehavior</c> — nhờ vậy nếu handler
    /// trả về thất bại ở bước sau, vé này bị bỏ đi cùng mọi thay đổi khác, thay vì đã
    /// nằm lại trong bảng.
    /// </summary>
    public void Add(RefreshToken token) => context.RefreshTokens.Add(token);
}
