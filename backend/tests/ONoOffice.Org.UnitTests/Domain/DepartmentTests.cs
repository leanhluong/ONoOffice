using ONoOffice.Org.Domain;
using ONoOffice.Org.Domain.Entities;

namespace ONoOffice.Org.UnitTests.Domain;

/// <summary>
/// Phòng ban — nút của cây tổ chức.
///
/// Cây lưu bằng <b>adjacency list</b>: mỗi phòng chỉ giữ <c>ParentId</c> của cha. Hệ quả
/// quan trọng cho tầng này: <b>một phòng ban không biết gì về cây</b> — nó không biết ai
/// là con nó, cũng không biết mình sâu bao nhiêu cấp. Nên mọi luật cần nhìn TOÀN cây
/// (chống vòng lặp) không thể sống ở đây; chúng thuộc về handler, nơi đọc được cả cây.
///
/// Ranh giới đó là chủ ý, không phải thiếu sót — và test cuối cùng ở dưới ghi rõ nó.
/// </summary>
public class DepartmentTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    [Fact]
    public void Tao_PhongGoc_ThiKhongCoCha()
    {
        var result = Department.Create(Tenant, "Khối Kỹ thuật", parentId: null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.ParentId);
        Assert.Equal(Tenant, result.Value.TenantId);
    }

    [Fact]
    public void Tao_PhongCon_ThiNhoCha()
    {
        var cha = Guid.NewGuid();

        var result = Department.Create(Tenant, "Tổ Backend", cha);

        Assert.Equal(cha, result.Value.ParentId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Tao_TenRong_ThiTuChoi(string? ten)
    {
        var result = Department.Create(Tenant, ten, parentId: null);

        Assert.True(result.IsFailure);
        Assert.Equal(OrgErrors.Departments.NameEmpty.Code, result.Error.Code);
    }

    [Fact]
    public void Tao_TenQuaDai_ThiTuChoi()
    {
        var result = Department.Create(Tenant, new string('a', 101), parentId: null);

        Assert.Equal(OrgErrors.Departments.NameTooLong.Code, result.Error.Code);
    }

    [Fact]
    public void Tao_ThieuWorkspace_ThiTuChoi()
    {
        var result = Department.Create(Guid.Empty, "Kỹ thuật", parentId: null);

        Assert.Equal(OrgErrors.Departments.TenantRequired.Code, result.Error.Code);
    }

    [Fact]
    public void Tao_TenCoKhoangTrangThua_ThiCatBo()
    {
        var result = Department.Create(Tenant, "  Khối Kỹ thuật  ", parentId: null);

        // Không cắt thì "Kỹ thuật" và "Kỹ thuật " là hai phòng khác nhau trong mắt ràng
        // buộc UNIQUE — người dùng nhìn danh sách thấy hai dòng y hệt nhau.
        Assert.Equal("Khối Kỹ thuật", result.Value.Name);
    }

    // ── Điều chuyển trong cây ────────────────────────────────────────────────

    [Fact]
    public void Chuyen_SangChaKhac_ThiDoiParentId()
    {
        var phong = Department.Create(Tenant, "Tổ Backend", Guid.NewGuid()).Value;
        var chaMoi = Guid.NewGuid();

        Assert.True(phong.MoveTo(chaMoi).IsSuccess);
        Assert.Equal(chaMoi, phong.ParentId);
    }

    [Fact]
    public void Chuyen_LenLamPhongGoc_ThiDuoc()
    {
        var phong = Department.Create(Tenant, "Tổ Backend", Guid.NewGuid()).Value;

        Assert.True(phong.MoveTo(null).IsSuccess);
        Assert.Null(phong.ParentId);
    }

    /// <summary>
    /// ⭐ Tự làm cha của chính mình — ca duy nhất của vòng lặp mà một phòng ban <b>tự
    /// nhìn thấy được</b>.
    ///
    /// Để lọt thì phòng đó biến mất khỏi cây (không nhánh nào với tới), và câu
    /// <c>WITH RECURSIVE</c> lấy cây sẽ chạy vô tận.
    /// </summary>
    [Fact]
    public void Chuyen_TuLamChaCuaChinhMinh_ThiNO()
    {
        var phong = Department.Create(Tenant, "Tổ Backend", null).Value;

        var result = phong.MoveTo(phong.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(OrgErrors.Departments.CannotBeItsOwnParent.Code, result.Error.Code);
    }

    [Fact]
    public void DoiTen_ThiDoi()
    {
        var phong = Department.Create(Tenant, "Tổ Backend", null).Value;

        Assert.True(phong.Rename("Tổ Nền tảng").IsSuccess);
        Assert.Equal("Tổ Nền tảng", phong.Name);
    }

    [Fact]
    public void DoiTen_ThanhRong_ThiTuChoi()
    {
        var phong = Department.Create(Tenant, "Tổ Backend", null).Value;

        Assert.True(phong.Rename("  ").IsFailure);
        Assert.Equal("Tổ Backend", phong.Name);
    }

    // ── Trưởng phòng ─────────────────────────────────────────────────────────

    [Fact]
    public void GanTruongPhong_ThiNho()
    {
        var phong = Department.Create(Tenant, "Tổ Backend", null).Value;
        var truong = Guid.NewGuid();

        Assert.True(phong.AssignHead(truong).IsSuccess);
        Assert.Equal(truong, phong.HeadEmployeeId);
    }

    [Fact]
    public void GoTruongPhong_ThiVeRong()
    {
        var phong = Department.Create(Tenant, "Tổ Backend", null).Value;
        phong.AssignHead(Guid.NewGuid());

        Assert.True(phong.AssignHead(null).IsSuccess);
        Assert.Null(phong.HeadEmployeeId);
    }

    /// <summary>
    /// Ghi lại RANH GIỚI của aggregate này, để không ai đi tìm luật chống vòng lặp ở đây.
    ///
    /// Một phòng ban chỉ biết cha của nó. Nó KHÔNG THỂ biết mình có đang bị chuyển vào
    /// một phòng con-cháu của chính mình hay không — thông tin đó nằm ở cây, tức là ở
    /// những hàng khác trong bảng. Luật đó sống ở <c>MoveDepartmentCommandHandler</c>,
    /// nơi đọc được cả nhánh.
    ///
    /// Chấp nhận trong aggregate là ĐÚNG: aggregate chỉ được canh thứ nó nhìn thấy.
    /// </summary>
    [Fact]
    public void Chuyen_VaoPhongConCuaChinhMinh_TangNAYKhongBietVaVANChoQua()
    {
        var cha = Department.Create(Tenant, "Khối Kỹ thuật", null).Value;
        var con = Department.Create(Tenant, "Tổ Backend", cha.Id).Value;

        // Chuyển cha vào dưới con nó — vòng lặp thật sự, nhưng `cha` không nhìn thấy.
        Assert.True(cha.MoveTo(con.Id).IsSuccess);
    }
}
