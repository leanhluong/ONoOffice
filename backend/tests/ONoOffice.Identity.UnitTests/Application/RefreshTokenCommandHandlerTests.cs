using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Application.Authentication.Logout;
using ONoOffice.Identity.Application.Authentication.Refresh;
using ONoOffice.Identity.Domain;
using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.UnitTests.Application;

public class RefreshTokenCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly LoginFakeUsers _users = new();
    private readonly LoginFakeRefreshTokens _refreshTokens = new();
    private readonly LoginFakeTokens _tokens = new();

    public RefreshTokenCommandHandlerTests() => CoTaiKhoan();

    private void CoTaiKhoan(bool userActive = true, bool tenantActive = true) =>
        _users.Data = new AuthUserData(
            UserId, TenantId, "hash-trong-db", "an@gmail.com", "Lê Anh Lượng",
            userActive, tenantActive, MustChangePassword: false,
            new HashSet<string> { Permissions.Employees.Read });

    private RefreshToken TaoVe(DateTimeOffset? createdAt = null) =>
        RefreshToken.Create(UserId, TenantId, "bam-cua-ve", createdAt ?? Now, TimeSpan.FromDays(30)).Value;

    private Task<Luong.Kernel.Primitives.Result<RefreshTokenResponse>> GiaHan() =>
        new RefreshTokenCommandHandler(_users, _refreshTokens, _tokens, new FrozenClock(Now.AddHours(1)))
            .Handle(new RefreshTokenCommand("chuoi-tho-client-gui-len"), CancellationToken.None);

    // ── Đường thành công ────────────────────────────────────────────────────

    [Fact]
    public async Task VeConSong_ThiCapCapTokenMoi()
    {
        _refreshTokens.TokenTraCuuDuoc = TaoVe();

        var result = await GiaHan();

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token-gia-lap", result.Value.AccessToken);
        Assert.Equal("chuoi-tho-gui-cho-client", result.Value.RefreshToken);
    }

    // Vé cũ bị thu hồi VÀ ghi lại vé thay thế — đó là "xoay vòng".
    [Fact]
    public async Task VeCu_BiThuHoiVaGhiLaiVeThayThe()
    {
        var veCu = TaoVe();
        _refreshTokens.TokenTraCuuDuoc = veCu;

        await GiaHan();

        Assert.Equal(Now.AddHours(1), veCu.RevokedAtUtc);
        Assert.NotNull(veCu.ReplacedByTokenId);
        Assert.Equal(Assert.Single(_refreshTokens.DaThem).Id, veCu.ReplacedByTokenId);
    }

    // Nạp LẠI quyền, không tin token cũ. Giữa hai lần gia hạn, người này có thể đã bị
    // thu hồi quyền — đây chính là chỗ thay đổi đó có hiệu lực.
    [Fact]
    public async Task Quyen_DuocNapLaiChuKhongLayTuTokenCu()
    {
        _refreshTokens.TokenTraCuuDuoc = TaoVe();

        await GiaHan();

        Assert.Equal(Permissions.Employees.Read, Assert.Single(_tokens.QuyenDaNhan!));
    }

    // ── Đường thất bại ──────────────────────────────────────────────────────

    [Fact]
    public async Task KhongTimThayVe_ThiTuChoiVaKhongThuHoiGiCa()
    {
        _refreshTokens.TokenTraCuuDuoc = null;

        var result = await GiaHan();

        Assert.Equal(IdentityErrors.Auth.InvalidRefreshToken, result.Error);
        Assert.Empty(_refreshTokens.DaThuHoiToanBoCua);
    }

    // Vé hết hạn là chuyện BÌNH THƯỜNG — người dùng đi vắng một tháng. Không phải
    // dấu hiệu bị trộm, nên KHÔNG thu hồi cả chuỗi.
    [Fact]
    public async Task VeHetHan_ThiTuChoiNhungKhongThuHoiCaChuoi()
    {
        _refreshTokens.TokenTraCuuDuoc = TaoVe(createdAt: Now.AddDays(-40));

        var result = await GiaHan();

        Assert.Equal(IdentityErrors.Auth.InvalidRefreshToken, result.Error);
        Assert.Empty(_refreshTokens.DaThuHoiToanBoCua);
    }

    // ⭐⭐ LUẬT QUAN TRỌNG NHẤT CỦA CẢ MODULE.
    //
    // Vé ĐÃ BỊ THU HỒI mà vẫn được đem ra dùng → có HAI bên cùng giữ nó → đã bị trộm.
    // Lúc đó không tin bên nào cả: thu hồi TOÀN BỘ chuỗi, bắt đăng nhập lại bằng mật khẩu.
    //
    // Chỉ thu hồi mỗi vé vừa bị dùng lại là vô dụng — nó vốn đã bị thu hồi rồi; kẻ trộm
    // chỉ cần dùng vé KẾ TIẾP trong chuỗi mà nó đã lấy được.
    [Fact]
    public async Task VeDaThuHoiMaVanDung_LaDAU_HIEU_BI_TROM_ThiHuyCaChuoi()
    {
        var ve = TaoVe();
        ve.Revoke(Now.AddMinutes(30));
        _refreshTokens.TokenTraCuuDuoc = ve;

        var result = await GiaHan();

        Assert.Equal(IdentityErrors.Auth.InvalidRefreshToken, result.Error);
        Assert.Equal(UserId, Assert.Single(_refreshTokens.DaThuHoiToanBoCua));
    }

    // Người dùng thật KHÔNG được biết vì sao bị từ chối — nói rõ "token này đã bị dùng
    // lại" là mách cho kẻ tấn công biết hệ thống đang theo dõi được nó.
    [Fact]
    public async Task MoiCaThatBai_DeuTraVeCungMotLoi()
    {
        _refreshTokens.TokenTraCuuDuoc = null;
        var loiKhongTimThay = (await GiaHan()).Error;

        var ve = TaoVe();
        ve.Revoke(Now);
        _refreshTokens.TokenTraCuuDuoc = ve;
        var loiBiTrom = (await GiaHan()).Error;

        Assert.Equal(loiKhongTimThay, loiBiTrom);
    }

    [Fact]
    public async Task TaiKhoanBiKhoa_ThiTuChoi()
    {
        _refreshTokens.TokenTraCuuDuoc = TaoVe();
        CoTaiKhoan(userActive: false);

        Assert.Equal(IdentityErrors.Auth.AccountDisabled, (await GiaHan()).Error);
    }

    [Fact]
    public async Task WorkspaceNgungHoatDong_ThiTuChoi()
    {
        _refreshTokens.TokenTraCuuDuoc = TaoVe();
        CoTaiKhoan(tenantActive: false);

        Assert.Equal(IdentityErrors.Auth.WorkspaceDisabled, (await GiaHan()).Error);
    }
}

