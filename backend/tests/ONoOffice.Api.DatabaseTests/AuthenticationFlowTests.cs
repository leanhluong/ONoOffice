using Microsoft.Extensions.DependencyInjection;
using ONoOffice.Identity.Infrastructure.Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ONoOffice.Identity.Domain;

namespace ONoOffice.Api.DatabaseTests;

/// <summary>
/// Luồng đăng nhập chạy đầu-tới-cuối: HTTP → handler → EF → Postgres → và ngược lại.
///
/// Đây là lần đầu tiên trong dự án có một câu truy vấn thật chạy. Ba thứ chỉ ở đây mới
/// biết đúng hay sai, và cả ba đều là loại hỏng <b>im lặng</b>:
///
/// <list type="number">
/// <item><c>IgnoreQueryFilters</c> lúc đăng nhập — thiếu thì KHÔNG AI đăng nhập được, và
/// lỗi trông y hệt lỗi sai mật khẩu.</item>
/// <item><c>TenantInterceptor</c> khi ghi <c>RefreshToken</c> lúc phiên chưa có tenant —
/// nó có quyền ném <c>CrossTenantWriteException</c>, và nếu ném thì đăng nhập ra 500.</item>
/// <item>Bộ lọc tenant ở chiều đọc — hỏng thì các workspace nhìn thấy dữ liệu của nhau.</item>
/// </list>
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class AuthenticationFlowTests(DatabaseFixture fixture)
{
    // ── Đăng nhập ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DangNhapDung_Tra200_KemDuBonThuTrongThanPhanHoi()
    {
        var than = await DangNhap(DatabaseFixture.OwnerEmail, DatabaseFixture.OwnerPassword);

        Assert.False(string.IsNullOrWhiteSpace(than.GetProperty("accessToken").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(than.GetProperty("refreshToken").GetString()));
        Assert.Equal(900, than.GetProperty("expiresInSeconds").GetInt32());
        Assert.Equal(DatabaseFixture.OwnerEmail, than.GetProperty("user").GetProperty("email").GetString());
    }

    /// <summary>
    /// ⭐ Chứng minh chuỗi <c>User → role_ids → Roles.permissions</c> chạy được trên
    /// Postgres thật, với hai cột mảng (<c>uuid[]</c> và <c>text[]</c>).
    ///
    /// Chỗ này rất dễ hỏng lặng lẽ: nếu phép nối hỏng, kết quả không phải là lỗi mà là
    /// một token <b>không có quyền nào</b>. Người dùng đăng nhập thành công rồi bấm gì
    /// cũng 403 — và không có dòng log nào nói vì sao.
    /// </summary>
    [Fact]
    public async Task TokenCuaChuWorkspace_MangDuTATCAQuyen()
    {
        var than = await DangNhap(DatabaseFixture.OwnerEmail, DatabaseFixture.OwnerPassword);

        var quyen = QuyenTrongToken(than.GetProperty("accessToken").GetString()!);

        Assert.Equal(
            Permissions.All.OrderBy(p => p, StringComparer.Ordinal),
            quyen.OrderBy(p => p, StringComparer.Ordinal));
    }

    [Fact]
    public async Task Token_MangTenantIdCuaWorkspace_LayTuTokenKhongPhaiTuClient()
    {
        var than = await DangNhap(DatabaseFixture.OwnerEmail, DatabaseFixture.OwnerPassword);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(than.GetProperty("accessToken").GetString());
        string? trongToken = token.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value;

        Assert.Equal(than.GetProperty("user").GetProperty("tenantId").GetString(), trongToken);
    }

    [Theory]
    [InlineData(DatabaseFixture.OwnerEmail, "mat-khau-sai")]
    [InlineData("khong-ton-tai@demo.vn", DatabaseFixture.OwnerPassword)]
    public async Task DangNhapSai_LuonTraCUNGMOTMaLoi(string email, string matKhau)
    {
        var (status, than) = await Goi("/api/auth/login", new { email, password = matKhau });

        Assert.Equal(HttpStatusCode.Unauthorized, status);

        // Sai email và sai mật khẩu KHÔNG được phân biệt. Tách bạch hai ca là tặng công
        // cụ dò tài khoản: gõ 10.000 email, cái nào báo "sai mật khẩu" là email có thật.
        Assert.Equal("Auth.InvalidCredentials", than.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    // ── Refresh token ────────────────────────────────────────────────────────

    /// <summary>
    /// ⭐ Database chỉ được giữ chuỗi BĂM. Lộ bảng thì kẻ đọc được cũng không chiếm được
    /// phiên của ai.
    /// </summary>
    [Fact]
    public async Task RefreshToken_LuuXuongDuoiDangBAM_KhongBaoGioLaChuoiTho()
    {
        var than = await DangNhap(DatabaseFixture.OwnerEmail, DatabaseFixture.OwnerPassword);
        string veTho = than.GetProperty("refreshToken").GetString()!;

        using var scope = fixture.CreateScope().Scope;
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var coChuoiTho = await context.RefreshTokens
            .IgnoreQueryFilters()
            .AnyAsync(t => t.TokenHash == veTho);

        Assert.False(coChuoiTho, "Chuỗi thô của refresh token đang nằm trong database.");

        // Và bản băm thì phải có — nếu không thì test trên xanh vì bảng rỗng.
        Assert.True(await context.RefreshTokens.IgnoreQueryFilters().AnyAsync());
    }

    [Fact]
    public async Task GiaHanPhien_TraVeVeMOI_KhacVeCu()
    {
        var dangNhap = await DangNhap(DatabaseFixture.OwnerEmail, DatabaseFixture.OwnerPassword);
        string veCu = dangNhap.GetProperty("refreshToken").GetString()!;

        var (status, giaHan) = await Goi("/api/auth/refresh", new { refreshToken = veCu });

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.NotEqual(veCu, giaHan.GetProperty("refreshToken").GetString());
    }

    /// <summary>
    /// ⭐⭐ Phát hiện trộm — luật đắt giá nhất của cả module.
    ///
    /// Vé đã xoay vòng mà còn được đem dùng nghĩa là HAI bên đang cùng giữ nó. Lúc đó
    /// không tin bên nào cả: thu hồi toàn bộ chuỗi, kể cả vé mới vừa cấp cho người thật.
    /// Chỉ thu hồi mỗi vé bị dùng lại là vô dụng — nó vốn đã bị thu hồi rồi.
    /// </summary>
    [Fact]
    public async Task DungLaiVeDaXoayVong_ThuHoiCA_CHUOI_KeCaVeConSong()
    {
        var dangNhap = await DangNhap(DatabaseFixture.OwnerEmail, DatabaseFixture.OwnerPassword);
        string veCu = dangNhap.GetProperty("refreshToken").GetString()!;

        var (_, giaHan) = await Goi("/api/auth/refresh", new { refreshToken = veCu });
        string veMoi = giaHan.GetProperty("refreshToken").GetString()!;

        // Kẻ trộm dùng lại vé cũ.
        var (statusTrom, _) = await Goi("/api/auth/refresh", new { refreshToken = veCu });
        Assert.Equal(HttpStatusCode.Unauthorized, statusTrom);

        // Và vé MỚI — vé của người dùng thật, vẫn còn hạn — cũng phải chết theo.
        var (statusNguoiThat, _) = await Goi("/api/auth/refresh", new { refreshToken = veMoi });

        Assert.Equal(HttpStatusCode.Unauthorized, statusNguoiThat);
    }

    [Fact]
    public async Task DangXuat_Tra204_VaVeKhongDungDuocNua()
    {
        var dangNhap = await DangNhap(DatabaseFixture.OwnerEmail, DatabaseFixture.OwnerPassword);
        string ve = dangNhap.GetProperty("refreshToken").GetString()!;

        var (statusThoat, _) = await Goi("/api/auth/logout", new { refreshToken = ve });
        Assert.Equal(HttpStatusCode.NoContent, statusThoat);

        var (statusGiaHan, _) = await Goi("/api/auth/refresh", new { refreshToken = ve });

        Assert.Equal(HttpStatusCode.Unauthorized, statusGiaHan);
    }

    [Fact]
    public async Task DangXuat_VeKhongTonTai_VanTra204()
    {
        // Báo "vé này không tồn tại" là tiết lộ vé nào TỪNG tồn tại.
        var (status, _) = await Goi("/api/auth/logout", new { refreshToken = "ve-hoan-toan-bia-ra" });

        Assert.Equal(HttpStatusCode.NoContent, status);
    }

    // ── Tiện ích ─────────────────────────────────────────────────────────────

    private async Task<JsonElement> DangNhap(string email, string matKhau)
    {
        var (status, than) = await Goi("/api/auth/login", new { email, password = matKhau });

        Assert.Equal(HttpStatusCode.OK, status);

        return than;
    }

    private async Task<(HttpStatusCode Status, JsonElement Than)> Goi(string duongDan, object than)
    {
        using var content = new StringContent(JsonSerializer.Serialize(than), Encoding.UTF8, "application/json");

        var response = await fixture.CreateClient().PostAsync(duongDan, content);
        string chuoi = await response.Content.ReadAsStringAsync();

        var element = string.IsNullOrWhiteSpace(chuoi)
            ? default
            : JsonDocument.Parse(chuoi).RootElement.Clone();

        return (response.StatusCode, element);
    }

    private static IEnumerable<string> QuyenTrongToken(string accessToken) =>
        new JwtSecurityTokenHandler()
            .ReadJwtToken(accessToken)
            .Claims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value);
}
