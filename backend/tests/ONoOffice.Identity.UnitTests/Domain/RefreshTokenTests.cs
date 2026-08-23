using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.UnitTests.Domain;

public class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 10, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan BaNgay = TimeSpan.FromDays(30);
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();

    private static RefreshToken Tao() =>
        RefreshToken.Create(UserId, TenantId, "hash-cua-token", Now, BaNgay).Value;

    // ── Tạo ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_TaoTokenConSong()
    {
        var token = Tao();

        Assert.Equal(UserId, token.UserId);
        Assert.Equal(Now + BaNgay, token.ExpiresAtUtc);
        Assert.True(token.IsActiveAt(Now));
        Assert.Null(token.RevokedAtUtc);
        Assert.Null(token.ReplacedByTokenId);
    }

    // Lưu chuỗi BĂM, không lưu token thô. Bảng này sống 30 ngày và chứa chìa khoá vào
    // mọi tài khoản — lộ bảng mà token nằm dạng thô thì kẻ tấn công đăng nhập được vào
    // tất cả. Cùng lý do không bao giờ lưu mật khẩu thô.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_TuChoiChuoiBamRong(string hash)
    {
        Assert.True(RefreshToken.Create(UserId, TenantId, hash, Now, BaNgay).IsFailure);
    }

    [Fact]
    public void Create_TuChoiThoiHanKhongDuong()
    {
        Assert.True(RefreshToken.Create(UserId, TenantId, "hash", Now, TimeSpan.Zero).IsFailure);
        Assert.True(RefreshToken.Create(UserId, TenantId, "hash", Now, TimeSpan.FromDays(-1)).IsFailure);
    }

    [Fact]
    public void Create_TuChoiUserRong()
    {
        Assert.True(RefreshToken.Create(Guid.Empty, TenantId, "hash", Now, BaNgay).IsFailure);
    }

    // ── Còn sống hay không ──────────────────────────────────────────────────

    [Fact]
    public void IsActiveAt_HetHanThiKhongConSong()
    {
        var token = Tao();

        Assert.True(token.IsActiveAt(Now + BaNgay - TimeSpan.FromSeconds(1)));
        Assert.False(token.IsActiveAt(Now + BaNgay));
        Assert.False(token.IsActiveAt(Now + BaNgay + TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void IsActiveAt_DaThuHoiThiKhongConSong()
    {
        var token = Tao();
        token.Revoke(Now.AddDays(1));

        Assert.False(token.IsActiveAt(Now.AddDays(2)));
    }

    // ── Thu hồi ─────────────────────────────────────────────────────────────

    [Fact]
    public void Revoke_DanhDauThoiDiemThuHoi()
    {
        var token = Tao();
        var luc = Now.AddDays(1);

        Assert.True(token.Revoke(luc).IsSuccess);
        Assert.Equal(luc, token.RevokedAtUtc);
    }

    [Fact]
    public void Revoke_TuChoiKhiDaThuHoi()
    {
        var token = Tao();
        token.Revoke(Now.AddDays(1));

        Assert.True(token.Revoke(Now.AddDays(2)).IsFailure);
    }

    // Thu hồi một token đã hết hạn vẫn phải được phép. Đây là hành động bảo mật —
    // khi nghi ngờ bị lộ, người ta thu hồi TẤT CẢ, và việc phải lọc ra "cái nào còn
    // hạn" chỉ tạo thêm chỗ để sót.
    [Fact]
    public void Revoke_ChoPhepThuHoiTokenDaHetHan()
    {
        var token = Tao();

        Assert.True(token.Revoke(Now + BaNgay + TimeSpan.FromDays(1)).IsSuccess);
    }

    // ── Xoay vòng ───────────────────────────────────────────────────────────

    [Fact]
    public void RotateTo_ThuHoiTokenCuVaGhiLaiTokenThayThe()
    {
        var token = Tao();
        var tokenMoi = Guid.NewGuid();
        var luc = Now.AddDays(1);

        Assert.True(token.RotateTo(tokenMoi, luc).IsSuccess);
        Assert.Equal(luc, token.RevokedAtUtc);
        Assert.Equal(tokenMoi, token.ReplacedByTokenId);
        Assert.False(token.IsActiveAt(luc));
    }

    // ⭐ LUẬT QUAN TRỌNG NHẤT CỦA CẢ LỚP NÀY.
    //
    // Xoay vòng nghĩa là mỗi refresh token chỉ dùng được ĐÚNG MỘT LẦN. Nếu một token
    // đã xoay rồi lại được dùng tiếp, chỉ có một cách giải thích: HAI BÊN đang cùng
    // giữ nó — tức là đã bị đánh cắp.
    //
    // Tầng Application bắt được thất bại này thì phải thu hồi TOÀN BỘ chuỗi token của
    // người đó, không phải chỉ mỗi cái vừa bị dùng lại. Không có luật này thì kẻ trộm
    // cứ thế gia hạn phiên mãi mãi, và người dùng thật không hề hay biết.
    [Fact]
    public void RotateTo_TuChoiKhiTokenDaDuocXoayRoi()
    {
        var token = Tao();
        token.RotateTo(Guid.NewGuid(), Now.AddDays(1));

        var dungLai = token.RotateTo(Guid.NewGuid(), Now.AddDays(2));

        Assert.True(dungLai.IsFailure);
    }

    [Fact]
    public void RotateTo_TuChoiKhiTokenDaHetHan()
    {
        var token = Tao();

        Assert.True(token.RotateTo(Guid.NewGuid(), Now + BaNgay + TimeSpan.FromSeconds(1)).IsFailure);
    }

    [Fact]
    public void RotateTo_TuChoiKhiThayTheBangChinhNo()
    {
        var token = Tao();

        Assert.True(token.RotateTo(token.Id, Now.AddDays(1)).IsFailure);
    }
}
