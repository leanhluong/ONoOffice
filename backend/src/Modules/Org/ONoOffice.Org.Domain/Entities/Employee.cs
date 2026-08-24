using Luong.Kernel.Domain;
using Luong.Kernel.Primitives;
using ONoOffice.Org.Domain.ValueObjects;

namespace ONoOffice.Org.Domain.Entities;

/// <summary>
/// Hồ sơ nhân viên — thứ mà <c>Org</c> sở hữu.
///
/// <b>Không nhầm với tài khoản đăng nhập.</b> Cùng một con người, hai khái niệm khác
/// nhau, và chúng đổi vì những lý do khác nhau: đổi mật khẩu là chuyện của
/// <c>Identity</c>, điều chuyển phòng ban là chuyện ở đây.
///
/// Ba ca có thật khiến chúng KHÔNG được gộp:
/// <list type="bullet">
/// <item>Người nghỉ việc → đóng hồ sơ, nhưng <b>tài khoản vẫn còn</b> để tra lịch sử
/// thao tác của họ.</item>
/// <item>Tài khoản bot chạy sao lưu → có tài khoản, <b>không phải nhân viên nào</b>.</item>
/// <item>Nhân viên mới → có hồ sơ, <b>chưa được cấp tài khoản</b>.</item>
/// </list>
///
/// Xoá mềm: hồ sơ nhân viên là dữ liệu người ta còn phải tra lại sau nhiều năm — hợp
/// đồng, bảo hiểm, tranh chấp. Xoá cứng một hàng ở đây là mất một mảnh lịch sử công ty.
/// </summary>
public sealed class Employee : AggregateRoot<Guid>, ITenantScoped, IAuditable, ISoftDeletable
{
    private const int MaxCodeLength = 30;
    private const int MaxFullNameLength = 200;
    private const int MaxJobTitleLength = 100;
    private const int MaxPhoneLength = 30;

    private Employee(Guid id, Guid tenantId, string code, string fullName) : base(id)
    {
        TenantId = tenantId;
        Code = code;
        FullName = fullName;
        IsActive = true;
    }

    /// <summary>Dành cho EF Core.</summary>
    private Employee()
    {
        Code = null!;
        FullName = null!;
    }

    public Guid TenantId { get; private set; }

    /// <summary>Mã nhân viên do công ty đặt. Unique trong một workspace.</summary>
    public string Code { get; private set; }

    public string FullName { get; private set; }

    public string? JobTitle { get; private set; }

    /// <summary>Email liên hệ trên danh bạ — được phép bỏ trống. Xem <see cref="WorkEmail"/>.</summary>
    public WorkEmail? WorkEmail { get; private set; }

    public string? Phone { get; private set; }

    /// <summary><c>null</c> = chưa được xếp phòng. Đây là trạng thái BÌNH THƯỜNG của người mới.</summary>
    public Guid? DepartmentId { get; private set; }

    /// <summary>
    /// Tài khoản đăng nhập tương ứng, nếu đã cấp.
    ///
    /// Chỉ là một <c>Guid</c>, <b>không phải khoá ngoại</b> — <c>Identity</c> nằm ở
    /// schema khác, và Luật 3 cấm ràng buộc lẫn JOIN xuyên schema. Đó chính là thứ khiến
    /// sau này cắt hai module thành hai dịch vụ chỉ cần đổi chuỗi kết nối.
    /// </summary>
    public Guid? UserId { get; private set; }

    public DateOnly? HiredOn { get; private set; }

