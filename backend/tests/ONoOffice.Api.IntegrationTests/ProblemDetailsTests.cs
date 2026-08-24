using System.Net;
using System.Text.Json;

namespace ONoOffice.Api.IntegrationTests;

/// <summary>
/// Canh hình dạng phản hồi lỗi và bản dịch của nó.
///
/// Hai thứ này là <b>hợp đồng</b> với frontend: frontend rẽ nhánh theo <c>errors[].code</c>
/// và hiển thị thẳng <c>errors[].description</c>. Đổi hình dạng là làm hỏng frontend, mà
/// compiler không nói một lời nào.
/// </summary>
public sealed class ProblemDetailsTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task LoiNghiepVu_TraDungMaHttp_TheoLoaiLoi()
    {
        var response = await _factory.CreateClient().GetAsync("/probe/that-bai-nghiep-vu");

        // Auth.InvalidCredentials là ErrorType.Unauthorized → 401. Endpoint này AllowAnonymous,
        // nên 401 ở đây đến từ chính kết quả nghiệp vụ, không phải từ tầng xác thực.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LoiNghiepVu_TraContentTypeChuanRfc7807()
    {
        var response = await _factory.CreateClient().GetAsync("/probe/that-bai-nghiep-vu");

        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task LoiNghiepVu_ThanLuonCoDanhSachErrors_KeCaKhiChiCoMotLoi()
    {
        var (code, _) = await LoiDauTien(languages: null);

        Assert.Equal("Auth.InvalidCredentials", code);
    }

    /// <summary>
    /// ⭐ Bản dịch phải thật sự được gọi.
    ///
    /// Đây là chỗ dễ tưởng là xong nhất: 41 khoá <c>.resx</c> nằm sẵn, có test đối chiếu
    /// canh đủ khoá, nhìn vào thấy "i18n đã làm rồi". Nhưng nếu không có ai GỌI
    /// <c>Localize</c> thì cả bộ đó nằm im, và người dùng luôn nhận câu tiếng Việt viết
    /// cứng trong <c>IdentityErrors.cs</c> — kể cả khi họ chọn tiếng Anh.
    /// </summary>
    [Fact]
    public async Task AcceptLanguage_en_TraCauTiengAnh()
    {
        var (_, description) = await LoiDauTien("en");

        Assert.Equal("Incorrect email or password.", description);
    }

    [Fact]
    public async Task KhongKhaiNgonNgu_TraCauTiengViet()
    {
        var (_, description) = await LoiDauTien(languages: null);

        Assert.Equal("Email hoặc mật khẩu không đúng.", description);
    }

    /// <summary>
    /// Ngôn ngữ không hỗ trợ thì lùi về tiếng Việt, KHÔNG được nổ và cũng không được
    /// trả ra mã kỹ thuật trần.
    /// </summary>
    [Fact]
    public async Task NgonNguKhongHoTro_LuiVeTiengViet()
    {
        var (_, description) = await LoiDauTien("ja-JP");

        Assert.Equal("Email hoặc mật khẩu không đúng.", description);
    }

    // ── Tiện ích ─────────────────────────────────────────────────────────────

    private async Task<(string Code, string Description)> LoiDauTien(string? languages)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/probe/that-bai-nghiep-vu");

        if (languages is not null)
        {
            request.Headers.Add("Accept-Language", languages);
        }

        var response = await _factory.CreateClient().SendAsync(request);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var loi = document.RootElement.GetProperty("errors")[0];

        return (loi.GetProperty("code").GetString()!, loi.GetProperty("description").GetString()!);
    }
}
