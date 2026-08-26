using Luong.Kernel.Abstractions;
using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Comm.Application.Abstractions;
using ONoOffice.Comm.Domain;
using ONoOffice.Comm.Domain.Entities;
using ONoOffice.Identity.Contracts;

namespace ONoOffice.Comm.Application.Messages.Send;

public sealed record SendMessageCommand(Guid ConversationId, string Body) : ICommand<MessageItem>;

/// <summary>
/// Gửi một tin vào hội thoại.
///
/// ═══════════════════════════════════════════════════════════════════════════
///  PHÂN QUYỀN CỦA MODULE NÀY LÀ TƯ CÁCH THAM GIA
/// ═══════════════════════════════════════════════════════════════════════════
///
/// Không có quyền <c>conversation.write</c> nào cả, và đó là quyết định chứ không phải
/// thiếu sót: một quyền mà cả bốn vai hệ thống đều có thì không phải quyền, nó là nhiễu
/// trong bảng phân quyền. Và nó trả lời sai câu hỏi — chuyện không phải "bạn có được nhắn
/// tin không" mà là <b>"bạn có ở trong hội thoại NÀY không"</b>.
///
/// Câu trả lời nằm ở bảng <c>comm.participants</c>. Thiếu phép kiểm dưới đây thì một tài
/// khoản hợp lệ bất kỳ chỉ cần đoán ra một mã hội thoại là nói được vào cuộc trò chuyện
/// riêng của hai người khác, và <c>[Authorize]</c> ở controller vẫn thấy mọi thứ bình
/// thường.
/// </summary>
internal sealed class SendMessageCommandHandler(
    IConversationRepository conversations,
    IMessageRepository messages,
    IUserDirectory users,
    ICurrentTenant currentTenant,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<SendMessageCommand, MessageItem>
{
    public async Task<Result<MessageItem>> Handle(
        SendMessageCommand command,
        CancellationToken cancellationToken)
    {
        if (currentTenant.TenantId is not { } tenantId)
        {
            return CommErrors.Conversations.TenantRequired;
        }

        if (currentUser.UserId is not { } toi)
        {
            return CommErrors.Conversations.NotAParticipant;
        }

        var ht = await conversations.GetAsync(command.ConversationId, cancellationToken);

        if (ht is null)
        {
            return CommErrors.Conversations.NotFound;
        }

        if (!ht.CoThanhVien(toi))
        {
            // Cùng một câu trả lời với ca hội thoại không tồn tại thì tốt hơn cho quyền
            // riêng tư, nhưng tệ hơn nhiều cho người dùng thật: bị đá khỏi một nhóm rồi
            // gửi tin từ tab cũ là chuyện xảy ra hằng ngày, và "không tìm thấy" khiến họ
            // đi tìm xem nhóm biến đi đâu. Mã hội thoại không phải bí mật — nó nằm ngay
            // trên thanh địa chỉ của mọi người trong nhóm.
            return CommErrors.Conversations.NotAParticipant;
        }

        var luc = clock.UtcNow;
        var tin = Message.Gui(tenantId, ht.Id, toi, command.Body, luc);

        if (tin.IsFailure)
        {
            return tin.Error;
        }

        messages.Add(tin.Value);

        // Gửi xong thì chính mình coi như đã đọc tới đó. Thiếu bước này thì vừa bấm Gửi là
        // huy hiệu đỏ nhảy lên một — cho đúng câu mình vừa gõ.
        ht.DanhDauDaDoc(toi, luc);

        var danhBa = await SoDanhBa.MoAsync(users, cancellationToken);

        return new MessageItem(
            tin.Value.Id,
            toi,
            danhBa[toi],
            tin.Value.Body,
            tin.Value.SentAtUtc);
    }
}
