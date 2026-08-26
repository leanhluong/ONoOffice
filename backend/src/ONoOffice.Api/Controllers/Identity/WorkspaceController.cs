using Luong.Kernel.AspNetCore.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ONoOffice.Identity.Application.Workspace.TransferOwnership;
using ONoOffice.Identity.Domain;

namespace ONoOffice.Api.Controllers.Identity;

/// <summary>
/// Cấu hình của chính WORKSPACE — không phải của một người trong đó.
///
/// Hiện có đúng một việc, và đó là việc nặng nhất hệ thống: chuyển quyền sở hữu.
/// </summary>
[ApiController]
[Route("api/workspace")]
[Authorize]
public sealed class WorkspaceController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Chuyển quyền sở hữu workspace cho người khác.
    ///
    /// <b>Không hoàn tác được bởi người vừa làm</b> — xong lệnh này, họ mất đúng cái quyền
    /// cần để lấy lại. Vì vậy nó có hai lớp mà chỗ khác không có: phải LÀ chủ sở hữu (đọc
    /// từ database, không tin claim trong token — access token sống 15 phút), và phải gõ
    /// lại MẬT KHẨU HIỆN TẠI. Xem <c>TransferOwnershipCommandHandler</c>.
    ///
    /// Quyền <c>workspace.transfer-ownership</c> là thứ DUY NHẤT Admin không có. Đó là
    /// toàn bộ ranh giới Admin ↔ Owner, nên nó cũng là lý do bốn chỗ khác trong hệ thống
    /// từ chối thao tác lên chủ sở hữu kèm câu "hãy chuyển nhượng quyền sở hữu trước".
    /// Trước endpoint này thì cả bốn câu đó chỉ vào một cánh cửa không tồn tại.
    /// </summary>
    [HttpPost("transfer-ownership")]
    [Authorize(Policy = Permissions.Workspace.TransferOwnership)]
    public async Task<IActionResult> TransferOwnership(
        TransferOwnershipBody body,
        CancellationToken cancellationToken)
        => (await sender.Send(
                new TransferOwnershipCommand(body.NewOwnerUserId, body.CurrentPassword),
                cancellationToken))
            .ToActionResult();
}

/// <summary>
/// Thân của <c>POST /api/workspace/transfer-ownership</c>.
///
/// Mã workspace KHÔNG có ở đây: nó lấy từ phiên đăng nhập. Nhận từ ngoài vào thì chủ sở
/// hữu của công ty này gõ tay mã công ty khác là chuyển nhượng được workspace của họ.
/// </summary>
public sealed record TransferOwnershipBody(Guid NewOwnerUserId, string CurrentPassword);
