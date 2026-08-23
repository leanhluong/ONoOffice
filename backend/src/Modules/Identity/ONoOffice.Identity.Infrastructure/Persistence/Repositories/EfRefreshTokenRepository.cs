using Microsoft.EntityFrameworkCore;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.Infrastructure.Persistence.Repositories;

internal sealed class EfRefreshTokenRepository(IdentityDbContext context) : IRefreshTokenRepository
{
    /// <summary>
    /// Chỉ ghi vào bộ theo dõi thay đổi, KHÔNG gọi <c>SaveChanges</c>.
    ///
    /// Chốt xuống database là việc của <c>TransactionBehavior</c> — nhờ vậy nếu handler
    /// trả về thất bại ở bước sau, vé này bị bỏ đi cùng mọi thay đổi khác.
    /// </summary>
    public void Add(RefreshToken token) => context.RefreshTokens.Add(token);

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        // ⚠️ IgnoreQueryFilters — NGOẠI LỆ CÓ CHỦ ĐÍCH thứ hai (và cuối cùng).
        //
        // Request gia hạn phiên không mang access token còn hạn — đó chính là lý do nó
        // phải gia hạn. Nên phiên chưa có tenant, và bộ lọc sẽ không khớp hàng nào.
        // Bù lại: tenant được lấy TỪ CHÍNH bản ghi token tìm thấy, không phải từ client.
        context.RefreshTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task<int> RevokeAllForUserAsync(
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        // ExecuteUpdate: một câu UPDATE thẳng xuống database, không nạp thực thể lên bộ nhớ.
        //
        // Đây là lúc cần nó thật: một người có thể có hàng chục vé còn sống trên nhiều
        // thiết bị, và đây là đường xử lý sự cố — càng nhanh càng tốt. Nạp từng cái lên
        // rồi sửa từng cái là chậm hơn nhiều mà không được gì.
        return await context.RefreshTokens
            .IgnoreQueryFilters()
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.RevokedAtUtc, now),
                cancellationToken);
    }
}
