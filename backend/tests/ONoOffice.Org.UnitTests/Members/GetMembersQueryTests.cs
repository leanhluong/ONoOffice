using Luong.Kernel.Pagination;
using ONoOffice.Identity.Contracts;
using ONoOffice.Org.Application.Abstractions;
using ONoOffice.Org.Application.Members.GetList;
using ONoOffice.Org.UnitTests.Fakes;

namespace ONoOffice.Org.UnitTests.Members;

/// <summary>
/// Phép gộp hai module thành MỘT danh sách người.
///
/// Đây là chỗ dễ sai nhất của cả module: nó ghép hai nguồn dữ liệu độc lập, và mỗi kiểu
/// ghép sai đều tạo ra một câu trả lời TRÔNG HỢP LÝ mà sai — dòng trùng, người biến mất,
/// hoặc hai người khác nhau bị gộp làm một.
/// </summary>
public sealed class GetMembersQueryTests
{
    private static readonly Guid UserLuong = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid UserBot = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid NvLuong = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly Guid NvMoi = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    private static UserSummary TaiKhoan(Guid id, string ten, string email, string vai = "Member")
        => new(id, email, ten, vai, true, false, DateTimeOffset.UnixEpoch);

    private static ContactListItem HoSo(Guid id, string ma, string ten, bool dangLam = true)
        => new(id, ma, ten, null, null, null, null, null, dangLam);

    private static async Task<IReadOnlyList<MemberListItem>> Chay(
        ContactListItem[] hoSo,
        EmployeeAccountLink[] noi,
        UserSummary[] taiKhoan)
    {
        var handler = new GetMembersQueryHandler(
            new RepoHoSo(hoSo, noi),
            new FakeUserDirectory(taiKhoan));

        return (await handler.Handle(new GetMembersQuery(), CancellationToken.None)).Value;
    }

    /// <summary>
    /// Ba loại dòng, và cả ba đều phải có mặt ĐÚNG MỘT LẦN.
    ///
    /// Đây là phép kiểm quan trọng nhất: quên nhánh "tài khoản chưa có hồ sơ" thì tài
    /// khoản bot biến mất khỏi danh sách quản trị — nó vẫn đăng nhập được, vẫn có quyền,
    /// mà không ai nhìn thấy nó ở đâu cả.
    /// </summary>
    [Fact]
    public async Task GopDuBaLoaiDong_MoiNguoiDungMotLan()
    {
        var ketQua = await Chay(
            [HoSo(NvLuong, "NV001", "Lê Anh Lượng"), HoSo(NvMoi, "NV005", "Đỗ Ngọc Hà")],
            [new EmployeeAccountLink(NvLuong, UserLuong)],
            [TaiKhoan(UserLuong, "Lê Anh Lượng", "chu@congty.vn", "Owner"),
             TaiKhoan(UserBot, "backup-bot", "bot@congty.vn", "Admin")]);

        Assert.Equal(3, ketQua.Count);

        var caHai = ketQua.Single(m => m.EmployeeId == NvLuong);
        Assert.Equal(UserLuong, caHai.UserId);
        Assert.Equal("Owner", caHai.RoleName);
        Assert.Equal("NV001", caHai.Code);

        var chiHoSo = ketQua.Single(m => m.EmployeeId == NvMoi);
        Assert.Null(chiHoSo.UserId);
        Assert.Null(chiHoSo.RoleName);

        var chiTaiKhoan = ketQua.Single(m => m.UserId == UserBot);
        Assert.Null(chiTaiKhoan.EmployeeId);
        Assert.Null(chiTaiKhoan.Code);
    }

