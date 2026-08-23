using ONoOffice.Identity.Domain;
using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.UnitTests.Domain;

public class RoleTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static Role TaoVaiTroTuChon() => Role.Create(TenantId, "Trợ lý nhân sự").Value;

    [Fact]
    public void Create_TaoVaiTroChuaCoQuyenNao()
    {
        var result = Role.Create(TenantId, "  Trợ lý nhân sự  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Trợ lý nhân sự", result.Value.Name);
        Assert.Equal(TenantId, result.Value.TenantId);
        Assert.False(result.Value.IsSystem);
        Assert.Empty(result.Value.Permissions);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_TuChoiTenRong(string name)
    {
        Assert.True(Role.Create(TenantId, name).IsFailure);
    }

    [Fact]
    public void Create_TuChoiTenantRong()
    {
        Assert.True(Role.Create(Guid.Empty, "Trợ lý").IsFailure);
    }

    // ── Cấp / thu quyền ─────────────────────────────────────────────────────

    [Fact]
    public void Grant_ThemQuyen()
    {
        var role = TaoVaiTroTuChon();

        Assert.True(role.Grant(Permissions.Employees.Read).IsSuccess);
        Assert.Equal(Permissions.Employees.Read, Assert.Single(role.Permissions));
    }

    // Đây là luật đáng giá nhất của lớp này. Gõ nhầm "employee.raed" mà vẫn cấp được
    // thì vai trò đó KHÔNG BAO GIỜ có tác dụng — và không có lỗi nào báo. Người dùng
    // chỉ thấy "sao tôi được cấp quyền rồi mà vẫn không vào được", còn người quản trị
    // mở lên nhìn thì thấy quyền nằm đó rành rành.
    [Fact]
    public void Grant_TuChoiQuyenKhongTonTai()
    {
        Assert.True(TaoVaiTroTuChon().Grant("employee.raed").IsFailure);
    }

    [Fact]
    public void Grant_TuChoiKhiDaCoQuyenDo()
    {
        var role = TaoVaiTroTuChon();
        role.Grant(Permissions.Employees.Read);

        Assert.True(role.Grant(Permissions.Employees.Read).IsFailure);
        Assert.Single(role.Permissions);
    }

    [Fact]
    public void Grant_ChuanHoaVeChuThuong()
    {
        var role = TaoVaiTroTuChon();

        role.Grant("EMPLOYEE.READ");

        Assert.Equal(Permissions.Employees.Read, Assert.Single(role.Permissions));
    }

    [Fact]
    public void Revoke_BoQuyen()
    {
        var role = TaoVaiTroTuChon();
        role.Grant(Permissions.Employees.Read);

        Assert.True(role.Revoke(Permissions.Employees.Read).IsSuccess);
        Assert.Empty(role.Permissions);
    }

    [Fact]
    public void Revoke_TuChoiKhiKhongCoQuyenDo()
    {
        Assert.True(TaoVaiTroTuChon().Revoke(Permissions.Employees.Read).IsFailure);
    }

    [Fact]
    public void Permissions_KhongSuaDuocTuBenNgoai()
    {
        var role = TaoVaiTroTuChon();

        Assert.False(role.Permissions is ICollection<string> { IsReadOnly: false });
    }

    // ── Vai trò hệ thống ────────────────────────────────────────────────────

    [Fact]
    public void CreateSystem_TaoVaiTroHeThongKemQuyen()
    {
        var role = Role.CreateSystem(TenantId, "Owner", Permissions.All).Value;

        Assert.True(role.IsSystem);
        Assert.Equal(Permissions.All.Count, role.Permissions.Count);
    }

    // Vai trò hệ thống là bất biến. Cho sửa thì một cú bấm nhầm có thể thu hết quyền
    // của Owner — và lúc đó KHÔNG CÒN AI trong workspace cấp lại được cho ai cả.
    [Fact]
    public void VaiTroHeThong_KhongCapThemQuyenDuoc()
    {
        var role = Role.CreateSystem(TenantId, "Member", [Permissions.Employees.Read]).Value;

        Assert.True(role.Grant(Permissions.Employees.Write).IsFailure);
    }

    [Fact]
    public void VaiTroHeThong_KhongThuQuyenDuoc()
    {
        var role = Role.CreateSystem(TenantId, "Member", [Permissions.Employees.Read]).Value;

        Assert.True(role.Revoke(Permissions.Employees.Read).IsFailure);
    }

    [Fact]
    public void VaiTroHeThong_KhongDoiTenDuoc()
    {
        var role = Role.CreateSystem(TenantId, "Member", [Permissions.Employees.Read]).Value;

        Assert.True(role.Rename("Thành viên").IsFailure);
    }

    [Fact]
    public void VaiTroTuChon_DoiTenDuoc()
    {
        var role = TaoVaiTroTuChon();

        Assert.True(role.Rename("Trợ lý nhân sự cấp cao").IsSuccess);
        Assert.Equal("Trợ lý nhân sự cấp cao", role.Name);
    }

    [Fact]
    public void CreateSystem_TuChoiQuyenKhongTonTai()
    {
        Assert.True(Role.CreateSystem(TenantId, "Member", ["khong.ton-tai"]).IsFailure);
    }

    // ── Kiểm quyền ──────────────────────────────────────────────────────────

    [Fact]
    public void Has_TraLoiDungVaKhongPhanBietHoaThuong()
    {
        var role = TaoVaiTroTuChon();
        role.Grant(Permissions.Employees.Read);

        Assert.True(role.Has("EMPLOYEE.READ"));
        Assert.False(role.Has(Permissions.Employees.Write));
    }
}
