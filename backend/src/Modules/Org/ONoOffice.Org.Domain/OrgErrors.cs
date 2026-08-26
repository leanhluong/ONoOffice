using Luong.Kernel.Primitives;

namespace ONoOffice.Org.Domain;

/// <summary>
/// Toàn bộ mã lỗi của module Org, gom về một chỗ — cùng lý do với
/// <c>IdentityErrors</c>: frontend rẽ nhánh theo mã này, và đọc một file là thấy hết
/// mọi cách module có thể từ chối.
///
/// Quy ước đặt mã: <c>{Vùng}.{ChuyệnGìXảyRa}</c>.
/// </summary>
public static class OrgErrors
{
    public static class WorkEmails
    {
        public static readonly Error Invalid =
            Error.Validation("WorkEmail.Invalid", "Email liên hệ không đúng định dạng.");

        public static readonly Error TooLong =
            Error.Validation("WorkEmail.TooLong", "Email liên hệ không được dài quá 254 ký tự.");
    }

    public static class Departments
    {
        public static readonly Error TenantRequired =
            Error.Validation("Department.TenantRequired", "Phòng ban phải thuộc về một workspace.");

        public static readonly Error NameEmpty =
            Error.Validation("Department.NameEmpty", "Tên phòng ban không được để trống.");

        public static readonly Error NameTooLong =
            Error.Validation("Department.NameTooLong", "Tên phòng ban không được dài quá 100 ký tự.");

        /// <summary>
        /// Ca vòng lặp DUY NHẤT mà một phòng ban tự nhìn thấy được. Vòng lặp qua nhiều
        /// cấp thì phải để handler bắt — nó mới đọc được cả cây.
        /// </summary>
        public static readonly Error CannotBeItsOwnParent = Error.Validation(
            "Department.CannotBeItsOwnParent",
            "Một phòng ban không thể là phòng ban cha của chính nó.");

        public static readonly Error WouldCreateCycle = Error.Conflict(
            "Department.WouldCreateCycle",
            "Không thể chuyển một phòng ban vào bên trong chính nhánh của nó.");

        public static readonly Error NameTaken =
            Error.Conflict("Department.NameTaken", "Workspace đã có phòng ban trùng tên này.");

        public static readonly Error NotFound =
            Error.NotFound("Department.NotFound", "Không tìm thấy phòng ban.");

        public static readonly Error HasChildren = Error.Conflict(
            "Department.HasChildren",
            "Phòng ban còn phòng ban con. Hãy chuyển hoặc xoá các phòng con trước.");

        public static readonly Error HasEmployees = Error.Conflict(
            "Department.HasEmployees",
            "Phòng ban còn nhân viên. Hãy điều chuyển họ sang phòng khác trước.");
    }

    public static class Employees
    {
        public static readonly Error TenantRequired =
            Error.Validation("Employee.TenantRequired", "Nhân viên phải thuộc về một workspace.");

        public static readonly Error CodeEmpty =
            Error.Validation("Employee.CodeEmpty", "Mã nhân viên không được để trống.");

        public static readonly Error CodeTooLong =
            Error.Validation("Employee.CodeTooLong", "Mã nhân viên không được dài quá 30 ký tự.");

        public static readonly Error CodeTaken =
            Error.Conflict("Employee.CodeTaken", "Workspace đã có nhân viên mang mã này.");

        public static readonly Error FullNameEmpty =
            Error.Validation("Employee.FullNameEmpty", "Họ tên không được để trống.");

        public static readonly Error FullNameTooLong =
            Error.Validation("Employee.FullNameTooLong", "Họ tên không được dài quá 200 ký tự.");

        public static readonly Error JobTitleTooLong =
            Error.Validation("Employee.JobTitleTooLong", "Chức danh không được dài quá 100 ký tự.");

        public static readonly Error PhoneTooLong =
            Error.Validation("Employee.PhoneTooLong", "Số điện thoại không được dài quá 30 ký tự.");

        public static readonly Error NotFound =
            Error.NotFound("Employee.NotFound", "Không tìm thấy nhân viên.");

        public static readonly Error AlreadyInThatDepartment =
            Error.Conflict("Employee.AlreadyInThatDepartment", "Nhân viên đã thuộc phòng ban này.");

        public static readonly Error AlreadyLeft =
            Error.Conflict("Employee.AlreadyLeft", "Nhân viên đã nghỉ việc.");

        public static readonly Error NotLeft =
            Error.Conflict("Employee.NotLeft", "Nhân viên đang làm việc, không cần nhận lại.");

        public static readonly Error AlreadyLinked = Error.Conflict(
            "Employee.AlreadyLinked",
            "Hồ sơ này đã nối với một tài khoản. Hãy gỡ liên kết cũ trước.");

        public static readonly Error NotLinked =
            Error.Conflict("Employee.NotLinked", "Hồ sơ này chưa nối với tài khoản nào.");

        /// <summary>
        /// Nhìn từ phía TÀI KHOẢN, khác <see cref="AlreadyLinked"/> nhìn từ phía hồ sơ.
        ///
        /// Hai câu phải khác nhau vì cách sửa khác nhau: <c>AlreadyLinked</c> thì gỡ hồ sơ
        /// đang mở ra là xong; còn ở đây phải đi tìm hồ sơ KHÁC đang giữ tài khoản đó.
        /// </summary>
        public static readonly Error UserAlreadyLinked = Error.Conflict(
            "Employee.UserAlreadyLinked",
            "Tài khoản này đã nối với một hồ sơ khác.");
    }
}
