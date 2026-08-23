using ONoOffice.Identity.Domain.ValueObjects;

namespace ONoOffice.Identity.UnitTests.Domain;

public class TenantCodeTests
{
    [Theory]
    [InlineData("acme", "acme")]
    [InlineData("ACME", "acme")]
    [InlineData("  cong-ty-abc  ", "cong-ty-abc")]
    [InlineData("nextx2026", "nextx2026")]
    public void Create_ChuanHoaVeChuThuong(string input, string expected)
    {
        var result = TenantCode.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]                                   // ngắn quá
    [InlineData("mot-ma-cong-ty-dai-qua-muc-cho-phep-that-su")] // dài quá
    [InlineData("có-dấu")]                               // ký tự ngoài a-z0-9-
    [InlineData("co khoang trang")]
    [InlineData("2acme")]                                // phải bắt đầu bằng chữ cái
    [InlineData("-acme")]
    [InlineData("acme-")]                                // không được kết thúc bằng gạch
    [InlineData("acme--corp")]                           // không có hai gạch liền
    [InlineData("acme_corp")]                            // gạch dưới không hợp lệ trong tên miền con
    public void Create_TuChoiMaSai(string input)
    {
        Assert.True(TenantCode.Create(input).IsFailure);
    }

    [Fact]
    public void HaiMaCungNoiDung_ThiBangNhau()
    {
        Assert.Equal(TenantCode.Create("acme").Value, TenantCode.Create("ACME").Value);
    }
}
