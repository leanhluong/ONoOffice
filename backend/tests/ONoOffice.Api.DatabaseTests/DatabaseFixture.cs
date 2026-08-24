using System.Security.Claims;
using Luong.Kernel.AspNetCore.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ONoOffice.Identity.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace ONoOffice.Api.DatabaseTests;

/// <summary>
/// Dựng một Postgres THẬT trong container, rồi khởi động ONoOffice trỏ vào đó.
///
/// <b>Vì sao bộ test này phải tồn tại tách khỏi <c>ONoOffice.Api.IntegrationTests</c>:</b>
/// bộ kia cố ý dừng lại trước lời gọi dữ liệu đầu tiên, nên nó chạy trong nửa giây và
/// không cần gì cả. Nhưng vì thế nó cũng <b>không chứng minh được một chữ nào</b> về
/// chuyện EF ánh xạ có đúng không, snake_case có ra đúng tên không, bộ lọc tenant có
/// chạy không, hay interceptor có nổ nhầm không. Những thứ đó chỉ Postgres mới trả lời được.
///
/// Hai bộ, hai mục đích, và cái giá khác nhau: bộ kia chạy ở mọi lần lưu file, bộ này
/// chạy khi cần biết sự thật.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    // Tài khoản mồi. Test đọc từ đây thay vì gõ lại chuỗi ở mỗi chỗ — gõ lại thì sửa
    // một chỗ quên chỗ kia, và test đỏ vì lý do chẳng liên quan gì tới thứ nó canh.
    public const string WorkspaceCode = "demo";
    public const string OwnerEmail = "chu@demo.vn";
    public const string OwnerPassword = "MatKhauDemo!2026";
    public const string OwnerFullName = "Chủ Workspace Demo";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        // Ghim đúng phiên bản đang dùng ở docker-compose. Test trên Postgres 17 mà chạy
        // thật trên 16 thì test đang canh một hệ khác với hệ sẽ chạy.
        .WithImage("postgres:16-alpine")
        .WithDatabase("onooffice_test")
        .Build();

    private WebApplicationFactory<Program>? _factory;

    public string ConnectionString => _container.GetConnectionString();

    public WebApplicationFactory<Program> Factory =>
        _factory ?? throw new InvalidOperationException("Fixture chưa khởi tạo xong.");

    public HttpClient CreateClient() => Factory.CreateClient();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Biến môi trường, không phải ConfigureAppConfiguration: Program ĐỌC cấu hình
        // ngay lúc đăng ký dịch vụ (để chết sớm nếu thiếu), sớm hơn thời điểm
        // WebApplicationFactory chen được vào. Xem chú thích dài ở ApiFactory bên
        // ONoOffice.Api.IntegrationTests.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("ConnectionStrings__IdentityDb", ConnectionString);

        Environment.SetEnvironmentVariable("Jwt__SecretKey", "khoa-ky-chi-dung-trong-test-va-du-32-ky-tu");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "onooffice-test");
        Environment.SetEnvironmentVariable("Jwt__Audience", "onooffice-test-client");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenLifetimeMinutes", "15");

        Environment.SetEnvironmentVariable("Cors__AllowedOrigins__0", "http://localhost:4200");

        // Bật gieo dữ liệu. Chính bước này chạy migration — nên nếu migration hỏng,
        // fixture chết ngay và MỌI test ở đây đỏ, chứ không có chuyện một nửa xanh giả.
        Environment.SetEnvironmentVariable("Seed__Enabled", "true");
        Environment.SetEnvironmentVariable("Seed__WorkspaceCode", WorkspaceCode);
        Environment.SetEnvironmentVariable("Seed__WorkspaceName", "Công ty Demo");
        Environment.SetEnvironmentVariable("Seed__OwnerEmail", OwnerEmail);
        Environment.SetEnvironmentVariable("Seed__OwnerPassword", OwnerPassword);
        Environment.SetEnvironmentVariable("Seed__OwnerFullName", OwnerFullName);

        _factory = new WebApplicationFactory<Program>();

        // Ép khởi động NGAY, ở đây, thay vì để nó xảy ra lười biếng bên trong test đầu
        // tiên. Không làm vậy thì lỗi migration hiện ra dưới tên của một test ngẫu nhiên
        // nào đó, và người đọc đi tìm lỗi ở nhầm chỗ.
        _ = _factory.CreateClient();
    }

    /// <summary>
    /// Lấy <c>DbContext</c> THẬT của ứng dụng, nhưng giả vờ đang ở trong một phiên đã
    /// đăng nhập vào workspace <paramref name="tenantId"/>.
    ///
    /// <b>Vì sao không tự dựng <c>DbContextOptions</c> cho nhanh</b> — đây là bài học
    /// phải trả giá một lần: bản tự dựng KHÔNG có bốn interceptor và KHÔNG có bảng lịch
    /// sử migration tuỳ chỉnh. Nghĩa là mọi test dùng nó đều đang soi một hệ thống
    /// <i>khác</i> với hệ thống sẽ chạy thật — và tệ nhất là chúng vẫn XANH, chỉ có điều
    /// xanh vì lớp bảo vệ mà chúng tưởng đang kiểm thì không có mặt.
    ///
    /// Cách dưới đây đi qua đúng đường DI của ứng dụng. Nó giả lập phiên bằng cách đặt
    /// một <c>HttpContext</c> mang claim <c>tenant_id</c> — cùng đường mà một request
    /// thật đi qua, vì <c>ICurrentTenant</c> chỉ đọc claim chứ không biết gì hơn.
    /// </summary>
    public (IServiceScope Scope, IdentityDbContext Context) CreateScope(Guid? tenantId = null)
    {
        var accessor = Factory.Services.GetRequiredService<IHttpContextAccessor>();

        accessor.HttpContext = tenantId is null
            ? null
            : new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(HttpContextCurrentUser.TenantClaimType, tenantId.Value.ToString())],
                    authenticationType: "test")),
            };

        var scope = Factory.Services.CreateScope();

        return (scope, scope.ServiceProvider.GetRequiredService<IdentityDbContext>());
    }

    public async Task DisposeAsync()
    {
        _factory?.Dispose();
        await _container.DisposeAsync();
    }
}

/// <summary>
/// Dùng chung một container cho mọi lớp test.
///
/// Dựng Postgres mất vài giây; mỗi lớp một container thì bộ test này thành thứ không ai
/// muốn chạy, và test không ai chạy thì cũng như không có.
/// </summary>
[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "database";
}
