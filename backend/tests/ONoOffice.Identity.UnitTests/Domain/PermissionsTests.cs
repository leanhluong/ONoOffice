using System.Text.RegularExpressions;
using ONoOffice.Identity.Domain;

namespace ONoOffice.Identity.UnitTests.Domain;

public class PermissionsTests
{
    [Fact]
    public void All_KhongRong()
    {
        Assert.NotEmpty(Permissions.All);
    }

    // Mọi quyền phải theo đúng khuôn "vùng.hành-động", chữ thường.
    // Không có luật này thì sáu tháng sau sẽ có "employee.read", "Employee.Write",
    // "employees_delete" cùng tồn tại — và không ai dám xoá cái nào vì sợ gãy chỗ khác.
    [Fact]
    public void MoiQuyenDeuTheoKhuonVungChamHanhDong()
    {
        var khuon = new Regex("^[a-z]+(?:-[a-z]+)*\\.[a-z]+(?:-[a-z]+)*$");

        var sai = Permissions.All.Where(p => !khuon.IsMatch(p)).ToList();

        Assert.True(sai.Count == 0, $"Quyền đặt tên sai khuôn: {string.Join(", ", sai)}");
    }

    // Trùng tên quyền là lỗi âm thầm: hai nơi tưởng đang nói về hai quyền khác nhau,
    // thực ra là một — và thu hồi một cái là thu hồi luôn cái kia.
    [Fact]
    public void KhongCoQuyenNaoBiTrung()
    {
        var tatCa = Permissions.Declared().ToList();

        Assert.Equal(tatCa.Count, tatCa.Distinct().Count());
    }

    [Fact]
    public void Contains_NhanDienDuocQuyenHopLe()
    {
        Assert.True(Permissions.Contains(Permissions.Employees.Read));
        Assert.True(Permissions.Contains("EMPLOYEE.READ"));   // không phân biệt hoa thường
    }

    [Fact]
    public void Contains_TuChoiQuyenKhongTonTai()
    {
        Assert.False(Permissions.Contains("employee.bay-len-troi"));
        Assert.False(Permissions.Contains(""));
        Assert.False(Permissions.Contains(null));
    }
}
