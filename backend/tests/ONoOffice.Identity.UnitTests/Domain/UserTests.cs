using ONoOffice.Identity.Domain.Entities;
using ONoOffice.Identity.Domain.Events;

namespace ONoOffice.Identity.UnitTests.Domain;

public class UserTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static User TaoUser() =>
        User.Create(TenantId, "an@gmail.com", "hash-gia-lap", "Lê Anh Lượng").Value;

    // ── Tạo tài khoản ───────────────────────────────────────────────────────

    [Fact]
    public void Create_TaoTaiKhoanDangHoatDong()
    {
        var result = User.Create(TenantId, "An@Gmail.COM", "hash-gia-lap", "  Lê Anh Lượng  ");

        Assert.True(result.IsSuccess);
        Assert.Equal(TenantId, result.Value.TenantId);
        Assert.Equal("an@gmail.com", result.Value.Email.Value);   // đã chuẩn hoá
        Assert.Equal("Lê Anh Lượng", result.Value.FullName);      // đã cắt khoảng trắng
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public void Create_TaiKhoanMoiChuaCoVaiTroNao()
    {
        Assert.Empty(TaoUser().RoleIds);
    }

    [Fact]
    public void Create_PhatSuKienUserCreated()
    {
        var user = TaoUser();

        var created = Assert.IsType<UserCreated>(Assert.Single(user.DomainEvents));
        Assert.Equal(user.Id, created.UserId);
        Assert.Equal(TenantId, created.TenantId);
    }

    [Fact]
    public void Create_TuChoiEmailSai()
    {
        Assert.True(User.Create(TenantId, "khong-phai-email", "hash", "Tên").IsFailure);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_TuChoiHoTenRong(string fullName)
    {
        Assert.True(User.Create(TenantId, "an@gmail.com", "hash", fullName).IsFailure);
    }

    // Tầng Domain KHÔNG biết băm mật khẩu bằng thuật toán nào — đó là việc của
    // Infrastructure. Nhưng nó biết chắc một điều: chuỗi băm rỗng nghĩa là ai đó
    // đã bỏ qua bước băm, và một tài khoản như vậy tuyệt đối không được tồn tại.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_TuChoiChuoiBamRong(string passwordHash)
    {
        Assert.True(User.Create(TenantId, "an@gmail.com", passwordHash, "Tên").IsFailure);
    }

    [Fact]
    public void Create_TuChoiTenantRong()
    {
        Assert.True(User.Create(Guid.Empty, "an@gmail.com", "hash", "Tên").IsFailure);
    }

    // ── Vai trò ─────────────────────────────────────────────────────────────

    [Fact]
    public void AssignRole_ThemVaiTro()
    {
        var user = TaoUser();
        var roleId = Guid.NewGuid();

        Assert.True(user.AssignRole(roleId).IsSuccess);
        Assert.Equal(roleId, Assert.Single(user.RoleIds));
    }

    [Fact]
    public void AssignRole_TuChoiKhiDaCoVaiTroDo()
    {
        var user = TaoUser();
        var roleId = Guid.NewGuid();
        user.AssignRole(roleId);

        Assert.True(user.AssignRole(roleId).IsFailure);
        Assert.Single(user.RoleIds);
    }

    [Fact]
    public void RemoveRole_BoVaiTro()
    {
        var user = TaoUser();
        var roleId = Guid.NewGuid();
        user.AssignRole(roleId);

        Assert.True(user.RemoveRole(roleId).IsSuccess);
        Assert.Empty(user.RoleIds);
    }

    [Fact]
    public void RemoveRole_TuChoiKhiKhongCoVaiTroDo()
    {
        Assert.True(TaoUser().RemoveRole(Guid.NewGuid()).IsFailure);
    }

    // Danh sách vai trò phải là bản CHỈ ĐỌC. Trả thẳng List ra ngoài thì bất kỳ ai
    // cũng Add/Remove được mà không đi qua luật của aggregate — và lúc đó mọi luật
    // viết trong AssignRole/RemoveRole đều thành trang trí.
    [Fact]
    public void RoleIds_KhongSuaDuocTuBenNgoai()
    {
        var user = TaoUser();

        Assert.False(user.RoleIds is ICollection<Guid> { IsReadOnly: false });
    }

    // ── Mật khẩu ────────────────────────────────────────────────────────────

    [Fact]
    public void ChangePassword_DoiChuoiBamVaPhatSuKien()
    {
        var user = TaoUser();
        user.ClearDomainEvents();

        Assert.True(user.ChangePassword("hash-moi").IsSuccess);
        Assert.Equal("hash-moi", user.PasswordHash);

        // Đổi mật khẩu phải phát sự kiện: nơi khác cần nó để thu hồi mọi refresh token
        // đang sống. Không làm vậy thì người vừa bị lộ mật khẩu đổi lại mật khẩu, mà
        // kẻ trộm vẫn ngồi trong phiên cũ suốt 30 ngày.
        Assert.IsType<UserPasswordChanged>(Assert.Single(user.DomainEvents));
    }

    [Fact]
    public void ChangePassword_TuChoiChuoiBamRong()
    {
        Assert.True(TaoUser().ChangePassword("  ").IsFailure);
    }

    // ── Ngừng hoạt động ─────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_TatTaiKhoanVaPhatSuKien()
    {
        var user = TaoUser();
        user.ClearDomainEvents();

        Assert.True(user.Deactivate().IsSuccess);
        Assert.False(user.IsActive);
        Assert.IsType<UserDeactivated>(Assert.Single(user.DomainEvents));
    }

    [Fact]
    public void Deactivate_TuChoiKhiDaTat()
    {
        var user = TaoUser();
        user.Deactivate();

        Assert.True(user.Deactivate().IsFailure);
    }

    [Fact]
    public void Activate_BatLaiTaiKhoanDaTat()
    {
        var user = TaoUser();
        user.Deactivate();

        Assert.True(user.Activate().IsSuccess);
        Assert.True(user.IsActive);
    }

    [Fact]
    public void Rename_DoiHoTen()
    {
        var user = TaoUser();

        Assert.True(user.Rename("Lê Anh Lượng B").IsSuccess);
        Assert.Equal("Lê Anh Lượng B", user.FullName);
    }
}
