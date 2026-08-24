using ONoOffice.Identity.Infrastructure.Security;

namespace ONoOffice.Identity.UnitTests.Infrastructure;

/// <summary>
/// Mật khẩu tạm cho tài khoản do quản trị viên tạo hộ.
///
/// Nó có một ràng buộc mà mật khẩu bình thường không có: <b>phải đọc được qua điện thoại
/// và gõ lại đúng</b>. Người tạo sẽ nhắn nó qua Zalo, đọc qua điện thoại, hoặc viết ra
/// giấy. Một chuỗi base64 32 ký tự thì đúng về mật mã và hỏng về thực tế.
/// </summary>
public class TemporaryPasswordGeneratorTests
{
    private readonly TemporaryPasswordGenerator _generator = new();

    [Fact]
    public void DuDaiDeQuaDuocLuatMatKhauCuaHeThong()
    {
        // Luật tối thiểu là 10 ký tự (RegisterWorkspaceCommandValidator). Sinh ra một mật
        // khẩu mà chính hệ thống từ chối thì người dùng không đăng nhập nổi, và người tạo
        // không hiểu vì sao.
        Assert.True(_generator.Generate().Length >= 10);
    }

    [Fact]
    public void KhongChuaKyTuDE_DOC_NHAM()
    {
        // 0 với O, 1 với l với I: đọc qua điện thoại là gõ sai. Người nhận thử ba lần rồi
        // gọi lại hỏi, và cả hai bên mất mười phút.
        var chuoi = string.Concat(Enumerable.Range(0, 200).Select(_ => _generator.Generate()));

        Assert.DoesNotContain('0', chuoi);
        Assert.DoesNotContain('O', chuoi);
        Assert.DoesNotContain('1', chuoi);
        Assert.DoesNotContain('l', chuoi);
        Assert.DoesNotContain('I', chuoi);
    }

    [Fact]
    public void HaiLanSinhLienTiep_KhongBaoGioTrungNhau()
    {
        // Đây là phép kiểm rẻ tiền cho một lỗi đắt: bộ sinh gieo từ đồng hồ sẽ trả cùng
        // một chuỗi cho hai tài khoản tạo trong cùng một mili-giây.
        var daSinh = new HashSet<string>();

        for (var i = 0; i < 500; i++)
        {
            Assert.True(daSinh.Add(_generator.Generate()), "sinh ra hai mật khẩu trùng nhau");
        }
    }

    [Fact]
    public void CoDauNGAT_deDocTheoTungCUM()
    {
        // "k7np-2wqx-hs4m" đọc dễ hơn hẳn "k7np2wqxhs4m". Cùng số ký tự, khác nhau ở chỗ
        // người đọc có mất dấu giữa chừng hay không.
        Assert.Contains('-', _generator.Generate());
    }
}
