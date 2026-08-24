using Luong.Kernel.Pagination;
using Luong.Kernel.Abstractions;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Application.Users.Create;
using ONoOffice.Identity.Domain;
using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.UnitTests.Application;

/// <summary>
/// Quản trị viên tạo tài khoản HỘ một đồng nghiệp.
///
/// Khác hẳn đăng ký workspace: ở đó người dùng tự chọn mật khẩu của mình. Ở đây người tạo
/// và người dùng là hai người khác nhau, nên mật khẩu phải do hệ thống sinh, phải giao tận
/// tay, và phải buộc đổi ngay — xem <c>MustChangePasswordTests</c>.
/// </summary>
public class CreateUserCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // ── Đồ giả ────────────────────────────────────────────────────────────

    private sealed class FakeUsers : IUserRepository
    {
        public readonly List<User> Added = [];
        public string? Taken;

        public void Add(User user) => Added.Add(user);

        public Task<bool> IsEmailTakenAsync(string email, CancellationToken c = default) =>
            Task.FromResult(string.Equals(Taken, email, StringComparison.OrdinalIgnoreCase));

        public Task<AuthUserData?> GetForLoginAsync(string e, CancellationToken c = default) =>
            Task.FromResult<AuthUserData?>(null);

        public Task<AuthUserData?> GetByIdAsync(Guid id, CancellationToken c = default) =>
            Task.FromResult<AuthUserData?>(null);

        public Task<PagedList<UserListItem>> SearchAsync(UserSearch c, CancellationToken t = default) =>
            Task.FromResult(PagedList<UserListItem>.Create([], 1, 20, 0));
    }

    private sealed class FakeRoles : IRoleRepository
    {
        public Role? Found;

        public void AddRange(IEnumerable<Role> roles) { }

        public Task<Role?> GetByIdAsync(Guid roleId, CancellationToken c = default) =>
            Task.FromResult(Found?.Id == roleId ? Found : null);
    }

    private sealed class FakeHasher : IPasswordHasher
    {
        public string Hash(string password) => $"bam::{password}";

        public bool Verify(string password, string passwordHash) => passwordHash == Hash(password);
    }

    private sealed class FakeGenerator : ITemporaryPasswordGenerator
    {
        public string Generate() => "mat-khau-tam-sinh-ra";
    }

    private sealed class FakeTenant : ICurrentTenant
    {
        public Guid? TenantId { get; set; } = CreateUserCommandHandlerTests.TenantId;
    }

    // ── Dựng ──────────────────────────────────────────────────────────────

    private readonly FakeUsers _users = new();
    private readonly FakeRoles _roles = new();
    private readonly FakeGenerator _generator = new();
    private readonly FakeTenant _tenant = new();

    private CreateUserCommandHandler Handler() =>
        new(_users, _roles, new FakeHasher(), _generator, _tenant);

    private static CreateUserCommand Command(Guid roleId) =>
        new("Nguyễn Văn An", "an@congty.vn", roleId, MustChangePassword: true);

    private Role GiveRole()
    {
        var role = SystemRoles.Member.CreateFor(TenantId).Value;

        _roles.Found = role;

        return role;
    }

    // ── Đường đi đúng ─────────────────────────────────────────────────────

    [Fact]
    public async Task TaoXong_ThiTaiKhoanThuocDungWorkspaceCuaNguoiTao()
    {
        // Không lấy tenant từ thân request. Nhận từ ngoài vào thì một quản trị viên gõ tay
        // tenant_id của công ty khác là tạo được tài khoản trong công ty đó.
        var role = GiveRole();

        var result = await Handler().Handle(Command(role.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(TenantId, Assert.Single(_users.Added).TenantId);
    }

    [Fact]
    public async Task TaoXong_ThiDuocGanDungVaiTroDaChon()
    {
        var role = GiveRole();

        await Handler().Handle(Command(role.Id), default);

        Assert.Equal(role.Id, Assert.Single(Assert.Single(_users.Added).RoleIds));
    }

    [Fact]
    public async Task MatKhauTam_DuocTRA_VE_MOT_LAN_va_KHONG_luu_tho()
    {
        // Đây là lần DUY NHẤT mật khẩu thô tồn tại ở đâu đó ngoài đầu người tạo. Không trả
        // về thì không ai biết mà đưa cho đồng nghiệp; lưu lại thì nó là mật khẩu thô nằm
        // trong database, đúng thứ mà cả việc băm sinh ra để tránh.
        var role = GiveRole();

        var result = await Handler().Handle(Command(role.Id), default);

        Assert.Equal("mat-khau-tam-sinh-ra", result.Value.TemporaryPassword);
        Assert.Equal(new FakeHasher().Hash("mat-khau-tam-sinh-ra"), Assert.Single(_users.Added).PasswordHash);
    }

    [Fact]
    public async Task BatDoiMatKhau_ThiCoDuocBat()
    {
        var role = GiveRole();

        await Handler().Handle(Command(role.Id), default);

        Assert.True(Assert.Single(_users.Added).MustChangePassword);
    }

    [Fact]
    public async Task KhongBatDoiMatKhau_ThiCoTat()
    {
        var role = GiveRole();

        await Handler().Handle(Command(role.Id) with { MustChangePassword = false }, default);

        Assert.False(Assert.Single(_users.Added).MustChangePassword);
    }

    // ── Ca hỏng ───────────────────────────────────────────────────────────

    [Fact]
    public async Task EmailDaCoTaiKhoan_ThiTuChoi()
    {
        var role = GiveRole();
        _users.Taken = "an@congty.vn";

        var result = await Handler().Handle(Command(role.Id), default);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.Emails.Taken, result.Error);
        Assert.Empty(_users.Added);
    }

    [Fact]
    public async Task EmailSaiDinhDang_ThiTuChoi_va_KHONG_hoi_database()
    {
        // Email sai định dạng thì không thể trùng với ai. Hỏi database là một vòng đi về
        // thừa — và là một cách đo xem email nào đã tồn tại.
        var role = GiveRole();
        _users.Taken = "an@congty.vn";

        var result = await Handler().Handle(Command(role.Id) with { Email = "khong-phai-email" }, default);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.Emails.Invalid, result.Error);
    }

    [Fact]
    public async Task VaiTroKhongTonTai_ThiTuChoi()
    {
        // Không kiểm thì tạo ra một tài khoản mang mã vai trò trỏ vào hư không: người đó
        // đăng nhập được mà không có quyền nào, và không có lỗi nào để lần ra vì sao.
        var result = await Handler().Handle(Command(Guid.NewGuid()), default);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.Roles.NotFound, result.Error);
        Assert.Empty(_users.Added);
    }

    [Fact]
    public async Task VaiTroCuaWorkspaceKHAC_ThiTuChoi()
    {
        // Bộ lọc theo tenant của EF đã chặn ở tầng dưới, nhưng handler không được dựa vào
        // đó: nó là lớp phòng thủ, không phải luật nghiệp vụ. Vai trò của công ty khác lọt
        // qua đây thì người này có quyền trong dữ liệu của công ty khác.
        var khac = SystemRoles.Admin.CreateFor(Guid.NewGuid()).Value;

        _roles.Found = khac;

        var result = await Handler().Handle(Command(khac.Id), default);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.Roles.NotFound, result.Error);
    }

    [Fact]
    public async Task KhongCoTenantTrongPhien_ThiTuChoi()
    {
        // Không thể xảy ra sau khi qua xác thực, nhưng nếu xảy ra thì `Guid.Empty` sẽ tạo
        // ra một tài khoản mồ côi không thuộc workspace nào — và không ai tìm ra nó.
        var role = GiveRole();
        _tenant.TenantId = null;

        var result = await Handler().Handle(Command(role.Id), default);

        Assert.True(result.IsFailure);
        Assert.Empty(_users.Added);
    }
}
