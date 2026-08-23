using System.Collections.ObjectModel;
using Luong.Kernel.Domain;
using Luong.Kernel.Primitives;

namespace ONoOffice.Identity.Domain.Entities;

/// <summary>
/// Vai trò — cái TÚI đựng quyền, thuộc về một workspace.
///
/// Vai trò tồn tại chỉ vì con người không quản nổi việc gán 40 quyền lẻ cho từng người.
/// Nó là chuyện tiện lợi của người quản trị, KHÔNG phải khái niệm mà code cần biết:
/// code luôn hỏi <c>"có quyền employee.write không?"</c>, không bao giờ hỏi
/// <c>"có phải HR không?"</c>.
///
/// Nhờ vậy hôm nào công ty muốn thêm vai <i>"Trợ lý nhân sự"</i> — sửa được hồ sơ nhưng
/// không xoá được — thì chỉ cần tạo vai mới rồi tick vài ô, <b>không đụng một dòng code</b>.
/// </summary>
public sealed class Role : AggregateRoot<Guid>
{
    private const int MaxNameLength = 100;

    private readonly HashSet<string> _permissions = new(StringComparer.OrdinalIgnoreCase);

    private ReadOnlySet<string>? _readOnlyPermissions;

    private Role(Guid id, Guid tenantId, string name, bool isSystem) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        IsSystem = isSystem;
    }

    /// <summary>Dành cho EF Core.</summary>
    private Role() => Name = null!;

    public Guid TenantId { get; private set; }

    public string Name { get; private set; }

    /// <summary>
    /// Vai trò do hệ thống gieo sẵn khi tạo workspace (<c>Owner</c>, <c>Admin</c>,
    /// <c>Manager</c>, <c>Member</c>). <b>Bất biến</b> — xem lý do ở <see cref="Grant"/>.
    /// </summary>
    public bool IsSystem { get; private set; }

    /// <summary>
    /// Bọc trong <see cref="ReadOnlySet{T}"/> chứ KHÔNG trả thẳng <c>HashSet</c>.
    ///
    /// Khai kiểu trả về là <c>IReadOnlySet</c> mà bên trong vẫn là <c>HashSet</c> thì
    /// người gọi chỉ cần ép kiểu về <c>ICollection&lt;string&gt;</c> là thêm/xoá được —
    /// đi vòng qua toàn bộ luật của <see cref="Grant"/>/<see cref="Revoke"/>, kể cả luật
    /// "vai trò hệ thống bất biến". Bản bọc chặn đường đó lại thật sự.
    ///
    /// Có một test canh đúng chuyện này, và nó đã bắt được lỗi ở lần viết đầu tiên.
    /// </summary>
    public IReadOnlySet<string> Permissions => _readOnlyPermissions ??= new ReadOnlySet<string>(_permissions);

    public static Result<Role> Create(Guid tenantId, string? name) =>
        Build(tenantId, name, isSystem: false, permissions: []);

    public static Result<Role> CreateSystem(Guid tenantId, string? name, IEnumerable<string> permissions) =>
        Build(tenantId, name, isSystem: true, permissions);

    public Result Grant(string? permission)
    {
        if (IsSystem)
        {
            // Vai trò hệ thống bất biến. Cho sửa thì một cú bấm nhầm có thể thu hết quyền
            // của Owner — và lúc đó KHÔNG CÒN AI trong workspace cấp lại được cho ai cả.
            // Đây là loại lỗi không có đường tự cứu, phải can thiệp thẳng vào database.
            return IdentityErrors.Roles.SystemRoleIsImmutable;
        }

        var normalized = Normalize(permission);

        if (normalized.IsFailure)
        {
            return normalized.Error;
        }

        return _permissions.Add(normalized.Value)
            ? Result.Success()
            : IdentityErrors.Roles.PermissionAlreadyGranted;
    }

    public Result Revoke(string? permission)
    {
        if (IsSystem)
        {
            return IdentityErrors.Roles.SystemRoleIsImmutable;
        }

        var normalized = Normalize(permission);

        if (normalized.IsFailure)
        {
            return normalized.Error;
        }

        return _permissions.Remove(normalized.Value)
            ? Result.Success()
            : IdentityErrors.Roles.PermissionNotGranted;
    }

    public bool Has(string? permission) =>
        !string.IsNullOrWhiteSpace(permission) && _permissions.Contains(permission.Trim());

    public Result Rename(string? name)
    {
        if (IsSystem)
        {
            return IdentityErrors.Roles.SystemRoleIsImmutable;
        }

        var nameResult = ValidateName(name);

        if (nameResult.IsFailure)
        {
            return nameResult.Error;
        }

        Name = nameResult.Value;

        return Result.Success();
    }

    private static Result<Role> Build(Guid tenantId, string? name, bool isSystem, IEnumerable<string> permissions)
    {
        if (tenantId == Guid.Empty)
        {
            return IdentityErrors.Roles.TenantRequired;
        }

        var nameResult = ValidateName(name);

        if (nameResult.IsFailure)
        {
            return nameResult.Error;
        }

        var role = new Role(Guid.NewGuid(), tenantId, nameResult.Value, isSystem);

        foreach (string permission in permissions)
        {
            var normalized = Normalize(permission);

            if (normalized.IsFailure)
            {
                return normalized.Error;
            }

            role._permissions.Add(normalized.Value);
        }

        return role;
    }

    /// <summary>
    /// Kiểm quyền có THẬT SỰ tồn tại, rồi chuẩn hoá.
    ///
    /// Đây là luật đáng giá nhất của lớp này. Gõ nhầm <c>"employee.raed"</c> mà vẫn cấp
    /// được thì vai trò đó không bao giờ có tác dụng — và <b>không có lỗi nào báo</b>.
    /// Người dùng chỉ thấy "được cấp quyền rồi mà vẫn không vào được", còn người quản trị
    /// mở màn hình lên thì thấy quyền nằm đó rành rành.
    /// </summary>
    private static Result<string> Normalize(string? permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            return IdentityErrors.Roles.PermissionEmpty;
        }

        string trimmed = permission.Trim();

        return Domain.Permissions.Contains(trimmed)
            ? trimmed.ToLowerInvariant()
            : IdentityErrors.Roles.PermissionUnknown;
    }

    private static Result<string> ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return IdentityErrors.Roles.NameEmpty;
        }

        string trimmed = name.Trim();

        return trimmed.Length > MaxNameLength
            ? IdentityErrors.Roles.NameTooLong
            : trimmed;
    }
}
