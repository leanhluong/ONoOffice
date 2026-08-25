using Luong.Kernel.Primitives;
using ONoOffice.Org.Application.Abstractions;
using ONoOffice.Org.Application.Departments.Create;
using ONoOffice.Org.Application.Departments.Delete;
using ONoOffice.Org.Application.Departments.GetTree;
using ONoOffice.Org.Application.Departments.Move;
using ONoOffice.Org.Domain;
using ONoOffice.Org.Domain.Entities;
using ONoOffice.Org.UnitTests.Fakes;

namespace ONoOffice.Org.UnitTests.Departments;

/// <summary>
/// Luật của tầng Application cho phòng ban.
///
/// <b>Vì sao những luật này KHÔNG nằm ở Domain:</b> <c>Department</c> lưu cây bằng
/// adjacency list, nên nó chỉ giữ <c>ParentId</c> — một bậc duy nhất. Từ bên trong một
/// phòng ban, ba câu hỏi dưới đây đều không trả lời được:
///
/// <list type="bullet">
/// <item>"chuyển tôi vào dưới B có tạo thành vòng lặp không?" — cần đọc cả nhánh</item>
/// <item>"tôi còn phòng con nào không?" — cần đọc bảng</item>
/// <item>"tôi còn nhân viên nào không?" — cần đọc bảng KHÁC</item>
/// </list>
///
/// Đó là ranh giới đúng của một aggregate: nó chỉ được canh thứ nó nhìn thấy. Ba luật
/// trên sống ở handler, nơi đọc được cả cây.
/// </summary>
public sealed class DepartmentHandlerTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static Department PhongBan(string ten = "Kỹ thuật", Guid? cha = null)
        => Department.Create(Tenant, ten, cha).Value;

    // ══════════════════════════════════════════════════════════════════
    // Chống vòng lặp NHIỀU CẤP — luật Domain không thấy được
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ChuyenPhongVaoChinhNhanhCuaNo_ThiBiTuChoi()
    {
        // Cây: A → B → C.  Chuyển A xuống dưới C là tự nuốt chính mình.
        var a = PhongBan("A");
        var c = PhongBan("C");

        var repo = new RepoCoToTien(a, tienCua: c.Id, toTien: [a.Id]);

        var ketQua = await new MoveDepartmentCommandHandler(repo)
            .Handle(new MoveDepartmentCommand(a.Id, c.Id), CancellationToken.None);

        Assert.True(ketQua.IsFailure);
        Assert.Equal(OrgErrors.Departments.WouldCreateCycle, ketQua.Error);
    }

    [Fact]
    public async Task ChuyenPhongSangNhanhKHAC_ThiDuocPhep()
    {
        var a = PhongBan("A");
        var d = PhongBan("D");

        // Tổ tiên của D không có A → không tạo vòng.
        var repo = new RepoCoToTien(a, tienCua: d.Id, toTien: [Guid.NewGuid()]);

        var ketQua = await new MoveDepartmentCommandHandler(repo)
            .Handle(new MoveDepartmentCommand(a.Id, d.Id), CancellationToken.None);

        Assert.True(ketQua.IsSuccess);
        Assert.Equal(d.Id, a.ParentId);
    }

    [Fact]
    public async Task ChuyenPhongLenLamGoc_ThiKhongCanKiemVongLap()
    {
        var a = PhongBan("A", cha: Guid.NewGuid());
        var repo = new RepoTraVe(a);

        var ketQua = await new MoveDepartmentCommandHandler(repo)
            .Handle(new MoveDepartmentCommand(a.Id, null), CancellationToken.None);

        Assert.True(ketQua.IsSuccess);
        Assert.Null(a.ParentId);
    }

    // ══════════════════════════════════════════════════════════════════
    // Xoá — hai chốt chặn, và thứ tự của chúng có nghĩa
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task XoaPhongConPhongCon_ThiBiTuChoi()
    {
        var a = PhongBan("A");
        var repo = new RepoXoa(a, coCon: true, coNguoi: false);

        var ketQua = await new DeleteDepartmentCommandHandler(repo)
            .Handle(new DeleteDepartmentCommand(a.Id), CancellationToken.None);

        Assert.True(ketQua.IsFailure);
        Assert.Equal(OrgErrors.Departments.HasChildren, ketQua.Error);
        Assert.Empty(repo.Removed);
    }

    [Fact]
    public async Task XoaPhongConNhanVien_ThiBiTuChoi()
    {
        var a = PhongBan("A");
        var repo = new RepoXoa(a, coCon: false, coNguoi: true);

        var ketQua = await new DeleteDepartmentCommandHandler(repo)
            .Handle(new DeleteDepartmentCommand(a.Id), CancellationToken.None);

        Assert.True(ketQua.IsFailure);
        Assert.Equal(OrgErrors.Departments.HasEmployees, ketQua.Error);
    }

    /// <summary>
    /// Còn cả con lẫn người thì báo PHÒNG CON trước.
    ///
    /// Không phải chuyện thẩm mỹ: phải chuyển phòng con đi trước rồi mới điều chuyển được
    /// người, nên báo lỗi kia trước sẽ khiến người dùng làm một việc rồi vẫn bị chặn.
    /// </summary>
    [Fact]
    public async Task ConCaConLanNguoi_ThiBaoPhongConTruoc()
    {
        var a = PhongBan("A");
        var repo = new RepoXoa(a, coCon: true, coNguoi: true);

        var ketQua = await new DeleteDepartmentCommandHandler(repo)
            .Handle(new DeleteDepartmentCommand(a.Id), CancellationToken.None);

        Assert.Equal(OrgErrors.Departments.HasChildren, ketQua.Error);
    }

    [Fact]
    public async Task XoaPhongRong_ThiThanhCong()
    {
        var a = PhongBan("A");
        var repo = new RepoXoa(a, coCon: false, coNguoi: false);

        var ketQua = await new DeleteDepartmentCommandHandler(repo)
            .Handle(new DeleteDepartmentCommand(a.Id), CancellationToken.None);

        Assert.True(ketQua.IsSuccess);
        Assert.Single(repo.Removed);
    }

    // ══════════════════════════════════════════════════════════════════
    // Tạo mới
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TaoPhongTrungTen_ThiBiTuChoi()
    {
        var repo = new RepoTrungTen();

        var ketQua = await new CreateDepartmentCommandHandler(repo, new FakeCurrentTenant(Tenant))
            .Handle(new CreateDepartmentCommand("Kỹ thuật", null), CancellationToken.None);

        Assert.True(ketQua.IsFailure);
        Assert.Equal(OrgErrors.Departments.NameTaken, ketQua.Error);
    }

    [Fact]
    public async Task ChuaDangNhapThiKhongTaoDuoc()
    {
        var ketQua = await new CreateDepartmentCommandHandler(
                new FakeDepartmentRepository(),
                new FakeCurrentTenant(null))
            .Handle(new CreateDepartmentCommand("Kỹ thuật", null), CancellationToken.None);

        Assert.Equal(OrgErrors.Departments.TenantRequired, ketQua.Error);
    }

    [Fact]
    public async Task TaoPhongVaoDuoiPhongKhongTonTai_ThiBiTuChoi()
    {
        // Không kiểm thì sinh ra một nhánh mồ côi: phòng có `ParentId` trỏ vào hư không,
        // nên nó biến mất khỏi cây mà vẫn nằm trong bảng.
        var repo = new RepoTrungTen { TrungTen = false };

        var ketQua = await new CreateDepartmentCommandHandler(repo, new FakeCurrentTenant(Tenant))
            .Handle(new CreateDepartmentCommand("Kỹ thuật", Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(OrgErrors.Departments.NotFound, ketQua.Error);
    }

    // ══════════════════════════════════════════════════════════════════
    // Dựng cây
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DungCayTuDanhSachPhang_GiuDungQuanHeChaCon()
    {
        var goc = Guid.NewGuid();
        var con = Guid.NewGuid();
        var chau = Guid.NewGuid();

        var repo = new RepoCay(
        [
            new DepartmentNode(chau, "Cháu", con, null, null, 1),
            new DepartmentNode(goc, "Gốc", null, null, null, 3),
            new DepartmentNode(con, "Con", goc, null, null, 2),
        ]);

        var ketQua = await new GetDepartmentTreeQueryHandler(repo)
            .Handle(new GetDepartmentTreeQuery(), CancellationToken.None);

        var cay = ketQua.Value;

        Assert.Single(cay);
        Assert.Equal("Gốc", cay[0].Name);
        Assert.Equal("Con", cay[0].Children[0].Name);
        Assert.Equal("Cháu", cay[0].Children[0].Children[0].Name);
    }

    /// <summary>
    /// Phòng có <c>ParentId</c> trỏ vào một phòng KHÔNG có trong danh sách vẫn phải hiện ra.
    ///
    /// Ca này xảy ra thật khi dữ liệu hỏng (xoá cứng bằng tay trong database). Bỏ qua nó
    /// thì cả một nhánh biến mất khỏi giao diện mà không có lỗi nào — quản trị viên nhìn
    /// thấy công ty thiếu mất một phòng và không có cách nào lần ra.
    /// </summary>
    [Fact]
    public async Task PhongMoCoi_VanHienRaOMucGoc()
    {
        var moCoi = Guid.NewGuid();

        var repo = new RepoCay([new DepartmentNode(moCoi, "Mồ côi", Guid.NewGuid(), null, null, 0)]);

        var cay = (await new GetDepartmentTreeQueryHandler(repo)
            .Handle(new GetDepartmentTreeQuery(), CancellationToken.None)).Value;

        Assert.Single(cay);
        Assert.Equal("Mồ côi", cay[0].Name);
    }

    // ── Bản giả cho từng ca ───────────────────────────────────────────

    private sealed class RepoTraVe(Department phong) : FakeDepartmentRepository
    {
        public override Task<Department?> GetAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult<Department?>(phong);
    }

    private sealed class RepoCoToTien(Department phong, Guid tienCua, Guid[] toTien)
        : FakeDepartmentRepository
    {
        public override Task<Department?> GetAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult<Department?>(id == phong.Id ? phong : Department.Create(Tenant, "X", null).Value);

        public override Task<IReadOnlyList<Guid>> GetAncestorIdsAsync(
            Guid id,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Guid>>(id == tienCua ? toTien : []);
    }

    private sealed class RepoXoa(Department phong, bool coCon, bool coNguoi) : FakeDepartmentRepository
    {
        public override Task<Department?> GetAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult<Department?>(phong);

        public override Task<bool> HasChildrenAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(coCon);

        public override Task<bool> HasEmployeesAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(coNguoi);
    }

    private sealed class RepoTrungTen : FakeDepartmentRepository
    {
        public bool TrungTen { get; init; } = true;

        public override Task<bool> NameTakenAsync(
            string name,
            Guid? exceptId,
            CancellationToken cancellationToken) => Task.FromResult(TrungTen);
    }

    private sealed class RepoCay(IReadOnlyList<DepartmentNode> nodes) : FakeDepartmentRepository
    {
        public override Task<IReadOnlyList<DepartmentNode>> GetTreeAsync(
            CancellationToken cancellationToken) => Task.FromResult(nodes);
    }
}
