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

        /// <summary>
        /// Email là danh tính, unique TOÀN hệ thống (ADR-0002) — nên trùng là chặn ngay
        /// ở khâu đăng ký, chứ không để tới lúc INSERT rồi nhận một lỗi ràng buộc thô.
        /// </summary>
        public static readonly Error Taken = Error.Conflict(
            "Email.Taken",
            "Email này đã có tài khoản. Bạn có muốn đăng nhập không?");
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

        public static readonly Error Taken = Error.Conflict(
            "TenantCode.Taken",
            "Mã workspace này đã có người dùng.");
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

        /// <summary>
        /// Đọc từ DATABASE, không tin claim trong token.
        ///
        /// Access token sống 15 phút, nên người vừa mất quyền sở hữu vẫn cầm một token
        /// mang <c>workspace.transfer-ownership</c> thêm một lúc. Không có phép kiểm này
        /// thì họ chuyển ngược lại được trong khoảng thời gian đó.
        /// </summary>
        public static readonly Error OnlyOwnerCanTransfer = Error.Conflict(
            "Tenant.OnlyOwnerCanTransfer",
            "Chỉ chủ sở hữu hiện tại mới chuyển nhượng được workspace.");

        /// <summary>
        /// Chuyển cho một tài khoản đang bị vô hiệu hoá là khoá cả workspace lại: người cũ
        /// mất quyền, người mới không đăng nhập được. Không có đường sửa nào ngoài can
        /// thiệp thẳng vào database.
        /// </summary>
        public static readonly Error NewOwnerMustBeActive = Error.Conflict(
            "Tenant.NewOwnerMustBeActive",
            "Không thể chuyển nhượng cho một tài khoản đang bị vô hiệu hoá.");

        public static readonly Error AlreadyInactive =
            Error.Conflict("Tenant.AlreadyInactive", "Workspace đã ngừng hoạt động.");

        public static readonly Error AlreadyActive =
            Error.Conflict("Tenant.AlreadyActive", "Workspace đang hoạt động.");
    }

    public static class Users
    {
        public static readonly Error TenantRequired =
            Error.Validation("User.TenantRequired", "Tài khoản phải thuộc về một workspace.");

        public static readonly Error NotFound =
            Error.NotFound("User.NotFound", "Không tìm thấy tài khoản này.");

        /// <summary>
        /// Tự khoá chính mình là cách nhanh nhất để một workspace mất hết quản trị viên.
        /// Chặn ở đây rẻ hơn nhiều so với đi khôi phục bằng tay trong database.
        /// </summary>
        public static readonly Error CannotDisableSelf =
            Error.Conflict("User.CannotDisableSelf", "Bạn không thể tự vô hiệu hoá tài khoản của chính mình.");

        /// <summary>
        /// Chủ sở hữu là người DUY NHẤT chuyển nhượng được workspace. Khoá họ lại thì
        /// không còn ai làm được việc đó, và workspace kẹt vĩnh viễn.
        /// </summary>
        public static readonly Error CannotDisableOwner =
            Error.Conflict("User.CannotDisableOwner", "Không thể vô hiệu hoá chủ sở hữu. Hãy chuyển nhượng quyền sở hữu trước.");

        public static readonly Error CannotChangeOwnerRole =
            Error.Conflict("User.CannotChangeOwnerRole", "Không thể đổi vai trò của chủ sở hữu. Hãy chuyển nhượng quyền sở hữu trước.");

        /// <summary>
        /// Chặn LEO THANG QUYỀN, không phải chặn nhầm lẫn.
        ///
        /// Đặt lại mật khẩu của ai đó nghĩa là đăng nhập được dưới danh nghĩa người đó.
        /// Admin có 11/12 quyền; thứ duy nhất họ thiếu là chuyển nhượng quyền sở hữu. Cho
        /// họ đặt lại mật khẩu của chủ sở hữu thì họ đăng nhập thành chủ sở hữu rồi tự
        /// chuyển nhượng — ranh giới Admin ↔ Owner biến mất, dù bảng phân quyền vẫn đúng.
        /// </summary>
        public static readonly Error CannotResetOwnerPassword = Error.Conflict(
            "User.CannotResetOwnerPassword",
            "Không thể đặt lại mật khẩu của chủ sở hữu. Chỉ chính họ làm được việc đó.");

        /// <summary>
        /// Đổi mật khẩu của chính mình phải đi qua <c>POST /api/me/password</c>, vì đường
        /// đó đòi <b>mật khẩu hiện tại</b>. Cho đi vòng qua đây là bỏ hẳn phép kiểm ấy: một
        /// máy bỏ quên lúc đang đăng nhập là đủ để người khác chiếm hẳn tài khoản.
        /// </summary>
        public static readonly Error CannotResetOwnPassword = Error.Conflict(
            "User.CannotResetOwnPassword",
            "Hãy đổi mật khẩu của chính bạn ở màn Hồ sơ — ở đó có kiểm mật khẩu hiện tại.");

        public static readonly Error WrongCurrentPassword =
            Error.Validation("User.WrongCurrentPassword", "Mật khẩu hiện tại không đúng.");

        public static readonly Error NewPasswordSameAsCurrent =
            Error.Validation("User.NewPasswordSameAsCurrent", "Mật khẩu mới phải khác mật khẩu hiện tại.");

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

        public static readonly Error NotFound =
            Error.NotFound("Role.NotFound", "Vai trò này không tồn tại trong workspace.");

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

        public static readonly Error NameTaken =
            Error.Conflict("Role.NameTaken", "Workspace đã có vai trò mang tên này.");

        /// <summary>
        /// Vai còn người giữ thì không xoá.
        ///
        /// Xoá đi thì những người đó mang một mã vai không còn tồn tại — mất SẠCH quyền
        /// ngay lập tức, và màn Thành viên hiện một ô vai trống. Bắt điều chuyển họ trước
        /// thì người quản trị buộc phải quyết định họ sẽ thành vai gì.
        /// </summary>
        public static readonly Error StillInUse = Error.Conflict(
            "Role.StillInUse",
            "Vai trò này vẫn còn người giữ. Hãy đổi vai cho họ trước khi xoá.");

        /// <summary>
        /// <c>workspace.transfer-ownership</c> chỉ thuộc về vai <c>Owner</c>.
        ///
        /// Nó là TOÀN BỘ ranh giới giữa Admin và Owner (xem <c>SystemRoles.cs</c>). Cho nó
        /// rơi vào một vai tự đặt thì màn Vai trò hiện một dòng quyền không bao giờ làm
        /// được gì — <c>TransferOwnershipCommandHandler</c> vẫn đọc <c>Tenant.OwnerUserId</c>
        /// từ database và từ chối — và người quản trị tin rằng họ vừa trao đi thứ mình
        /// không trao.
        /// </summary>
        public static readonly Error PermissionIsOwnerOnly = Error.Conflict(
            "Role.PermissionIsOwnerOnly",
            "Quyền chuyển nhượng workspace chỉ thuộc về chủ sở hữu, không gán cho vai trò khác được.");
    }

    public static class Auth
    {
        /// <summary>
        /// MỘT thông báo duy nhất cho cả "email không tồn tại" lẫn "sai mật khẩu".
        ///
        /// Tách bạch hai ca là tặng công cụ dò tài khoản: gõ 10.000 email, cái nào báo
        /// "sai mật khẩu" nghĩa là email đó CÓ THẬT trong hệ thống — từ đó tập trung
        /// tấn công đúng những email có thật.
        /// </summary>
        public static readonly Error InvalidCredentials =
            Error.Unauthorized("Auth.InvalidCredentials", "Email hoặc mật khẩu không đúng.");

        // Hai lỗi dưới đây CỐ Ý nói thẳng, khác với lỗi trên. Người tới được đây đã gõ
        // ĐÚNG mật khẩu, tức là gần như chắc chắn chủ tài khoản thật. Giấu thì họ gọi
        // điện cho IT hỏi "sao tôi không vào được" — tốn thời gian cả hai bên mà không
        // bảo vệ được gì.
        public static readonly Error AccountDisabled = Error.Forbidden(
            "Auth.AccountDisabled",
            "Tài khoản đã bị vô hiệu hoá. Vui lòng liên hệ quản trị viên.");

        /// <summary>
        /// Một thông báo duy nhất cho MỌI ca hỏng của refresh token: không tìm thấy,
        /// hết hạn, đã thu hồi, hay bị dùng lại.
        ///
        /// Nói rõ "vé này đã bị dùng lại" là mách cho kẻ tấn công biết hệ thống đang
        /// theo dõi được nó — và hắn sẽ đổi cách. Chi tiết thật ghi ở log server.
        /// </summary>
        public static readonly Error InvalidRefreshToken = Error.Unauthorized(
            "Auth.InvalidRefreshToken",
            "Phiên đăng nhập không còn hiệu lực. Vui lòng đăng nhập lại.");

        public static readonly Error WorkspaceDisabled = Error.Forbidden(
            "Auth.WorkspaceDisabled",
            "Workspace đã ngừng hoạt động. Vui lòng liên hệ quản trị viên.");
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
