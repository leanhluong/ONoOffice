using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Org.Application.Abstractions;
using ONoOffice.Org.Domain;

namespace ONoOffice.Org.Application.Departments.Move;

public sealed record MoveDepartmentCommand(Guid Id, Guid? NewParentId) : ICommand;

/// <summary>
/// Điều chuyển một phòng ban sang phòng cha khác, hoặc nâng nó lên làm phòng gốc.
///
/// ═══════════════════════════════════════════════════════════════════════
///  ĐÂY LÀ CHỖ LUẬT CHỐNG VÒNG LẶP SỐNG, VÀ VÌ SAO NÓ KHÔNG Ở DOMAIN
/// ═══════════════════════════════════════════════════════════════════════
///
/// <c>Department.MoveTo</c> chỉ chặn được ca tự làm cha của chính mình — ca DUY NHẤT nhìn
/// thấy được từ bên trong một aggregate lưu adjacency list, vì nó chỉ giữ <c>ParentId</c>.
///
/// Vòng lặp thật thì nhiều cấp: cây A → B → C, chuyển A xuống dưới C. Không chỗ nào trong
/// A, B hay C tự thấy được điều đó; phải đi ngược chuỗi tổ tiên của C và tìm A. Đó là một
/// truy vấn, nên nó thuộc về handler.
///
/// <b>Hỏng thế nào nếu thiếu:</b> nhánh đó tách khỏi gốc và <b>biến mất khỏi cây</b> —
/// truy vấn dựng cây bắt đầu từ những phòng có <c>ParentId = null</c>, mà vòng lặp thì
/// không có nút nào như vậy. Dữ liệu còn nguyên trong bảng, giao diện thì trống. Và tệ
/// hơn: bất kỳ đoạn code nào đi ngược lên gốc sẽ chạy vô tận.
/// </summary>
internal sealed class MoveDepartmentCommandHandler(IDepartmentRepository departments)
    : ICommandHandler<MoveDepartmentCommand>
{
    public async Task<Result> Handle(
        MoveDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        var phong = await departments.GetAsync(command.Id, cancellationToken);

        if (phong is null)
        {
            return OrgErrors.Departments.NotFound;
        }

        if (command.NewParentId is { } chaMoi)
        {
            var cha = await departments.GetAsync(chaMoi, cancellationToken);

            if (cha is null)
            {
                return OrgErrors.Departments.NotFound;
            }

            // Tổ tiên của phòng cha MỚI mà có chính phòng đang chuyển → vòng lặp.
            var toTien = await departments.GetAncestorIdsAsync(chaMoi, cancellationToken);

            if (toTien.Contains(command.Id))
            {
                return OrgErrors.Departments.WouldCreateCycle;
            }
        }

        // Nâng lên làm gốc (`null`) thì không cần kiểm gì: gốc không có tổ tiên nào để
        // đụng vào, nên không vòng lặp nào tạo ra được.
        return phong.MoveTo(command.NewParentId);
    }
}
