using Luong.Kernel.Abstractions;
using Luong.Kernel.EntityFrameworkCore.Conventions;
using Luong.Kernel.EntityFrameworkCore.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using ONoOffice.Comm.Domain.Entities;

namespace ONoOffice.Comm.Infrastructure.Persistence;

/// <summary>
/// Cửa vào database của module Trao đổi.
///
/// <b>Schema riêng (<c>comm</c>), DbContext riêng</b> — luật số 2 trong
/// <c>docs/02-kien-truc</c>, và ở module này nó là luật đáng giá nhất của cả ba: bảng tin
/// nhắn sẽ là bảng LỚN NHẤT hệ thống, lớn hơn mọi bảng khác cộng lại. Ngày phải cắt nó ra
/// máy riêng — vì sao lưu, vì phân vùng theo thời gian, vì bất cứ lý do gì — là ngày biết
/// mình đã giữ luật hay chưa. Một câu <c>JOIN</c> sang <c>identity</c> để lấy tên người
/// gửi là đủ để ngày đó thành ngày viết lại.
///
/// Cái giá phải trả nhìn thấy được ngay: <c>SoDanhBa</c> ở tầng Application phải hỏi
/// Identity một lượt cho mỗi request để đổi mã người thành tên. Đó là cái giá đúng.
/// </summary>
public sealed class CommDbContext(
    DbContextOptions<CommDbContext> options,
    ICurrentTenant currentTenant) : DbContext(options), IUnitOfWork, ITenantAwareContext
{
    public const string Schema = "comm";

    /// <summary>
    /// Chưa đăng nhập thì trả <see cref="Guid.Empty"/> — mọi truy vấn có lọc tenant không
    /// khớp hàng nào. Module này không có luồng nào chạy trước khi có tenant, nên
    /// <c>IgnoreQueryFilters</c> xuất hiện ở bất kỳ đâu trong đây là một lỗi cần hỏi lại.
    /// </summary>
    public Guid CurrentTenantId => currentTenant.TenantId ?? Guid.Empty;

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommDbContext).Assembly);

        // KHÔNG có `AddOutbox()`/`AddInbox()` — cùng lý do với Org: module này chưa phát
        // domain event nào, và dựng hai bảng không ai ghi vào là một lời hứa code không
        // giữ. Ngày Comm phát sự kiện đầu tiên (nhiều khả năng là "có tin mới" cho
        // realtime) thì thêm bảng, thêm interceptor VÀ nối bộ rút — cả ba cùng lúc.

        modelBuilder.ApplyTenantQueryFilter(this);
        modelBuilder.ApplySoftDeleteQueryFilter();

        // Gọi CUỐI CÙNG: nó đổi tên những gì đang có, nên thứ khai sau nó sẽ bị bỏ sót.
        modelBuilder.UseSnakeCaseNames();

        base.OnModelCreating(modelBuilder);
    }
}
