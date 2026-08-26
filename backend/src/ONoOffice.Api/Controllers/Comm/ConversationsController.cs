using Luong.Kernel.AspNetCore.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ONoOffice.Comm.Application.Conversations.CreateGroup;
using ONoOffice.Comm.Application.Conversations.GetList;
using ONoOffice.Comm.Application.Conversations.MarkRead;
using ONoOffice.Comm.Application.Conversations.OpenDirect;
using ONoOffice.Comm.Application.Messages.GetList;
using ONoOffice.Comm.Application.Messages.Send;

namespace ONoOffice.Api.Controllers.Comm;

/// <summary>
/// Màn <b>Trao đổi</b> — hội thoại và tin nhắn.
///
/// ═══════════════════════════════════════════════════════════════════════════
///  KHÔNG CÓ MỘT [Authorize(Policy = ...)] NÀO Ở ĐÂY, VÀ ĐÓ LÀ CHỦ Ý
/// ═══════════════════════════════════════════════════════════════════════════
///
/// Mọi controller khác trong dự án gắn một quyền cho mỗi action. Ở đây thì không, vì
/// module này không có quyền nào cả: một quyền mà cả bốn vai hệ thống đều có thì không
/// phải quyền, nó là nhiễu trong bảng phân quyền — và nó trả lời sai câu hỏi. Chuyện
/// không phải "bạn có được nhắn tin không" mà là <b>"bạn có ở trong hội thoại NÀY
/// không"</b>, và câu đó chỉ trả lời được sau khi đã đọc bảng <c>comm.participants</c>.
///
/// Nên phép kiểm nằm ở handler, từng cái một. <c>[Authorize]</c> trên lớp vẫn cần: nó
/// chặn người chưa đăng nhập, và đó là tất cả những gì nó biết làm ở đây.
/// </summary>
[ApiController]
[Route("api/conversations")]
[Authorize]
public sealed class ConversationsController(ISender sender) : ControllerBase
{
    /// <summary>Cột trái: mọi hội thoại của tôi, mới nhất trước.</summary>
    [HttpGet]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
        => (await sender.Send(new GetConversationsQuery(), cancellationToken)).ToActionResult();

    /// <summary>
    /// Mở hội thoại riêng với một người — trả lại đúng hội thoại cũ nếu đã có.
    ///
    /// <c>POST</c> chứ không <c>PUT</c> dù nó idempotent: <c>PUT</c> đòi client biết
    /// trước định danh của thứ mình đang tạo, mà ở đây định danh là một khoá cặp do máy
    /// chủ tính ra.
    /// </summary>
    [HttpPost("direct")]
    public async Task<IActionResult> OpenDirect(
        [FromBody] OpenDirectConversationCommand command,
        CancellationToken cancellationToken)
        => (await sender.Send(command, cancellationToken)).ToActionResult();

    [HttpPost("group")]
    public async Task<IActionResult> CreateGroup(
        [FromBody] CreateGroupConversationCommand command,
        CancellationToken cancellationToken)
        => (await sender.Send(command, cancellationToken)).ToActionResult();

    [HttpGet("{id:guid}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid id,
        [FromQuery] Guid? before,
        [FromQuery] int take,
        CancellationToken cancellationToken)
        => (await sender.Send(new GetMessagesQuery(id, before, take), cancellationToken)).ToActionResult();

    /// <summary>
    /// Gửi một tin.
    ///
    /// Mã hội thoại lấy từ ĐƯỜNG DẪN, không lấy từ thân request — thân chỉ mang nội dung.
    /// Nhận cả hai thì có hai nguồn sự thật cho cùng một câu hỏi, và ngày chúng lệch nhau
    /// là ngày một tin nhắn đi vào nhầm hội thoại.
    /// </summary>
    [HttpPost("{id:guid}/messages")]
    public async Task<IActionResult> Send(
        Guid id,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
        => (await sender.Send(new SendMessageCommand(id, request.Body), cancellationToken)).ToActionResult();

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
        => (await sender.Send(new MarkConversationReadCommand(id), cancellationToken)).ToActionResult();
}

/// <summary>Thân của lệnh gửi tin. Mã hội thoại nằm ở đường dẫn.</summary>
public sealed record SendMessageRequest(string Body);
