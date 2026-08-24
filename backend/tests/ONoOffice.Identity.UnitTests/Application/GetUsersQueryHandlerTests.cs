using Luong.Kernel.Pagination;
using Luong.Kernel.Primitives;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.UnitTests.Fakes;
using ONoOffice.Identity.Domain.Entities;
using ONoOffice.Identity.Application.Users.GetList;

namespace ONoOffice.Identity.UnitTests.Application;

/// <summary>
/// Danh sách nhân sự — đường ĐỌC, nên handler cố ý mỏng: lọc và phân trang là việc của
/// database, không phải của C#.
///
/// Thứ đáng kiểm ở đây <b>không</b> phải "có gọi repository không" — đó là kiểm rằng mã
/// làm đúng những gì mã viết. Thứ đáng kiểm là <b>cái chặn</b> mà handler thêm vào giữa
/// client và database. Phần truy vấn thật kiểm ở <c>UserListQueryTests</c>, chạy trên
/// Postgres thật.
/// </summary>
public class GetUsersQueryHandlerTests
{
    private readonly FakeUserRepository _users = new();

    private GetUsersQueryHandler Handler() => new(_users);

    [Fact]
    public async Task KhongNeuGiThi_LayTrangMotVoiCoTrangMacDinh()
    {
        await Handler().Handle(new GetUsersQuery(), default);

        Assert.Equal(1, _users.ReceivedSearch!.Page);
        Assert.Equal(20, _users.ReceivedSearch!.PageSize);
    }

    [Fact]
    public async Task DoiCoTrangQUALON_ThiBiKEO_VE_TRAN()
    {
        // Không chặn thì `?pageSize=1000000` kéo cả bảng lên bộ nhớ trong một request —
        // rẻ tiền để gửi, đắt để phục vụ, và không cần quyền gì đặc biệt.
        await Handler().Handle(new GetUsersQuery(PageSize: 1_000_000), default);

        Assert.Equal(100, _users.ReceivedSearch!.PageSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CoTrangKHONG_HOP_LE_ThiVeMacDinh(int pageSize)
    {
        // `pageSize=0` mà truyền thẳng xuống thì EF sinh `LIMIT 0` — danh sách trống, và
        // người dùng tưởng công ty không có ai.
        await Handler().Handle(new GetUsersQuery(PageSize: pageSize), default);

        Assert.Equal(20, _users.ReceivedSearch!.PageSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task SoTrangKHONG_HOP_LE_ThiVeTrangMot(int page)
    {
        // `page=0` cho `OFFSET -20`, và Postgres từ chối bằng một lỗi 500 khó hiểu.
        await Handler().Handle(new GetUsersQuery(Page: page), default);

        Assert.Equal(1, _users.ReceivedSearch!.Page);
    }

    [Fact]
    public async Task TuKhoaTIM_duoc_CAT_KHOANG_TRANG()
    {
        // Người dùng dán email từ chỗ khác vào thì gần như luôn kèm một dấu cách ở cuối.
        // Không cắt thì họ tìm đúng email của mình mà ra kết quả rỗng.
        await Handler().Handle(new GetUsersQuery(Search: "  an@congty.vn  "), default);

        Assert.Equal("an@congty.vn", _users.ReceivedSearch!.Search);
    }

    [Fact]
    public async Task TuKhoaCHI_CO_KHOANG_TRANG_thi_coi_nhu_khong_loc()
    {
        // Chuỗi rỗng mà đưa xuống truy vấn thì thành `LIKE '%%'` — vô hại nhưng làm
        // Postgres bỏ qua chỉ mục. Coi như không lọc thì rẻ hơn.
        await Handler().Handle(new GetUsersQuery(Search: "   "), default);

        Assert.Null(_users.ReceivedSearch!.Search);
    }
}
