using Luong.Kernel.Abstractions;
using Luong.Kernel.Pagination;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Application.Me.GetProfile;
using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.UnitTests.Fakes;

/// <summary>
/// Bản giả <b>đầy đủ</b> của mỗi cổng, với hành vi mặc định vô hại.
///
/// <b>Vì sao gom vào đây thay vì mỗi test tự viết:</b> mỗi lần thêm một phương thức vào
/// một cổng, sáu file test lại đỏ vì thiếu thành viên — dù năm trong sáu file đó chẳng
/// quan tâm gì tới phương thức mới. Đó không phải là test bắt lỗi, đó là công việc tay
/// chân, và nó đủ nhàm để người ta bắt đầu dán bừa cho hết đỏ.
///
/// Với bộ này, một test chỉ ghi đè đúng thứ nó cần. Đọc test cũng dễ hơn: những gì được
/// ghi đè CHÍNH LÀ những gì use case đó dùng tới.
///
/// Mặc định chọn theo hướng "không có gì": trả <c>null</c>, danh sách rỗng, <c>false</c>.
/// Test nào cần dữ liệu thì phải nói ra — đó là điều đáng đọc trong test.
/// </summary>
public class FakeUserRepository : IUserRepository
{
    public readonly List<User> Added = [];

    /// <summary>Số người đang giữ một vai bất kỳ — dùng cho phép kiểm "vai còn người giữ".</summary>
    public int CountByRole;

    public virtual Task<int> CountByRoleAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        Task.FromResult(CountByRole);

    /// <summary>Email bị coi là đã có người dùng. <c>null</c> = mọi email đều trống.</summary>
    public string? TakenEmail;

    /// <summary>Tài khoản mà <see cref="GetForUpdateAsync"/> trả về nếu khớp mã.</summary>
    public User? Existing;

    /// <summary>Dữ liệu cho đường đăng nhập và gia hạn phiên.</summary>
    public AuthUserData? AuthData;

    public MyProfile? Profile;

    public PagedList<UserListItem> SearchResult = PagedList<UserListItem>.Create([], 1, 20, 0);

    /// <summary>Điều kiện lọc mà handler đã truyền xuống — dùng để kiểm phần làm sạch.</summary>
    public UserSearch? ReceivedSearch;

    public virtual void Add(User user) => Added.Add(user);

    public virtual Task<bool> IsEmailTakenAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(string.Equals(TakenEmail, email, StringComparison.OrdinalIgnoreCase));

    public virtual Task<AuthUserData?> GetForLoginAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(AuthData);

    public virtual Task<AuthUserData?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(AuthData);

    public virtual Task<PagedList<UserListItem>> SearchAsync(
        UserSearch criteria,
        CancellationToken cancellationToken = default)
    {
        ReceivedSearch = criteria;

        return Task.FromResult(SearchResult);
    }

    public virtual Task<User?> GetForUpdateAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Existing?.Id == userId ? Existing : null);

    public virtual Task<MyProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Profile);
}

public class FakeRoleRepository : IRoleRepository
{
    public readonly List<Role> Added = [];

    /// <summary>Vai trò mà <see cref="GetByIdAsync"/> trả về nếu khớp mã.</summary>
    public Role? Existing;

    public IReadOnlyList<RoleListItem> All = [];

    public virtual void AddRange(IEnumerable<Role> roles) => Added.AddRange(roles);

    public virtual Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Existing?.Id == roleId ? Existing : null);

    public virtual Task<IReadOnlyList<RoleListItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(All);

    public virtual Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(Existing?.Name == name ? Existing : null);

    public readonly List<Role> Removed = [];

    /// <summary>Đặt `true` để mọi phép kiểm trùng tên trả về "đã có người dùng".</summary>
    public bool NameTaken;

    public virtual void Add(Role role) => Added.Add(role);

    public virtual void Remove(Role role) => Removed.Add(role);

    public virtual Task<bool> NameTakenAsync(
        string name,
        Guid? exceptId,
        CancellationToken cancellationToken = default) => Task.FromResult(NameTaken);
}

public class FakeTenantRepository : ITenantRepository
{
    public readonly List<Tenant> Added = [];

    public string? TakenCode;

    public Guid? OwnerUserId;

    public virtual void Add(Tenant tenant) => Added.Add(tenant);

    public virtual Task<bool> IsCodeTakenAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(string.Equals(TakenCode, code, StringComparison.OrdinalIgnoreCase));

    public virtual Task<Guid?> GetOwnerUserIdAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(OwnerUserId);

    public virtual Task<Tenant?> GetCurrentForUpdateAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<Tenant?>(null);
}

public class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    public readonly List<RefreshToken> Added = [];

    public RefreshToken? Existing;

    /// <summary>Mã người bị thu hồi sạch phiên. <c>null</c> = chưa ai bị thu hồi.</summary>
    public Guid? RevokedFor;

    public virtual void Add(RefreshToken token) => Added.Add(token);

    public virtual Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        Task.FromResult(Existing);

    public virtual Task<int> RevokeAllForUserAsync(
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        RevokedFor = userId;

        return Task.FromResult(Added.Count);
    }
}

/// <summary>Người đang thao tác. <c>UserId</c> đặt được để thử ca "chính mình".</summary>
public sealed class FakeCurrentUser : ICurrentUser
{
    public Guid? UserId { get; set; } = Guid.NewGuid();

    public bool IsAuthenticated => UserId is not null;

    public IReadOnlySet<string> Permissions { get; set; } = new HashSet<string>();

    public bool HasPermission(string permission) => Permissions.Contains(permission);
}

public sealed class FakeCurrentTenant : ICurrentTenant
{
    public Guid? TenantId { get; set; } = Guid.NewGuid();
}

/// <summary>Đồng hồ đứng yên — mọi test về thời gian phải nói rõ mốc của nó.</summary>
public sealed class FakeClock(DateTimeOffset? now = null) : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; } = now ?? new DateTimeOffset(2026, 8, 24, 10, 0, 0, TimeSpan.Zero);
}
