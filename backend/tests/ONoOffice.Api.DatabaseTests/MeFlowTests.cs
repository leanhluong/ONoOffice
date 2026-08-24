using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ONoOffice.Api.DatabaseTests;

/// <summary>
/// Hồ sơ và mật khẩu của chính người đang đăng nhập, trên Postgres thật.
///
/// Test quan trọng nhất ở đây là <c>DoiMatKhauXong_ThiVeGiaHanCU_KHONG_dung_duoc_nua</c>.
/// Nó là thứ duy nhất chứng minh được rằng việc đổi mật khẩu <b>thật sự</b> đá kẻ trộm ra
/// — mọi test đơn vị chỉ chứng minh được rằng handler có GỌI hàm thu hồi.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class MeFlowTests(DatabaseFixture fixture)
{
    private static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private static async Task<(HttpStatusCode Status, JsonElement Body)> Read(HttpResponseMessage response)
    {
        string raw = await response.Content.ReadAsStringAsync();

        var element = string.IsNullOrWhiteSpace(raw)
            ? default
            : JsonDocument.Parse(raw).RootElement.Clone();

        return (response.StatusCode, element);
    }

    private async Task<(string Token, string RefreshToken, string Email)> NewOwner(string suffix)
    {
        string email = $"chu.{suffix}@congty.vn";

        using var content = Json(new
        {
            companyName = $"Công ty {suffix}",
            workspaceCode = $"me-{suffix}",
            fullName = "Chủ Sở Hữu",
            email,
            password = "mot-cau-rat-de-nho",
        });

        var response = await fixture.CreateClient().PostAsync("/api/auth/register-workspace", content);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

        return (
            body.GetProperty("accessToken").GetString()!,
            body.GetProperty("refreshToken").GetString()!,
            email);
    }

    private HttpClient ClientFor(string token)
    {
        var client = fixture.CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    // ── Hồ sơ ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task HoSoCuaToi_KemVaiTroVaCoChuSoHuu()
    {
        var (token, _, email) = await NewOwner($"hs{Guid.NewGuid():N}"[..14]);

        var (status, body) = await Read(await ClientFor(token).GetAsync("/api/me"));

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal(email, body.GetProperty("email").GetString());
        Assert.Equal("Owner", body.GetProperty("roleName").GetString());

        // Giao diện dùng cờ này để ẩn bớt lựa chọn: chủ sở hữu không tự đổi vai trò được.
        Assert.True(body.GetProperty("isOwner").GetBoolean());
    }

    [Fact]
    public async Task SuaHoTen_ThiLanSauDocRaTenMoi()
    {
        var (token, _, _) = await NewOwner($"ten{Guid.NewGuid():N}"[..13]);
        var client = ClientFor(token);

        using var content = Json(new { fullName = "Lê Anh Lượng" });

        var (status, _) = await Read(await client.PatchAsync("/api/me", content));

        Assert.Equal(HttpStatusCode.NoContent, status);

        var (_, body) = await Read(await client.GetAsync("/api/me"));

        Assert.Equal("Lê Anh Lượng", body.GetProperty("fullName").GetString());
    }

    // ── Đổi mật khẩu ──────────────────────────────────────────────────────

    [Fact]
    public async Task DoiMatKhauXong_ThiDangNhapBangMatKhauMOI()
    {
        var (token, _, email) = await NewOwner($"mk{Guid.NewGuid():N}"[..14]);

        using var doi = Json(new
        {
            currentPassword = "mot-cau-rat-de-nho",
            newPassword = "mot-cau-khac-cung-de-nho",
        });

        var (status, _) = await Read(await ClientFor(token).PostAsync("/api/me/password", doi));

        Assert.Equal(HttpStatusCode.NoContent, status);

        using var login = Json(new { email, password = "mot-cau-khac-cung-de-nho" });

        var (loginStatus, _) = await Read(await fixture.CreateClient().PostAsync("/api/auth/login", login));

        Assert.Equal(HttpStatusCode.OK, loginStatus);
    }

    [Fact]
    public async Task DoiMatKhauXong_ThiVeGiaHanCU_KHONG_dung_duoc_nua()
    {
        // ⭐ Đây là điểm quan trọng nhất của cả use case, và là thứ chỉ database chứng
        // minh được. Người ta đổi mật khẩu vì nghĩ nó bị lộ; nếu vé gia hạn cũ vẫn sống
        // thì kẻ trộm ngồi yên thêm 30 ngày và việc đổi mật khẩu chỉ để cho yên tâm.
        var (token, refreshToken, _) = await NewOwner($"ve{Guid.NewGuid():N}"[..14]);

        using var doi = Json(new
        {
            currentPassword = "mot-cau-rat-de-nho",
            newPassword = "mot-cau-khac-cung-de-nho",
        });

        await ClientFor(token).PostAsync("/api/me/password", doi);

        using var giaHan = Json(new { refreshToken });

        var (status, _) = await Read(await fixture.CreateClient().PostAsync("/api/auth/refresh", giaHan));

        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    [Fact]
    public async Task SaiMatKhauHienTai_ThiTuChoi_va_ve_gia_han_VAN_song()
    {
        // Thu hồi khi kiểm tra thất bại thì bất kỳ ai ngồi vào máy đang mở cũng đá được
        // người dùng ra khỏi mọi thiết bị chỉ bằng cách gõ bừa.
        var (token, refreshToken, _) = await NewOwner($"sai{Guid.NewGuid():N}"[..13]);

        using var doi = Json(new { currentPassword = "doan-bua-cho-vui", newPassword = "mot-cau-khac-de-nho" });

        var (status, body) = await Read(await ClientFor(token).PostAsync("/api/me/password", doi));

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal("User.WrongCurrentPassword", body.GetProperty("errors")[0].GetProperty("code").GetString());

        using var giaHan = Json(new { refreshToken });

        var (refreshStatus, _) = await Read(await fixture.CreateClient().PostAsync("/api/auth/refresh", giaHan));

        Assert.Equal(HttpStatusCode.OK, refreshStatus);
    }

    // ── Không tự khoá mình ────────────────────────────────────────────────

    [Fact]
    public async Task TuVoHieuHoaChinhMinh_ThiBiChan()
    {
        // Cách nhanh nhất để một workspace mất hết quản trị viên.
        var (token, _, _) = await NewOwner($"tu{Guid.NewGuid():N}"[..14]);
        var client = ClientFor(token);

        var (_, me) = await Read(await client.GetAsync("/api/me"));

        var myId = me.GetProperty("id").GetGuid();

        using var empty = Json(new { });

        var (status, body) = await Read(await client.PostAsync($"/api/users/{myId}/disable", empty));

        Assert.Equal(HttpStatusCode.Conflict, status);
        Assert.Equal("User.CannotDisableSelf", body.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task QuanTriKHAC_cung_khong_vo_hieu_hoa_duoc_CHU_SO_HUU()
    {
        // ⭐ Luật này chỉ database chứng minh được, vì nó phụ thuộc vào việc truy vấn
        // "ai là chủ workspace" có nêu đích danh workspace hay không. Bảng tenants KHÔNG
        // có bộ lọc theo tenant (một Tenant chính là workspace, không thuộc workspace nào),
        // nên thiếu điều kiện thì nó trả về chủ của một công ty bất kỳ — và luật này im lặng
        // không chạy. Test đơn vị với repository giả không bao giờ thấy được chuyện đó.
        var (ownerToken, _, _) = await NewOwner($"own{Guid.NewGuid():N}"[..13]);
        var ownerClient = ClientFor(ownerToken);

        var (_, me) = await Read(await ownerClient.GetAsync("/api/me"));
        var ownerId = me.GetProperty("id").GetGuid();

        // Chủ sở hữu tạo một quản trị viên khác, rồi người đó thử khoá chủ.
        var (_, roles) = await Read(await ownerClient.GetAsync("/api/roles"));

        var adminRoleId = roles.EnumerateArray()
            .Single(role => role.GetProperty("name").GetString() == "Admin")
            .GetProperty("id").GetGuid();

        string adminEmail = $"admin.{Guid.NewGuid():N}"[..22] + "@congty.vn";

        using var taoAdmin = Json(new
        {
            fullName = "Quản Trị Khác",
            email = adminEmail,
            roleId = adminRoleId,
            mustChangePassword = false,
        });

        var (_, created) = await Read(await ownerClient.PostAsync("/api/users", taoAdmin));

        using var dangNhap = Json(new
        {
            email = adminEmail,
            password = created.GetProperty("temporaryPassword").GetString(),
        });

        var (_, adminLogin) = await Read(await fixture.CreateClient().PostAsync("/api/auth/login", dangNhap));

        using var rong = Json(new { });

        var (status, body) = await Read(
            await ClientFor(adminLogin.GetProperty("accessToken").GetString()!)
                .PostAsync($"/api/users/{ownerId}/disable", rong));

        Assert.Equal(HttpStatusCode.Conflict, status);
        Assert.Equal("User.CannotDisableOwner", body.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    // ── Vai trò ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DanhSachVaiTro_CoDuBonVaiHeThongVaDemDungSoNguoi()
    {
        var (token, _, _) = await NewOwner($"vt{Guid.NewGuid():N}"[..14]);

        var (status, body) = await Read(await ClientFor(token).GetAsync("/api/roles"));

        Assert.Equal(HttpStatusCode.OK, status);

        var roles = body.EnumerateArray().ToList();

        Assert.Equal(4, roles.Count);
        Assert.All(roles, role => Assert.True(role.GetProperty("isSystem").GetBoolean()));

        var owner = roles.Single(role => role.GetProperty("name").GetString() == "Owner");

        Assert.Equal(1, owner.GetProperty("memberCount").GetInt32());
        Assert.Equal(12, owner.GetProperty("permissions").GetArrayLength());

        // Member là vai hẹp nhất — nếu nó cũng có 12 quyền thì bộ quyền đã bị trộn lẫn.
        var member = roles.Single(role => role.GetProperty("name").GetString() == "Member");

        Assert.Equal(0, member.GetProperty("memberCount").GetInt32());
        Assert.True(member.GetProperty("permissions").GetArrayLength() < 12);
    }
}
