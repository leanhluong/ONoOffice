using Luong.Kernel.Abstractions;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Application.Users.ResetPassword;
using ONoOffice.Identity.Domain;
using ONoOffice.Identity.Domain.Entities;
using ONoOffice.Identity.UnitTests.Fakes;

namespace ONoOffice.Identity.UnitTests.Application;

/// <summary>
/// Quản trị viên đặt lại mật khẩu HỘ một đồng nghiệp.
///
/// ═══════════════════════════════════════════════════════════════════════
///  VÌ SAO USE CASE NÀY BẮT BUỘC PHẢI CÓ
/// ═══════════════════════════════════════════════════════════════════════
///
/// Chưa nối dịch vụ gửi email, nên "Quên mật khẩu" ở màn đăng nhập vẫn hiện *đang phát
/// triển*. Nghĩa là <b>người quên mật khẩu hiện không có đường nào quay lại</b> — không
/// phải bất tiện, mà là mất hẳn quyền truy cập. Đây là lối thoát duy nhất cho tới khi có
/// mail.
///
/// ═══════════════════════════════════════════════════════════════════════
///  VÀ VÌ SAO NÓ NGUY HIỂM HƠN VẺ NGOÀI
/// ═══════════════════════════════════════════════════════════════════════
///
/// Đặt lại mật khẩu của ai đó = <b>đăng nhập được dưới danh nghĩa người đó</b>. Admin có
/// đủ 11/12 quyền; thứ DUY NHẤT họ không có là chuyển nhượng quyền sở hữu. Cho phép Admin
/// đặt lại mật khẩu của chủ sở hữu thì họ nhận mật khẩu tạm, đăng nhập thành chủ sở hữu,
/// rồi tự chuyển nhượng workspace cho mình — và ranh giới Admin ↔ Owner biến mất hoàn
/// toàn, dù bảng phân quyền vẫn trông đúng.
/// </summary>
public class ResetUserPasswordCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ActorId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private sealed class FakeHasher : IPasswordHasher
    {
        public string Hash(string password) => $"bam::{password}";

        public bool Verify(string password, string passwordHash) => passwordHash == Hash(password);
    }

    private sealed class FakeGenerator : ITemporaryPasswordGenerator
    {
        public string Generate() => "k7np-2wqx-hs4m";
    }

    private readonly FakeUserRepository _users = new();
    private readonly FakeTenantRepository _tenants = new();
    private readonly FakeRefreshTokenRepository _refreshTokens = new();
    private readonly FakeCurrentUser _actor = new() { UserId = ActorId };
    private readonly FakeClock _clock = new();

    private ResetUserPasswordCommandHandler Handler() =>
        new(_users, _tenants, _refreshTokens, new FakeHasher(), new FakeGenerator(), _actor, _clock);

    private User GiveUser()
    {
        var user = User.Create(TenantId, "an@congty.vn", "bam::cu", "Nguyễn An").Value;

        _users.Existing = user;

        return user;
    }

    // ══════════════════════════════════════════════════════════════════
    // Đường chính
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DatLai_ThiLuuBAM_cua_mat_khau_tam_va_TRA_VE_ban_tho()
    {
        var user = GiveUser();

        var result = await Handler().Handle(new ResetUserPasswordCommand(user.Id), default);

        Assert.True(result.IsSuccess);

        // Trả về bản THÔ đúng một lần cho người đưa tận tay, nhưng thứ NẰM LẠI trong
        // database phải là bản băm. Lẫn hai thứ này là rò mật khẩu ra bảng người dùng.
        Assert.Equal("k7np-2wqx-hs4m", result.Value.TemporaryPassword);
        Assert.Equal("bam::k7np-2wqx-hs4m", user.PasswordHash);
    }

    /// <summary>
    /// Buộc đổi ở lần đăng nhập đầu.
    ///
    /// Mật khẩu tạm đi qua Zalo, tin nhắn, hoặc lời nói — nó phải chết ngay khi dùng xong.
    /// Thiếu cờ này thì nó sống mãi, và cả người đặt lại lẫn bất kỳ ai đọc lỏm tin nhắn
    /// đều đăng nhập được suốt.
    /// </summary>
    [Fact]
    public async Task DatLai_ThiBAT_co_buoc_doi_mat_khau()
    {
        var user = GiveUser();

        await Handler().Handle(new ResetUserPasswordCommand(user.Id), default);

        Assert.True(user.MustChangePassword);
    }

    /// <summary>
    /// Thu hồi MỌI phiên của người đó.
    ///
    /// Ca dùng thật hay gặp nhất của việc đặt lại mật khẩu là <b>nghi ngờ bị chiếm tài
    /// khoản</b>. Đổi mật khẩu mà không thu hồi vé gia hạn thì kẻ đang ngồi trong phiên cũ
    /// vẫn ở nguyên đó thêm 30 ngày — tức là thao tác này trông như đã cứu, mà không cứu gì.
    /// </summary>
    [Fact]
    public async Task DatLai_ThiTHU_HOI_moi_phien_cua_nguoi_do()
    {
        var user = GiveUser();

        await Handler().Handle(new ResetUserPasswordCommand(user.Id), default);

        Assert.Equal(user.Id, _refreshTokens.RevokedFor);
    }

    // ══════════════════════════════════════════════════════════════════
    // Hai cửa chặn
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// KHÔNG đặt lại được mật khẩu của chủ sở hữu — trừ khi chính chủ sở hữu làm.
    ///
    /// Đây là phép kiểm quan trọng nhất tệp này. Xem chú thích đầu lớp: thiếu nó thì Admin
    /// leo thẳng lên Owner, và bảng phân quyền vẫn trông đúng trong khi ranh giới đã mất.
    /// </summary>
    [Fact]
    public async Task ADMIN_DatLaiMatKhauCuaCHU_SO_HUU_ThiTuChoi()
    {
        var user = GiveUser();

        _tenants.OwnerUserId = user.Id;

        var result = await Handler().Handle(new ResetUserPasswordCommand(user.Id), default);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.Users.CannotResetOwnerPassword, result.Error);

        // Và mật khẩu phải còn NGUYÊN. Trả lỗi mà vẫn kịp ghi đè thì lời từ chối là vô nghĩa.
        Assert.Equal("bam::cu", user.PasswordHash);
    }

    /// <summary>
    /// Chủ sở hữu tự đặt lại mật khẩu của MÌNH thì được — không có ai để leo lên nữa.
    ///
    /// Chặn cả ca này thì chủ sở hữu quên mật khẩu là workspace kẹt vĩnh viễn, và cách sửa
    /// duy nhất là can thiệp thẳng vào database.
    /// </summary>
    [Fact]
    public async Task CHU_SO_HUU_TuDatLaiCuaChinhMinh_ThiDuoc()
    {
        var user = GiveUser();

        _tenants.OwnerUserId = user.Id;
        _actor.UserId = user.Id;

        var result = await Handler().Handle(new ResetUserPasswordCommand(user.Id), default);

        Assert.True(result.IsSuccess);
    }

    /// <summary>
    /// Người THƯỜNG không tự đặt lại mật khẩu của mình qua đường này.
    ///
    /// Đổi mật khẩu của chính mình đã có <c>POST /api/me/password</c>, và nó đòi <b>mật
    /// khẩu hiện tại</b>. Cho phép đi vòng qua đây là bỏ hẳn phép kiểm đó: một máy bỏ quên
    /// lúc đang đăng nhập là đủ để người khác chiếm hẳn tài khoản.
    ///
    /// Chủ sở hữu là ngoại lệ vì họ không có đường nào khác — xem phép kiểm ngay trên.
    /// </summary>
    [Fact]
    public async Task NguoiThuong_TuDatLaiCuaChinhMinh_ThiTuChoi()
    {
        var user = GiveUser();

        _actor.UserId = user.Id;

        var result = await Handler().Handle(new ResetUserPasswordCommand(user.Id), default);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.Users.CannotResetOwnPassword, result.Error);
    }

    [Fact]
    public async Task KhongTimThayTaiKhoan_ThiTraVe404()
    {
        var result = await Handler().Handle(new ResetUserPasswordCommand(Guid.NewGuid()), default);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.Users.NotFound, result.Error);
    }
}
