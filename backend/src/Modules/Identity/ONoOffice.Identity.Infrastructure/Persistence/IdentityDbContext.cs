using Luong.Kernel.Abstractions;
using Luong.Kernel.EntityFrameworkCore.Conventions;
using Luong.Kernel.EntityFrameworkCore.Inbox;
using Luong.Kernel.EntityFrameworkCore.MultiTenancy;
using Luong.Kernel.EntityFrameworkCore.Outbox;
using Microsoft.EntityFrameworkCore;
using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.Infrastructure.Persistence;

/// <summary>
/// Cửa vào database của module Identity.
///
/// <b>Mỗi module một DbContext, mỗi module một schema Postgres riêng.</b> Đây là luật số 2
/// trong <c>docs/02-kien-truc</c>, và nó chính là thứ khiến sau này muốn cắt module ra
/// thành dịch vụ riêng chỉ cần đổi chuỗi kết nối. Lọt một câu <c>JOIN</c> xuyên schema
/// thì ngày cắt là ngày viết lại.
///
/// Nó cài <see cref="IUnitOfWork"/> chỉ bằng cách... đã có sẵn <c>SaveChangesAsync</c>.
/// <c>DbContext</c> vốn ĐÃ là một unit of work; ta chỉ đặt cho nó một cái tên mà tầng
/// Application gọi được mà không phải tham chiếu EF.
/// </summary>
public sealed class IdentityDbContext(
    DbContextOptions<IdentityDbContext> options,
    ICurrentTenant currentTenant) : DbContext(options), IUnitOfWork, ITenantAwareContext
{
    public const string Schema = "identity";

    /// <summary>
    /// Bộ lọc toàn cục đọc giá trị này ở MỖI truy vấn.
    ///
    /// Chưa đăng nhập thì trả <see cref="Guid.Empty"/> — nghĩa là mọi truy vấn có lọc
    /// tenant sẽ không khớp hàng nào. Đó là hành vi ĐÚNG: chưa chứng minh được mình
    /// thuộc workspace nào thì không được thấy dữ liệu của workspace nào cả.
    ///
    /// Riêng luồng xác thực (đăng nhập, gia hạn phiên) phải tra cứu TRƯỚC khi có tenant,
    /// nên nó gọi <c>IgnoreQueryFilters()</c> một cách rõ ràng — xem <c>EfUserRepository</c>.
    /// </summary>
    public Guid CurrentTenantId => currentTenant.TenantId ?? Guid.Empty;

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);

        // Hai bảng hạ tầng của Luong.Kernel. Chúng nằm trong CÙNG database với dữ liệu
        // nghiệp vụ — đó là toàn bộ lý do outbox hoạt động: sự kiện và dữ liệu cùng đi
        // xuống trong một transaction.
        modelBuilder.AddOutbox();
        modelBuilder.AddInbox();

        // Thứ tự không quan trọng vì EF 10 dùng bộ lọc CÓ TÊN, chúng sống song song.
        modelBuilder.ApplyTenantQueryFilter(this);
        modelBuilder.ApplySoftDeleteQueryFilter();

        // Gọi CUỐI CÙNG: nó đổi tên những gì đang có, nên thứ khai báo sau nó sẽ bị bỏ sót.
        modelBuilder.UseSnakeCaseNames();

        base.OnModelCreating(modelBuilder);
    }
}