    /// <summary>
    /// KHÔNG ghép theo email trùng nhau.
    ///
    /// Ghép theo email nghe rất tiện và sai một cách nguy hiểm: phòng kinh doanh dùng
    /// chung <c>sales@</c> thì hai người bị gộp thành một dòng, và mọi thao tác lên dòng
    /// đó chạm vào nhầm người. Chỉ nối khi đã có ai đó nối tay.
    /// </summary>
    [Fact]
    public async Task TrungEmail_NhungChuaNoi_ThiVanLaHaiDong()
    {
        var ketQua = await Chay(
            [HoSo(NvLuong, "NV001", "Lê Anh Lượng") with { WorkEmail = "chu@congty.vn" }],
            [],
            [TaiKhoan(UserLuong, "Lê Anh Lượng", "chu@congty.vn")]);

        Assert.Equal(2, ketQua.Count);
    }

    /// <summary>
    /// Đủ MỘT trong hai điều kiện là "không hoạt động".
    ///
    /// Người đã nghỉ việc mà tài khoản vẫn bật, hoặc tài khoản bị vô hiệu mà hồ sơ vẫn
    /// mở — cả hai đều nghĩa là người đó không dùng được hệ thống. Lấy mỗi một bên thì
    /// danh sách nói họ vẫn đang làm việc bình thường.
    /// </summary>
    [Fact]
    public async Task NghiViecHoacBiVoHieu_DeuTinhLaKhongHoatDong()
    {
        var daNghi = await Chay(
            [HoSo(NvLuong, "NV001", "A", dangLam: false)],
            [new EmployeeAccountLink(NvLuong, UserLuong)],
            [TaiKhoan(UserLuong, "A", "a@congty.vn")]);

        Assert.False(daNghi.Single().IsActive);

        var biVoHieu = await Chay(
            [HoSo(NvLuong, "NV001", "A")],
            [new EmployeeAccountLink(NvLuong, UserLuong)],
            [TaiKhoan(UserLuong, "A", "a@congty.vn") with { IsActive = false }]);

        Assert.False(biVoHieu.Single().IsActive);
    }

    /// <summary>
    /// Email CÔNG VIỆC trên hồ sơ đứng trước email đăng nhập.
    ///
    /// Email đăng nhập có thể là một địa chỉ nội bộ không ai gửi thư tới; đồng nghiệp cần
    /// địa chỉ để liên hệ.
    /// </summary>
    [Fact]
    public async Task EmailCongViec_UuTienHonEmailDangNhap()
    {
        var ketQua = await Chay(
            [HoSo(NvLuong, "NV001", "A") with { WorkEmail = "lienhe@congty.vn" }],
            [new EmployeeAccountLink(NvLuong, UserLuong)],
            [TaiKhoan(UserLuong, "A", "u-001@noi-bo.local")]);

        Assert.Equal("lienhe@congty.vn", ketQua.Single().Email);
    }

    /// <summary>
    /// Sắp theo TÊN, không phải theo nguồn.
    ///
    /// Không sắp lại thì mọi hồ sơ đứng trước mọi tài khoản, và danh sách đọc như hai bảng
    /// dán chồng lên nhau — người dùng phải tìm một cái tên ở hai chỗ.
    /// </summary>
    [Fact]
    public async Task SapTheoTen_KhongPhaiTheoNguon()
    {
        var ketQua = await Chay(
            [HoSo(NvMoi, "NV005", "Zét")],
            [],
            [TaiKhoan(UserBot, "An", "an@congty.vn")]);

        Assert.Equal(["An", "Zét"], ketQua.Select(m => m.FullName));
    }

    private sealed class RepoHoSo(ContactListItem[] hoSo, EmployeeAccountLink[] noi)
        : FakeEmployeeRepository
    {
        public override Task<PagedList<ContactListItem>> SearchAsync(
            ContactSearch criteria,
            CancellationToken cancellationToken)
            => Task.FromResult(
                PagedList<ContactListItem>.Create(hoSo, criteria.Page, criteria.PageSize, hoSo.Length));

        public override Task<IReadOnlyList<EmployeeAccountLink>> LinkedUserIdsAsync(
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<EmployeeAccountLink>>(noi);
    }
}
