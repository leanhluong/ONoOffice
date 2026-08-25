using Microsoft.EntityFrameworkCore;
using ONoOffice.Org.Application.Abstractions;
using ONoOffice.Org.Domain.Entities;

namespace ONoOffice.Org.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository của phòng ban.
///
/// Tên phương thức nói đúng CÂU HỎI NGHIỆP VỤ, không phải thao tác database — đó là lý do
/// dự án này không làm repository generic: <c>DbSet&lt;T&gt;</c> đã là repository rồi, bọc
/// thêm một lớp `GetById/GetAll` chỉ làm mất <c>Include</c>, projection và <c>AsNoTracking</c>.
/// </summary>
internal sealed class EfDepartmentRepository(OrgDbContext context) : IDepartmentRepository
{
    public void Add(Department department) => context.Departments.Add(department);

    public void Remove(Department department) => context.Departments.Remove(department);

    public Task<Department?> GetAsync(Guid id, CancellationToken cancellationToken)
        => context.Departments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    /// <summary>
    /// Toàn bộ cây, dạng phẳng, kèm số người và tên trưởng phòng — <b>một lượt đi database</b>.
    ///
    /// Số người đếm bằng truy vấn con trên <c>Employees</c> thay vì kéo hết nhân viên về
    /// rồi đếm bằng C#: công ty 3.000 người thì cách kia kéo 3.000 hàng chỉ để in ra 20
    /// con số. Bộ lọc tenant toàn cục áp cho CẢ truy vấn con, nên không có đường nào đếm
    /// nhầm sang workspace khác.
    ///
    /// Tên trưởng phòng lấy bằng phép nối trong CÙNG schema (<c>Employees</c>) — hợp lệ.
    /// Nếu trưởng phòng lưu bằng <c>UserId</c> của Identity thì đây sẽ là JOIN xuyên schema
    /// và bị luật 3 cấm; đó chính là lý do <c>Department.HeadEmployeeId</c> trỏ vào
    /// <c>Employee</c> chứ không vào <c>User</c>.
    /// </summary>
    public async Task<IReadOnlyList<DepartmentNode>> GetTreeAsync(CancellationToken cancellationToken)
        => await context.Departments
            .AsNoTracking()
            .Select(d => new DepartmentNode(
                d.Id,
                d.Name,
                d.ParentId,
                d.HeadEmployeeId,
                context.Employees
                    .Where(e => e.Id == d.HeadEmployeeId)
                    .Select(e => e.FullName)
                    .FirstOrDefault(),
                context.Employees.Count(e => e.DepartmentId == d.Id && e.IsActive)))
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Chuỗi tổ tiên, từ cha trực tiếp đi ngược lên gốc.
    ///
    /// <b>Đi từng bậc, và có TRẦN CỨNG.</b> Trần không phải để tối ưu mà để không treo:
    /// nếu dữ liệu đã hỏng và có sẵn một vòng lặp trong bảng (ai đó sửa tay), vòng lặp
    /// dưới đây sẽ chạy vô tận và giữ luôn một luồng. Chạm trần thì dừng và trả về những
    /// gì đã đi qua — handler vẫn kết luận "có vòng lặp" và từ chối, đúng cái ta muốn.
    ///
    /// Một cây tổ chức sâu quá 32 cấp thì vấn đề không nằm ở đoạn code này.
    /// </summary>
    public async Task<IReadOnlyList<Guid>> GetAncestorIdsAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        const int TranCung = 32;

        var toTien = new List<Guid>();
        var daQua = new HashSet<Guid> { id };

        Guid? hienTai = await context.Departments
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => d.ParentId)
            .FirstOrDefaultAsync(cancellationToken);

        while (hienTai is { } cha && toTien.Count < TranCung && daQua.Add(cha))
        {
            toTien.Add(cha);

            hienTai = await context.Departments
                .AsNoTracking()
                .Where(d => d.Id == cha)
                .Select(d => d.ParentId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return toTien;
    }

    /// <summary>
    /// So tên KHÔNG phân biệt hoa thường.
    ///
    /// Domain đã cắt khoảng trắng nhưng cố ý giữ nguyên hoa thường (tên phòng ban là thứ
    /// hiện lên màn hình, "Kỹ thuật" khác "KỸ THUẬT" về mặt trình bày). Nhưng coi chúng là
    /// hai phòng khác nhau thì danh sách có hai dòng đọc lên giống hệt nhau.
    /// </summary>
    public Task<bool> NameTakenAsync(string name, Guid? exceptId, CancellationToken cancellationToken)
        => context.Departments
            .AsNoTracking()
            .AnyAsync(
                d => d.Name.ToLower() == name.ToLower() && (exceptId == null || d.Id != exceptId),
                cancellationToken);

    public Task<bool> HasChildrenAsync(Guid id, CancellationToken cancellationToken)
        => context.Departments.AsNoTracking().AnyAsync(d => d.ParentId == id, cancellationToken);

    /// <summary>
    /// Còn nhân viên không — kể cả người đã nghỉ việc.
    ///
    /// Cố ý KHÔNG lọc <c>IsActive</c>: hồ sơ người đã nghỉ vẫn trỏ vào phòng ban này, và
    /// xoá phòng đi thì những hồ sơ đó mất luôn thông tin "từng làm ở đâu" — thứ người ta
    /// tra lại sau nhiều năm khi có tranh chấp hợp đồng hay bảo hiểm.
    ///
    /// Bộ lọc xoá mềm toàn cục vẫn áp, nên hồ sơ đã xoá hẳn thì không tính.
    /// </summary>
    public Task<bool> HasEmployeesAsync(Guid id, CancellationToken cancellationToken)
        => context.Employees.AsNoTracking().AnyAsync(e => e.DepartmentId == id, cancellationToken);
}
