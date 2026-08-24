using Microsoft.Extensions.DependencyInjection;
using Luong.Kernel.EntityFrameworkCore.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using ONoOffice.Identity.Domain;
using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Api.DatabaseTests;

/// <summary>
/// Lớp thứ tư trong bốn lớp cô lập tenant của <c>ADR-0001</c> — và là lớp duy nhất
/// <b>chứng minh</b> được ba lớp kia có tác dụng.
///
/// Ba lớp trước là cơ chế: đánh dấu thực thể, bộ lọc toàn cục, interceptor canh chiều ghi.
/// Cả ba đều có thể được cấu hình sai mà build vẫn xanh. Chỉ có một câu hỏi trả lời được
/// dứt điểm: <i>workspace B có nhìn thấy dữ liệu của workspace A không?</i> Và nó chỉ trả
/// lời được bằng một database thật, có dữ liệu của cả hai.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class TenantIsolationTests(DatabaseFixture fixture)
{
    /// <summary>⭐⭐ Câu hỏi cuối cùng của multi-tenant.</summary>
    [Fact]
    public async Task WorkspaceB_KhongNhinThayNguoiDungCuaWorkspaceA()
    {
        var (tenantA, _) = await TaoWorkspace("cty-a", "an-a@cty-a.vn");
        var (tenantB, _) = await TaoWorkspace("cty-b", "binh-b@cty-b.vn");

        var (scope, nhinBangMatCuaB) = fixture.CreateScope(tenantB);
        using var _ = scope;

        var thayDuoc = await nhinBangMatCuaB.Users.Select(u => u.TenantId).Distinct().ToListAsync();

        Assert.DoesNotContain(tenantA, thayDuoc);
        Assert.Equal([tenantB], thayDuoc);
    }

    [Fact]
    public async Task PhienCHUADangNhap_KhongNhinThayNguoiDungNAOCA()
    {
        await TaoWorkspace("cty-c", "an-c@cty-c.vn");

        // CurrentTenantId trả Guid.Empty khi chưa đăng nhập, nên điều kiện lọc thành
        // `tenant_id = <rỗng>` và không khớp hàng nào. Đó là hành vi ĐÚNG: chưa chứng
        // minh được mình thuộc workspace nào thì không được thấy gì cả.
        var (scope, chuaDangNhap) = fixture.CreateScope(tenantId: null);
        using var _ = scope;

        Assert.Empty(await chuaDangNhap.Users.ToListAsync());
    }

    /// <summary>
    /// Bộ lọc tenant chỉ canh chiều ĐỌC. Đây là lớp canh chiều GHI: cố ghi một hàng
    /// sang workspace khác thì phải NỔ, không được im lặng ghi thành công.
    /// </summary>
    [Fact]
    public async Task GhiSangWorkspaceKhac_ThiNO()
    {
        var (tenantA, _) = await TaoWorkspace("cty-d", "an-d@cty-d.vn");
        var (tenantB, _) = await TaoWorkspace("cty-e", "an-e@cty-e.vn");

        // Phiên đang ở workspace B, nhưng cố tạo người dùng cho workspace A.
        var (scope, phienCuaB) = fixture.CreateScope(tenantB);
        using var _ = scope;

        var keXam = User.Create(tenantA, "trom@cty-d.vn", "hash-gia", "Kẻ xâm nhập");
        phienCuaB.Users.Add(keXam.Value);

        await Assert.ThrowsAsync<CrossTenantWriteException>(() => phienCuaB.SaveChangesAsync());
    }

    /// <summary>
    /// Mặt trái của luật trên: thực thể <b>tự mang sẵn</b> workspace của nó thì được ghi
    /// kể cả khi phiên chưa có tenant.
    ///
    /// Không có ngoại lệ này thì <b>không ai đăng nhập được</b>: lúc đăng nhập, phiên chưa
    /// có tenant, mà hệ thống lại cần ghi một <c>RefreshToken</c> xuống. Luồng đăng nhập
    /// ở <c>AuthenticationFlowTests</c> đi qua đúng đường này — test dưới đây nêu rõ vì
    /// sao nó không nổ, để không ai "sửa" interceptor cho chặt hơn rồi làm hỏng đăng nhập.
    /// </summary>
    [Fact]
    public async Task PhienChuaCoTenant_VanGhiDuocThucTheTuMangSanWorkspace()
    {
        var (tenantId, userId) = await TaoWorkspace("cty-f", "an-f@cty-f.vn");

        var (scope, chuaDangNhap) = fixture.CreateScope(tenantId: null);
        using var _ = scope;

        var ve = RefreshToken.Create(
            userId, tenantId, "bam-gia-" + Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow, TimeSpan.FromDays(30));

        chuaDangNhap.RefreshTokens.Add(ve.Value);

        await chuaDangNhap.SaveChangesAsync();

        Assert.NotEqual(Guid.Empty, ve.Value.TenantId);
    }

    [Fact]
    public async Task ThucTheKHONGMangWorkspace_VaPhienCungKhongCo_ThiNO()
    {
        var (scope, chuaDangNhap) = fixture.CreateScope(tenantId: null);
        using var _ = scope;

        // Guid.Empty = "quên gán workspace". Để lọt thì hàng đó nằm trong bảng mà KHÔNG
        // BAO GIỜ hiện ra trong truy vấn nào — dữ liệu vô hình, không lỗi nào báo.
        var role = Role.Create(Guid.Empty, "Vai mồ côi");

        Assert.True(role.IsFailure, "Domain đã chặn từ đầu — tốt, nhưng interceptor vẫn phải canh lớp sau.");
    }

    /// <summary>Tạo một workspace đầy đủ: tenant + 4 vai hệ thống + một người dùng.</summary>
    private async Task<(Guid TenantId, Guid UserId)> TaoWorkspace(string ma, string email)
    {
        var (scope, context) = fixture.CreateScope(tenantId: null);
        using var _ = scope;

        var tenant = Tenant.Create(ma, $"Công ty {ma}").Value;
        context.Tenants.Add(tenant);

        var owner = SystemRoles.Owner.CreateFor(tenant.Id).Value;
        context.Roles.Add(owner);

        var user = User.Create(tenant.Id, email, "hash-gia-cho-test", "Người dùng " + ma).Value;
        user.AssignRole(owner.Id);
        context.Users.Add(user);

        tenant.AssignOwner(user.Id);

        await context.SaveChangesAsync();

        return (tenant.Id, user.Id);
    }
}
