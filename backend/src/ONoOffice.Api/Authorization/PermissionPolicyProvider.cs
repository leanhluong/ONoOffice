using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using ONoOffice.Identity.Domain;

namespace ONoOffice.Api.Authorization;

/// <summary>
/// Sinh policy <b>lúc chạy</b> cho mỗi tên quyền, thay vì đăng ký trước từng cái.
///
/// <b>Vì sao phải làm vậy.</b> ASP.NET mặc định đòi mọi policy được khai lúc khởi động:
/// <c>options.AddPolicy("employee.read", ...)</c>. Hệ này đã có 12 quyền và còn thêm mỗi
/// khi có màn hình mới. Khai tay nghĩa là thêm một quyền phải sửa hai chỗ — và chỗ thứ
/// hai là chỗ người ta quên. Quên thì endpoint đó chết ngay khi có người gọi vào, chứ
/// không phải lúc build.
///
/// Ở đây, tên policy <b>chính là</b> tên quyền. Thêm quyền mới vào
/// <see cref="Permissions"/> là dùng được ngay, không có bước thứ hai nào để quên.
/// </summary>
internal sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : IAuthorizationPolicyProvider
{
    /// <summary>
    /// Bộ xử lý mặc định của ASP.NET, giữ lại để lo những policy KHÔNG phải là quyền —
    /// chính sách mặc định, chính sách dự phòng, và mấy policy đặt tên riêng nếu sau này có.
    /// </summary>
    private readonly DefaultAuthorizationPolicyProvider _macDinh = new(options);

    /// <summary>
    /// Nhớ lại policy đã dựng.
    ///
    /// ASP.NET gọi <see cref="GetPolicyAsync"/> ở MỌI request tới endpoint có phân quyền.
    /// Dựng lại một <c>AuthorizationPolicy</c> mỗi lần thì rẻ, nhưng rẻ nhân với mọi
    /// request thì không còn rẻ. Tập tên quyền là hữu hạn và cố định trong code nên
    /// không có nguy cơ từ điển này phình vô hạn.
    /// </summary>
    private readonly ConcurrentDictionary<string, AuthorizationPolicy> _daDung = new(StringComparer.OrdinalIgnoreCase);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _macDinh.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _macDinh.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // ⭐ Chỉ nhận những tên CÓ THẬT trong Permissions.
        //
        // Cám dỗ ở đây là dựng policy cho bất kỳ chuỗi nào nhận được — code ngắn hơn một
        // dòng. Nhưng khi đó [Authorize(Policy = "employe.read")] (thiếu một chữ 'e') sẽ
        // sinh ra một policy hợp lệ mà KHÔNG AI trên đời thoả được: mọi người, kể cả quản
        // trị viên cao nhất, đều nhận 403. Nhìn vào thì giống hệt lỗi cấu hình phân quyền,
        // và người ta sẽ đi soi bảng vai trò hàng giờ.
        //
        // Từ chối ở đây thì ASP.NET ném lỗi nói thẳng tên policy không tìm thấy — hỏng to,
        // hỏng ngay, và hỏng đúng chỗ.
        if (!Permissions.Contains(policyName))
        {
            return _macDinh.GetPolicyAsync(policyName);
        }

        var policy = _daDung.GetOrAdd(policyName, ten =>
            new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)

                // Bắt buộc: thiếu dòng này thì người CHƯA đăng nhập cũng nhận 403 thay vì
                // 401, và frontend sẽ không biết phải đá họ về màn đăng nhập.
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(ten))
                .Build());

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
