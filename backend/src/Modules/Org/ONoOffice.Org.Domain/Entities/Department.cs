using Luong.Kernel.Domain;
using Luong.Kernel.Primitives;

namespace ONoOffice.Org.Domain.Entities;

/// <summary>
/// Một nút của cây tổ chức.
///
/// <b>Cây lưu bằng adjacency list</b> — mỗi phòng chỉ giữ <c>ParentId</c>. Hệ quả quan
/// trọng: <b>một phòng ban không biết gì về cây</b>. Nó không biết ai là con nó, không
/// biết mình sâu bao nhiêu cấp, và không biết mình có đang bị chuyển vào chính nhánh của
/// mình hay không.
///
/// Đó là ranh giới đúng của một aggregate: <b>nó chỉ được canh thứ nó nhìn thấy</b>.
/// Luật chống vòng lặp nhiều cấp sống ở handler, nơi đọc được cả nhánh — xem
/// <c>OrgErrors.Departments.WouldCreateCycle</c>.
///
/// Vì sao chọn adjacency list mà không phải materialized path: điều chuyển một phòng chỉ
/// là <c>UPDATE</c> một ô, và không có dữ liệu dư thừa nào để lệch. Materialized path
/// đọc nhánh nhanh hơn, nhưng mỗi lần điều chuyển phải cập nhật path của mọi con cháu —
/// một lần hỏng giữa chừng là cây sai vĩnh viễn. Ở quy mô 20–40 phòng ban, truy vấn đệ
/// quy là tức thì.
/// </summary>
public sealed class Department : AggregateRoot<Guid>, ITenantScoped, IAuditable
{
    private const int MaxNameLength = 100;

    private Department(Guid id, Guid tenantId, string name, Guid? parentId) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        ParentId = parentId;
    }

    /// <summary>Dành cho EF Core.</summary>
    private Department() => Name = null!;

    public Guid TenantId { get; private set; }

    public string Name { get; private set; }

    /// <summary><c>null</c> nghĩa là phòng gốc — công ty có thể có nhiều gốc.</summary>
    public Guid? ParentId { get; private set; }

    /// <summary>
    /// Trưởng phòng. Chỉ giữ KHOÁ, không giữ tham chiếu tới <c>Employee</c> — hai gốc
    /// tổng hợp khác nhau thì không ôm nhau, nếu không nạp một cái là kéo theo cả cái kia.
    /// </summary>
    public Guid? HeadEmployeeId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public static Result<Department> Create(Guid tenantId, string? name, Guid? parentId)
    {
        if (tenantId == Guid.Empty)
        {
            return OrgErrors.Departments.TenantRequired;
        }

        var validated = ValidateName(name);

        if (validated.IsFailure)
        {
            return validated.Error;
        }

        return new Department(Guid.NewGuid(), tenantId, validated.Value, parentId);
    }

    public Result Rename(string? name)
    {
        var validated = ValidateName(name);

        if (validated.IsFailure)
        {
            return validated.Error;
        }

        Name = validated.Value;

        return Result.Success();
    }

    /// <summary>
    /// Chuyển sang một phòng cha khác, hoặc lên làm phòng gốc (<paramref name="parentId"/>
    /// là <c>null</c>).
    ///
    /// Chỉ chặn được ca tự làm cha của chính mình — ca duy nhất nhìn thấy từ trong đây.
    /// Vòng lặp qua nhiều cấp (A → B → A) phải do handler chặn.
    /// </summary>
    public Result MoveTo(Guid? parentId)
    {
        if (parentId == Id)
        {
            return OrgErrors.Departments.CannotBeItsOwnParent;
        }

        ParentId = parentId;

        return Result.Success();
    }

    /// <summary>Gán trưởng phòng, hoặc gỡ bằng cách truyền <c>null</c>.</summary>
    public Result AssignHead(Guid? employeeId)
    {
        HeadEmployeeId = employeeId;

        return Result.Success();
    }

    private static Result<string> ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return OrgErrors.Departments.NameEmpty;
        }

        // Cắt khoảng trắng thừa TRƯỚC khi đo và trước khi lưu: không cắt thì "Kỹ thuật"
        // và "Kỹ thuật " là hai phòng khác nhau trong mắt ràng buộc UNIQUE, và người
        // dùng nhìn danh sách thấy hai dòng y hệt nhau.
        string trimmed = name.Trim();

        return trimmed.Length > MaxNameLength ? OrgErrors.Departments.NameTooLong : trimmed;
    }
}
