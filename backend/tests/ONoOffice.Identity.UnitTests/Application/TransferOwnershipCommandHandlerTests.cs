using Luong.Kernel.Abstractions;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Application.Workspace.TransferOwnership;
using ONoOffice.Identity.Domain;
using ONoOffice.Identity.Domain.Entities;
using ONoOffice.Identity.UnitTests.Fakes;

namespace ONoOffice.Identity.UnitTests.Application;

/// <summary>
/// Chuyển nhượng quyền sở hữu workspace.
///
/// ═══════════════════════════════════════════════════════════════════════
///  VÌ SAO VIỆC NÀY PHẢI CÓ
/// ═══════════════════════════════════════════════════════════════════════
///
/// Bốn thông báo lỗi trong hệ thống đang bảo người dùng <i>"hãy chuyển nhượng quyền sở hữu
/// trước"</i>: khi vô hiệu hoá chủ sở hữu, khi hạ vai họ, khi đặt lại mật khẩu của họ, và
/// khi chính họ muốn rời workspace. Cho tới trước lệnh này, cả bốn đều là <b>ngõ cụt</b> —
/// hệ thống chỉ vào một cánh cửa không tồn tại.
///
/// ═══════════════════════════════════════════════════════════════════════
///  VÀ VÌ SAO NÓ LÀ THAO TÁC NGUY HIỂM NHẤT HỆ THỐNG
/// ═══════════════════════════════════════════════════════════════════════
///
/// Nó <b>không hoàn tác được bởi người vừa làm</b>. Xong lệnh này, người cũ mất đúng cái
/// quyền cần để lấy lại. Mọi thao tác khác trong app đều có đường lùi; cái này thì không.
///
/// Vì vậy nó có hai lớp mà chỗ khác không có: phải LÀ chủ sở hữu (đọc từ database, không
/// tin claim trong token), và phải gõ lại MẬT KHẨU HIỆN TẠI.
/// </summary>
public class TransferOwnershipCommandHandlerTests
{

    private sealed class FakeHasher : IPasswordHasher
    {
        public string Hash(string password) => $"bam::{password}";

        public bool Verify(string password, string passwordHash) => passwordHash == Hash(password);
    }

    private readonly FakeUsersById _users = new();
    private readonly FakeRolesByName _roles = new();
    private readonly FakeTenants _tenants = new();
    private readonly FakeCurrentUser _actor = new();

    private Tenant _tenant = null!;
    private User _chu = null!;
    private User _nguoiNhan = null!;
    private Role _vaiChu = null!;
    private Role _vaiAdmin = null!;

    public TransferOwnershipCommandHandlerTests()
    {
        _tenant = Tenant.Create("acme", "Công ty ACME").Value;

        // Mọi thứ gắn vào ĐÚNG mã của workspace vừa dựng, không dùng một hằng số riêng.
        // Handler so `newOwner.TenantId` với `tenant.Id` — dùng hai nguồn mã khác nhau thì
        // test đỏ vì lý do sai, và phép kiểm cô lập workspace trông như đang hỏng.
        _vaiChu = SystemRoles.Owner.CreateFor(_tenant.Id).Value;
        _vaiAdmin = SystemRoles.Admin.CreateFor(_tenant.Id).Value;

        _chu = User.Create(_tenant.Id, "chu@congty.vn", "bam::MatKhauChu", "Lê Anh Lượng").Value;
        _nguoiNhan = User.Create(_tenant.Id, "ha@congty.vn", "bam::x", "Phạm Hà").Value;

        _chu.AssignRole(_vaiChu.Id);
        _nguoiNhan.AssignRole(_vaiAdmin.Id);

        _tenant.AssignOwner(_chu.Id);

        _tenants.Current = _tenant;
        _users.Co(_chu);
        _users.Co(_nguoiNhan);
        _roles.Co(_vaiChu);
        _roles.Co(_vaiAdmin);

        _actor.UserId = _chu.Id;
    }

    private TransferOwnershipCommandHandler Handler() =>
        new(_users, _roles, _tenants, new FakeHasher(), _actor);

    private TransferOwnershipCommand Lenh(string matKhau = "MatKhauChu") =>
        new(_nguoiNhan.Id, matKhau);

    // ══════════════════════════════════════════════════════════════════
    // Đường chính
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Chuyển xong thì <b>vai trò cũng phải đổi theo</b>, không chỉ mỗi cờ chủ sở hữu.
    ///
    /// Để người mới giữ vai Admin trong khi họ đã là chủ workspace nghĩa là màn Thành viên
    /// hiện họ với huy hiệu "Admin", còn hệ thống thì đối xử như chủ sở hữu. Hai câu trả
    /// lời khác nhau cho cùng một câu hỏi, và người quản trị tin vào cái nhìn thấy.
    /// </summary>
    [Fact]
    public async Task ChuyenXong_ThiCA_CO_lan_VAI_TRO_deu_doi()
    {
        var ketQua = await Handler().Handle(Lenh(), default);

        Assert.True(ketQua.IsSuccess);
        Assert.Equal(_nguoiNhan.Id, _tenant.OwnerUserId);

        Assert.Equal(_vaiChu.Id, Assert.Single(_nguoiNhan.RoleIds));

        // Người cũ KHÔNG bị bỏ rơi thành Member: họ vừa là chủ công ty, và hạ thẳng xuống
        // vai hẹp nhất là lấy mất khả năng làm việc của họ ngay trong một cú bấm.
        Assert.Equal(_vaiAdmin.Id, Assert.Single(_chu.RoleIds));
    }

