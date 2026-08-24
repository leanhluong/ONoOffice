using ONoOffice.Org.Domain;
using ONoOffice.Org.Domain.Entities;

namespace ONoOffice.Org.UnitTests.Domain;

/// <summary>
/// Hồ sơ nhân viên — thứ mà <c>Org</c> sở hữu.
///
/// <b>Không nhầm với tài khoản đăng nhập.</b> Cùng một con người, hai khái niệm: đổi mật
/// khẩu là chuyện của <c>Identity</c>, điều chuyển phòng ban là chuyện ở đây. Chúng đổi
/// vì những lý do khác nhau, vào những lúc khác nhau.
/// </summary>
public class EmployeeTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static Employee Mot(string code = "NV001", string name = "Lê Anh Lượng") =>
        Employee.Create(Tenant, code, name, "an.luong@congty.vn", "0900000000").Value;

    [Fact]
    public void Tao_DuThongTin_ThiThanhCong()
    {
        var result = Employee.Create(Tenant, "NV001", "Lê Anh Lượng", "an.luong@congty.vn", "0900000000");

        Assert.True(result.IsSuccess);
        Assert.Equal("NV001", result.Value.Code);
        Assert.Equal("Lê Anh Lượng", result.Value.FullName);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public void Tao_ThiCHUAThuocPhongBanNao()
    {
        // Người mới vào chưa được xếp phòng là chuyện BÌNH THƯỜNG, không phải trạng thái
        // lỗi. Ép phải có phòng ngay thì HR buộc phải bịa một phòng "Chưa phân công".
        Assert.Null(Mot().DepartmentId);
    }

    [Fact]
    public void Tao_ThiCHUANoiVoiTaiKhoanNao()
    {
        // Có hồ sơ trước, cấp tài khoản sau — đó là thứ tự thật ở mọi công ty.
        Assert.Null(Mot().UserId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Tao_ThieuMaNhanVien_ThiTuChoi(string? code)
    {
        var result = Employee.Create(Tenant, code, "Lê Anh Lượng", "a.b@congty.vn", null);

        Assert.Equal(OrgErrors.Employees.CodeEmpty.Code, result.Error.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Tao_ThieuHoTen_ThiTuChoi(string? name)
    {
        var result = Employee.Create(Tenant, "NV001", name, "a.b@congty.vn", null);

        Assert.Equal(OrgErrors.Employees.FullNameEmpty.Code, result.Error.Code);
    }

    [Fact]
    public void Tao_MaNhanVien_ThiVIETHOAVaCatKhoangTrang()
    {
        var result = Employee.Create(Tenant, "  nv001  ", "Lê Anh Lượng", "a.b@congty.vn", null);

        // Mã nhân viên là thứ người ta gõ tay khi tra cứu. Không chuẩn hoá thì "nv001"
        // và "NV001" thành hai người, và ràng buộc UNIQUE không bắt được.
        Assert.Equal("NV001", result.Value.Code);
    }

    [Fact]
    public void Tao_EmailSaiDinhDang_ThiTuChoi()
    {
        var result = Employee.Create(Tenant, "NV001", "Lê Anh Lượng", "khong-phai-email", null);

        Assert.Equal(OrgErrors.WorkEmails.Invalid.Code, result.Error.Code);
    }

    /// <summary>
    /// Email liên hệ được phép BỎ TRỐNG.
    ///
    /// Khác hẳn email đăng nhập bên <c>Identity</c> — cái đó là danh tính, bắt buộc phải
    /// có. Cái này chỉ là thông tin danh bạ: công nhân xưởng có thể không có email công ty.
    /// </summary>
    [Fact]
    public void Tao_KhongCoEmail_ThiVANDuoc()
    {
        var result = Employee.Create(Tenant, "NV001", "Nguyễn Văn X", null, "0900000000");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.WorkEmail);
    }

    // ── Điều chuyển phòng ban ────────────────────────────────────────────────

    [Fact]
    public void DieuChuyen_ThiDoiPhong()
    {
        var nv = Mot();
        var phong = Guid.NewGuid();

        Assert.True(nv.TransferTo(phong).IsSuccess);
        Assert.Equal(phong, nv.DepartmentId);
    }

    [Fact]
    public void DieuChuyen_VeKhongPhongNao_ThiDuoc()
    {
        var nv = Mot();
        nv.TransferTo(Guid.NewGuid());

        Assert.True(nv.TransferTo(null).IsSuccess);
        Assert.Null(nv.DepartmentId);
    }

    [Fact]
    public void DieuChuyen_VaoDungPhongDangO_ThiTuChoi()
    {
        var nv = Mot();
        var phong = Guid.NewGuid();
        nv.TransferTo(phong);

        // Cho qua im lặng thì nhật ký thay đổi (lát 2) sẽ đầy những dòng "điều chuyển"
        // mà không có gì đổi.
        Assert.Equal(OrgErrors.Employees.AlreadyInThatDepartment.Code, nv.TransferTo(phong).Error.Code);
    }

    // ── Nghỉ việc ────────────────────────────────────────────────────────────

    [Fact]
    public void ChoNghiViec_ThiKhongConHoatDong_VaGhiNgay()
    {
        var nv = Mot();
        var ngay = new DateOnly(2026, 8, 24);

        Assert.True(nv.Leave(ngay).IsSuccess);
        Assert.False(nv.IsActive);
        Assert.Equal(ngay, nv.LeftOn);
    }

    [Fact]
    public void ChoNghiViec_HaiLan_ThiTuChoi()
    {
        var nv = Mot();
        nv.Leave(new DateOnly(2026, 8, 24));

        Assert.Equal(
            OrgErrors.Employees.AlreadyLeft.Code,
            nv.Leave(new DateOnly(2026, 9, 1)).Error.Code);
    }

    [Fact]
    public void NhanLaiViec_ThiXoaNgayNghi()
    {
        var nv = Mot();
        nv.Leave(new DateOnly(2026, 8, 24));

        Assert.True(nv.Reinstate().IsSuccess);
        Assert.True(nv.IsActive);
        Assert.Null(nv.LeftOn);
    }

    [Fact]
    public void NhanLaiViec_KhiDangLam_ThiTuChoi()
    {
        Assert.Equal(OrgErrors.Employees.NotLeft.Code, Mot().Reinstate().Error.Code);
    }

    // ── Nối với tài khoản đăng nhập ──────────────────────────────────────────

    [Fact]
    public void NoiVoiTaiKhoan_ThiNho()
    {
        var nv = Mot();
        var user = Guid.NewGuid();

        Assert.True(nv.LinkAccount(user).IsSuccess);
        Assert.Equal(user, nv.UserId);
    }

    /// <summary>
    /// Đã nối rồi thì KHÔNG gán đè.
    ///
    /// Gán đè im lặng nghĩa là một lỗi lập trình có thể nối hồ sơ của người này sang tài
    /// khoản của người khác — và từ đó mọi thao tác của họ bị ghi tên nhầm người.
    /// </summary>
    [Fact]
    public void NoiVoiTaiKhoanKhac_KhiDaCo_ThiNO()
    {
        var nv = Mot();
        nv.LinkAccount(Guid.NewGuid());

        Assert.Equal(OrgErrors.Employees.AlreadyLinked.Code, nv.LinkAccount(Guid.NewGuid()).Error.Code);
    }

    [Fact]
    public void GoTaiKhoan_RoiNoiLai_ThiDuoc()
    {
        var nv = Mot();
        nv.LinkAccount(Guid.NewGuid());

        Assert.True(nv.UnlinkAccount().IsSuccess);
        Assert.True(nv.LinkAccount(Guid.NewGuid()).IsSuccess);
    }
}
