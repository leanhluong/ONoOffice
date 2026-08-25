using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Org.Application.Abstractions;

namespace ONoOffice.Org.Application.Departments.GetTree;

public sealed record GetDepartmentTreeQuery : IQuery<IReadOnlyList<DepartmentTreeItem>>;

/// <summary>Một nút trên cây, đã nối sẵn con.</summary>
public sealed record DepartmentTreeItem(
    Guid Id,
    string Name,
    Guid? ParentId,
    Guid? HeadEmployeeId,
    string? HeadName,
    int EmployeeCount,
    IReadOnlyList<DepartmentTreeItem> Children);

/// <summary>
/// Cây phòng ban của workspace.
///
/// <b>Một truy vấn, dựng cây trong bộ nhớ.</b> Repository trả về danh sách PHẲNG; nối cha
/// con là việc của handler. Ở quy mô 20–40 phòng ban thì rẻ hơn hẳn một truy vấn đệ quy,
/// và không có câu SQL nào phải bảo trì. Ngưỡng đổi ý: hàng nghìn phòng.
///
/// Không phân trang, cố ý: một cây bị cắt làm nhiều trang thì không còn là cây.
/// </summary>
internal sealed class GetDepartmentTreeQueryHandler(IDepartmentRepository departments)
    : IQueryHandler<GetDepartmentTreeQuery, IReadOnlyList<DepartmentTreeItem>>
{
    public async Task<Result<IReadOnlyList<DepartmentTreeItem>>> Handle(
        GetDepartmentTreeQuery query,
        CancellationToken cancellationToken)
    {
        var phang = await departments.GetTreeAsync(cancellationToken);

        return Result.Success(Dung(phang));
    }

    /// <summary>
    /// Nối danh sách phẳng thành cây, sắp xếp theo tên ở từng cấp.
    ///
    /// <b>Phòng MỒ CÔI được nâng lên mức gốc.</b> `ParentId` trỏ vào một phòng không có
    /// trong danh sách là chuyện xảy ra thật khi ai đó xoá cứng bằng tay trong database.
    /// Bỏ qua chúng thì cả một nhánh biến mất khỏi giao diện mà không có lỗi nào — quản
    /// trị viên thấy công ty thiếu mất một phòng và không có cách nào lần ra.
    /// </summary>
    private static IReadOnlyList<DepartmentTreeItem> Dung(IReadOnlyList<DepartmentNode> phang)
    {
        var coMat = phang.Select(n => n.Id).ToHashSet();

        // `ToLookup` chứ không `ToDictionary`: khoá ở đây là `Guid?` (mức gốc là `null`),
        // mà `ToDictionary` đòi khoá `notnull`. Lookup cũng tiện hơn ở chỗ khoá không có
        // thì trả về chuỗi rỗng thay vì ném lỗi.
        var theoCha = phang.ToLookup(
            n => n.ParentId is { } cha && coMat.Contains(cha) ? cha : (Guid?)null);

        IReadOnlyList<DepartmentTreeItem> Con(Guid? chaId) =>
            theoCha[chaId]
                .OrderBy(n => n.Name, StringComparer.CurrentCulture)
                .Select(n => new DepartmentTreeItem(
                    n.Id,
                    n.Name,
                    n.ParentId,
                    n.HeadEmployeeId,
                    n.HeadName,
                    n.EmployeeCount,
                    Con(n.Id)))
                .ToList();

        return Con(null);
    }
}
