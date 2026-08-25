using ONoOffice.Org.Domain.Entities;

namespace ONoOffice.Org.Application.Abstractions;

/// <summary>Một phòng ban trên cây, đã kèm số người — dạng phẳng, chưa nối cha con.</summary>
public sealed record DepartmentNode(
    Guid Id,
    string Name,
    Guid? ParentId,
    Guid? HeadEmployeeId,
    string? HeadName,
    int EmployeeCount);

public interface IDepartmentRepository
{
    /// <summary>Chỉ ghi vào bộ theo dõi thay đổi; <c>SaveChanges</c> mới thật sự lưu.</summary>
    void Add(Department department);

    void Remove(Department department);

    Task<Department?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// TOÀN BỘ cây của workspace, dạng phẳng, một truy vấn.
    ///
    /// Không có tham số lọc và cũng không phân trang — cố ý. Một công ty có 20–40 phòng
    /// ban; kéo hết về rồi nối cha con bằng C# rẻ hơn một truy vấn đệ quy, và không có câu
    /// SQL nào phải bảo trì. Ngưỡng đổi ý là khi số phòng lên tới hàng nghìn, lúc đó mới
    /// đáng đổi sang recursive CTE hoặc materialized path.
    /// </summary>
    Task<IReadOnlyList<DepartmentNode>> GetTreeAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Chuỗi tổ tiên của một phòng, từ cha trực tiếp đi ngược lên gốc.
    ///
    /// Đây là thứ handler cần để chặn vòng lặp NHIỀU CẤP: chuyển A vào dưới B là hợp lệ,
    /// trừ khi B đang nằm đâu đó trong nhánh của A. Bản thân <c>Department</c> không trả
    /// lời được câu này — nó chỉ giữ <c>ParentId</c>, nên nó chỉ thấy được đúng một bậc.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetAncestorIdsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> NameTakenAsync(string name, Guid? exceptId, CancellationToken cancellationToken);

    Task<bool> HasChildrenAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> HasEmployeesAsync(Guid id, CancellationToken cancellationToken);
}
