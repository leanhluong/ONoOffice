using Luong.Kernel.Abstractions;
using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Comm.Application.Abstractions;
using ONoOffice.Comm.Domain;
using ONoOffice.Comm.Domain.Entities;
using ONoOffice.Identity.Contracts;

namespace ONoOffice.Comm.Application.Conversations.CreateGroup;

public sealed record CreateGroupConversationCommand(string Name, IReadOnlyList<Guid> MemberUserIds)
    : ICommand<ConversationSummary>;

/// <summary>
/// Mở một nhóm.
///
/// Khác hẳn hội thoại riêng ở chỗ nó KHÔNG idempotent: bấm hai lần là hai nhóm, và đó là
/// đúng — mười nhóm cùng tên "Dự án A" là mười cuộc trò chuyện khác nhau, người dùng biết
/// mình đang làm gì.
/// </summary>
internal sealed class CreateGroupConversationCommandHandler(
    IConversationRepository conversations,
    IUserDirectory users,
    ICurrentTenant currentTenant,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<CreateGroupConversationCommand, ConversationSummary>
{
    public async Task<Result<ConversationSummary>> Handle(
        CreateGroupConversationCommand command,
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

        // Bỏ trùng và bỏ chính mình TRƯỚC khi đếm: mời cùng một người ba lần rồi bị báo
        // "nhóm phải có người khác" là một câu trả lời sai. Và "mời chính mình" thì gốc
        // tổng hợp vẫn xử được, nhưng nó không tính là một người khác.
        var moi = command.MemberUserIds.Where(id => id != toi).Distinct().ToList();

        if (moi.Count == 0)
        {
            return CommErrors.Conversations.GroupNeedsSomeone;
        }

        /*
          Một mã bịa làm hỏng CẢ nhóm, không phải bị bỏ qua riêng nó.

          Bỏ qua âm thầm thì người tạo thấy nhóm dựng xong và tưởng đủ người; họ nói
          chuyện vào đó suốt một tuần rồi mới phát hiện một người chưa bao giờ ở trong —
          và đến lúc đó thì thứ cần sửa không còn là danh sách người nữa.

          Hỏi một lượt cho cả danh sách chứ không `ExistsAsync` từng người: mời 30 người
          là 30 vòng mạng, mà cổng đã có sẵn phép lấy tất cả.
        */
        var coThat = (await users.GetAllAsync(cancellationToken)).Select(u => u.Id).ToHashSet();

        if (moi.Exists(id => !coThat.Contains(id)))
        {
            return Error.NotFound("User.NotFound", "Không tìm thấy tài khoản.");
        }

        var nhom = Conversation.MoNhom(tenantId, command.Name, toi, moi, clock.UtcNow);

        if (nhom.IsFailure)
        {
            return nhom.Error;
        }

        conversations.Add(nhom.Value);

        return new ConversationSummary(
            nhom.Value.Id,
            nhom.Value.Kind,
            nhom.Value.Name!,
            OtherUserId: null,
            nhom.Value.Participants.Count,
            LastMessageBody: null,
            LastMessageSenderName: null,
            LastMessageAtUtc: null,
            UnreadCount: 0);
    }
}
