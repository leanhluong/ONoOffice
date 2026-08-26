using Luong.Kernel.Abstractions;
using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Comm.Application.Abstractions;
using ONoOffice.Comm.Domain;
using ONoOffice.Comm.Domain.Entities;
using ONoOffice.Identity.Contracts;

namespace ONoOffice.Comm.Application.Conversations.OpenDirect;

public sealed record OpenDirectConversationCommand(Guid OtherUserId)
    : ICommand<ConversationSummary>;

/// <summary>
/// Mở hội thoại riêng với một người — <b>và mở lại đúng cái cũ nếu đã có</b>.
///
/// Người dùng không có khái niệm "tạo hội thoại". Họ bấm vào một cái tên trong danh bạ và
/// mong cửa sổ chat hiện ra với đúng những gì hai người đã nói. Vì thế lệnh này KHÔNG
/// phải "tạo": gọi mười lần thì mười lần trả về cùng một hội thoại.
///
/// ⚠️ <b>Còn một khe hở đã biết.</b> Phép kiểm ở đây là "hỏi rồi ghi", nên hai người bấm
/// đúng cùng một khoảnh khắc thì cả hai đều thấy "chưa có" và cả hai đều ghi. Ràng buộc
/// UNIQUE trên <c>pair_key</c> chặn được hàng thứ hai, nhưng người thua cuộc nhận một lỗi
/// chứ không được trả về hội thoại vừa tạo — bấm lại là xong. Đổi lỗi im lặng (hai phòng
/// song song, không ai thấy tin của ai) lấy một lỗi ồn ào và tự chữa được: đó là chủ ý.
/// </summary>
internal sealed class OpenDirectConversationCommandHandler(
    IConversationRepository conversations,
    IUserDirectory users,
    ICurrentTenant currentTenant,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<OpenDirectConversationCommand, ConversationSummary>
{
    public async Task<Result<ConversationSummary>> Handle(
        OpenDirectConversationCommand command,
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

        if (toi == command.OtherUserId)
        {
            return CommErrors.Conversations.CannotChatWithSelf;
        }

        // Không tin con số client gửi lên: `user_id` ở schema `comm` là Guid trần, không
        // phải khoá ngoại (Luật 3 cấm ràng buộc xuyên schema). Không hỏi thì một mã bịa
        // tạo ra hội thoại với người không tồn tại, và không lớp nào phía dưới bắt được.
        //
        // Dùng mã lỗi của Identity, không phải của Comm: người dùng đang chọn một TÀI
        // KHOẢN, nên câu trả lời phải nói về tài khoản.
        if (!await users.ExistsAsync(command.OtherUserId, cancellationToken))
        {
            return Error.NotFound("User.NotFound", "Không tìm thấy tài khoản.");
        }

        var moi = Conversation.MoRieng(tenantId, toi, command.OtherUserId, clock.UtcNow);

        if (moi.IsFailure)
        {
            return moi.Error;
        }

        // Khoá cặp tính từ chính gốc tổng hợp, không tính lại ở đây — một công thức viết ở
        // hai nơi là một công thức sẽ lệch.
        var daCo = await conversations.GetDirectAsync(moi.Value.PairKey!, cancellationToken);

        if (daCo is not null)
        {
            return await TomTat(daCo, cancellationToken);
        }

        conversations.Add(moi.Value);

        return await TomTat(moi.Value, cancellationToken);
    }

    private async Task<ConversationSummary> TomTat(
        Conversation ht,
        CancellationToken cancellationToken)
    {
        var danhBa = await SoDanhBa.MoAsync(users, cancellationToken);
        var nguoiKia = ht.Participants.First(p => p.UserId != currentUser.UserId).UserId;

        // Hội thoại vừa mở thì chưa có tin nào, và hội thoại cũ thì tin cuối nằm ở bảng
        // khác. Cố ý KHÔNG đi lấy: màn hình gọi lệnh này rồi gọi ngay danh sách tin nhắn,
        // nên một lượt hỏi thêm ở đây chỉ để điền một dòng xem trước sắp bị đè lên.
        return new ConversationSummary(
            ht.Id,
            ht.Kind,
            danhBa[nguoiKia],
            nguoiKia,
            ht.Participants.Count,
            LastMessageBody: null,
            LastMessageSenderName: null,
            LastMessageAtUtc: null,
            UnreadCount: 0);
    }
}
