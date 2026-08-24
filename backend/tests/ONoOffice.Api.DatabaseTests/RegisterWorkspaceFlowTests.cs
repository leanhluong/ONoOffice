using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ONoOffice.Identity.Domain;
using ONoOffice.Identity.Infrastructure.Persistence;

namespace ONoOffice.Api.DatabaseTests;

/// <summary>
/// Đăng ký workspace, chạy đầu-tới-cuối trên Postgres thật.
///
/// Use case này ghi vào <b>bốn bảng</b> trong một transaction — nhiều hơn bất kỳ chỗ nào
/// khác trong hệ. Test đơn vị chứng minh được thứ tự và luật, nhưng không chứng minh
/// được rằng bốn lần ghi đó thật sự xuống được database cùng nhau: ràng buộc UNIQUE,
/// bốn interceptor, và <c>TenantInterceptor</c> khi phiên CHƯA có workspace nào.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class RegisterWorkspaceFlowTests(DatabaseFixture fixture)
{
    private static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    /// <summary>Mỗi test một mã riêng — chúng dùng chung một database. </summary>
    private static object NewCompany(string suffix, string? email = null) =>
        new
        {
            companyName = $"Công ty {suffix}",
            workspaceCode = $"cty-{suffix}",
            fullName = "Người Đăng Ký",
            email = email ?? $"chu.{suffix}@congty.vn",
            password = "mot-cau-rat-de-nho",
        };

    private async Task<(HttpStatusCode Status, JsonElement Body)> Post(string path, object body)
    {
        using var content = Json(body);

        var response = await fixture.CreateClient().PostAsync(path, content);
        string raw = await response.Content.ReadAsStringAsync();

        var element = string.IsNullOrWhiteSpace(raw)
            ? default
            : JsonDocument.Parse(raw).RootElement.Clone();

        return (response.StatusCode, element);
    }

    // ── Đường thành công ──────────────────────────────────────────────────

    /// <summary>
    /// ⭐ Bốn bảng, một transaction: <c>tenants</c>, <c>roles</c> ×4, <c>users</c>,
    /// <c>refresh_tokens</c>.
    /// </summary>
    [Fact]
    public async Task DangKy_ThiGhiDuBonBang()
    {
        var (status, body) = await Post("/api/auth/register-workspace", NewCompany("alpha"));

        Assert.Equal(HttpStatusCode.OK, status);

        var tenantId = Guid.Parse(body.GetProperty("workspace").GetProperty("id").GetString()!);
        var userId = Guid.Parse(body.GetProperty("user").GetProperty("id").GetString()!);

        using var scope = fixture.CreateScope().Scope;
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var tenant = await context.Tenants.SingleAsync(t => t.Id == tenantId);

        Assert.Equal("cty-alpha", tenant.Code.Value);
        Assert.Equal(userId, tenant.OwnerUserId);

        Assert.Equal(4, await context.Roles.IgnoreQueryFilters().CountAsync(r => r.TenantId == tenantId));
        Assert.True(await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == userId));
        Assert.True(await context.RefreshTokens.IgnoreQueryFilters().AnyAsync(t => t.UserId == userId));
    }

    /// <summary>
    /// Đăng ký xong là dùng được ngay — token trả về phải mang đủ quyền của Owner.
    ///
    /// Sai chỗ này thì người vừa tạo công ty không quản trị được chính công ty mình, và
    /// không còn ai cấp quyền lại cho họ được.
    /// </summary>
    [Fact]
    public async Task DangKy_TraVeTokenMangDuQuyenOwner()
    {
        var (_, body) = await Post("/api/auth/register-workspace", NewCompany("beta"));

        var quyen = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
            .ReadJwtToken(body.GetProperty("accessToken").GetString())
            .Claims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value);

        Assert.Equal(
            Permissions.All.OrderBy(p => p, StringComparer.Ordinal),
            quyen.OrderBy(p => p, StringComparer.Ordinal));
    }

    /// <summary>
    /// ⭐⭐ Đăng ký xong thì <b>đăng nhập được bằng đúng mật khẩu vừa đặt</b>.
    ///
    /// Đây là phép kiểm khép kín cả hai luồng: nếu băm lúc đăng ký và kiểm lúc đăng nhập
    /// dùng hai đường khác nhau, mọi test khác vẫn xanh và chỉ có test này đỏ.
    /// </summary>
    [Fact]
    public async Task DangKyXong_ThiDangNhapDuocBangMatKhauVuaDat()
    {
        await Post("/api/auth/register-workspace", NewCompany("gamma"));

        var (status, body) = await Post(
            "/api/auth/login",
            new { email = "chu.gamma@congty.vn", password = "mot-cau-rat-de-nho" });

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("chu.gamma@congty.vn", body.GetProperty("user").GetProperty("email").GetString());
    }

    [Fact]
    public async Task DangKy_ThiCoDuBonVaiTroDungTen()
    {
        var (_, body) = await Post("/api/auth/register-workspace", NewCompany("delta"));

        var tenantId = Guid.Parse(body.GetProperty("workspace").GetProperty("id").GetString()!);

        using var scope = fixture.CreateScope().Scope;
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var names = await context.Roles
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantId)
            .Select(r => r.Name)
            .ToListAsync();

        Assert.Equal(
            SystemRoles.All.Select(r => r.Name).OrderBy(n => n, StringComparer.Ordinal),
            names.OrderBy(n => n, StringComparer.Ordinal));
    }

    // ── Đường từ chối ─────────────────────────────────────────────────────

    [Fact]
    public async Task MaWorkspace_DaCoNguoiDung_Tra409()
    {
        await Post("/api/auth/register-workspace", NewCompany("epsilon"));

        // Cùng mã, khác email.
        var (status, body) = await Post(
            "/api/auth/register-workspace",
            NewCompany("epsilon", "nguoi.khac@congty.vn"));

        Assert.Equal(HttpStatusCode.Conflict, status);
        Assert.Equal("TenantCode.Taken", body.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task Email_DaCoTaiKhoan_Tra409()
    {
        await Post("/api/auth/register-workspace", NewCompany("zeta"));

        // Khác mã workspace, nhưng cùng email — email unique TOÀN hệ thống (ADR-0002).
        var (status, body) = await Post(
            "/api/auth/register-workspace",
            NewCompany("eta", "chu.zeta@congty.vn"));

        Assert.Equal(HttpStatusCode.Conflict, status);
        Assert.Equal("Email.Taken", body.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task BiTuChoi_ThiKHONGDeLaiWorkspaceNaoTrongDatabase()
    {
        await Post("/api/auth/register-workspace", NewCompany("theta"));

        await Post("/api/auth/register-workspace", NewCompany("theta", "khac@congty.vn"));

        using var scope = fixture.CreateScope().Scope;
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        // Đúng MỘT workspace mang mã đó — lần thứ hai không để lại gì.
        Assert.Equal(1, await context.Tenants.CountAsync(t => t.Code == ONoOffice.Identity.Domain.ValueObjects.TenantCode.Create("cty-theta").Value));
    }

    [Fact]
    public async Task MatKhauQuaNgan_Tra400()
    {
        var (status, _) = await Post(
            "/api/auth/register-workspace",
            new
            {
                companyName = "Công ty X",
                workspaceCode = "cty-x",
                fullName = "Người X",
                email = "x@congty.vn",
                password = "ngan",
            });

        Assert.Equal(HttpStatusCode.BadRequest, status);
    }

    [Fact]
    public async Task MaWorkspaceCoDau_Tra400()
    {
        var (status, body) = await Post(
            "/api/auth/register-workspace",
            new
            {
                companyName = "Công ty Y",
                workspaceCode = "Mã Có Dấu",
                fullName = "Người Y",
                email = "y@congty.vn",
                password = "mot-cau-rat-de-nho",
            });

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal("TenantCode.Invalid", body.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    /// <summary>
    /// Hai workspace khác nhau, mỗi bên chỉ thấy vai trò của mình.
    ///
    /// Đăng ký là chỗ dễ rò nhất: nó chạy khi CHƯA có phiên nào, nên mọi bộ lọc tenant
    /// đều không có gì để lọc. Test này kiểm rằng dữ liệu vẫn nằm đúng ngăn của nó.
    /// </summary>
    [Fact]
    public async Task HaiWorkspace_KhongThayVaiTroCuaNhau()
    {
        var (_, a) = await Post("/api/auth/register-workspace", NewCompany("iota"));
        var (_, b) = await Post("/api/auth/register-workspace", NewCompany("kappa"));

        var tenantA = Guid.Parse(a.GetProperty("workspace").GetProperty("id").GetString()!);
        var tenantB = Guid.Parse(b.GetProperty("workspace").GetProperty("id").GetString()!);

        var (scope, context) = fixture.CreateScope(tenantB);
        using var _ = scope;

        var thayDuoc = await context.Roles.Select(r => r.TenantId).Distinct().ToListAsync();

        Assert.DoesNotContain(tenantA, thayDuoc);
        Assert.Equal([tenantB], thayDuoc);
    }
}
