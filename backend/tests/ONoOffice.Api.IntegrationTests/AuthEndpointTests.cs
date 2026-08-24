using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ONoOffice.Api.IntegrationTests;

/// <summary>
/// Ba endpoint đăng nhập, kiểm ở mức <b>không chạm database</b>.
///
/// Mọi test ở đây cố ý dừng lại trước lời gọi dữ liệu đầu tiên. Nhờ vậy chúng chạy được
/// mà không cần Postgres — và cũng chính vì vậy, một phản hồi <c>500</c> ở đây là bằng
/// chứng rõ ràng rằng request đã đi xa hơn nó được phép: <c>ValidationBehavior</c> đã
/// không chặn kịp, và handler đã chạm tới database với dữ liệu rác.
/// </summary>
public sealed class AuthEndpointTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    [Fact]
    public async Task Login_ThieuCaEmailLanMatKhau_Tra400_ChuKhongPhai500()
    {
        var response = await _factory.CreateClient()
            .PostAsync("/api/auth/login", Json("""{"email":"","password":""}"""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ThieuCaHaiO_BaoDuCAHAI_KhongDungOSaiDauTien()
    {
        var response = await _factory.CreateClient()
            .PostAsync("/api/auth/login", Json("""{"email":"","password":""}"""));

        var errors = await DocErrors(response);

        // Dừng ở ô sai đầu tiên nghĩa là người dùng sửa xong ô này mới lộ ra ô sau,
        // phải gửi lại nhiều lần. ValidationBehavior cố ý chạy hết mọi luật rồi mới gom.
        Assert.Equal(2, errors.GetArrayLength());
    }

    /// <summary>
    /// Đăng nhập phải <c>[AllowAnonymous]</c> — nghe hiển nhiên, nhưng đây đúng là loại
    /// lỗi sinh ra khi ai đó thêm <c>[Authorize]</c> ở cấp controller cho "an toàn".
    /// Lúc đó không ai đăng nhập được nữa: muốn có token thì phải có token.
    /// </summary>
    [Fact]
    public async Task Login_KhongCanToken()
    {
        var response = await _factory.CreateClient()
            .PostAsync("/api/auth/login", Json("""{"email":"","password":""}"""));

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// ⭐ JSON hỏng phải trả về <b>cùng một hình dạng lỗi</b> với mọi lỗi khác.
    ///
    /// Mặc định của <c>[ApiController]</c> là tự trả một khuôn <c>ValidationProblemDetails</c>
    /// riêng, có trường <c>errors</c> là một TỪ ĐIỂN theo tên trường — khác hẳn mảng
    /// <c>errors[]</c> mà mọi lỗi khác dùng. Để nguyên thì frontend phải viết hai nhánh
    /// xử lý lỗi, và nhánh thứ hai chỉ lộ ra khi có người gửi JSON hỏng.
    /// </summary>
    [Fact]
    public async Task Login_JsonHong_VanTraDungHinhDangLoiChung()
    {
        var response = await _factory.CreateClient()
            .PostAsync("/api/auth/login", Json("""{"email": """));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var errors = await DocErrors(response);

        Assert.True(errors.ValueKind == JsonValueKind.Array, "errors phải là MẢNG, giống mọi lỗi khác.");
        Assert.True(errors.GetArrayLength() > 0);
    }

    [Theory]
    [InlineData("/api/auth/login")]
    [InlineData("/api/auth/refresh")]
    [InlineData("/api/auth/logout")]
    public async Task BaEndpointDangNhap_DeuTonTai_VaDeuChoPhepAnDanh(string duongDan)
    {
        // Đọc thẳng bảng định tuyến thay vì gọi thật: gọi thật thì refresh và logout sẽ
        // chạm database (chúng không có validator), mà bộ test này cố ý không có database.
        var endpoints = _factory.Services.GetRequiredService<EndpointDataSource>().Endpoints;

        var endpoint = endpoints
            .OfType<RouteEndpoint>()
            .SingleOrDefault(e => "/" + e.RoutePattern.RawText?.TrimStart('/') == duongDan);

        Assert.NotNull(endpoint);

        Assert.NotNull(endpoint.Metadata.GetMetadata<IAllowAnonymous>());
    }

    private static async Task<JsonElement> DocErrors(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Clone vì JsonDocument bị giải phóng khi ra khỏi using.
        return document.RootElement.GetProperty("errors").Clone();
    }
}