public class LogoutCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 10, 0, 0, TimeSpan.Zero);

    private readonly LoginFakeRefreshTokens _refreshTokens = new();
    private readonly LoginFakeTokens _tokens = new();

    private Task<Luong.Kernel.Primitives.Result> DangXuat() =>
        new LogoutCommandHandler(_refreshTokens, _tokens, new FrozenClock(Now))
            .Handle(new LogoutCommand("chuoi-tho"), CancellationToken.None);

    [Fact]
    public async Task ThuHoiVeDangSong()
    {
        var ve = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "bam", Now.AddDays(-1), TimeSpan.FromDays(30)).Value;
        _refreshTokens.TokenTraCuuDuoc = ve;

        Assert.True((await DangXuat()).IsSuccess);
        Assert.Equal(Now, ve.RevokedAtUtc);
    }

    // Đăng xuất phải LUÔN thành công, kể cả khi vé không tồn tại hay đã thu hồi.
    // Báo lỗi ở đây vừa vô ích với người dùng (họ muốn thoát, và họ đã thoát rồi),
    // vừa tiết lộ vé nào từng tồn tại.
    [Fact]
    public async Task VeKhongTonTai_VanBaoThanhCong()
    {
        _refreshTokens.TokenTraCuuDuoc = null;

        Assert.True((await DangXuat()).IsSuccess);
    }

    [Fact]
    public async Task VeDaThuHoiRoi_VanBaoThanhCong()
    {
        var ve = RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "bam", Now.AddDays(-1), TimeSpan.FromDays(30)).Value;
        ve.Revoke(Now.AddHours(-1));
        _refreshTokens.TokenTraCuuDuoc = ve;

        Assert.True((await DangXuat()).IsSuccess);
    }
}
