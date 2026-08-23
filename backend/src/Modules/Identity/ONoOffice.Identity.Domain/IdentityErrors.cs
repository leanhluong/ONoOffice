using Luong.Kernel.Primitives;

namespace ONoOffice.Identity.Domain;

/// <summary>
/// Toàn bộ mã lỗi của module Identity, gom về một chỗ.
///
/// Vì sao gom lại thay vì rải <c>Error.Validation("...")</c> khắp nơi:
///
/// 1. <b>Frontend rẽ nhánh theo mã này.</b> Rải ra thì sớm muộn có hai chỗ dùng hai mã
///    khác nhau cho cùng một chuyện, hoặc gõ sai một chữ mà không ai phát hiện — vì
///    chuỗi thì compiler không kiểm.
/// 2. <b>Đọc file này là thấy hết mọi cách module có thể từ chối.</b> Đó là tài liệu
///    sống, không bao giờ lệch với code.
///
/// Quy ước đặt mã: <c>{Vùng}.{ChuyệnGìXảyRa}</c>.
/// </summary>
public static class IdentityErrors
{
    public static class Emails
    {
        public static readonly Error Empty =
            Error.Validation("Email.Empty", "Email không được để trống.");

        public static readonly Error Invalid =
            Error.Validation("Email.Invalid", "Email không đúng định dạng.");

        public static readonly Error TooLong =
            Error.Validation("Email.TooLong", "Email không được dài quá 254 ký tự.");
    }

    public static class TenantCodes
    {
        public static readonly Error Empty =
            Error.Validation("TenantCode.Empty", "Mã workspace không được để trống.");

        public static readonly Error WrongLength =
            Error.Validation("TenantCode.WrongLength", "Mã workspace phải dài từ 3 đến 30 ký tự.");

        public static readonly Error Invalid = Error.Validation(
            "TenantCode.Invalid",
            "Mã workspace chỉ gồm chữ thường, số và gạch nối; phải bắt đầu bằng chữ cái và kết thúc bằng chữ hoặc số.");
    }

    public static class Tenants
    {
        public static readonly Error NameEmpty =
            Error.Validation("Tenant.NameEmpty", "Tên workspace không được để trống.");

        public static readonly Error NameTooLong =
            Error.Validation("Tenant.NameTooLong", "Tên workspace không được dài quá 200 ký tự.");

        // Loại Conflict chứ không phải Validation: dữ liệu gửi lên không sai, chỉ là
        // TRẠNG THÁI hiện tại không cho phép làm việc đó. Nhờ phân biệt này mà tầng web
        // trả 409 thay vì 400 — và người gọi biết "thử lại y hệt cũng vô ích".
        public static readonly Error AlreadyHasOwner = Error.Conflict(
            "Tenant.AlreadyHasOwner",
            "Workspace đã có chủ sở hữu. Dùng chức năng chuyển nhượng thay vì gán mới.");

        public static readonly Error HasNoOwner =
            Error.Conflict("Tenant.HasNoOwner", "Workspace chưa có chủ sở hữu để chuyển nhượng.");

        public static readonly Error AlreadyTheOwner =
            Error.Conflict("Tenant.AlreadyTheOwner", "Người này đã là chủ sở hữu workspace.");

        public static readonly Error AlreadyInactive =
            Error.Conflict("Tenant.AlreadyInactive", "Workspace đã ngừng hoạt động.");

        public static readonly Error AlreadyActive =
            Error.Conflict("Tenant.AlreadyActive", "Workspace đang hoạt động.");
    }

    public static class Users
    {
        public static readonly Error TenantRequired =
            Error.Validation("User.TenantRequired", "Tài khoản phải thuộc về một workspace.");

        public static readonly Error FullNameEmpty =
            Error.Validation("User.FullNameEmpty", "Họ tên không được để trống.");

        public static readonly Error FullNameTooLong =
            Error.Validation("User.FullNameTooLong", "Họ tên không được dài quá 200 ký tự.");

        // Đây là lỗi LẬP TRÌNH chứ không phải lỗi người dùng — người dùng không bao giờ
        // gửi lên chuỗi băm. Vẫn trả về Result thay vì ném exception, để nó đi cùng một
        // đường với mọi thất bại khác và không có nhánh xử lý riêng nào phải nhớ.
        public static readonly Error PasswordHashRequired =
            Error.Validation("User.PasswordHashRequired", "Thiếu chuỗi băm mật khẩu.");

        public static readonly Error RoleAlreadyAssigned =
            Error.Conflict("User.RoleAlreadyAssigned", "Tài khoản đã có vai trò này.");

        public static readonly Error RoleNotAssigned =
            Error.Conflict("User.RoleNotAssigned", "Tài khoản không có vai trò này.");

        public static readonly Error AlreadyInactive =
            Error.Conflict("User.AlreadyInactive", "Tài khoản đã bị vô hiệu hoá.");

        public static readonly Error AlreadyActive =
            Error.Conflict("User.AlreadyActive", "Tài khoản đang hoạt động.");
    }

    public static class Roles
    {
        public static readonly Error TenantRequired =
            Error.Validation("Role.TenantRequired", "Vai trò phải thuộc về một workspace.");

        public static readonly Error NameEmpty =
            Error.Validation("Role.NameEmpty", "Tên vai trò không được để trống.");

        public static readonly Error NameTooLong =
            Error.Validation("Role.NameTooLong", "Tên vai trò không được dài quá 100 ký tự.");

        public static readonly Error PermissionEmpty =
            Error.Validation("Role.PermissionEmpty", "Tên quyền không được để trống.");

        public static readonly Error PermissionUnknown = Error.Validation(
            "Role.PermissionUnknown",
            "Quyền này không tồn tại trong hệ thống.");

        public static readonly Error PermissionAlreadyGranted =
            Error.Conflict("Role.PermissionAlreadyGranted", "Vai trò đã có quyền này.");

        public static readonly Error PermissionNotGranted =
            Error.Conflict("Role.PermissionNotGranted", "Vai trò không có quyền này.");

        public static readonly Error SystemRoleIsImmutable = Error.Conflict(
            "Role.SystemRoleIsImmutable",
            "Vai trò hệ thống không được phép sửa. Hãy tạo một vai trò mới nếu cần bộ quyền khác.");
    }

    public static class RefreshTokens
    {
        public static readonly Error OwnerRequired =
            Error.Validation("RefreshToken.OwnerRequired", "Thiếu tài khoản hoặc workspace của phiên.");

        public static readonly Error HashRequired =
            Error.Validation("RefreshToken.HashRequired", "Thiếu chuỗi băm của refresh token.");

        public static readonly Error InvalidLifetime =
            Error.Validation("RefreshToken.InvalidLifetime", "Thời hạn refresh token phải lớn hơn 0.");

        public static readonly Error InvalidReplacement =
            Error.Validation("RefreshToken.InvalidReplacement", "Token thay thế không hợp lệ.");

        public static readonly Error AlreadyRevoked =
            Error.Conflict("RefreshToken.AlreadyRevoked", "Refresh token đã bị thu hồi.");

        // CỐ Ý gộp "đã thu hồi" và "đã hết hạn" vào cùng một thông báo. Nói rõ cho
        // người gọi biết token hỏng vì lý do nào là giúp kẻ tấn công dò ra token nào
        // từng tồn tại. Chi tiết thật ghi ở log phía server, nơi chỉ mình đọc được.
        public static readonly Error NotActive = Error.Unauthorized(
            "RefreshToken.NotActive",
            "Phiên đăng nhập không còn hiệu lực. Vui lòng đăng nhập lại.");
    }
}
