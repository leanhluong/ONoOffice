using ONoOffice.Identity.Domain.Entities;
using ONoOffice.Identity.Domain.Events;

namespace ONoOffice.Identity.UnitTests.Domain;

public class TenantTests
{
    private static Tenant TaoTenant() => Tenant.Create("acme", "Công ty ACME").Value;

    [Fact]
    public void Create_TaoWorkspaceDangHoatDong()
    {
        var result = Tenant.Create("acme", "Công ty ACME");

        Assert.True(result.IsSuccess);
        Assert.Equal("acme", result.Value.Code.Value);
        Assert.Equal("Công ty ACME", result.Value.Name);
        Assert.True(result.Value.IsActive);
    }

    // Workspace vừa tạo thì CHƯA có chủ — người chủ được gán ngay sau đó, khi tài khoản
    // đầu tiên được tạo. Đây là cái vòng "gà và trứng": tenant cần chủ, mà chủ lại cần
    // thuộc về một tenant. Giải bằng cách cho phép trống ở đúng một khoảnh khắc này.
    [Fact]
    public void Create_ChuaCoChuSoHuu()
    {
        Assert.Null(TaoTenant().OwnerUserId);
    }

    [Fact]
    public void Create_PhatSuKienTenantCreated()
    {
        var tenant = TaoTenant();

        var raised = Assert.Single(tenant.DomainEvents);
        var created = Assert.IsType<TenantCreated>(raised);
        Assert.Equal(tenant.Id, created.TenantId);
        Assert.Equal("acme", created.Code);
    }

    [Fact]
    public void Create_TuChoiMaSai()
    {
        Assert.True(Tenant.Create("x", "Công ty ACME").IsFailure);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_TuChoiTenRong(string name)
    {
        Assert.True(Tenant.Create("acme", name).IsFailure);
    }

    // ── Quyền sở hữu ────────────────────────────────────────────────────────

    [Fact]
    public void AssignOwner_GanChuChoWorkspaceChuaCoChu()
    {
        var tenant = TaoTenant();
        var ownerId = Guid.NewGuid();

        var result = tenant.AssignOwner(ownerId);

        Assert.True(result.IsSuccess);
        Assert.Equal(ownerId, tenant.OwnerUserId);
    }

    // Đã có chủ rồi thì phải đi đường CHUYỂN NHƯỢNG, không được gán đè.
    // Gán đè im lặng nghĩa là một lỗi lập trình có thể lấy mất quyền sở hữu
    // của người khác mà không để lại dấu vết nào.
    [Fact]
    public void AssignOwner_TuChoiKhiDaCoChu()
    {
        var tenant = TaoTenant();
        tenant.AssignOwner(Guid.NewGuid());

        Assert.True(tenant.AssignOwner(Guid.NewGuid()).IsFailure);
    }

    [Fact]
    public void TransferOwnership_DoiChuVaPhatSuKien()
    {
        var tenant = TaoTenant();
        var chuCu = Guid.NewGuid();
        var chuMoi = Guid.NewGuid();
        tenant.AssignOwner(chuCu);
        tenant.ClearDomainEvents();

        var result = tenant.TransferOwnership(chuMoi);

        Assert.True(result.IsSuccess);
        Assert.Equal(chuMoi, tenant.OwnerUserId);

        var transferred = Assert.IsType<TenantOwnershipTransferred>(Assert.Single(tenant.DomainEvents));
        Assert.Equal(chuCu, transferred.PreviousOwnerId);
        Assert.Equal(chuMoi, transferred.NewOwnerId);
    }

    [Fact]
    public void TransferOwnership_TuChoiKhiChuaCoChu()
    {
        Assert.True(TaoTenant().TransferOwnership(Guid.NewGuid()).IsFailure);
    }

    [Fact]
    public void TransferOwnership_TuChoiKhiChuyenChoChinhNguoiDangSoHuu()
    {
        var tenant = TaoTenant();
        var owner = Guid.NewGuid();
        tenant.AssignOwner(owner);

        Assert.True(tenant.TransferOwnership(owner).IsFailure);
    }

    // ── Ngừng hoạt động ─────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_TatWorkspaceVaPhatSuKien()
    {
        var tenant = TaoTenant();
        tenant.ClearDomainEvents();

        var result = tenant.Deactivate();

        Assert.True(result.IsSuccess);
        Assert.False(tenant.IsActive);
        Assert.IsType<TenantDeactivated>(Assert.Single(tenant.DomainEvents));
    }

    // Tắt hai lần là lệnh thứ hai không có ý nghĩa gì. Trả về thất bại thay vì lặng lẽ
    // bỏ qua, để chỗ gọi biết là giả định của mình đã sai.
    [Fact]
    public void Deactivate_TuChoiKhiDaTat()
    {
        var tenant = TaoTenant();
        tenant.Deactivate();

        Assert.True(tenant.Deactivate().IsFailure);
    }

    [Fact]
    public void Activate_BatLaiWorkspaceDaTat()
    {
        var tenant = TaoTenant();
        tenant.Deactivate();

        Assert.True(tenant.Activate().IsSuccess);
        Assert.True(tenant.IsActive);
    }

    [Fact]
    public void Activate_TuChoiKhiDangHoatDong()
    {
        Assert.True(TaoTenant().Activate().IsFailure);
    }

    [Fact]
    public void Rename_DoiTenHienThi()
    {
        var tenant = TaoTenant();

        Assert.True(tenant.Rename("Công ty ACME Việt Nam").IsSuccess);
        Assert.Equal("Công ty ACME Việt Nam", tenant.Name);
    }

    [Fact]
    public void Rename_TuChoiTenRong()
    {
        Assert.True(TaoTenant().Rename("  ").IsFailure);
    }
}
