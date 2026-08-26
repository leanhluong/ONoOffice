using Luong.Kernel.Abstractions;
using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Comm.Application.Abstractions;
using ONoOffice.Comm.Domain;

namespace ONoOffice.Comm.Application.Conversations.MarkRead;

public sealed record MarkConversationReadCommand(Guid ConversationId) : ICommand;

/// <summary>
/// "Tôi đã xem tới đây."
///
/// Mốc là <b>lúc này</b>, không phải mã tin nhắn cuối mà client nhìn thấy. Tin cậy con số
/// client gửi lên thì một client cũ (hoặc một tab mở từ hôm qua) sẽ đẩy mốc về quá khứ và
/// làm mọi tin trong khoảng đó chưa đọc trở lại. Gốc tổng hợp cũng đã chặn chiều lùi, nên
/// đây là lớp thứ hai.
/// </summary>
internal sealed class MarkConversationReadCommandHandler(
    IConversationRepository conversations,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<MarkConversationReadCommand>
{
    public async Task<Result> Handle(
        MarkConversationReadCommand command,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } toi)
        {
            return CommErrors.Conversations.NotAParticipant;
        }

        var ht = await conversations.GetAsync(command.ConversationId, cancellationToken);

        if (ht is null)
        {
            return CommErrors.Conversations.NotFound;
        }

        // `DanhDauDaDoc` tự trả `NotAParticipant` nếu người này không ở trong — không kiểm
        // trước ở đây, vì một luật viết ở hai nơi là một luật sẽ lệch.
        return ht.DanhDauDaDoc(toi, clock.UtcNow);
    }
}
