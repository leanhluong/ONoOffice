using Luong.Kernel.Abstractions;
using Luong.Kernel.EntityFrameworkCore.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ONoOffice.Org.Application.Abstractions;
using ONoOffice.Org.Infrastructure.Persistence;
using ONoOffice.Org.Infrastructure.Persistence.Repositories;

namespace ONoOffice.Org.Infrastructure;

public static class OrgModuleRegistration
{
    /// <summary>
    /// Nối toàn bộ module Org vào DI. Gọi một lần từ <c>Program.cs</c>.
    /// </summary>
    public static IServiceCollection AddOrgModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        /*
          Dùng CHUNG chuỗi kết nối với Identity nếu không khai riêng.

          Hai module, hai schema, một database — đó là hình dạng của modular monolith. Khai
          riêng `OrgDb` thì tách được sang database khác mà không sửa một dòng code nào,
          nên chỗ này để mở; nhưng bắt buộc phải khai cả hai ngay từ đầu là bắt người ta
          chép cùng một chuỗi hai lần rồi sớm muộn sửa mỗi một chỗ.
        */
        string? connectionString =
            configuration.GetConnectionString("OrgDb")
            ?? configuration.GetConnectionString("IdentityDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Chết ngay lúc khởi động. Để lọt thì app chạy được cho tới lần đầu ai đó mở
            // danh bạ, rồi mới nổ — muộn hơn nhiều so với chỗ đáng nổ.
            throw new InvalidOperationException("Thiếu chuỗi kết nối 'OrgDb' (hoặc 'IdentityDb').");
        }

        services.AddDbContext<OrgDbContext>((provider, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__migrations", OrgDbContext.Schema));

            // Ba interceptor. KHÔNG có `InsertOutboxMessagesInterceptor` như Identity —
            // module này chưa phát domain event nào, và đăng ký một bộ gom sự kiện cho
            // thứ không có sự kiện chỉ tạo ấn tượng sai rằng outbox đang chạy ở đây.
            options.AddInterceptors(
                new AuditableEntityInterceptor(provider.GetRequiredService<IDateTimeProvider>()),
                new SoftDeleteInterceptor(provider.GetRequiredService<IDateTimeProvider>()),
                new TenantInterceptor(provider.GetRequiredService<ICurrentTenant>()));
        });

        /*
          ⚠️ KHÔNG đăng ký `IUnitOfWork` ở đây.

          Identity đã đăng ký `IUnitOfWork` trỏ vào `IdentityDbContext`. Đăng ký thêm một
          cái nữa thì cái sau THẮNG, và mọi lệnh của Identity sẽ gọi `SaveChanges` trên
          `OrgDbContext` — tức là không lưu gì cả, im lặng, vì context đó không theo dõi
          thực thể nào của Identity.

          `TransactionBehavior` của kernel giải quyết bằng cách nào thì xem `Program.cs`:
          hai module dùng hai context, nên lệnh của Org phải lưu qua chính context của nó.
          Ở lát này mọi lệnh Org đều gọn trong một aggregate nên chưa cần transaction chung.
        */

        services.AddScoped<IDepartmentRepository, EfDepartmentRepository>();
        services.AddScoped<IEmployeeRepository, EfEmployeeRepository>();

        return services;
    }
}
