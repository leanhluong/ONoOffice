using ONoOffice.Identity.Contracts;
using ONoOffice.Org.Application.Employees.Create;
using ONoOffice.Org.Application.Employees.Leave;
using ONoOffice.Org.Application.Employees.LinkAccount;
using ONoOffice.Org.Application.Employees.Transfer;
using ONoOffice.Org.Application.Employees.Update;
using ONoOffice.Org.Domain;
using ONoOffice.Org.Domain.Entities;
using ONoOffice.Org.UnitTests.Fakes;

namespace ONoOffice.Org.UnitTests.Employees;

/// <summary>
/// Luật tầng Application cho hồ sơ nhân sự.
///
/// <b>Ba luật ở đây mà Domain không tự thấy được</b>, và cả ba đều vì cùng một lý do:
/// <c>Employee</c> chỉ biết về CHÍNH NÓ.
///
/// <list type="bullet">
/// <item>"mã này đã có ai dùng chưa?" — phải đọc cả bảng</item>
/// <item>"phòng ban tôi sắp vào có tồn tại không?" — phải đọc bảng KHÁC</item>
/// <item>"tôi thuộc workspace nào?" — nằm ở phiên đăng nhập, không ở trong hồ sơ</item>
/// </list>
/// </summary>
public sealed class EmployeeHandlerTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static Employee NhanVien(string ma = "NV001", string ten = "Trần Bình")
        => Employee.Create(Tenant, ma, ten, null, null).Value;

    // ══════════════════════════════════════════════════════════════════
    // Tạo hồ sơ
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ChuaDangNhapThiKhongTaoDuoc()
    {
        var ketQua = await new CreateEmployeeCommandHandler(
                new FakeEmployeeRepository(),
                new FakeDepartmentRepository(),
                new FakeCurrentTenant(null))
            .Handle(Lenh(), CancellationToken.None);

        Assert.Equal(OrgErrors.Employees.TenantRequired, ketQua.Error);
    }

    [Fact]
    public async Task TrungMaNhanVien_ThiBiTuChoi()
    {
        var ketQua = await new CreateEmployeeCommandHandler(
                new RepoTrungMa(),
                new FakeDepartmentRepository(),
                new FakeCurrentTenant(Tenant))
            .Handle(Lenh(), CancellationToken.None);

        Assert.Equal(OrgErrors.Employees.CodeTaken, ketQua.Error);
    }

    /// <summary>
    /// Mã nhân viên được VIẾT HOA trước khi hỏi database.
    ///
    /// <c>Employee.Create</c> chuẩn hoá <c>nv001</c> thành <c>NV001</c>. Nếu handler hỏi
    /// database bằng chuỗi THÔ người dùng gõ thì phép kiểm trùng chạy trên một giá trị
    /// khác với giá trị sắp lưu — tạo được hai hồ sơ <c>nv001</c> và <c>NV001</c>, rồi
    /// ràng buộc UNIQUE mới nổ, bằng một lỗi 500 không ai đọc được.
    /// </summary>
    [Fact]
    public async Task KiemTrungMa_DungMaDaVIETHOA()
    {
        var repo = new RepoGhiLaiMa();

        await new CreateEmployeeCommandHandler(
                repo,
                new FakeDepartmentRepository(),
                new FakeCurrentTenant(Tenant))
            .Handle(Lenh(ma: "  nv001  "), CancellationToken.None);

        Assert.Equal("NV001", repo.MaDaHoi);
    }

    [Fact]
    public async Task TaoVaoPhongKhongTonTai_ThiBiTuChoi()
    {
        var ketQua = await new CreateEmployeeCommandHandler(
                new FakeEmployeeRepository(),
                new FakeDepartmentRepository(),
                new FakeCurrentTenant(Tenant))
            .Handle(Lenh(phong: Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(OrgErrors.Departments.NotFound, ketQua.Error);
    }

    /// <summary>Chưa xếp phòng là trạng thái BÌNH THƯỜNG của người mới, không phải lỗi.</summary>
    [Fact]
    public async Task TaoKhongKemPhongBan_ThiVanThanhCong()
    {
        var repo = new FakeEmployeeRepository();

        var ketQua = await new CreateEmployeeCommandHandler(
                repo,
                new FakeDepartmentRepository(),
                new FakeCurrentTenant(Tenant))
            .Handle(Lenh(), CancellationToken.None);

        Assert.True(ketQua.IsSuccess);
        Assert.Single(repo.Added);
        Assert.Null(repo.Added[0].DepartmentId);
    }

    // ══════════════════════════════════════════════════════════════════
    // Điều chuyển
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ChuyenSangPhongKhongTonTai_ThiBiTuChoi()
    {
        var ketQua = await new TransferEmployeeCommandHandler(
                new RepoCoNguoi(NhanVien()),
                new FakeDepartmentRepository())
            .Handle(new TransferEmployeeCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(OrgErrors.Departments.NotFound, ketQua.Error);
    }

    /// <summary>Rút khỏi mọi phòng (<c>null</c>) thì không có phòng nào để kiểm.</summary>
    [Fact]
    public async Task RutKhoiMoiPhong_ThiKhongCanKiemPhongBan()
    {
        var nguoi = NhanVien();

        // Đưa vào một phòng trước, để lệnh rút ra không bị Domain từ chối vì "đã ở đó rồi".
        nguoi.TransferTo(Guid.NewGuid());

        var ketQua = await new TransferEmployeeCommandHandler(
                new RepoCoNguoi(nguoi),
                new FakeDepartmentRepository())
            .Handle(new TransferEmployeeCommand(nguoi.Id, null), CancellationToken.None);

        Assert.True(ketQua.IsSuccess);
        Assert.Null(nguoi.DepartmentId);
    }

    [Fact]
    public async Task ChuyenNguoiKhongTonTai_ThiBaoKhongTimThay()
    {
        var ketQua = await new TransferEmployeeCommandHandler(
                new FakeEmployeeRepository(),
                new FakeDepartmentRepository())
            .Handle(new TransferEmployeeCommand(Guid.NewGuid(), null), CancellationToken.None);

        Assert.Equal(OrgErrors.Employees.NotFound, ketQua.Error);
    }

    // ══════════════════════════════════════════════════════════════════
    // Sửa thông tin · nghỉ việc
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SuaHoSo_CapNhatDuCaBonTruong()
    {
        var nguoi = NhanVien();

        var ketQua = await new UpdateEmployeeCommandHandler(new RepoCoNguoi(nguoi))
            .Handle(
                new UpdateEmployeeCommand(nguoi.Id, "Trần Văn Bình", "Trưởng nhóm", "binh@congty.vn", "0901234567"),
                CancellationToken.None);

        Assert.True(ketQua.IsSuccess);
        Assert.Equal("Trần Văn Bình", nguoi.FullName);
        Assert.Equal("Trưởng nhóm", nguoi.JobTitle);
        Assert.Equal("binh@congty.vn", nguoi.WorkEmail?.Value);
        Assert.Equal("0901234567", nguoi.Phone);
    }

    /// <summary>
    /// Xoá email đi là THÀNH CÔNG, không phải "email không hợp lệ".
    ///
    /// Rỗng và sai định dạng là hai chuyện khác nhau. Nhập nhằng chúng nghĩa là người dùng
    /// xoá email của một đồng nghiệp sẽ nhận thông báo lỗi về định dạng — và không có cách
    /// nào xoá được.
    /// </summary>
    [Fact]
    public async Task XoaEmailBangCachDeTrong_ThiThanhCong()
    {
        var nguoi = Employee.Create(Tenant, "NV002", "Nguyễn An", "an@congty.vn", null).Value;

        var ketQua = await new UpdateEmployeeCommandHandler(new RepoCoNguoi(nguoi))
            .Handle(new UpdateEmployeeCommand(nguoi.Id, "Nguyễn An", null, null, null), CancellationToken.None);

        Assert.True(ketQua.IsSuccess);
        Assert.Null(nguoi.WorkEmail);
    }

    [Fact]
    public async Task NghiViecHaiLan_ThiBiTuChoi()
    {
        var nguoi = NhanVien();
        var repo = new RepoCoNguoi(nguoi);
        var handler = new LeaveEmployeeCommandHandler(repo);
        var ngay = new DateOnly(2026, 8, 26);

        Assert.True((await handler.Handle(new LeaveEmployeeCommand(nguoi.Id, ngay), CancellationToken.None)).IsSuccess);

        var lanHai = await handler.Handle(new LeaveEmployeeCommand(nguoi.Id, ngay), CancellationToken.None);

        Assert.Equal(OrgErrors.Employees.AlreadyLeft, lanHai.Error);
    }

    // ══════════════════════════════════════════════════════════════════
    // Nối hồ sơ với tài khoản — chỗ hai module gặp nhau
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task NoiVaoTaiKhoanKhongTonTai_ThiBiTuChoi()
    {
        // `Employee.UserId` KHÔNG phải khoá ngoại (Luật 3 cấm ràng buộc xuyên schema), nên
        // database không canh giúp. Tin thẳng con số client gửi lên thì hồ sơ nối vào một
        // tài khoản không tồn tại, và không lớp nào phía dưới bắt được.
        var nguoi = NhanVien();

        var ketQua = await new LinkAccountCommandHandler(
                new RepoCoNguoi(nguoi),
                new FakeUserDirectory())
            .Handle(new LinkAccountCommand(nguoi.Id, TaiKhoanA), CancellationToken.None);

        Assert.Equal("User.NotFound", ketQua.Error.Code);
        Assert.Null(nguoi.UserId);
    }

    /// <summary>
    /// Một tài khoản chỉ được nối vào ĐÚNG MỘT hồ sơ.
    ///
    /// <c>Employee.LinkAccount</c> không thấy được luật này — nó chỉ biết về chính nó, và
    /// từ góc nhìn của hồ sơ thứ hai thì mọi thứ đều hợp lệ: hồ sơ chưa nối ai, tài khoản
    /// có thật. Phải đọc cả bảng mới trả lời được, nên luật nằm ở handler.
    ///
    /// Hỏng thế nào nếu thiếu: hai người cùng "là" một tài khoản. Mọi thao tác lên tài
    /// khoản đó — đổi vai, vô hiệu hoá — hiện lên ở cả hai dòng, và quản trị viên không có
    /// cách nào biết mình vừa chạm vào ai.
    /// </summary>
    [Fact]
    public async Task NoiVaoTaiKhoanNguoiKhacDangDung_ThiBiTuChoi()
    {
        var nguoi = NhanVien("NV002", "Nguyễn An");

        var ketQua = await new LinkAccountCommandHandler(
                new RepoTaiKhoanDaCoChu(nguoi),
                new FakeUserDirectory(TaiKhoan(TaiKhoanA)))
            .Handle(new LinkAccountCommand(nguoi.Id, TaiKhoanA), CancellationToken.None);

        Assert.Equal(OrgErrors.Employees.UserAlreadyLinked, ketQua.Error);
        Assert.Null(nguoi.UserId);
    }

    [Fact]
    public async Task NoiBinhThuong_ThiHoSoMangUserId()
    {
        var nguoi = NhanVien();

        var ketQua = await new LinkAccountCommandHandler(
                new RepoCoNguoi(nguoi),
                new FakeUserDirectory(TaiKhoan(TaiKhoanA)))
            .Handle(new LinkAccountCommand(nguoi.Id, TaiKhoanA), CancellationToken.None);

        Assert.True(ketQua.IsSuccess);
        Assert.Equal(TaiKhoanA, nguoi.UserId);
    }

    /// <summary>
    /// Gỡ rồi nối lại sang tài khoản khác — đường thoát khi nối nhầm.
    ///
    /// <c>LinkAccount</c> cố ý KHÔNG gán đè, nên nếu không có đường gỡ thì một cú nối nhầm
    /// là vĩnh viễn, và cách duy nhất để sửa là vào thẳng database.
    /// </summary>
    [Fact]
    public async Task GoRoiNoiLai_ThiSangDuocTaiKhoanKhac()
    {
        var nguoi = NhanVien();
        var repo = new RepoCoNguoi(nguoi);

        await new LinkAccountCommandHandler(repo, new FakeUserDirectory(TaiKhoan(TaiKhoanA)))
            .Handle(new LinkAccountCommand(nguoi.Id, TaiKhoanA), CancellationToken.None);

        Assert.True((await new UnlinkAccountCommandHandler(repo)
            .Handle(new UnlinkAccountCommand(nguoi.Id), CancellationToken.None)).IsSuccess);

        var noiLai = await new LinkAccountCommandHandler(
                repo,
                new FakeUserDirectory(TaiKhoan(TaiKhoanB)))
            .Handle(new LinkAccountCommand(nguoi.Id, TaiKhoanB), CancellationToken.None);

        Assert.True(noiLai.IsSuccess);
        Assert.Equal(TaiKhoanB, nguoi.UserId);
    }

    // ── Tiện ích ──────────────────────────────────────────────────────

    private static readonly Guid TaiKhoanA = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000000a");
    private static readonly Guid TaiKhoanB = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000000b");

    private static UserSummary TaiKhoan(Guid id)
        => new(id, "a@congty.vn", "A", "Member", true, false, DateTimeOffset.UnixEpoch);

    private static CreateEmployeeCommand Lenh(string ma = "NV001", Guid? phong = null)
        => new(ma, "Trần Bình", null, null, null, phong);

    /// <summary>Hồ sơ tìm thấy, nhưng tài khoản kia đã có hồ sơ KHÁC nhận rồi.</summary>
    private sealed class RepoTaiKhoanDaCoChu(Employee nguoi) : FakeEmployeeRepository
    {
        public override Task<Employee?> GetAsync(Guid id, CancellationToken ct)
            => Task.FromResult<Employee?>(nguoi);

        public override Task<bool> UserLinkedAsync(Guid userId, Guid? exceptId, CancellationToken ct)
            => Task.FromResult(true);
    }

    private sealed class RepoTrungMa : FakeEmployeeRepository
    {
        public override Task<bool> CodeTakenAsync(string code, Guid? exceptId, CancellationToken ct)
            => Task.FromResult(true);
    }

    private sealed class RepoGhiLaiMa : FakeEmployeeRepository
    {
        public string? MaDaHoi { get; private set; }

        public override Task<bool> CodeTakenAsync(string code, Guid? exceptId, CancellationToken ct)
        {
            MaDaHoi = code;

            return Task.FromResult(false);
        }
    }

    private sealed class RepoCoNguoi(Employee nguoi) : FakeEmployeeRepository
    {
        public override Task<Employee?> GetAsync(Guid id, CancellationToken ct)
            => Task.FromResult<Employee?>(nguoi);
    }
}
