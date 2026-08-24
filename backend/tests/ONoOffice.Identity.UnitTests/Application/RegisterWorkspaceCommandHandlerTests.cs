using Luong.Kernel.Abstractions;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.UnitTests.Fakes;
using ONoOffice.Identity.Application.Authentication.Register;
using ONoOffice.Identity.Domain;
using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.UnitTests.Application;

/// <summary>
/// Đăng ký workspace — use case tạo MỘT LÚC ba thứ: công ty, bốn vai trò hệ thống, và
/// tài khoản chủ sở hữu.
///
/// Đây chính là việc mà <c>IdentityDataSeeder</c> làm bằng tay ở môi trường phát triển.
/// Khác biệt duy nhất: ở đây người lạ trên Internet gọi vào, nên mọi thứ phải được kiểm.
/// </summary>
public class RegisterWorkspaceCommandHandlerTests
{
    // ── Đồ giả ────────────────────────────────────────────────────────────

    private sealed class FakeHasher : IPasswordHasher
    {
        public string Hash(string password) => $"bam::{password}";

        public bool Verify(string password, string passwordHash) => passwordHash == Hash(password);
    }

    private sealed class FakeTokens : ITokenService
    {
        public IReadOnlySet<string>? IssuedWith;

        public AccessToken IssueAccessToken(Guid userId, Guid tenantId, IReadOnlySet<string> permissions)
        {
            IssuedWith = permissions;
            return new AccessToken("token-gia", TimeSpan.FromMinutes(15));
        }

        public RefreshTokenPair IssueRefreshToken() => new("ve-tho", "ve-bam");

