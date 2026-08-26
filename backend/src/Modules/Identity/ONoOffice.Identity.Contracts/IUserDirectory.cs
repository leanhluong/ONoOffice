namespace ONoOffice.Identity.Contracts;

/// <summary>
/// Một tài khoản đăng nhập, ở mức module khác được phép biết.
///
/// <b>Cố ý HẸP.</b> Không có mật khẩu băm, không có refresh token, không có danh sách
/// quyền. Module khác cần biết "ai có tài khoản, tên gì, vai gì" để hiển thị — không cần
/// gì hơn, và cổng càng hẹp thì càng ít thứ có thể dùng sai.
/// </summary>
public sealed record UserSummary(
    Guid Id,
    string Email,
    string FullName,
    string RoleName,
    bool IsActive,
    bool MustChangePassword,
    DateTimeOffset CreatedAtUtc);

/// <summary>
/// Cổng liên module ĐẦU TIÊN của dự án: Org hỏi Identity về tài khoản.
///
/// ═══════════════════════════════════════════════════════════════════════
///  VÌ SAO PHẢI LÀ MỘT CỔNG, KHÔNG PHẢI MỘT CÂU JOIN
/// ═══════════════════════════════════════════════════════════════════════
///
/// Luật số 3 của kiến trúc cấm JOIN xuyên schema, và <c>Employee.UserId</c> cố ý là một
/// <c>Guid</c> trần chứ không phải khoá ngoại. Đó chính là thứ khiến sau này muốn cắt hai
/// module thành hai dịch vụ chỉ cần đổi chuỗi kết nối — một câu JOIN lọt vào thì ngày cắt
/// là ngày viết lại.
///
/// Nhưng màn "Thành viên" phải hiện MỘT danh sách người, trong đó mỗi dòng có thể có tài
/// khoản, có hồ sơ nhân sự, hoặc cả hai. Gộp hai nguồn đó phải xảy ra ở đâu đó, và ba chỗ
/// đều bị loại trừ:
///
/// <list type="bullet">
/// <item><b>Database</b> — Luật 3 cấm.</item>
/// <item><b>Controller</b> — <c>ControllerRuleTests</c> bắt mỗi action là MỘT biểu thức,
/// không câu điều kiện nào.</item>
/// <item><b>Bên trong Identity</b> — Identity không được biết "nhân viên" là gì; nó phục
/// vụ cả những workspace chỉ dùng đăng nhập.</item>
/// </list>
///
/// Còn lại đúng một chỗ đúng: <b>handler của Org</b>, gọi qua cổng này. Org được phép
/// thấy <c>Identity.Contracts</c> — <c>BoundaryRuleTests</c> cho phép đúng điều đó, và
/// project tham chiếu đã có sẵn từ ngày dựng khung.
///
/// ⚠️ Bản cài đặt phải tôn trọng bộ lọc tenant. Nó đọc qua <c>IdentityDbContext</c> nên
/// bộ lọc toàn cục tự áp — nhưng đây là chỗ một lần thêm <c>IgnoreQueryFilters</c> sẽ rò
/// tài khoản của công ty khác sang danh sách nhân sự của công ty này.
/// </summary>
public interface IUserDirectory
{
    /// <summary>
    /// Mọi tài khoản của workspace hiện tại.
    ///
    /// Không phân trang, cố ý: bên gọi cần TOÀN BỘ để gộp với danh sách nhân viên — lấy
    /// một trang thì những người ở trang sau sẽ bị coi là "chưa có tài khoản". Ở quy mô
    /// vài trăm tài khoản thì một lượt đọc là rẻ; đến hàng chục nghìn thì phải đổi sang
    /// hỏi theo lô id, và lúc đó cổng này cần thêm một phương thức chứ không sửa cái này.
    /// </summary>
    Task<IReadOnlyList<UserSummary>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Tài khoản này có thật trong workspace hiện tại không.
    ///
    /// Dùng khi nối hồ sơ nhân sự với tài khoản. Tin thẳng con số client gửi lên thì nối
    /// được hồ sơ vào một tài khoản không tồn tại — hoặc tệ hơn, vào tài khoản của công ty
    /// khác — và không lớp nào phía dưới bắt được, vì <c>UserId</c> không phải khoá ngoại.
    /// </summary>
    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken);
}
