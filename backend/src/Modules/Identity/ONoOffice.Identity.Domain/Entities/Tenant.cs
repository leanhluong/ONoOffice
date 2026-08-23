using Luong.Kernel.Domain;
using Luong.Kernel.Primitives;
using ONoOffice.Identity.Domain.Events;
using ONoOffice.Identity.Domain.ValueObjects;

namespace ONoOffice.Identity.Domain.Entities;

/// <summary>
/// Một workspace — tức là một công ty dùng ONoOffice.
///
/// Đây là gốc của toàn bộ cây dữ liệu: mọi bảng nghiệp vụ khác đều mang <c>tenant_id</c>
/// trỏ về đây. Xem <c>docs/02-kien-truc/adr/ADR-0001</c>.
///
/// <b>Vì sao mọi hành vi đều trả <see cref="Result"/> chứ không ném exception:</b>
/// "đã có chủ rồi", "đang tắt sẵn rồi" đều là những câu TRẢ LỜI bình thường của nghiệp
/// vụ, không phải sự cố. Ném exception cho chúng vừa đắt, vừa giấu mất luồng thật của
/// code — đọc chữ ký hàm không biết được nó có thể từ chối kiểu gì.
/// </summary>
public sealed class Tenant : AggregateRoot<Guid>, IAuditable
{
    private const int MaxNameLength = 200;

    private Tenant(Guid id, TenantCode code, string name) : base(id)
    {
        Code = code;
        Name = name;
        IsActive = true;
    }

    /// <summary>Dành cho EF Core dựng lại đối tượng khi đọc từ database.</summary>
    private Tenant()
    {
        Code = null!;
        Name = null!;
    }

    public TenantCode Code { get; private set; }

    public string Name { get; private set; }

    /// <summary>
    /// <c>null</c> ngay sau khi tạo. Đây là cái vòng "gà và trứng": workspace cần một
    /// người chủ, mà người chủ lại phải thuộc về một workspace. Giải bằng cách cho phép
    /// trống ở đúng một khoảnh khắc — giữa lúc tạo workspace và lúc tạo tài khoản đầu tiên.
    /// </summary>
    public Guid? OwnerUserId { get; private set; }

    public bool IsActive { get; private set; }

    // Hạ tầng tự điền qua AuditableEntityInterceptor. Public setter là nhượng bộ có chủ ý
    // của Luong.Kernel: đổi lại, KHÔNG chỗ nào trong tầng nghiệp vụ được phép gán tay —
    // gán tay nghĩa là đang nói dối về thời điểm.
    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public static Result<Tenant> Create(string? code, string? name)
    {
        var codeResult = TenantCode.Create(code);

        if (codeResult.IsFailure)
        {
            return codeResult.Error;
        }

        var nameResult = ValidateName(name);

        if (nameResult.IsFailure)
        {
            return nameResult.Error;
        }

        var tenant = new Tenant(Guid.NewGuid(), codeResult.Value, nameResult.Value);

        tenant.Raise(new TenantCreated(tenant.Id, tenant.Code.Value, tenant.Name));

        return tenant;
    }

    /// <summary>Gán chủ lần đầu. Đã có chủ rồi thì phải đi đường <see cref="TransferOwnership"/>.</summary>
    public Result AssignOwner(Guid ownerUserId)
    {
        if (OwnerUserId is not null)
        {
            // Cố ý KHÔNG gán đè. Gán đè im lặng nghĩa là một lỗi lập trình có thể lấy
            // mất quyền sở hữu của người khác mà không để lại dấu vết nào.
            return IdentityErrors.Tenants.AlreadyHasOwner;
        }

        OwnerUserId = ownerUserId;

        return Result.Success();
    }

    public Result TransferOwnership(Guid newOwnerId)
    {
        if (OwnerUserId is null)
        {
            return IdentityErrors.Tenants.HasNoOwner;
        }

        if (OwnerUserId == newOwnerId)
        {
            return IdentityErrors.Tenants.AlreadyTheOwner;
        }

        Guid previousOwnerId = OwnerUserId.Value;
        OwnerUserId = newOwnerId;

        // Chuyển quyền sở hữu là chuyện phải để lại vết. Sự kiện này sẽ đi vào
        // nhật ký kiểm toán, và có thể kéo theo mail báo cho cả hai bên.
        Raise(new TenantOwnershipTransferred(Id, previousOwnerId, newOwnerId));

        return Result.Success();
    }

    public Result Rename(string? name)
    {
        var nameResult = ValidateName(name);

        if (nameResult.IsFailure)
        {
            return nameResult.Error;
        }

        Name = nameResult.Value;

        return Result.Success();
    }

    public Result Deactivate()
    {
        // Tắt hai lần thì lệnh thứ hai không có ý nghĩa gì. Trả thất bại thay vì lặng lẽ
        // bỏ qua, để chỗ gọi biết giả định của mình đã sai.
        if (!IsActive)
        {
            return IdentityErrors.Tenants.AlreadyInactive;
        }

        IsActive = false;
        Raise(new TenantDeactivated(Id));

        return Result.Success();
    }

    public Result Activate()
    {
        if (IsActive)
        {
            return IdentityErrors.Tenants.AlreadyActive;
        }

        IsActive = true;
        Raise(new TenantActivated(Id));

        return Result.Success();
    }

    private static Result<string> ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return IdentityErrors.Tenants.NameEmpty;
        }

        string trimmed = name.Trim();

        return trimmed.Length > MaxNameLength
            ? IdentityErrors.Tenants.NameTooLong
            : trimmed;
    }
}
