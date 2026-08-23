using Luong.Kernel.Abstractions;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Application.Authentication.Login;
using ONoOffice.Identity.Domain;
using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.UnitTests.Application;

// ── Cổng giả ────────────────────────────────────────────────────────────────

internal sealed class FakeUserRepository : IUserRepository
{
    public LoginUserData? Data { get; set; }

    public string? EmailDaHoi { get; private set; }

    public Task<LoginUserData?> GetForLoginAsync(string email, CancellationToken ct = default)
    {
        EmailDaHoi = email;
        return Task.FromResult(Data);
    }
}

internal sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> DaThem { get; } = [];

    public void Add(RefreshToken token) => DaThem.Add(token);
}

/// <summary>Bộ băm giả, có đếm số lần gọi — dùng để kiểm chống dò tài khoản qua thời gian.</summary>
internal sealed class SpyPasswordHasher : IPasswordHasher
{
    public int SoLanVerify { get; private set; }

    public bool KetQua { get; set; } = true;

    public string Hash(string password) => $"hash::{password}";

    public bool Verify(string password, string passwordHash)
    {
        SoLanVerify++;
        return KetQua;
    }
}

internal sealed class FakeTokenService : ITokenService
{
    public IReadOnlySet<string>? QuyenDaNhan { get; private set; }

    public AccessToken IssueAccessToken(Guid userId, Guid tenantId, IReadOnlySet<string> permissions)
    {
        QuyenDaNhan = permissions;
        return new AccessToken("access-token-gia-lap", TimeSpan.FromMinutes(15));
    }

    public RefreshTokenPair IssueRefreshToken() => new("chuoi-tho-gui-cho-client", "chuoi-bam-luu-db");
}

internal sealed class FrozenClock(DateTimeOffset now) : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; } = now;
}

// ── Test ────────────────────────────────────────────────────────────────────

