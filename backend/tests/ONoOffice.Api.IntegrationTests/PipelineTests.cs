using System.Net;
using Luong.Kernel.AspNetCore.Middleware;
using ONoOffice.Api.IntegrationTests.Probe;

namespace ONoOffice.Api.IntegrationTests;

/// <summary>
/// Canh <b>thứ tự</b> các lớp middleware — thứ mà không unit test nào bắt được.
///
/// Mỗi middleware tách riêng ra thì cái nào cũng đúng; hỏng nằm ở chỗ chúng được xếp
/// theo thứ tự nào. Mà thứ tự sai thường không làm build đỏ, không làm test đơn vị đỏ —
/// nó chỉ hiện ra khi có một trình duyệt thật gọi vào, thường là ở môi trường thật.
/// </summary>
public sealed class PipelineTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    // ── Mã lần vết ───────────────────────────────────────────────────────────

    [Fact]
    public async Task MoiPhanHoi_DeuMangMaLanVet()
    {
        var response = await _factory.CreateClient().GetAsync("/probe/an-danh");

        Assert.True(
            response.Headers.Contains(CorrelationIdMiddleware.HeaderName),
            "Không có mã lần vết trong phản hồi — người dùng báo lỗi sẽ không có gì để đọc cho hỗ trợ.");
    }

    [Fact]
    public async Task MaLanVetGuiLenTuTruoc_DuocGiuNguyen()
    {
        const string maGuiLen = "ma-tu-gateway-gui-xuong";

        using var request = new HttpRequestMessage(HttpMethod.Get, "/probe/an-danh");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, maGuiLen);

        var response = await _factory.CreateClient().SendAsync(request);

        // Sinh mã mới ở đây là cắt đứt sợi dây nối log của gateway với log của API.
        Assert.Equal(maGuiLen, response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single());
    }

    // ── CORS ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// ⭐⭐ Đây mới là chỗ thứ tự <c>UseCors</c> thật sự cắn.
    ///
    /// Request xuyên origin bị từ chối 401 <b>vẫn phải mang header CORS</b>. Vì sao lại
    /// quan trọng đến thế: <c>UseAuthorization</c> cắt ngang chuỗi và trả 401 ngay tại
    /// chỗ nó đứng. Nếu <c>UseCors</c> nằm SAU nó thì middleware CORS không bao giờ chạy
    /// cho phản hồi đó, và phản hồi 401 đi ra mà không có <c>Access-Control-Allow-Origin</c>.
    ///
    /// Lúc đó trình duyệt <b>không cho JavaScript đọc phản hồi</b> — kể cả mã trạng thái.
    /// Frontend không phân biệt được "phiên hết hạn" với "máy chủ hỏng", nên nó không
    /// biết phải đưa người dùng về màn đăng nhập. Trên console hiện lỗi CORS, và người
    /// ta sẽ đi sửa CORS, trong khi chuyện thật chỉ là token hết hạn.
    ///
    /// Đặt <c>UseCors</c> trước thì middleware CORS gắn header qua <c>OnStarting</c>,
    /// nên nó dính vào cả những phản hồi do tầng dưới cắt ngang.
    /// </summary>
    [Fact]
    public async Task PhanHoi401ChoRequestXuyenOrigin_VanPhaiMangHeaderCors()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/probe/can-dang-nhap");
        request.Headers.Add("Origin", ApiFactory.OriginDuocPhep);

        var response = await _factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        Assert.True(
            response.Headers.Contains("Access-Control-Allow-Origin"),
            "401 đi ra không kèm header CORS — UseCors đang nằm SAU UseAuthorization. "
                + "Trình duyệt sẽ báo lỗi CORS thay vì để frontend thấy 401.");
    }

    /// <summary>
    /// Preflight <c>OPTIONS</c> phải qua được, dù nó <b>không bao giờ</b> mang token.
    ///
    /// Ghi lại cho rõ điều đã kiểm chứng được bằng thực nghiệm: ở cấu hình này, tự thân
    /// preflight KHÔNG bị <c>UseAuthorization</c> chặn — vì <c>OPTIONS</c> không khớp
    /// endpoint nào (định tuyến theo thuộc tính chỉ map <c>GET</c>), nên không có policy
    /// nào để áp. Nó sẽ bị chặn nếu sau này hệ thống đặt <c>FallbackPolicy</c> đòi đăng
    /// nhập cho MỌI request, kể cả request không khớp endpoint. Test này canh đúng ngày đó.
    /// </summary>
    [Fact]
    public async Task Preflight_KhongMangToken_VanQuaDuoc_DuEndpointDoiXacThuc()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/probe/can-dang-nhap");
        request.Headers.Add("Origin", ApiFactory.OriginDuocPhep);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await _factory.CreateClient().SendAsync(request);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);

        Assert.True(
            response.Headers.Contains("Access-Control-Allow-Origin"),
            "Preflight không nhận được header cho phép — UseCors nhiều khả năng đang nằm sau UseAuthentication.");
    }

    [Fact]
    public async Task Origin_KhongCoTrongDanhSach_ThiKhongDuocPhepDoc()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/probe/an-danh");
        request.Headers.Add("Origin", ApiFactory.OriginNguoiLa);

        var response = await _factory.CreateClient().SendAsync(request);

        // Thiếu header này thì trình duyệt vứt bỏ phản hồi. Đó chính là điều ta muốn:
        // AllowAnyOrigin sẽ làm test này đỏ.
        Assert.False(
            response.Headers.Contains("Access-Control-Allow-Origin"),
            "Origin lạ vẫn được cấp quyền đọc — nhiều khả năng đang dùng AllowAnyOrigin.");
    }

    // ── Header an toàn ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("X-Content-Type-Options", "nosniff")]
    [InlineData("X-Frame-Options", "DENY")]
    [InlineData("Referrer-Policy", "no-referrer")]
    public async Task HeaderAnToan_CoTrenMoiPhanHoi(string ten, string giaTri)
    {
        var response = await _factory.CreateClient().GetAsync("/probe/an-danh");

        Assert.True(response.Headers.Contains(ten), $"Thiếu header an toàn '{ten}'.");
        Assert.Equal(giaTri, response.Headers.GetValues(ten).Single());
    }

    // ── Lưới an toàn cuối cùng ───────────────────────────────────────────────

    /// <summary>
    /// ⭐ Exception lọt lưới phải thành 500 <b>rỗng ruột</b>.
    ///
    /// Thông báo exception thật hay chứa chuỗi kết nối, tên máy chủ, đường dẫn file. Đẩy
    /// nguyên ra ngoài là tặng không bản đồ hệ thống cho người đang dò.
    /// </summary>
    [Fact]
    public async Task LoiNgoaiDuKien_Tra500_VaKhongLoMotChuNaoBenTrong()
    {
        var response = await _factory.CreateClient().GetAsync("/probe/no-tung");
        string than = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        Assert.DoesNotContain(ProbeController.ChuoiNhayCam, than, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", than, StringComparison.Ordinal);

        // Đổi lại, người dùng phải cầm được mã lần vết để đọc cho bộ phận hỗ trợ.
        Assert.Contains("correlationId", than, StringComparison.Ordinal);
    }
}