        public string HashRefreshToken(string rawToken) => $"bam::{rawToken}";
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 8, 24, 10, 0, 0, TimeSpan.Zero);
    }

    // ── Dựng ──────────────────────────────────────────────────────────────

    private readonly FakeTenantRepository _tenants = new();
    private readonly FakeRoleRepository _roles = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakeRefreshTokenRepository _refreshTokens = new();
    private readonly FakeTokens _tokenService = new();

    private RegisterWorkspaceCommandHandler Handler() =>
        new(_tenants, _roles, _users, _refreshTokens, new FakeHasher(), _tokenService, new FixedClock());

    private static RegisterWorkspaceCommand Command(string code = "acme", string email = "chu@acme.vn") =>
        new("Công ty TNHH ACME", code, "Lê Anh Lượng", email, "mot-cau-de-nho-va-dai");

    // ── Đường thành công ──────────────────────────────────────────────────

    [Fact]
    public async Task DangKy_ThanhCong_ThiTaoDuBaThu()
    {
        var result = await Handler().Handle(Command(), default);

        Assert.True(result.IsSuccess);
        Assert.Single(_tenants.Added);
        Assert.Equal(4, _roles.Added.Count);
        Assert.Single(_users.Added);
    }

    [Fact]
    public async Task DangKy_ThiBonVaiTroDeuLaVaiHeThong_VaThuocDungWorkspace()
    {
        await Handler().Handle(Command(), default);

        var tenantId = _tenants.Added[0].Id;

        // Vai hệ thống thì bất biến — không ai lỡ tay thu hết quyền của Owner được.
        Assert.All(_roles.Added, role => Assert.True(role.IsSystem));
        Assert.All(_roles.Added, role => Assert.Equal(tenantId, role.TenantId));
    }

    /// <summary>
    /// ⭐ Người đăng ký phải là <b>Owner</b>, và Owner phải có đủ mọi quyền.
    ///
    /// Sai chỗ này thì người vừa tạo công ty không quản trị được chính công ty mình, và
    /// KHÔNG CÒN AI cấp quyền lại cho họ được — không có đường tự cứu.
    /// </summary>
    [Fact]
    public async Task NguoiDangKy_DuocGanVaiOwner_VaCoDuMoiQuyen()
    {
        await Handler().Handle(Command(), default);

        var owner = _roles.Added.Single(r => r.Name == SystemRoles.Owner.Name);

        Assert.Contains(owner.Id, _users.Added[0].RoleIds);
        Assert.Equal(Permissions.All.Count, owner.Permissions.Count);
    }

    [Fact]
    public async Task DangKy_ThiWorkspaceNhanNguoiDoLamChuSoHuu()
    {
        await Handler().Handle(Command(), default);

        Assert.Equal(_users.Added[0].Id, _tenants.Added[0].OwnerUserId);
    }

    /// <summary>
    /// Đăng ký xong là <b>đăng nhập luôn</b>, không bắt gõ lại mật khẩu vừa đặt.
    ///
    /// Bắt đăng nhập lại ngay sau khi đăng ký là một bước thừa hoàn toàn: hệ thống vừa
    /// nhận chính mật khẩu đó cách đây một giây.
    /// </summary>
    [Fact]
    public async Task DangKy_TraVeLuonCapToken_VaLuuVeGiaHan()
    {
        var result = await Handler().Handle(Command(), default);

        Assert.Equal("token-gia", result.Value.AccessToken);
        Assert.Equal("ve-tho", result.Value.RefreshToken);
        Assert.Equal(900, result.Value.ExpiresInSeconds);
        Assert.Single(_refreshTokens.Added);
    }

    [Fact]
    public async Task Token_MangDUMoiQuyenCuaOwner()
    {
        await Handler().Handle(Command(), default);

        Assert.Equal(
            Permissions.All.OrderBy(p => p, StringComparer.Ordinal),
            _tokenService.IssuedWith!.OrderBy(p => p, StringComparer.Ordinal));
    }

    [Fact]
    public async Task DangKy_TraVeThongTinWorkspaceDeManXongHienRa()
    {
        var result = await Handler().Handle(Command("nextx"), default);

        Assert.Equal("nextx", result.Value.Workspace.Code);
        Assert.Equal("Công ty TNHH ACME", result.Value.Workspace.Name);
    }

    [Fact]
    public async Task MatKhau_DuocBAM_KhongLuuNguyenVan()
    {
        await Handler().Handle(Command(), default);

        string stored = _users.Added[0].PasswordHash;

        // So với KẾT QUẢ BĂM chứ không soi chuỗi: bản băm giả ở đây cố tình mang theo mật
        // khẩu để Verify hoạt động, nên phép "không chứa" sẽ đỏ vì lý do sai. Điều cần
        // canh là handler lưu thứ đi ra từ IPasswordHasher, không phải thứ người dùng gõ.
        Assert.NotEqual("mot-cau-de-nho-va-dai", stored);
        Assert.Equal(new FakeHasher().Hash("mot-cau-de-nho-va-dai"), stored);
    }

    // ── Đường từ chối ─────────────────────────────────────────────────────

    [Fact]
    public async Task MaWorkspace_DaCoNguoiDung_ThiTuChoi()
    {
        _tenants.TakenCode = "acme";

        var result = await Handler().Handle(Command("acme"), default);

        Assert.Equal(IdentityErrors.TenantCodes.Taken.Code, result.Error.Code);
    }

    [Fact]
    public async Task Email_DaCoTaiKhoan_ThiTuChoi()
    {
        _users.TakenEmail = "chu@acme.vn";

        var result = await Handler().Handle(Command(email: "chu@acme.vn"), default);

        Assert.Equal(IdentityErrors.Emails.Taken.Code, result.Error.Code);
    }

    /// <summary>
    /// Bị từ chối thì KHÔNG được để lại gì trong bộ theo dõi thay đổi.
    ///
    /// <c>TransactionBehavior</c> sẽ không chốt khi handler trả thất bại, nên trên thực tế
    /// không có gì xuống database. Nhưng thêm rồi mới thất bại vẫn là thói quen xấu: chỉ
    /// cần một ngày nào đó ai đó gọi <c>SaveChanges</c> sớm hơn là có một workspace ma.
    /// </summary>
    [Fact]
    public async Task BiTuChoi_ThiKhongTaoGiCa()
    {
        _users.TakenEmail = "chu@acme.vn";

        await Handler().Handle(Command(email: "chu@acme.vn"), default);

        Assert.Empty(_tenants.Added);
        Assert.Empty(_roles.Added);
        Assert.Empty(_users.Added);
        Assert.Empty(_refreshTokens.Added);
    }

    /// <summary>
    /// Kiểm mã workspace TRƯỚC email.
    ///
    /// Không phải chuyện thẩm mỹ: người dùng sửa mã workspace dễ hơn nhiều so với đổi
    /// email công ty. Báo lỗi dễ sửa trước thì họ đi tiếp được ngay.
    /// </summary>
    [Fact]
    public async Task CaHaiDeuTrung_ThiBaoMaWorkspaceTruoc()
    {
        _tenants.TakenCode = "acme";
        _users.TakenEmail = "chu@acme.vn";

        var result = await Handler().Handle(Command("acme", "chu@acme.vn"), default);

        Assert.Equal(IdentityErrors.TenantCodes.Taken.Code, result.Error.Code);
    }

    [Fact]
    public async Task MaWorkspace_SaiDinhDang_ThiTuChoi()
    {
        var result = await Handler().Handle(Command("Mã Có Dấu"), default);

        Assert.Equal(IdentityErrors.TenantCodes.Invalid.Code, result.Error.Code);
    }

    [Fact]
    public async Task Email_SaiDinhDang_ThiTuChoi()
    {
        var result = await Handler().Handle(Command(email: "khong-phai-email"), default);

        Assert.Equal(IdentityErrors.Emails.Invalid.Code, result.Error.Code);
    }
}
