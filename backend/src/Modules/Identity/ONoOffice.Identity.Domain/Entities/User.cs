using Luong.Kernel.Domain;
using Luong.Kernel.Primitives;
using ONoOffice.Identity.Domain.Events;
using ONoOffice.Identity.Domain.ValueObjects;

namespace ONoOffice.Identity.Domain.Entities;

/// <summary>
/// Tài khoản đăng nhập — ai được vào hệ thống và được làm gì.
///
/// <b>Không phải hồ sơ nhân viên.</b> Hồ sơ (họ tên đầy đủ, mã NV, phòng ban, chức danh)
/// thuộc module <c>Org</c>. Ở đây chỉ giữ <see cref="FullName"/> để hiện lên góc màn hình
/// sau khi đăng nhập — không phải để tra cứu nhân sự.
///
/// Người nghỉ việc thì <c>Org</c> đóng hồ sơ, nhưng tài khoản này VẪN CÒN để tra lại
/// "ai đã duyệt đơn kia năm ngoái". Đó là lý do hai module tách nhau.
///
/// <b>Domain không biết băm mật khẩu bằng gì.</b> Argon2id là chuyện của Infrastructure.
/// Ở đây chỉ nhận chuỗi băm đã có sẵn — và biết chắc một điều: chuỗi rỗng nghĩa là ai đó
/// đã bỏ qua bước băm, và tài khoản như vậy tuyệt đối không được tồn tại.
/// </summary>
public sealed class User : AggregateRoot<Guid>
{
    private const int MaxFullNameLength = 200;

    private readonly List<Guid> _roleIds = [];

    private User(Guid id, Guid tenantId, Email email, string passwordHash, string fullName) : base(id)
    {
        TenantId = tenantId;
        Email = email;
        PasswordHash = passwordHash;
        FullName = fullName;
        IsActive = true;
    }

    /// <summary>Dành cho EF Core.</summary>
    private User()
    {
        Email = null!;
        PasswordHash = null!;
        FullName = null!;
    }

    /// <summary>
    /// Workspace mà tài khoản này thuộc về. Mỗi người thuộc ĐÚNG một workspace —
    /// xem <c>docs/02-kien-truc/adr/ADR-0002</c>.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>Unique TOÀN HỆ THỐNG, không phải chỉ trong một workspace. Lý do ở ADR-0002.</summary>
    public Email Email { get; private set; }

    /// <summary>Chuỗi băm. KHÔNG BAO GIỜ là mật khẩu thô, dù chỉ trong bộ nhớ.</summary>
    public string PasswordHash { get; private set; }

    public string FullName { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>
    /// Chỉ đọc. Trả thẳng <c>List</c> ra ngoài thì bất kỳ ai cũng <c>Add</c>/<c>Remove</c>
    /// được mà không đi qua luật của aggregate — và lúc đó mọi luật viết trong
    /// <see cref="AssignRole"/>/<see cref="RemoveRole"/> chỉ còn là trang trí.
    /// </summary>
    public IReadOnlyList<Guid> RoleIds => _roleIds.AsReadOnly();

    public static Result<User> Create(Guid tenantId, string? email, string? passwordHash, string? fullName)
    {
        if (tenantId == Guid.Empty)
        {
            return IdentityErrors.Users.TenantRequired;
        }

        var emailResult = Email.Create(email);

        if (emailResult.IsFailure)
        {
            return emailResult.Error;
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return IdentityErrors.Users.PasswordHashRequired;
        }

        var nameResult = ValidateFullName(fullName);

        if (nameResult.IsFailure)
        {
            return nameResult.Error;
        }

        var user = new User(Guid.NewGuid(), tenantId, emailResult.Value, passwordHash, nameResult.Value);

        user.Raise(new UserCreated(user.Id, user.TenantId, user.Email.Value));

        return user;
    }

    public Result AssignRole(Guid roleId)
    {
        if (_roleIds.Contains(roleId))
        {
            return IdentityErrors.Users.RoleAlreadyAssigned;
        }

        _roleIds.Add(roleId);

        return Result.Success();
    }

    public Result RemoveRole(Guid roleId) =>
        _roleIds.Remove(roleId) ? Result.Success() : IdentityErrors.Users.RoleNotAssigned;

    public Result ChangePassword(string? newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            return IdentityErrors.Users.PasswordHashRequired;
        }

        PasswordHash = newPasswordHash;

        // Sự kiện này bắt buộc phải có: nơi khác lắng nghe nó để THU HỒI mọi refresh
        // token đang sống. Thiếu nó thì người vừa bị lộ mật khẩu đổi lại mật khẩu, mà
        // kẻ trộm vẫn ngồi yên trong phiên cũ suốt 30 ngày — đổi mật khẩu thành vô nghĩa.
        Raise(new UserPasswordChanged(Id, TenantId));

        return Result.Success();
    }

    public Result Rename(string? fullName)
    {
        var nameResult = ValidateFullName(fullName);

        if (nameResult.IsFailure)
        {
            return nameResult.Error;
        }

        FullName = nameResult.Value;

        return Result.Success();
    }

    public Result Deactivate()
    {
        if (!IsActive)
        {
            return IdentityErrors.Users.AlreadyInactive;
        }

        IsActive = false;

        // Cũng dùng để thu hồi phiên đăng nhập: khoá tài khoản mà token cũ vẫn dùng được
        // thì việc khoá chỉ có hiệu lực sau khi access token hết hạn.
        Raise(new UserDeactivated(Id, TenantId));

        return Result.Success();
    }

    public Result Activate()
    {
        if (IsActive)
        {
            return IdentityErrors.Users.AlreadyActive;
        }

        IsActive = true;

        return Result.Success();
    }

    private static Result<string> ValidateFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return IdentityErrors.Users.FullNameEmpty;
        }

        string trimmed = fullName.Trim();

        return trimmed.Length > MaxFullNameLength
            ? IdentityErrors.Users.FullNameTooLong
            : trimmed;
    }
}
