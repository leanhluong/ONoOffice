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
public sealed class User : AggregateRoot<Guid>, ITenantScoped, IAuditable
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
    /// Buộc đổi mật khẩu ở lần đăng nhập tới.
    ///
    /// Bật khi quản trị viên tạo tài khoản HỘ người khác. Lát này chưa có dịch vụ gửi
    /// email, nên mật khẩu tạm đi qua tin nhắn, lời nói hoặc mẩu giấy — nó coi như đã lộ
    /// ngay từ lúc sinh ra. Không bắt đổi thì nó nằm nguyên nhiều tháng.
    ///
    /// Cờ này KHÔNG chặn đăng nhập. Chặn ở tầng đăng nhập thì người dùng kẹt cứng: muốn
    /// đổi mật khẩu phải đăng nhập, mà muốn đăng nhập phải đổi mật khẩu. Họ vẫn vào được,
    /// giao diện chỉ đưa thẳng tới màn đổi mật khẩu.
    /// </summary>
    public bool MustChangePassword { get; private set; }

    // Hạ tầng tự điền qua AuditableEntityInterceptor. Public setter là nhượng bộ có chủ ý
    // của Luong.Kernel: đổi lại, KHÔNG chỗ nào trong tầng nghiệp vụ được phép gán tay —
    // gán tay nghĩa là đang nói dối về thời điểm.
    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

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

        // Đổi xong thì hết lý do bắt đổi. Không tắt ở đây thì người dùng bị hỏi lại mãi,
        // và họ sẽ đổi đi đổi lại vài lần trước khi kết luận là app hỏng.
        //
        // Đặt SAU phép kiểm băm rỗng ở trên là có chủ ý: băm hỏng thì mật khẩu không đổi,
        // nên cờ cũng không được tắt — nếu không, gửi một request hỏng là thoát được yêu
        // cầu đổi mật khẩu.
        MustChangePassword = false;

        // ⚠️ Sự kiện này HIỆN CHƯA CÓ AI LẮNG NGHE. Việc thu hồi refresh token làm thẳng
        // trong ChangeMyPasswordCommandHandler.
        //
        // Bình luận cũ ở đây nói rằng "nơi khác lắng nghe nó để thu hồi" — điều đó chưa
        // bao giờ đúng, và một bình luận sai còn nguy hiểm hơn không có bình luận: người
        // đọc tin là đã có lớp bảo vệ đó rồi.
        //
        // Vẫn phát sự kiện, vì nó là dữ kiện nghiệp vụ có thật và sẽ cần khi có nhật ký
        // kiểm toán. Nhưng đừng dựa vào nó cho tới khi có consumer thật.
        Raise(new UserPasswordChanged(Id, TenantId));

        return Result.Success();
    }

    /// <summary>
    /// Đánh dấu tài khoản này phải đổi mật khẩu ở lần đăng nhập tới.
    ///
    /// Không trả <c>Result</c> vì không có ca nào hỏng: gọi hai lần cũng chỉ là bật một
    /// cờ đã bật. Trả <c>Result</c> ở đây chỉ ép mọi nơi gọi phải viết một nhánh lỗi chết.
    /// </summary>
    public void RequirePasswordChange() => MustChangePassword = true;

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

        // ⚠️ Cũng CHƯA CÓ AI LẮNG NGHE. Trên thực tế việc khoá vẫn có hiệu lực, nhưng
        // bằng đường khác: RefreshTokenCommandHandler nạp lại IsUserActive ở mỗi lần
        // gia hạn và từ chối tài khoản đã khoá. Nghĩa là người bị khoá mất quyền truy cập
        // trong vòng 15 phút — đúng bằng tuổi thọ của access token, và đó chính là lý do
        // access token cố tình ngắn.
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
