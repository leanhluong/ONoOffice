using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ONoOffice.Identity.Infrastructure.Persistence;

namespace ONoOffice.Api.DatabaseTests;

/// <summary>
/// Quản trị viên tạo tài khoản hộ đồng nghiệp, rồi xem danh sách nhân sự — trên Postgres thật.
///
/// <b>Vì sao phải chạy trên database thật:</b> phần lớn use case này KHÔNG nằm trong C#.
/// Nó nằm trong SQL mà EF sinh ra — bộ lọc theo tenant, lọc trạng thái, phân trang, và
/// truy vấn thứ hai gom tên vai trò từ cột mảng <c>uuid[]</c>. Test đơn vị với repository
/// giả chứng minh được handler chặn đúng những con số client gửi lên, và <b>không chứng
/// minh được gì</b> về việc câu SQL kia có chạy nổi hay không.
///
/// Đúng kiểu bẫy đã gặp một lần ở dự án này: mô hình EF chưa từng dựng nổi trên Postgres
/// mà 194 test đơn vị vẫn xanh.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class UserManagementFlowTests(DatabaseFixture fixture)
{
    private static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    /// <summary>Dựng một workspace mới và trả về token của chủ sở hữu.</summary>
    private async Task<(string Token, Guid TenantId, JsonElement Body)> NewWorkspace(string suffix)
    {
        using var content = Json(new
        {
            companyName = $"Công ty {suffix}",
            workspaceCode = $"ql-{suffix}",
            fullName = "Chủ Sở Hữu",
            email = $"chu.{suffix}@congty.vn",
            password = "mot-cau-rat-de-nho",
        });

        var response = await fixture.CreateClient().PostAsync("/api/auth/register-workspace", content);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

        return (
            body.GetProperty("accessToken").GetString()!,
            body.GetProperty("workspace").GetProperty("id").GetGuid(),
            body);
    }

    private HttpClient ClientFor(string token)
    {
        var client = fixture.CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> Read(HttpResponseMessage response)
    {
        string raw = await response.Content.ReadAsStringAsync();

        var element = string.IsNullOrWhiteSpace(raw)
            ? default
            : JsonDocument.Parse(raw).RootElement.Clone();

        return (response.StatusCode, element);
    }

    /// <summary>
    /// Mã của một vai trò hệ thống trong một workspace.
    ///
    /// Tra thẳng database vì chưa có endpoint đọc vai trò. Đây là chỗ DUY NHẤT trong bộ
    /// test này đi vòng qua HTTP — mọi bước còn lại đều gọi API như một client thật.
    /// </summary>
    private async Task<Guid> RoleId(Guid tenantId, string roleName)
    {
        var (scope, context) = fixture.CreateScope(tenantId);

        using (scope)
        {
            return await context.Roles.Where(r => r.Name == roleName).Select(r => r.Id).FirstAsync();
        }
    }

    // ── Tạo tài khoản ─────────────────────────────────────────────────────

    [Fact]
    public async Task QuanTriTaoTaiKhoan_ThiTraVeMatKhauTamVaNguoiDoDangNhapDuoc()
    {
        // ⭐ Vòng khép kín: tạo → nhận mật khẩu tạm → đăng nhập bằng chính nó.
        // Đây là mắt xích mà mọi test đơn vị đều bỏ qua, vì nó đi qua băm Argon2 thật,
        // ràng buộc UNIQUE thật, và một transaction thật.
        var (token, tenantId, _) = await NewWorkspace($"tao{Guid.NewGuid():N}"[..14]);
        var client = ClientFor(token);
        var memberRoleId = await RoleId(tenantId, "Member");

        string email = $"nv.{Guid.NewGuid():N}"[..18] + "@congty.vn";

        using var content = Json(new
        {
            fullName = "Nguyễn Văn An",
            email,
            roleId = memberRoleId,
            mustChangePassword = true,
        });

        var (status, body) = await Read(await client.PostAsync("/api/users", content));

        Assert.Equal(HttpStatusCode.OK, status);

        string tempPassword = body.GetProperty("temporaryPassword").GetString()!;

        Assert.False(string.IsNullOrWhiteSpace(tempPassword));

        using var login = Json(new { email, password = tempPassword });
        var (loginStatus, loginBody) = await Read(await fixture.CreateClient().PostAsync("/api/auth/login", login));

        Assert.Equal(HttpStatusCode.OK, loginStatus);

        // Và giao diện phải biết mà đưa họ thẳng tới màn đổi mật khẩu.
        Assert.True(loginBody.GetProperty("user").GetProperty("mustChangePassword").GetBoolean());
    }

    [Fact]
    public async Task NguoiTuDangKyWorkspace_KHONG_bi_bat_doi_mat_khau()
    {
        // Họ tự chọn mật khẩu của mình rồi. Bắt đổi ngay là bắt làm lại đúng việc vừa làm.
        var (_, _, body) = await NewWorkspace($"tu{Guid.NewGuid():N}"[..14]);

        Assert.False(body.GetProperty("user").GetProperty("mustChangePassword").GetBoolean());
    }

    [Fact]
    public async Task TaoTrungEmail_ThiTraVe409_ChuKhongPhai500()
    {
        // Ràng buộc UNIQUE ở database cũng chặn, nhưng nó ném ra một lỗi 500 khó hiểu.
        // Phép kiểm trong handler tồn tại để người dùng nhận được một câu tiếng Việt.
        var (token, tenantId, _) = await NewWorkspace($"trung{Guid.NewGuid():N}"[..14]);
        var client = ClientFor(token);
        var roleId = await RoleId(tenantId, "Member");

        string email = $"trung.{Guid.NewGuid():N}"[..20] + "@congty.vn";

        using var first = Json(new { fullName = "Người Một", email, roleId, mustChangePassword = true });
        await client.PostAsync("/api/users", first);

        using var second = Json(new { fullName = "Người Hai", email, roleId, mustChangePassword = true });
        var (status, body) = await Read(await client.PostAsync("/api/users", second));

        Assert.Equal(HttpStatusCode.Conflict, status);
        Assert.Equal("Email.Taken", body.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    // ── Danh sách ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DanhSachChiTraVeNguoiCuaWorkspaceMinh()
    {
        // ⭐ Đây là thứ chỉ database chứng minh được: bộ lọc theo tenant có thật sự áp
        // vào câu SQL hay không. Hỏng chỗ này thì quản trị viên công ty A đọc được danh
        // bạ công ty B — và không có gì trong giao diện lộ ra điều đó.
        var (tokenA, _, _) = await NewWorkspace($"a{Guid.NewGuid():N}"[..14]);
        var (_, _, bodyB) = await NewWorkspace($"b{Guid.NewGuid():N}"[..14]);

        string emailB = bodyB.GetProperty("user").GetProperty("email").GetString()!;

        var (status, body) = await Read(await ClientFor(tokenA).GetAsync("/api/users?pageSize=100"));

        Assert.Equal(HttpStatusCode.OK, status);

        var emails = body.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("email").GetString())
            .ToList();

        Assert.DoesNotContain(emailB, emails);
    }

    [Fact]
    public async Task DanhSachKemTenVaiTro_KhongPhaiMaVaiTro()
    {
        // Truy vấn thứ hai gom tên vai trò từ cột mảng uuid[]. Nếu nó hỏng thì cột "Vai trò"
        // hiện dấu gạch ngang cho mọi dòng — trông như dữ liệu thiếu, không như lỗi.
        var (token, tenantId, _) = await NewWorkspace($"vai{Guid.NewGuid():N}"[..14]);

        var (_, body) = await Read(await ClientFor(token).GetAsync("/api/users"));

        Assert.Equal("Owner", body.GetProperty("items")[0].GetProperty("roleName").GetString());
    }

    [Fact]
    public async Task LocTheoTrangThai_ChoNguoiChuaTungDangNhap()
    {
        var (token, tenantId, _) = await NewWorkspace($"loc{Guid.NewGuid():N}"[..14]);
        var client = ClientFor(token);
        var roleId = await RoleId(tenantId, "Member");

        string email = $"cho.{Guid.NewGuid():N}"[..18] + "@congty.vn";

        using var content = Json(new { fullName = "Người Chờ", email, roleId, mustChangePassword = true });
        await client.PostAsync("/api/users", content);

        // PendingFirstLogin = 2
        var (_, body) = await Read(await client.GetAsync("/api/users?status=2&pageSize=100"));

        var emails = body.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("email").GetString())
            .ToList();

        Assert.Equal([email], emails);
    }

    [Fact]
    public async Task CoTrangQuaLon_BiKeoVeTran_ChuKhongKeoCaBang()
    {
        var (token, tenantId, _) = await NewWorkspace($"tran{Guid.NewGuid():N}"[..13]);

        var (_, body) = await Read(await ClientFor(token).GetAsync("/api/users?pageSize=999999"));

        Assert.Equal(100, body.GetProperty("pageSize").GetInt32());
    }

    // ── Phân quyền ────────────────────────────────────────────────────────

    [Fact]
    public async Task KhongCoToken_ThiKhongXemDuocDanhSach()
    {
        using var response = await fixture.CreateClient().GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
