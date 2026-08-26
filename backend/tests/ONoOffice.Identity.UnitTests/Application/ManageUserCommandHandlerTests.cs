using Luong.Kernel.Abstractions;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.UnitTests.Fakes;
using ONoOffice.Identity.Application.Users.SetActive;
using ONoOffice.Identity.Application.Users.Update;
using ONoOffice.Identity.Domain;
using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.UnitTests.Application;

/// <summary>
/// Sửa và vô hiệu hoá tài khoản của NGƯỜI KHÁC.
///
/// Hai use case này nhỏ, nhưng chúng là chỗ một workspace có thể tự khoá chính mình ra
/// ngoài. Phần lớn test dưới đây canh đúng hai cửa đó: <b>không tự khoá mình</b> và
/// <b>không khoá chủ sở hữu</b>.
/// </summary>
public class ManageUserCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ActorId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // ── Đồ giả ────────────────────────────────────────────────────────────

    // ── Dựng ──────────────────────────────────────────────────────────────

    private readonly FakeUserRepository _users = new();
    private readonly FakeRoleRepository _roles = new();
    private readonly FakeTenantRepository _tenants = new();
    private readonly FakeCurrentUser _actor = new() { UserId = ActorId };
    private readonly FakeCurrentTenant _tenantScope = new() { TenantId = TenantId };

    private UpdateUserCommandHandler UpdateHandler() => new(_users, _roles, _tenants, _tenantScope);

    private SetUserActiveCommandHandler SetActiveHandler() => new(_users, _tenants, _actor);

    private User GiveUser()
    {
        var user = User.Create(TenantId, "an@congty.vn", "bam::x", "Nguyễn An").Value;

        _users.Existing = user;

        return user;
    }

    /// <summary>Dựng một tài khoản và đặt nó LÀ người đang thao tác.</summary>
    private User GiveMyself()
    {
        var user = GiveUser();

        _actor.UserId = user.Id;

        return user;
    }

    private Role GiveRole()
    {
        var role = SystemRoles.Manager.CreateFor(TenantId).Value;

        _roles.Existing = role;

        return role;
    }

    // ── Đổi vai trò và họ tên ─────────────────────────────────────────────

    [Fact]
    public async Task DoiVaiTro_ThiVaiCuBiTHAY_khong_phai_them_vao()
    {
        // Mô hình của app là MỘT người MỘT vai. Thêm mà không gỡ thì quyền chỉ có tăng —
        // hạ vai một người từ Admin xuống Member sẽ không lấy lại được quyền nào.
        var user = GiveUser();
        var vaiCu = SystemRoles.Admin.CreateFor(TenantId).Value;

        user.AssignRole(vaiCu.Id);

        var vaiMoi = GiveRole();

        var result = await UpdateHandler().Handle(
            new UpdateUserCommand(user.Id, "Nguyễn An", vaiMoi.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(vaiMoi.Id, Assert.Single(user.RoleIds));
    }

    [Fact]
    public async Task DoiHoTen_ThiLuuTenMoi()
    {
        var user = GiveUser();
        var role = GiveRole();

        await UpdateHandler().Handle(new UpdateUserCommand(user.Id, "Nguyễn Văn An", role.Id), default);

        Assert.Equal("Nguyễn Văn An", user.FullName);
    }

    /// <summary>
    /// <c>FullName = null</c> nghĩa là ĐỪNG ĐỘNG VÀO TÊN.
    ///
    /// Đây là thứ màn Thành viên cần khi đổi vai trò cho nhiều người một lúc: nó chỉ định
    /// đổi vai. Bắt gửi kèm tên thì nó phải gửi lại cái tên đã tải về vài giây trước — và
    /// nếu trong khoảng đó có người khác vừa đổi tên, thao tác hàng loạt sẽ <b>ghi đè</b>
    /// tên mới bằng tên cũ. Mất một thay đổi mà không có gì báo, chỉ vì một trường mà
    /// người bấm nút còn không biết là mình đang gửi.
    /// </summary>
    [Fact]
    public async Task DoiVaiTroMaKhongGuiTen_ThiGiuNguyenTenCu()
    {
        var user = GiveUser();
        var role = GiveRole();

        string tenCu = user.FullName;

        var result = await UpdateHandler().Handle(
            new UpdateUserCommand(user.Id, null, role.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(tenCu, user.FullName);
        Assert.Equal(role.Id, Assert.Single(user.RoleIds));
    }

    /// <summary>
    /// Bỏ tên đi KHÔNG được bỏ luôn phép kiểm chủ sở hữu.
    ///
    /// Nhánh mới là một đường vào khác cho cùng một việc, nên nó phải đi qua đúng những
    /// hàng rào cũ. Thiếu chỗ này thì thao tác hàng loạt trở thành cách duy nhất hạ vai
    /// chủ sở hữu — và workspace kẹt vĩnh viễn.
    /// </summary>
    [Fact]
    public async Task DoiVaiTroCuaCHU_SO_HUU_MaKhongGuiTen_VanBiTuChoi()
    {
        var user = GiveUser();
        var role = GiveRole();

        _tenants.OwnerUserId = user.Id;

        var result = await UpdateHandler().Handle(
            new UpdateUserCommand(user.Id, null, role.Id), default);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.Users.CannotChangeOwnerRole, result.Error);
    }

    [Fact]
    public async Task DoiVaiTroCuaCHU_SO_HUU_ThiTuChoi()
    {
        // Chủ sở hữu là người DUY NHẤT chuyển nhượng được workspace. Hạ vai họ xuống
        // Member thì không còn ai làm được việc đó, và workspace kẹt vĩnh viễn.
        var user = GiveUser();
        var role = GiveRole();

        _tenants.OwnerUserId = user.Id;

        var result = await UpdateHandler().Handle(new UpdateUserCommand(user.Id, "Chủ", role.Id), default);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.Users.CannotChangeOwnerRole, result.Error);
    }

    [Fact]
    public async Task KhongTimThayTaiKhoan_ThiTraVe404_chu_khong_im_lang()
    {
        var role = GiveRole();

        var result = await UpdateHandler().Handle(new UpdateUserCommand(Guid.NewGuid(), "Ai Đó", role.Id), default);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.Users.NotFound, result.Error);
    }

    [Fact]
    public async Task VaiTroCuaWorkspaceKHAC_ThiTuChoi()
    {
        var user = GiveUser();

        _roles.Existing = SystemRoles.Admin.CreateFor(Guid.NewGuid()).Value;

        var result = await UpdateHandler().Handle(
            new UpdateUserCommand(user.Id, "Nguyễn An", _roles.Existing.Id), default);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.Roles.NotFound, result.Error);
    }

    // ── Vô hiệu hoá ───────────────────────────────────────────────────────

    [Fact]
    public async Task VoHieuHoaNguoiKhac_ThiThanhCong()
    {
        var user = GiveUser();

        var result = await SetActiveHandler().Handle(new SetUserActiveCommand(user.Id, false), default);

        Assert.True(result.IsSuccess);
        Assert.False(user.IsActive);
    }

    [Fact]
    public async Task TuVoHieuHoaCHINH_MINH_ThiTuChoi()
    {
        // ⭐ Cách nhanh nhất để một workspace mất hết quản trị viên: người cuối cùng tự
        // khoá mình. Chặn ở đây rẻ hơn nhiều so với đi sửa tay trong database.
        var user = GiveMyself();

        var result = await SetActiveHandler().Handle(new SetUserActiveCommand(user.Id, false), default);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.Users.CannotDisableSelf, result.Error);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task VoHieuHoaCHU_SO_HUU_ThiTuChoi()
    {
        var user = GiveUser();

        _tenants.OwnerUserId = user.Id;

        var result = await SetActiveHandler().Handle(new SetUserActiveCommand(user.Id, false), default);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.Users.CannotDisableOwner, result.Error);
    }

    [Fact]
    public async Task BAT_LAI_chinh_minh_thi_KHONG_bi_chan()
    {
        // Luật chỉ cấm TỰ KHOÁ. Bật lại thì vô hại — và trên thực tế không xảy ra, vì
        // người đang bị khoá thì không đăng nhập được để mà bật. Chặn cả hai chiều là
        // chặn thừa, và mã thừa thì sẽ có người tưởng nó có lý do.
        var user = GiveMyself();

        user.Deactivate();

        var result = await SetActiveHandler().Handle(new SetUserActiveCommand(user.Id, true), default);

        Assert.True(result.IsSuccess);
        Assert.True(user.IsActive);
    }
}