    // ══════════════════════════════════════════════════════════════════
    // Bốn cửa chặn
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Người KHÔNG phải chủ sở hữu thì không chuyển được, dù token của họ có quyền.
    ///
    /// Quyền <c>workspace.transfer-ownership</c> chỉ Owner mới có, nên tầng HTTP đã chặn
    /// gần hết. Nhưng access token sống 15 phút: người vừa mất quyền sở hữu vẫn cầm một
    /// token mang claim đó thêm một lúc. Phép kiểm này đọc từ DATABASE, nên nó đóng đúng
    /// khoảng thời gian ấy — không có nó, người cũ chuyển ngược lại được.
    /// </summary>
    [Fact]
    public async Task KhongPhaiChuSoHuu_ThiTuChoi_du_token_con_quyen()
    {
        _actor.UserId = _nguoiNhan.Id;

        var ketQua = await Handler().Handle(new TransferOwnershipCommand(_chu.Id, "x"), default);

        Assert.True(ketQua.IsFailure);
        Assert.Equal(IdentityErrors.Tenants.OnlyOwnerCanTransfer, ketQua.Error);
        Assert.Equal(_chu.Id, _tenant.OwnerUserId);
    }

    /// <summary>
    /// Sai mật khẩu hiện tại thì từ chối — và quyền sở hữu phải còn NGUYÊN.
    ///
    /// Đây là thao tác duy nhất trong app mà người vừa làm không tự hoàn tác được, nên nó
    /// đáng một lần gõ lại mật khẩu. Ca nó chặn rất cụ thể: một cái máy bỏ quên lúc đang
    /// đăng nhập, và người ngồi xuống sau đó.
    /// </summary>
    [Fact]
    public async Task SaiMatKhauHienTai_ThiTuChoi()
    {
        var ketQua = await Handler().Handle(Lenh("sai-be-bet"), default);

        Assert.True(ketQua.IsFailure);
        Assert.Equal(IdentityErrors.Users.WrongCurrentPassword, ketQua.Error);
        Assert.Equal(_chu.Id, _tenant.OwnerUserId);
    }

    /// <summary>
    /// KHÔNG chuyển cho một tài khoản đang bị vô hiệu hoá.
    ///
    /// Chuyển xong thì người cũ mất quyền, còn người mới không đăng nhập được — workspace
    /// còn chủ trên giấy tờ mà <b>không ai vào được chỗ đó</b>. Không có đường sửa nào
    /// ngoài can thiệp thẳng vào database.
    /// </summary>
    [Fact]
    public async Task NguoiNhanDangBiVoHieuHoa_ThiTuChoi()
    {
        _nguoiNhan.Deactivate();

        var ketQua = await Handler().Handle(Lenh(), default);

        Assert.True(ketQua.IsFailure);
        Assert.Equal(IdentityErrors.Tenants.NewOwnerMustBeActive, ketQua.Error);
        Assert.Equal(_chu.Id, _tenant.OwnerUserId);
    }

    [Fact]
    public async Task ChuyenChoChinhMinh_ThiTuChoi()
    {
        var ketQua = await Handler().Handle(new TransferOwnershipCommand(_chu.Id, "MatKhauChu"), default);

        Assert.True(ketQua.IsFailure);
        Assert.Equal(IdentityErrors.Tenants.AlreadyTheOwner, ketQua.Error);
    }

    [Fact]
    public async Task NguoiNhanKhongTonTai_ThiTraVe404()
    {
        var ketQua = await Handler().Handle(
            new TransferOwnershipCommand(Guid.NewGuid(), "MatKhauChu"),
            default);

        Assert.True(ketQua.IsFailure);
        Assert.Equal(IdentityErrors.Users.NotFound, ketQua.Error);
    }

    // ── Đồ giả riêng cho tệp này ──────────────────────────────────────

    private sealed class FakeUsersById : FakeUserRepository
    {
        private readonly Dictionary<Guid, User> _theoId = [];

        /// <summary>Nạp sẵn vào bộ giả — khác hẳn `Add` của cổng (ghi một tài khoản MỚI).</summary>
        public void Co(User user) => _theoId[user.Id] = user;

        public override Task<User?> GetForUpdateAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_theoId.GetValueOrDefault(id));
    }

    private sealed class FakeRolesByName : FakeRoleRepository
    {
        private readonly Dictionary<string, Role> _theoTen = [];

        public void Co(Role role) => _theoTen[role.Name] = role;

        public override Task<Role?> GetByNameAsync(string name, CancellationToken ct = default) =>
            Task.FromResult(_theoTen.GetValueOrDefault(name));
    }

    private sealed class FakeTenants : FakeTenantRepository
    {
        public Tenant? Current;

        public override Task<Tenant?> GetCurrentForUpdateAsync(CancellationToken ct = default) =>
            Task.FromResult(Current);

        public override Task<Guid?> GetOwnerUserIdAsync(CancellationToken ct = default) =>
            Task.FromResult(Current?.OwnerUserId);
    }
}