    public DateOnly? LeftOn { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public static Result<Employee> Create(
        Guid tenantId,
        string? code,
        string? fullName,
        string? workEmail,
        string? phone)
    {
        if (tenantId == Guid.Empty)
        {
            return OrgErrors.Employees.TenantRequired;
        }

        var validatedCode = ValidateCode(code);

        if (validatedCode.IsFailure)
        {
            return validatedCode.Error;
        }

        var validatedName = ValidateFullName(fullName);

        if (validatedName.IsFailure)
        {
            return validatedName.Error;
        }

        var employee = new Employee(Guid.NewGuid(), tenantId, validatedCode.Value, validatedName.Value);

        var contact = employee.UpdateContact(workEmail, phone);

        return contact.IsFailure ? contact.Error : employee;
    }

    public Result Rename(string? fullName)
    {
        var validated = ValidateFullName(fullName);

        if (validated.IsFailure)
        {
            return validated.Error;
        }

        FullName = validated.Value;

        return Result.Success();
    }

    public Result ChangeJobTitle(string? jobTitle)
    {
        string? trimmed = string.IsNullOrWhiteSpace(jobTitle) ? null : jobTitle.Trim();

        if (trimmed is not null && trimmed.Length > MaxJobTitleLength)
        {
            return OrgErrors.Employees.JobTitleTooLong;
        }

        JobTitle = trimmed;

        return Result.Success();
    }

    /// <summary>
    /// Cập nhật thông tin liên hệ. <b>Cả hai đều được phép bỏ trống.</b>
    ///
    /// Rỗng và sai định dạng là hai chuyện khác nhau, và đây là chỗ phân biệt: rỗng thì
    /// xoá giá trị cũ và thành công; có chữ nhưng không phải email thì từ chối. Nhập
    /// nhằng hai ca đó nghĩa là người dùng xoá email đi sẽ nhận thông báo "email không
    /// hợp lệ".
    /// </summary>
    public Result UpdateContact(string? workEmail, string? phone)
    {
        WorkEmail? email = null;

        if (!string.IsNullOrWhiteSpace(workEmail))
        {
            var parsed = ValueObjects.WorkEmail.Create(workEmail);

            if (parsed.IsFailure)
            {
                return parsed.Error;
            }

            email = parsed.Value;
        }

        string? trimmedPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

        if (trimmedPhone is not null && trimmedPhone.Length > MaxPhoneLength)
        {
            return OrgErrors.Employees.PhoneTooLong;
        }

        WorkEmail = email;
        Phone = trimmedPhone;

        return Result.Success();
    }

    public Result SetHiredOn(DateOnly? hiredOn)
    {
        HiredOn = hiredOn;

        return Result.Success();
    }

    /// <summary>
    /// Điều chuyển sang phòng khác, hoặc rút khỏi mọi phòng (<c>null</c>).
    ///
    /// Chuyển vào đúng phòng đang ở là bị từ chối, không phải cho qua im lặng: cho qua
    /// thì nhật ký thay đổi (lát 2) đầy những dòng "điều chuyển" mà không có gì đổi.
    /// </summary>
    public Result TransferTo(Guid? departmentId)
    {
        if (departmentId == DepartmentId)
        {
            return OrgErrors.Employees.AlreadyInThatDepartment;
        }

        DepartmentId = departmentId;

        return Result.Success();
    }

    public Result Leave(DateOnly leftOn)
    {
        if (!IsActive)
        {
            return OrgErrors.Employees.AlreadyLeft;
        }

        IsActive = false;
        LeftOn = leftOn;

        return Result.Success();
    }

    public Result Reinstate()
    {
        if (IsActive)
        {
            return OrgErrors.Employees.NotLeft;
        }

        IsActive = true;
        LeftOn = null;

        return Result.Success();
    }

    /// <summary>
    /// Nối hồ sơ với một tài khoản đăng nhập.
    ///
    /// Đã nối rồi thì KHÔNG gán đè: gán đè im lặng nghĩa là một lỗi lập trình có thể nối
    /// hồ sơ người này sang tài khoản người khác, và từ đó mọi thao tác của họ bị ghi tên
    /// nhầm người. Muốn đổi thì phải gỡ trước — một bước cố ý.
    /// </summary>
    public Result LinkAccount(Guid userId)
    {
        if (UserId is not null)
        {
            return OrgErrors.Employees.AlreadyLinked;
        }

        UserId = userId;

        return Result.Success();
    }

    public Result UnlinkAccount()
    {
        if (UserId is null)
        {
            return OrgErrors.Employees.NotLinked;
        }

        UserId = null;

        return Result.Success();
    }

    private static Result<string> ValidateCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return OrgErrors.Employees.CodeEmpty;
        }

        // VIẾT HOA và cắt khoảng trắng: mã nhân viên là thứ người ta gõ tay khi tra cứu.
        // Không chuẩn hoá thì "nv001" và "NV001" thành hai người, và ràng buộc UNIQUE
        // không bắt được.
        string normalized = code.Trim().ToUpperInvariant();

        return normalized.Length > MaxCodeLength ? OrgErrors.Employees.CodeTooLong : normalized;
    }

    private static Result<string> ValidateFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return OrgErrors.Employees.FullNameEmpty;
        }

        string trimmed = fullName.Trim();

        return trimmed.Length > MaxFullNameLength ? OrgErrors.Employees.FullNameTooLong : trimmed;
    }
}
