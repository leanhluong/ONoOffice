using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ONoOffice.Api.IntegrationTests.Probe;
using ONoOffice.Identity.Application.Abstractions;

namespace ONoOffice.Api.IntegrationTests;

/// <summary>
/// Dựng một máy chủ ONoOffice thật trong bộ nhớ: <b>đúng</b> <c>Program.cs</c>, đúng thứ
/// tự middleware, đúng cấu hình xác thực. Không có gì bị thay bằng đồ giả trừ hai chỗ
/// nói bên dưới.
///
/// <b>Vì sao không cần Postgres:</b> <c>AddDbContext</c> chỉ ghi nhận chuỗi kết nối chứ
/// không mở kết nối nào lúc khởi động — kết nối chỉ mở ở truy vấn đầu tiên. Nên mọi thứ
/// nằm TRƯỚC lời gọi database (định tuyến, CORS, xác thực, phân quyền, kiểm dữ liệu,
/// dựng Problem Details, bản dịch) đều test được mà không cần một database nào.
///
/// Ranh giới đó cũng là ranh giới của bộ test này: chỗ nào cần chạm dữ liệu thật thì
/// KHÔNG test ở đây — nó thuộc về test có Postgres thật, làm sau.
/// </summary>
internal sealed class ApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Khoá ký dùng cho test. Phải ≥ 32 ký tự, nếu không <c>JwtOptions</c> chặn ngay.</summary>
    public const string SecretKey = "khoa-ky-chi-dung-trong-test-va-du-32-ky-tu";

    public const string Issuer = "onooffice-test";
    public const string Audience = "onooffice-test-client";

    /// <summary>Origin nằm trong danh sách cho phép.</summary>
    public const string OriginDuocPhep = "http://localhost:4200";

    /// <summary>Origin KHÔNG nằm trong danh sách — dùng để kiểm CORS thật sự có lọc.</summary>
    public const string OriginNguoiLa = "https://ke-xau.example.com";

    /// <summary>
    /// Đưa cấu hình test vào bằng BIẾN MÔI TRƯỜNG, không phải bằng
    /// <c>ConfigureAppConfiguration</c>.
    ///
    /// <b>Vì sao phải vậy — và đây là chỗ dễ mất một buổi để hiểu ra:</b> với mô hình
    /// khởi động tối giản, <c>WebApplicationFactory</c> chỉ chen được vào cấu hình ở
    /// thời điểm <c>builder.Build()</c>. Nhưng <c>Program.cs</c> ĐỌC cấu hình sớm hơn
    /// thế — ngay lúc đăng ký dịch vụ, để kiểm tra và chết sớm nếu thiếu. Nghĩa là mọi
    /// giá trị đưa qua <c>ConfigureAppConfiguration</c> đều tới muộn, và test sẽ đỏ với
    /// đúng thông báo "thiếu cấu hình" mà chính nó vừa cung cấp.
    ///
    /// Biến môi trường thì được <c>WebApplication.CreateBuilder</c> nạp ngay từ đầu.
    /// Tiện thể, đây cũng chính là đường mà máy chủ thật nhận bí mật — nên test đang
    /// đi đúng con đường của môi trường thật, không phải một lối tắt riêng.
    ///
    /// Dấu <c>__</c> thay cho dấu <c>:</c> trong tên khoá; số ở cuối là chỉ số mảng.
    /// </summary>
    static ApiFactory()
    {
        // Không có appsettings.Development.json xen vào; test tự cấp đủ mọi thứ.
        Dat("ASPNETCORE_ENVIRONMENT", "Testing");

        // Chuỗi kết nối phải CÓ và hợp lệ về cú pháp, vì Program dừng ngay lúc khởi động
        // nếu thiếu. Nó không cần trỏ tới database nào có thật — cổng 1 để nếu có test
        // nào lỡ chạm database thì hỏng NGAY chứ không treo chờ hết thời gian.
        Dat("ConnectionStrings__IdentityDb", "Host=127.0.0.1;Port=1;Database=khong_ton_tai;Username=t;Password=t");

        Dat("Jwt__SecretKey", SecretKey);
        Dat("Jwt__Issuer", Issuer);
        Dat("Jwt__Audience", Audience);
        Dat("Jwt__AccessTokenLifetimeMinutes", "15");

        Dat("Cors__AllowedOrigins__0", OriginDuocPhep);
    }

    private static void Dat(string ten, string giaTri) => Environment.SetEnvironmentVariable(ten, giaTri);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Nạp ProbeController từ assembly test vào cùng một máy chủ.
            // ApplicationPartManager là singleton dùng chung, nên lời gọi này bổ sung
            // vào danh sách controller mà Program đã dựng, không thay thế nó.
            services.AddControllers().AddApplicationPart(typeof(ProbeController).Assembly);
        });
    }

    /// <summary>
    /// Phát một access token bằng CHÍNH <c>ITokenService</c> mà sản phẩm dùng.
    ///
    /// Cố ý không tự dựng JWT bằng tay ở đây: dựng tay thì test chỉ chứng minh
    /// "bên xác minh đọc được thứ do test tạo ra". Lấy đúng bên phát của sản phẩm thì
    /// nó chứng minh được điều đáng giá hơn nhiều — <b>bên phát và bên xác minh khớp
    /// nhau</b>. Lệch issuer, lệch audience, lệch tên claim quyền đều lộ ra ngay.
    /// </summary>
    public string PhatToken(params string[] quyen)
    {
        var tokenService = Services.GetRequiredService<ITokenService>();

        var token = tokenService.IssueAccessToken(
            Guid.NewGuid(),
            Guid.NewGuid(),
            quyen.ToHashSet(StringComparer.OrdinalIgnoreCase));

        return token.Value;
    }

    /// <summary>
    /// Một token trông y hệt token thật nhưng ký bằng khoá khác — dùng để chứng minh
    /// chữ ký THẬT SỰ được kiểm, chứ không phải chỉ đọc phần thân rồi tin.
    /// </summary>
    public static string PhatTokenKySaiKhoa()
    {
        var handler = new JsonWebTokenHandler();

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Expires = DateTime.UtcNow.AddMinutes(15),
            Claims = new Dictionary<string, object> { ["sub"] = Guid.NewGuid().ToString() },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes("khoa-hoan-toan-khac-nhung-van-du-32-ky-tu")),
                SecurityAlgorithms.HmacSha256),
        };

        return handler.CreateToken(descriptor);
    }

    /// <summary>Gắn sẵn token vào mọi request của client trả về.</summary>
    public HttpClient TaoClientDaDangNhap(params string[] quyen)
    {
        var client = CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PhatToken(quyen));

        return client;
    }
}
