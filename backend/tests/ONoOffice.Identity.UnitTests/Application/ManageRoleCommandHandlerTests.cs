using Luong.Kernel.Abstractions;
using ONoOffice.Identity.Application.Roles.Create;
using ONoOffice.Identity.Application.Roles.Delete;
using ONoOffice.Identity.Application.Roles.Update;
using ONoOffice.Identity.Domain;
using ONoOffice.Identity.Domain.Entities;
using ONoOffice.Identity.UnitTests.Fakes;

namespace ONoOffice.Identity.UnitTests.Application;

/// <summary>
/// Tạo, sửa và xoá VAI TRÒ TỰ ĐẶT.
///
/// ═══════════════════════════════════════════════════════════════════════
///  VÌ SAO CẦN
/// ═══════════════════════════════════════════════════════════════════════
///
/// Màn Vai trò đang nói với người dùng: <i>"Quyền đến TỪ vai trò, không gán lẻ cho từng
/// người. Muốn khác đi thì tạo một vai trò mới."</i> — rồi không cho tạo. Bốn vai hệ thống
/// là bất biến, nên trước lệnh này câu đó là một <b>ngõ cụt</b>, y hệt chuyện chuyển nhượng
/// quyền sở hữu trước đó.
///
/// Nhu cầu thật rất cụ thể: <c>Manager</c> và <c>Member</c> hiện trùng khít nhau (cùng đúng
/// một quyền <c>employee.read</c>), nên công ty nào muốn một vai "kế toán xem được danh bạ
/// nhưng không sửa phòng ban" thì không có cách nào.
///
/// ═══════════════════════════════════════════════════════════════════════
///  CỬA CHẶN QUAN TRỌNG NHẤT
/// ═══════════════════════════════════════════════════════════════════════
///
/// <c>workspace.transfer-ownership</c> KHÔNG được gán cho vai tự đặt. Nó là <b>toàn bộ</b>
/// ranh giới giữa Admin và Owner (xem <c>SystemRoles.cs</c>); cho nó rơi vào một vai tự
/// đặt thì bảng phân quyền nói dối về chính ranh giới đó.
/// </summary>
public class ManageRoleCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly FakeRoleRepository _roles = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakeCurrentTenant _tenant = new() { TenantId = TenantId };

    private CreateRoleCommandHandler CreateHandler() => new(_roles, _tenant);

    private UpdateRoleCommandHandler UpdateHandler() => new(_roles);

    private DeleteRoleCommandHandler DeleteHandler() => new(_roles, _users);

    private static CreateRoleCommand Lenh(string ten = "Kế toán", params string[] quyen) =>
        new(ten, quyen.Length == 0 ? [Permissions.Employees.Read] : quyen);

    // ══════════════════════════════════════════════════════════════════
    // Tạo
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TaoVaiMoi_ThiLuuTenVaQuyen()
    {
        var ketQua = await CreateHandler().Handle(
            Lenh("Kế toán", Permissions.Employees.Read, Permissions.Departments.Read),
            default);

        Assert.True(ketQua.IsSuccess);

        var vai = Assert.Single(_roles.Added);

        Assert.Equal("Kế toán", vai.Name);
        Assert.False(vai.IsSystem);
        Assert.Equal(
            [Permissions.Departments.Read, Permissions.Employees.Read],
            vai.Permissions.Order());
    }

    /// <summary>
    /// Vai TỰ ĐẶT không bao giờ nhận được quyền chuyển nhượng workspace.
    ///
    /// Đây là phép kiểm quan trọng nhất tệp này. Quyền đó là toàn bộ ranh giới Admin ↔
    /// Owner; một Admin có <c>role.manage</c> mà gán được nó cho một vai tự đặt rồi tự
    /// khoác lên mình thì <b>bảng phân quyền vẫn trông đúng trong khi ranh giới đã mất</b>.
    ///
    /// <c>TransferOwnershipCommandHandler</c> vẫn chặn ở tầng cuối vì nó đọc
    /// <c>Tenant.OwnerUserId</c> từ database — nhưng để quyền đó nằm trong một vai tự đặt
    /// nghĩa là màn Vai trò hiện một dòng quyền <b>không bao giờ làm được gì</b>, và người
    /// quản trị tin rằng họ vừa trao đi thứ mình không trao.
    /// </summary>
    [Fact]
    public async Task GanQuyenCHUYEN_NHUONG_choVaiTuDat_ThiTuChoi()
    {
        var ketQua = await CreateHandler().Handle(
            Lenh("Phó chủ", Permissions.Workspace.TransferOwnership),
            default);

        Assert.True(ketQua.IsFailure);
        Assert.Equal(IdentityErrors.Roles.PermissionIsOwnerOnly, ketQua.Error);
        Assert.Empty(_roles.Added);
    }

    [Fact]
    public async Task TrungTenVaiTro_ThiTuChoi()
    {
        _roles.NameTaken = true;

        var ketQua = await CreateHandler().Handle(Lenh(), default);

        Assert.True(ketQua.IsFailure);
        Assert.Equal(IdentityErrors.Roles.NameTaken, ketQua.Error);
    }

    [Fact]
    public async Task ChuaDangNhap_ThiKhongTaoDuoc()
    {
        // `FakeCurrentTenant` mặc định CÓ tenant (một Guid ngẫu nhiên), nên phải đặt null
        // tay. Quên chỗ này thì test xanh mà chẳng kiểm gì.
        var ketQua = await new CreateRoleCommandHandler(
                _roles,
                new FakeCurrentTenant { TenantId = null })
            .Handle(Lenh(), default);

        Assert.Equal(IdentityErrors.Roles.TenantRequired, ketQua.Error);
    }

    // ══════════════════════════════════════════════════════════════════
    // Sửa
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sửa quyền là ĐẶT LẠI cả bộ, không phải cộng thêm.
    ///
    /// Màn hình đưa lên đúng những ô đang tick, nên thứ gửi xuống là trạng thái mong muốn.
    /// Hiểu nó thành "thêm" thì bỏ tick một quyền chẳng gỡ được gì — quyền chỉ có tăng, và
    /// không có chỗ nào trên giao diện lộ ra điều đó.
    /// </summary>
    [Fact]
    public async Task SuaQuyen_ThiDAT_LAI_ca_bo_chu_khong_cong_them()
    {
        var vai = Role.Create(TenantId, "Kế toán").Value;

        vai.Grant(Permissions.Employees.Read);
        vai.Grant(Permissions.Departments.Read);

        _roles.Existing = vai;

        var ketQua = await UpdateHandler().Handle(
            new UpdateRoleCommand(vai.Id, "Kế toán trưởng", [Permissions.Departments.Read]),
            default);

        Assert.True(ketQua.IsSuccess);
        Assert.Equal("Kế toán trưởng", vai.Name);
        Assert.Equal([Permissions.Departments.Read], vai.Permissions);
    }

    [Fact]
    public async Task SuaVaiHE_THONG_ThiTuChoi()
    {
        var vai = SystemRoles.Admin.CreateFor(TenantId).Value;

        _roles.Existing = vai;

        var ketQua = await UpdateHandler().Handle(
            new UpdateRoleCommand(vai.Id, "Quản trị", [Permissions.Users.Read]),
            default);

        Assert.True(ketQua.IsFailure);
        Assert.Equal(IdentityErrors.Roles.SystemRoleIsImmutable, ketQua.Error);
    }

    // ══════════════════════════════════════════════════════════════════
    // Xoá
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Vai còn NGƯỜI GIỮ thì không xoá được.
    ///
    /// Xoá đi thì những người đó mang một mã vai không còn tồn tại: họ không mất quyền dần
    /// dần mà mất SẠCH ngay lập tức, và màn Thành viên hiện một ô vai trống. Bắt điều
    /// chuyển họ trước thì người quản trị buộc phải quyết định họ sẽ thành vai gì.
    /// </summary>
    [Fact]
    public async Task XoaVaiConNguoiGiu_ThiTuChoi()
    {
        var vai = Role.Create(TenantId, "Kế toán").Value;

        _roles.Existing = vai;
        _users.CountByRole = 3;

        var ketQua = await DeleteHandler().Handle(new DeleteRoleCommand(vai.Id), default);

        Assert.True(ketQua.IsFailure);
        Assert.Equal(IdentityErrors.Roles.StillInUse, ketQua.Error);
        Assert.Empty(_roles.Removed);
    }

    [Fact]
    public async Task XoaVaiHE_THONG_ThiTuChoi()
    {
        var vai = SystemRoles.Member.CreateFor(TenantId).Value;

        _roles.Existing = vai;

        var ketQua = await DeleteHandler().Handle(new DeleteRoleCommand(vai.Id), default);

        Assert.True(ketQua.IsFailure);
        Assert.Equal(IdentityErrors.Roles.SystemRoleIsImmutable, ketQua.Error);
    }

    [Fact]
    public async Task XoaVaiTuDatKhongConAiGiu_ThiDuoc()
    {
        var vai = Role.Create(TenantId, "Kế toán").Value;

        _roles.Existing = vai;

        var ketQua = await DeleteHandler().Handle(new DeleteRoleCommand(vai.Id), default);

        Assert.True(ketQua.IsSuccess);
        Assert.Same(vai, Assert.Single(_roles.Removed));
    }

    [Fact]
    public async Task XoaVaiKhongTonTai_ThiTraVe404()
    {
        var ketQua = await DeleteHandler().Handle(new DeleteRoleCommand(Guid.NewGuid()), default);

        Assert.Equal(IdentityErrors.Roles.NotFound, ketQua.Error);
    }
}
