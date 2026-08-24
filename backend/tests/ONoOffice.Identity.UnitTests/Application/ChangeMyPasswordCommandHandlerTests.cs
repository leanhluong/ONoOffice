using Luong.Kernel.Abstractions;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.UnitTests.Fakes;
using ONoOffice.Identity.Application.Me.ChangePassword;
using ONoOffice.Identity.Domain;
using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.UnitTests.Application;

/// <summary>
/// Người dùng tự đổi mật khẩu của mình.
///
/// Use case ngắn nhưng là chỗ dễ hỏng nhất về bảo mật, vì <b>lý do người ta đổi mật khẩu
/// gần như luôn là "tôi nghĩ nó bị lộ"</b>. Nếu đổi xong mà phiên cũ vẫn sống thì kẻ trộm
/// ngồi yên thêm 30 ngày, và việc đổi mật khẩu chỉ là một động tác cho yên tâm.
/// </summary>
public class ChangeMyPasswordCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // ── Đồ giả ────────────────────────────────────────────────────────────

    private sealed class FakeHasher : IPasswordHasher
    {
        public string Hash(string password) => $"bam::{password}";

        public bool Verify(string password, string passwordHash) => passwordHash == Hash(password);
    }

    // ── Dựng ──────────────────────────────────────────────────────────────

    private readonly FakeUserRepository _users = new();
    private readonly FakeRefreshTokenRepository _refreshTokens = new();
    private readonly FakeCurrentUser _actor = new();

    private ChangeMyPasswordCommandHandler Handler() =>
        new(_users, _refreshTokens, new FakeHasher(), _actor, new FakeClock());

    private User GiveMyself(string currentPassword = "mat-khau-hien-tai")
    {
        var user = User.Create(TenantId, "an@congty.vn", $"bam::{currentPassword}", "Nguyễn An").Value;

        _users.Existing = user;
        _actor.UserId = user.Id;

        return user;
    }

    private static ChangeMyPasswordCommand Command(
        string current = "mat-khau-hien-tai",
        string moi = "mot-cau-rat-de-nho") => new(current, moi);

    // ── Đường đi đúng ─────────────────────────────────────────────────────

    [Fact]
    public async Task DungMatKhauHienTai_ThiDoiDuoc()
    {
        var user = GiveMyself();

        var result = await Handler().Handle(Command(), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("bam::mot-cau-rat-de-nho", user.PasswordHash);
    }

    [Fact]
    public async Task DoiXong_ThiTHU_HOI_moi_phien_dang_song()
    {
        // ⭐ Đây là điểm quan trọng nhất của cả use case. Người ta đổi mật khẩu vì nghĩ nó
        // bị lộ; không thu hồi thì kẻ trộm vẫn ngồi trong phiên cũ suốt 30 ngày, và việc
        // đổi mật khẩu chỉ là một động tác cho yên tâm.
        var user = GiveMyself();

        await Handler().Handle(Command(), default);

        Assert.Equal(user.Id, _refreshTokens.RevokedFor);
    }

    [Fact]
    public async Task DoiXong_ThiCoBUOC_DOI_MAT_KHAU_duoc_tat()
    {
        // Tài khoản do quản trị viên tạo hộ: đổi xong là hết lý do bắt đổi.
        var user = GiveMyself();

        user.RequirePasswordChange();

        await Handler().Handle(Command(), default);

        Assert.False(user.MustChangePassword);
    }

    // ── Ca hỏng ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SaiMatKhauHienTai_ThiTuChoi()
    {
        // Hỏi lại mật khẩu hiện tại KHÔNG phải thủ tục cho có: nó chặn người ngồi vào máy
        // đang mở sẵn của đồng nghiệp rồi đổi mật khẩu để chiếm tài khoản.
        var user = GiveMyself();

        var result = await Handler().Handle(Command(current: "doan-bua"), default);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.Users.WrongCurrentPassword, result.Error);
        Assert.Equal("bam::mat-khau-hien-tai", user.PasswordHash);
    }

    [Fact]
    public async Task SaiMatKhauHienTai_Thi_KHONG_thu_hoi_phien_nao()
    {
        // Thu hồi khi kiểm tra thất bại thì bất kỳ ai ngồi vào máy cũng đá được người dùng
        // ra khỏi mọi thiết bị, chỉ bằng cách gõ bừa — một kiểu quấy rối rất rẻ.
        GiveMyself();

        await Handler().Handle(Command(current: "doan-bua"), default);

        Assert.Null(_refreshTokens.RevokedFor);
    }

    [Fact]
    public async Task MatKhauMoiTRUNG_mat_khau_cu_thi_tu_choi()
    {
        // Đổi sang chính nó thì không đổi gì cả, nhưng lại thu hồi hết phiên và làm người
        // dùng tin rằng mình vừa xử lý xong một vụ lộ mật khẩu.
        var user = GiveMyself();

        var result = await Handler().Handle(Command(moi: "mat-khau-hien-tai"), default);

        Assert.True(result.IsFailure);
        Assert.Null(_refreshTokens.RevokedFor);
        Assert.Equal("bam::mat-khau-hien-tai", user.PasswordHash);
    }

    [Fact]
    public async Task KhongCoPhien_ThiTuChoi()
    {
        _actor.UserId = null;

        var result = await Handler().Handle(Command(), default);

        Assert.True(result.IsFailure);
    }
}
