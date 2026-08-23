using Luong.Kernel.Primitives;
using ONoOffice.Identity.Domain.ValueObjects;

namespace ONoOffice.Identity.UnitTests.Domain;

public class EmailTests
{
    // Chuẩn hoá ngay lúc tạo. Không chuẩn hoá thì "An@Gmail.com" và "an@gmail.com"
    // là hai bản ghi khác nhau trong DB — và người dùng sẽ đăng ký được hai lần
    // cùng một email, rồi lần đăng nhập sau không biết mình là ai.
    [Theory]
    [InlineData("an@gmail.com", "an@gmail.com")]
    [InlineData("An@Gmail.COM", "an@gmail.com")]
    [InlineData("   an@gmail.com   ", "an@gmail.com")]
    public void Create_ChuanHoaVeChuThuongVaCatKhoangTrang(string input, string expected)
    {
        var result = Email.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_TuChoiChuoiRong(string? input)
    {
        var result = Email.Create(input!);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Theory]
    [InlineData("khong-phai-email")]
    [InlineData("thieu-phan-sau@")]
    [InlineData("@thieu-phan-truoc.com")]
    [InlineData("co khoang trang@gmail.com")]
    [InlineData("hai@@dau.com")]
    [InlineData("khong-co-dau-cham@localhost")]
    public void Create_TuChoiDangSai(string input)
    {
        Assert.True(Email.Create(input).IsFailure);
    }

    // 254 ký tự là trần theo RFC 5321. Không chặn ở đây thì nó đi thẳng xuống DB
    // và nổ ở tầng hạ tầng — nơi thông báo lỗi chẳng nói được gì cho người dùng.
    [Fact]
    public void Create_TuChoiEmailQuaDai()
    {
        string qua_dai = new string('a', 250) + "@gmail.com";

        Assert.True(Email.Create(qua_dai).IsFailure);
    }

    // Đối tượng giá trị so sánh bằng NỘI DUNG, không bằng ô nhớ.
    [Fact]
    public void HaiEmailCungNoiDung_ThiBangNhau()
    {
        var a = Email.Create("an@gmail.com").Value;
        var b = Email.Create("AN@GMAIL.COM").Value;

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_TraVeChinhDiaChi()
    {
        Assert.Equal("an@gmail.com", Email.Create("an@gmail.com").Value.ToString());
    }
}
