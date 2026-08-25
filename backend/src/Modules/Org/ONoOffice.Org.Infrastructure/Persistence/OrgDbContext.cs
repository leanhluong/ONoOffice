using Luong.Kernel.Abstractions;
using Luong.Kernel.EntityFrameworkCore.Conventions;
using Luong.Kernel.EntityFrameworkCore.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using ONoOffice.Org.Domain.Entities;

namespace ONoOffice.Org.Infrastructure.Persistence;

/// <summary>
/// Cửa vào database của module Org.
///
/// <b>Schema RIÊNG (<c>org</c>), DbContext RIÊNG</b> — luật số 2 trong
/// <c>docs/02-kien-truc</c>. Đây chính là thứ khiến sau này muốn cắt Org ra thành dịch vụ
/// riêng chỉ cần đổi chuỗi kết nối. Lọt một câu <c>JOIN</c> sang schema <c>identity</c>
/// thì ngày cắt là ngày viết lại.
///
/// Hệ quả cụ thể của luật đó, và nó xuất hiện ngay ở màn đầu tiên: <c>Employee.UserId</c>
/// chỉ là một <c>Guid</c> trần, <b>không phải khoá ngoại</b>. Muốn biết tài khoản tương
/// ứng tên gì thì phải hỏi module Identity qua cổng của nó, không JOIN thẳng.
/// </summary>
public sealed class OrgDbContext(
    DbContextOptions<OrgDbContext> options,
    ICurrentTenant currentTenant) : DbContext(options), IUnitOfWork, ITenantAwareContext
{
    public const string Schema = "org";

    /// <summary>
    /// Bộ lọc toàn cục đọc giá trị này ở MỖI truy vấn.
    ///
    /// Chưa đăng nhập thì trả <see cref="Guid.Empty"/> — mọi truy vấn có lọc tenant không
    /// khớp hàng nào. Đó là hành vi ĐÚNG: chưa chứng minh được mình thuộc workspace nào
    /// thì không được thấy dữ liệu của workspace nào cả.
    ///
    /// Khác Identity ở một điểm: module này <b>không có luồng nào chạy trước khi có
    /// tenant</b>. Identity phải tra cứu lúc đăng nhập, nên nó có hai chỗ dùng
    /// <c>IgnoreQueryFilters</c>. Ở Org, thấy <c>IgnoreQueryFilters</c> ở bất kỳ đâu là
    /// một lỗi cần hỏi lại.
    /// </summary>
    public Guid CurrentTenantId => currentTenant.TenantId ?? Guid.Empty;

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrgDbContext).Assembly);

        /*
          KHÔNG có `AddOutbox()` / `AddInbox()` — khác Identity, và khác có chủ ý.

          Module này chưa phát một domain event nào. Dựng hai bảng mà không ai ghi vào,
          không ai đọc ra, và không có `InsertOutboxMessagesInterceptor` nào đứng sau thì
          đó là một lời hứa code không giữ: người đọc schema thấy `org.outbox_messages` và
          kết luận rằng module này đã nối vào đường sự kiện.

          Chuyện đó nguy hiểm hơn nó nghe: bên Identity, bảng outbox CÓ được ghi vào nhưng
          KHÔNG có `BackgroundService` nào rút ra — sự kiện nằm đó vĩnh viễn. Thêm một bộ
          bảng nữa cùng hình dạng chỉ làm cái bẫy đó khó thấy hơn.

          Ngày Org phát sự kiện đầu tiên thì thêm lại hai dòng này, thêm interceptor ở
          `OrgModuleRegistration`, VÀ nối bộ rút — cả ba trong cùng một thay đổi.
        */

        // Thứ tự không quan trọng vì EF 10 dùng bộ lọc CÓ TÊN, chúng sống song song.
        modelBuilder.ApplyTenantQueryFilter(this);
        modelBuilder.ApplySoftDeleteQueryFilter();

        // Gọi CUỐI CÙNG: nó đổi tên những gì đang có, nên thứ khai báo sau nó sẽ bị bỏ sót.
        modelBuilder.UseSnakeCaseNames();

        base.OnModelCreating(modelBuilder);
    }
}
