using Luong.Kernel.AspNetCore.Results;
using Luong.Kernel.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ONoOffice.Identity.Domain;

namespace ONoOffice.Api.IntegrationTests.Probe;

/// <summary>
/// Mấy endpoint chỉ tồn tại lúc chạy test, nằm trong assembly TEST chứ không nằm trong
/// sản phẩm.
///
/// <b>Vì sao phải có nó, thay vì cứ test bằng endpoint thật:</b> ở lát 1 sản phẩm mới có
/// ba endpoint đăng nhập, và cả ba đều <c>[AllowAnonymous]</c>. Nghĩa là KHÔNG có chỗ nào
/// để chứng minh 401, 403, hay cái bẫy thứ tự CORS — mà đó lại đúng là những luật dễ làm
/// hỏng nhất. Chờ tới khi module Org có endpoint thật mới test thì hạ tầng phân quyền đã
/// nằm đó vài tuần mà chưa ai biết nó có chạy hay không.
///
/// <b>Vì sao đặt ở assembly test chứ không ở sản phẩm:</b> endpoint giả nằm trong sản
/// phẩm là endpoint sẽ theo lên máy chủ thật. Nghe thì ai cũng bảo "nhớ xoá trước khi
/// deploy" — và đó chính là câu nói đi trước mọi sự cố loại này. Ở đây nó được nạp vào
/// qua <c>AddApplicationPart</c> lúc dựng máy chủ test, nên bản build sản phẩm KHÔNG
/// THỂ chứa nó, không phụ thuộc vào trí nhớ của ai cả.
/// </summary>
[ApiController]
[Route("probe")]
public sealed class ProbeController : ControllerBase
{
    [HttpGet("an-danh")]
    [AllowAnonymous]
    public IActionResult AnDanh() => Ok(new { ok = true });

    /// <summary>Chỉ đòi "anh là ai" — có token hợp lệ là vào được, không cần quyền gì.</summary>
    [HttpGet("can-dang-nhap")]
    [Authorize]
    public IActionResult CanDangNhap() => Ok(new { ok = true });

    /// <summary>
    /// Đòi đúng một quyền cụ thể. Tên policy chính là tên quyền — không có chỗ nào đăng ký
    /// trước cái tên này, <c>PermissionPolicyProvider</c> phải dựng nó lúc chạy.
    /// </summary>
    [HttpGet("can-quyen")]
    [Authorize(Policy = Permissions.Employees.Read)]
    public IActionResult CanQuyen() => Ok(new { ok = true });

    /// <summary>
    /// Ném một exception mang thông tin nhạy cảm giả lập, để kiểm rằng nó KHÔNG lọt ra ngoài.
    /// </summary>
    [HttpGet("no-tung")]
    [AllowAnonymous]
    public IActionResult NoTung() =>
        throw new InvalidOperationException(ChuoiNhayCam);

    /// <summary>Chuỗi này đóng vai "chuỗi kết nối lỡ nằm trong thông báo exception".</summary>
    public const string ChuoiNhayCam = "Host=db-noi-bo;Password=SieuBiMat123";

    /// <summary>
    /// Trả về một thất bại nghiệp vụ có mã nằm trong bảng dịch, để kiểm đường
    /// <c>Result → Problem Details → bản dịch</c>.
    /// </summary>
    [HttpGet("that-bai-nghiep-vu")]
    [AllowAnonymous]
    public IActionResult ThatBaiNghiepVu() =>
        Result.Failure(IdentityErrors.Auth.InvalidCredentials).ToActionResult();
}
