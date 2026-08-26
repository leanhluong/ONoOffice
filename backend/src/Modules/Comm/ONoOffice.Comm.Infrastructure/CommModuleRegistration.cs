using Luong.Kernel.Abstractions;
using Luong.Kernel.EntityFrameworkCore.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ONoOffice.Comm.Application.Abstractions;
using ONoOffice.Comm.Infrastructure.Persistence;
using ONoOffice.Comm.Infrastructure.Persistence.Repositories;

namespace ONoOffice.Comm.Infrastructure;

public static class CommModuleRegistration
{
    /// <summary>Nối toàn bộ module Trao đổi vào DI. Gọi một lần từ <c>Program.cs</c>.</summary>
    public static IServiceCollection AddCommModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Dùng chung chuỗi kết nối nếu không khai riêng — ba module, ba schema, một
        // database. Khai riêng `CommDb` thì tách được sang database khác mà không sửa một
        // dòng code, và module này là module có nhiều khả năng phải tách nhất.
        string? connectionString =
            configuration.GetConnectionString("CommDb")
            ?? configuration.GetConnectionString("IdentityDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Thiếu chuỗi kết nối 'CommDb' (hoặc 'IdentityDb').");
        }

        services.AddDbContext<CommDbContext>((provider, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__migrations", CommDbContext.Schema));

            options.AddInterceptors(
                new AuditableEntityInterceptor(provider.GetRequiredService<IDateTimeProvider>()),
                new SoftDeleteInterceptor(provider.GetRequiredService<IDateTimeProvider>()),
                new TenantInterceptor(provider.GetRequiredService<ICurrentTenant>()));
        });

        // ⚠️ KHÔNG đăng ký `IUnitOfWork` ở đây — Identity đã đăng ký, và cái sau THẮNG.
        // Xem chú thích dài ở `OrgModuleRegistration` và cách `Program.cs` giải quyết.

        services.AddScoped<IConversationRepository, EfConversationRepository>();
        services.AddScoped<IMessageRepository, EfMessageRepository>();

        return services;
    }
}
