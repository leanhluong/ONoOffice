using Luong.Kernel.AspNetCore.Security;
using Microsoft.AspNetCore.Authorization;

namespace ONoOffice.Api.Authorization;

/// <summary>
/// Trả lời đúng một câu: token đang cầm có mang quyền được đòi hay không.
///
/// <b>KHÔNG tra database.</b> Quyền đã nằm sẵn trong token đã ký (xem
/// <c>JwtTokenService</c>), nên mỗi request tiết kiệm được một vòng đi về database.
/// Với một API mà gần như endpoint nào cũng kiểm quyền, đó là chênh lệch giữa "một truy
/// vấn mỗi request" và "không truy vấn nào".
///
/// Cái giá đã ghi ở <c>ADR-0002</c> và phải nói thẳng: <b>thu hồi quyền không có hiệu
/// lực ngay.</b> Người vừa bị gỡ quyền vẫn dùng được nó tới khi access token hết hạn —
/// tối đa 15 phút. Đó chính là lý do 15 phút, chứ không phải 8 tiếng. Chỗ nào cần cắt
/// tức thì (ví dụ khoá tài khoản vì lý do an ninh) thì phải có danh sách đen riêng,
/// không phải sửa chỗ này.
/// </summary>
internal sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        bool coQuyen = context.User
            .FindAll(HttpContextCurrentUser.PermissionClaimType)

            // Không phân biệt hoa thường, khớp với cách ICurrentUser đọc quyền. Hai bên
            // so khác nhau là loại lỗi tệ nhất: nhìn bằng mắt thấy "giống nhau" mà một
            // bên cho qua, bên kia chặn.
            .Any(claim => string.Equals(claim.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase));

        if (coQuyen)
        {
            context.Succeed(requirement);
        }

        // KHÔNG gọi context.Fail(). Fail() là phủ quyết tuyệt đối — nó chặn cả những
        // handler khác đã (hoặc sẽ) chấp thuận cùng requirement này. Cứ im lặng thì
        // requirement không được thoả và ASP.NET tự trả 403; đó là điều ta muốn, mà
        // vẫn chừa đường cho cách cấp quyền khác sau này (ví dụ quyền của chủ workspace).
        return Task.CompletedTask;
    }
}
