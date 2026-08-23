using System.Reflection;

namespace ONoOffice.Identity.Domain;

/// <summary>
/// Toàn bộ quyền của hệ thống, khai báo dưới dạng HẰNG SỐ trong code.
///
/// <b>Vì sao quyền là hằng số, còn vai trò thì nằm trong database:</b>
/// <list type="bullet">
/// <item><b>Quyền</b> gắn với code — chỗ nào kiểm <c>employee.write</c> thì đoạn code đó
/// phải tồn tại. Thêm một quyền mới mà không có code nào dùng thì nó vô nghĩa. Nên quyền
/// phải nằm cùng chỗ với code, và compiler kiểm hộ chính tả.</item>
/// <item><b>Vai trò</b> là chuyện của từng công ty: nơi gọi "Trưởng phòng", nơi gọi
/// "Quản lý bộ phận". Nó phải sửa được lúc chạy, nên nằm trong database.</item>
/// </list>
///
/// Đây chính là lý do code kiểm <c>permission</c> chứ không kiểm <c>role</c>: thêm một
/// vai trò mới là việc của người quản trị, không phải việc của lập trình viên.
/// Xem <c>docs/02-kien-truc/adr/ADR-0002</c>.
/// </summary>
public static class Permissions
{
    public static class Workspace
    {
        public const string Read = "workspace.read";
        public const string Manage = "workspace.manage";
        public const string TransferOwnership = "workspace.transfer-ownership";
    }

    public static class Users
    {
        public const string Read = "user.read";
        public const string Manage = "user.manage";
    }

    public static class Roles
    {
        public const string Read = "role.read";
        public const string Manage = "role.manage";
    }

    public static class Employees
    {
        public const string Read = "employee.read";
        public const string Write = "employee.write";
        public const string Delete = "employee.delete";
    }

    public static class Departments
    {
        public const string Read = "department.read";
        public const string Manage = "department.manage";
    }

    private static readonly Lazy<IReadOnlySet<string>> Cache =
        new(() => Declared().ToHashSet(StringComparer.OrdinalIgnoreCase));

    /// <summary>Tra cứu nhanh, không phân biệt hoa thường.</summary>
    public static IReadOnlySet<string> All => Cache.Value;

    public static bool Contains(string? permission) =>
        !string.IsNullOrWhiteSpace(permission) && All.Contains(permission.Trim());

    /// <summary>
    /// Đọc mọi hằng số <c>string</c> trong các lớp lồng bên trên.
    ///
    /// Dùng phản chiếu thay vì tự gõ lại một danh sách: gõ tay thì thêm quyền mới xong
    /// quên cập nhật danh sách, và quyền đó bị coi là "không tồn tại" — cấp cho ai cũng
    /// bị từ chối, mà nhìn code thì thấy nó nằm đó rành rành.
    ///
    /// Chạy đúng một lần rồi cất vào bộ nhớ, nên chi phí phản chiếu không đáng kể.
    /// </summary>
    public static IEnumerable<string> Declared() =>
        typeof(Permissions)
            .GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
            .SelectMany(nested => nested.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!);
}