public class LoginCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly FakeUserRepository _users = new();
    private readonly FakeRefreshTokenRepository _refreshTokens = new();
    private readonly SpyPasswordHasher _hasher = new();
    private readonly FakeTokenService _tokens = new();

    private LoginCommandHandler CreateHandler() =>
        new(_users, _refreshTokens, _hasher, _tokens, new FrozenClock(Now));

    private void CoTaiKhoan(
        bool userActive = true,
        bool tenantActive = true,
        params string[] permissions) =>
        _users.Data = new LoginUserData(
            UserId,
            TenantId,
            "hash-trong-db",
            "an@gmail.com",
            "Lê Anh Lượng",
            userActive,
            tenantActive,
            permissions.ToHashSet(StringComparer.OrdinalIgnoreCase));

    private Task<Luong.Kernel.Primitives.Result<LoginResponse>> DangNhap(
        string email = "an@gmail.com",
        string password = "MatKhauDung123!") =>
        CreateHandler().Handle(new LoginCommand(email, password), CancellationToken.None);

    // ── Đường thành công ────────────────────────────────────────────────────

    [Fact]
    public async Task DungThongTin_ThiTraVeCapToken()
    {
        CoTaiKhoan(permissions: Permissions.Employees.Read);

        var result = await DangNhap();

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token-gia-lap", result.Value.AccessToken);
        Assert.Equal("chuoi-tho-gui-cho-client", result.Value.RefreshToken);
        Assert.Equal(900, result.Value.ExpiresInSeconds);
    }

    [Fact]
    public async Task DungThongTin_ThiTraVeThongTinNguoiDung()
    {
        CoTaiKhoan();

        var result = await DangNhap();

        Assert.Equal(UserId, result.Value.User.Id);
        Assert.Equal("Lê Anh Lượng", result.Value.User.FullName);
        Assert.Equal(TenantId, result.Value.User.TenantId);
    }

    [Fact]
    public async Task Email_DuocChuanHoaTruocKhiTraCuu()
    {
        CoTaiKhoan();

        await DangNhap(email: "  An@Gmail.COM  ");

        Assert.Equal("an@gmail.com", _users.EmailDaHoi);
    }

    // Quyền gom từ vai trò được nhét vào token — đúng ADR-0002.
    [Fact]
    public async Task Quyen_DuocDuaVaoAccessToken()
    {
        CoTaiKhoan(permissions: [Permissions.Employees.Read, Permissions.Employees.Write]);

        await DangNhap();

        Assert.Equal(2, _tokens.QuyenDaNhan!.Count);
        Assert.Contains(Permissions.Employees.Write, _tokens.QuyenDaNhan);
    }

    // ── Refresh token ───────────────────────────────────────────────────────

    // ⭐ Server lưu chuỗi BĂM, gửi cho client chuỗi THÔ. Lưu nhầm chuỗi thô nghĩa là
    // lộ bảng database = đăng nhập được vào mọi tài khoản suốt 30 ngày.
    [Fact]
    public async Task RefreshToken_LuuDangBAM_KhongLuuChuoiTho()
    {
        CoTaiKhoan();

        var result = await DangNhap();

        var daLuu = Assert.Single(_refreshTokens.DaThem);
        Assert.Equal("chuoi-bam-luu-db", daLuu.TokenHash);
        Assert.NotEqual(result.Value.RefreshToken, daLuu.TokenHash);
    }

    [Fact]
    public async Task RefreshToken_GanDungNguoiVaHan30Ngay()
    {
        CoTaiKhoan();

        await DangNhap();

        var daLuu = Assert.Single(_refreshTokens.DaThem);
        Assert.Equal(UserId, daLuu.UserId);
        Assert.Equal(TenantId, daLuu.TenantId);
        Assert.Equal(Now.AddDays(30), daLuu.ExpiresAtUtc);
    }

    // ── Đường thất bại ──────────────────────────────────────────────────────

    [Fact]
    public async Task KhongTimThayEmail_ThiTuChoi()
    {
        _users.Data = null;

        var result = await DangNhap();

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.Auth.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task SaiMatKhau_ThiTuChoi()
    {
        CoTaiKhoan();
        _hasher.KetQua = false;

        var result = await DangNhap();

        Assert.Equal(IdentityErrors.Auth.InvalidCredentials, result.Error);
    }

    // ⭐ QUYẾT ĐỊNH ❶: hai ca trên phải trả về lỗi GIỐNG HỆT NHAU.
    // Tách bạch là tặng công cụ dò tài khoản: gõ 10.000 email, cái nào báo "sai mật khẩu"
    // nghĩa là email đó CÓ THẬT — từ đó tập trung tấn công đúng những email có thật.
    [Fact]
    public async Task SaiEmailVaSaiMatKhau_TraVeCUNG_MOT_LOI()
    {
        _users.Data = null;
        var loiSaiEmail = (await DangNhap()).Error;

        CoTaiKhoan();
        _hasher.KetQua = false;
        var loiSaiMatKhau = (await DangNhap()).Error;

        Assert.Equal(loiSaiEmail, loiSaiMatKhau);
    }

    // ⭐ Không tìm thấy email thì VẪN phải chạy Verify một lần.
    // Bỏ qua bước băm khi email không tồn tại làm request đó trả về nhanh hơn hẳn
    // (Argon2id cố ý chậm ~100ms). Kẻ tấn công đo thời gian phản hồi là biết email nào
    // có thật — dò được tài khoản mà không cần đọc nội dung lỗi.
    [Fact]
    public async Task KhongTimThayEmail_VanChayVerifyDeThoiGianKhongToGiac()
    {
        _users.Data = null;

        await DangNhap();

        Assert.Equal(1, _hasher.SoLanVerify);
    }

    // ⭐ QUYẾT ĐỊNH ❷: tài khoản bị khoá thì BÁO THẲNG.
    // Khác ca sai mật khẩu: người này đã gõ ĐÚNG mật khẩu, gần như chắc chắn là chủ
    // tài khoản thật. Giấu thì họ gọi IT hỏi "sao tôi không vào được" — tốn thời gian
    // cả hai bên mà chẳng bảo vệ được gì.
    [Fact]
    public async Task TaiKhoanBiKhoa_ThiBaoThang()
    {
        CoTaiKhoan(userActive: false);

        var result = await DangNhap();

        Assert.Equal(IdentityErrors.Auth.AccountDisabled, result.Error);
    }

    [Fact]
    public async Task WorkspaceNgungHoatDong_ThiBaoThang()
    {
        CoTaiKhoan(tenantActive: false);

        var result = await DangNhap();

        Assert.Equal(IdentityErrors.Auth.WorkspaceDisabled, result.Error);
    }

    // Kiểm mật khẩu TRƯỚC khi kiểm trạng thái tài khoản. Đảo ngược thứ tự là để lộ
    // "tài khoản này tồn tại nhưng đang bị khoá" cho người chưa chứng minh được mình là chủ.
    [Fact]
    public async Task TaiKhoanBiKhoaMaSaiMatKhau_ThiBaoLoiChungChung()
    {
        CoTaiKhoan(userActive: false);
        _hasher.KetQua = false;

        var result = await DangNhap();

        Assert.Equal(IdentityErrors.Auth.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task ThatBai_ThiKhongTaoRefreshToken()
    {
        CoTaiKhoan();
        _hasher.KetQua = false;

        await DangNhap();

        Assert.Empty(_refreshTokens.DaThem);
    }
}
