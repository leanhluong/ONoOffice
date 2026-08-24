using Luong.Kernel.Primitives;
using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.Domain;

/// <summary>
/// Bốn vai trò được gieo sẵn cho <b>mọi</b> workspace lúc nó ra đời — định nghĩa ở
/// <c>ADR-0002</c>.
///
/// <b>Vì sao có sẵn bốn vai, trong khi vai trò vốn là chuyện của từng công ty:</b> một
/// workspace vừa tạo mà chưa có vai nào thì người chủ không có quyền gì cả — kể cả quyền
/// tạo vai trò. Không có đường tự thoát. Bốn vai này là bộ đồ tối thiểu để workspace
/// dùng được ngay; công ty nào cần khác thì tạo thêm vai của riêng mình.
///
/// <b>Vì sao chúng là vai HỆ THỐNG (bất biến):</b> cho sửa thì một cú bấm nhầm có thể
/// thu hết quyền của <c>Owner</c>, và lúc đó không còn ai trong workspace cấp lại được
/// cho ai — phải can thiệp thẳng vào database. Muốn bộ quyền khác thì tạo vai mới.
/// </summary>
public static class SystemRoles
{
    /// <summary>Một dòng trong bảng "vai mặc định": tên, và bộ quyền nó mang.</summary>
    public sealed record Definition(string Name, IReadOnlySet<string> Permissions)
    {
        public Result<Role> CreateFor(Guid tenantId) => Role.CreateSystem(tenantId, Name, Permissions);
    }

    /// <summary>
    /// Chủ workspace: <b>tất cả</b> quyền, tính động từ <see cref="Permissions"/>.
    ///
    /// Cố ý không liệt kê tay. Liệt kê tay thì thêm một quyền mới xong quên cập nhật ở
    /// đây, và chủ workspace không dùng được tính năng vừa thêm — trong khi nhìn code
    /// thì thấy vai của họ tên là "Owner", nên chẳng ai nghĩ tới chỗ này.
    /// </summary>
    public static readonly Definition Owner = new("Owner", Domain.Permissions.All);

    /// <summary>
    /// Quản trị: tất cả, <b>trừ đúng một quyền</b> — chuyển nhượng quyền sở hữu.
    ///
    /// Đó là toàn bộ ranh giới giữa Admin và Owner. Cho Admin chuyển nhượng nghĩa là
    /// Admin tự trao workspace cho chính mình được, và hai vai trở thành một.
    /// </summary>
    public static readonly Definition Admin = new(
        "Admin",
        Domain.Permissions.All
            .Where(quyen => !string.Equals(
                quyen,
                Domain.Permissions.Workspace.TransferOwnership,
                StringComparison.OrdinalIgnoreCase))
            .ToHashSet(StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Quản lý.
    ///
    /// ⚠️ ADR-0002 ghi <c>employee.read · leave.approve (trong phạm vi phòng mình)</c>.
    /// Hai chỗ chưa làm được, và nói thẳng ra ở đây thay vì lặng lẽ bịa cho đủ:
    ///
    /// 1. <c>leave.approve</c> chưa tồn tại — chưa có module nghỉ phép. Nên hiện tại
    ///    Manager <b>trùng khít</b> Member. Có test canh đúng chuyện này, và nó sẽ ĐỎ
    ///    vào ngày quyền đó ra đời, để nhắc quay lại đây.
    /// 2. "trong phạm vi phòng mình" là giới hạn theo DỮ LIỆU, không phải theo quyền.
    ///    Hệ thống quyền hiện tại chỉ trả lời được "được hay không được", chưa trả lời
    ///    được "được với những hàng nào". Sẽ cần một cơ chế riêng khi làm module Org.
    /// </summary>
    public static readonly Definition Manager = new(
        "Manager",
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Domain.Permissions.Employees.Read });

    /// <summary>Nhân viên bình thường: chỉ xem được danh bạ.</summary>
    public static readonly Definition Member = new(
        "Member",
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Domain.Permissions.Employees.Read });

    /// <summary>
    /// Thứ tự trong danh sách này là thứ tự tạo, và nó có ý nghĩa: <c>Owner</c> phải là
    /// vai đầu tiên tồn tại để gán được cho người tạo workspace.
    /// </summary>
    public static readonly IReadOnlyList<Definition> All = [Owner, Admin, Manager, Member];
}
