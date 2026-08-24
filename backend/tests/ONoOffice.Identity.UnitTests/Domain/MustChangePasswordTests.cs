using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.UnitTests.Domain;

/// <summary>
/// Cờ "buộc đổi mật khẩu ở lần đăng nhập đầu".
///
/// Sinh ra từ một ràng buộc rất cụ thể của lát này: <b>chưa có dịch vụ gửi email</b>, nên
/// quản trị viên tạo tài khoản hộ và tự đưa mật khẩu tạm cho đồng nghiệp — qua tin nhắn,
/// qua lời nói, qua mẩu giấy. Mật khẩu đó coi như đã lộ ngay từ lúc sinh ra.
///
/// Không bắt đổi thì nó nằm nguyên đó nhiều tháng, và ai từng nhìn qua vai đều vào được.
/// </summary>
public class MustChangePasswordTests
{
    private static User NewUser() =>
        User.Create(Guid.NewGuid(), "an@congty.vn", "bam::mat-khau", "Nguyễn An").Value;

    [Fact]
    public void TaiKhoanBinhThuong_KhongBiBatDoiMatKhau()
    {
        // Người tự đăng ký workspace đã tự chọn mật khẩu rồi — bắt họ đổi ngay là vô nghĩa.
        var user = NewUser();

        Assert.False(user.MustChangePassword);
    }

    [Fact]
    public void QuanTriTaoHo_ThiBiBatDoiMatKhau()
    {
        var user = NewUser();

        user.RequirePasswordChange();

        Assert.True(user.MustChangePassword);
    }

    [Fact]
    public void DoiMatKhauXong_ThiCoTuTat()
    {
        // Nếu không tự tắt thì người dùng đổi mật khẩu xong vẫn bị hỏi lại mãi mãi —
        // và họ sẽ đổi đi đổi lại vài lần trước khi kết luận là app hỏng.
        var user = NewUser();
        user.RequirePasswordChange();

        user.ChangePassword("bam::mat-khau-moi");

        Assert.False(user.MustChangePassword);
    }

    [Fact]
    public void DoiMatKhauHONG_Thi_KHONG_tat_co()
    {
        // Băm rỗng bị từ chối, mật khẩu giữ nguyên — nên cờ cũng phải giữ nguyên. Tắt cờ ở
        // đây là mở cửa: gửi một request hỏng là thoát được yêu cầu đổi mật khẩu.
        var user = NewUser();
        user.RequirePasswordChange();

        var result = user.ChangePassword("   ");

        Assert.True(result.IsFailure);
        Assert.True(user.MustChangePassword);
    }
}
