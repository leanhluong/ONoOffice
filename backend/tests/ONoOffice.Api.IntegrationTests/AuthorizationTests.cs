using System.Net;
using System.Net.Http.Headers;
using ONoOffice.Identity.Domain;

namespace ONoOffice.Api.IntegrationTests;

/// <summary>
/// Canh phân quyền động: policy mang tên một QUYỀN, và không ai đăng ký cái tên đó trước.
///
/// ASP.NET mặc định đòi mọi policy phải khai báo lúc khởi động. Với quyền thì không làm
/// được — hệ này đã có 12 quyền và sẽ còn thêm; khai tay từng cái nghĩa là thêm một quyền
/// phải sửa hai chỗ, và chỗ thứ hai là chỗ người ta quên. Nên
/// <c>IAuthorizationPolicyProvider</c> dựng policy lúc chạy.
/// </summary>
public sealed class AuthorizationTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task KhongCoToken_GoiEndpointDoiDangNhap_Tra401()
    {
        var response = await _factory.CreateClient().GetAsync("/probe/can-dang-nhap");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// ⭐ Chữ ký phải THẬT SỰ được kiểm.
    ///
    /// Không có test này thì một cấu hình sai kiểu <c>ValidateIssuerSigningKey = false</c>
    /// vẫn cho mọi test khác xanh — vì token do test phát vẫn hợp lệ. Chỉ có token ký
    /// bằng khoá khác mới phân biệt được "kiểm chữ ký" với "đọc phần thân rồi tin".
    /// </summary>
    [Fact]
    public async Task TokenKyBangKhoaKhac_Tra401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiFactory.PhatTokenKySaiKhoa());

        var response = await client.GetAsync("/probe/can-dang-nhap");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CoTokenHopLe_GoiEndpointChiDoiDangNhap_Tra200()
    {
        var response = await _factory.TaoClientDaDangNhap().GetAsync("/probe/can-dang-nhap");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Đã đăng nhập nhưng thiếu quyền phải là <b>403</b>, không phải 401.
    ///
    /// Phân biệt này không phải chuyện câu chữ: 401 nghĩa là "tôi chưa biết anh là ai,
    /// hãy đăng nhập" — frontend thấy 401 sẽ đá người dùng về màn đăng nhập. Trả 401 cho
    /// người ĐÃ đăng nhập nhưng thiếu quyền là đá họ ra khỏi phiên đang dùng dở, rồi họ
    /// đăng nhập lại và gặp đúng chuyện đó lần nữa — một vòng lặp không lối ra.
    /// </summary>
    [Fact]
    public async Task CoTokenNhungThieuQuyen_Tra403()
    {
        var client = _factory.TaoClientDaDangNhap(Permissions.Departments.Read);

        var response = await client.GetAsync("/probe/can-quyen");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CoDungQuyen_Tra200()
    {
        var client = _factory.TaoClientDaDangNhap(Permissions.Employees.Read);

        var response = await client.GetAsync("/probe/can-quyen");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// So khớp quyền KHÔNG phân biệt hoa thường.
    ///
    /// Token do một hệ khác phát có thể viết <c>Employee.Read</c>. Từ chối vì khác chữ
    /// hoa là từ chối oan người thật sự có quyền — và lỗi đó cực khó nhìn ra, vì nhìn
    /// bằng mắt thì hai chuỗi "giống nhau".
    /// </summary>
    [Fact]
    public async Task QuyenVietKhacHoaThuong_VanDuocVao()
    {
        var client = _factory.TaoClientDaDangNhap("EMPLOYEE.READ");

        var response = await client.GetAsync("/probe/can-quyen");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EndpointAnDanh_KhongCanToken()
    {
        var response = await _factory.CreateClient().GetAsync("/probe/an-danh");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
