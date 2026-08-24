using Luong.Kernel.Abstractions;
using Luong.Kernel.EntityFrameworkCore.Interceptors;
using Luong.Kernel.EntityFrameworkCore.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Infrastructure.Persistence;
using ONoOffice.Identity.Infrastructure.Persistence.Repositories;
using ONoOffice.Identity.Infrastructure.Security;

namespace ONoOffice.Identity.Infrastructure;

public static class IdentityModuleRegistration
{
    /// <summary>
    /// Nối toàn bộ module Identity vào DI. Gọi một lần từ <c>Program.cs</c>.
    ///
    /// Đây là chỗ DUY NHẤT biết rằng cổng <c>IPasswordHasher</c> được cài bằng Argon2id
    /// và <c>ITokenService</c> được cài bằng JWT. Đổi thuật toán hay đổi cách phát token
    /// thì sửa đúng file này, không đụng một dòng nghiệp vụ nào.
    /// </summary>
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options =>
            {
                options.Validate();
                return true;
            });

        string? connectionString = configuration.GetConnectionString("IdentityDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Chết ngay lúc khởi động. Để lọt thì app chạy được cho tới lần đầu ai đó
            // đăng nhập, rồi mới nổ — muộn hơn nhiều so với chỗ đáng nổ.
            throw new InvalidOperationException("Thiếu chuỗi kết nối 'IdentityDb'.");
        }

        services.AddDbContext<IdentityDbContext>((provider, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__migrations", IdentityDbContext.Schema));

            // Bốn interceptor của Luong.Kernel, mỗi cái một việc:
            options.AddInterceptors(
                // tự điền CreatedAtUtc / UpdatedAtUtc
                new AuditableEntityInterceptor(provider.GetRequiredService<IDateTimeProvider>()),

                // Remove() thành UPDATE is_deleted = true
                new SoftDeleteInterceptor(provider.GetRequiredService<IDateTimeProvider>()),

                // tự điền tenant_id khi thêm mới, và NỔ nếu ai đó ghi sang workspace khác
                new TenantInterceptor(provider.GetRequiredService<ICurrentTenant>()),

                // gom domain event thành hàng outbox, CÙNG transaction với dữ liệu nghiệp vụ
                new InsertOutboxMessagesInterceptor());
        });

        // DbContext chính là IUnitOfWork — TransactionBehavior gọi qua cổng này.
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<IdentityDbContext>());

        services
            .AddOptions<SeedOptions>()
            .Bind(configuration.GetSection(SeedOptions.SectionName));

        services.AddScoped<IdentityDataSeeder>();

        services.AddScoped<ITenantRepository, EfTenantRepository>();
        services.AddScoped<IRoleRepository, EfRoleRepository>();
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<IRefreshTokenRepository, EfRefreshTokenRepository>();

        // Băm mật khẩu không giữ trạng thái gì -> Singleton là đủ và rẻ nhất.
        // Không trạng thái, chỉ gọi RandomNumberGenerator — singleton là đủ và rẻ nhất.
        services.AddSingleton<ITemporaryPasswordGenerator, TemporaryPasswordGenerator>();

        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();

        // Phát token phụ thuộc IDateTimeProvider (Singleton) và IOptions -> Singleton được.
        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }
}
