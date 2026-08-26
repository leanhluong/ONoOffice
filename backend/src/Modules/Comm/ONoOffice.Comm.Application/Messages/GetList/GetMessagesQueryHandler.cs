using Luong.Kernel.Abstractions;
using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Comm.Application.Abstractions;
using ONoOffice.Comm.Domain;
using ONoOffice.Identity.Contracts;

namespace ONoOffice.Comm.Application.Messages.GetList;

/// <summary>
/// <paramref name="Before"/> là mã tin cũ nhất đang hiện trên màn — cuộn lên thì xin tiếp
/// những tin cũ hơn nó. <c>null</c> nghĩa là mở lần đầu: lấy những tin mới nhất.
/// </summary>
public sealed record GetMessagesQuery(Guid ConversationId, Guid? Before = null, int Take = 30)
    : IQuery<MessagePageResponse>;

/// <summary>
/// Đọc tin nhắn của một hội thoại.
///
/// Hai thứ handler này canh mà một câu <c>SELECT</c> không tự canh được:
///
/// <list type="number">
/// <item><b>Tư cách tham gia</b> — xem chú thích ở <c>SendMessageCommandHandler</c>.</item>
/// <item><b>Chặn dưới theo <c>JoinedAtUtc</c></b> — người mới vào nhóm chỉ thấy từ lúc họ
/// vào. Đây là thứ dễ quên nhất của cả module, vì quên thì mọi thứ trông vẫn đúng: danh
/// sách vẫn hiện, thứ tự vẫn chuẩn, chỉ là có thêm những gì người ta nói về họ trước khi
/// họ có mặt.</item>
/// </list>
/// </summary>
internal sealed class GetMessagesQueryHandler(
    IConversationRepository conversations,
    IMessageRepository messages,
    IUserDirectory users,
    ICurrentUser currentUser) : IQueryHandler<GetMessagesQuery, MessagePageResponse>
{
    private const int DefaultTake = 30;

    /// <summary>
    /// Trần cứng. Không có nó thì <c>?take=1000000</c> kéo cả bảng lớn nhất hệ thống lên
    /// bộ nhớ trong một request — rẻ để gửi, đắt để phục vụ, và không đòi hỏi quyền gì
    /// đặc biệt.
    /// </summary>
    private const int MaxTake = 100;

    public async Task<Result<MessagePageResponse>> Handle(
        GetMessagesQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } toi)
        {
            return CommErrors.Conversations.NotAParticipant;
        }

        var ht = await conversations.GetAsync(query.ConversationId, cancellationToken);

        if (ht is null)
        {
            return CommErrors.Conversations.NotFound;
        }

        var toiOTrongDo = ht.Participants.SingleOrDefault(p => p.UserId == toi);

        if (toiOTrongDo is null)
        {
            return CommErrors.Conversations.NotAParticipant;
        }

        int take = query.Take switch
        {
            < 1 => DefaultTake,
            > MaxTake => MaxTake,
            _ => query.Take,
        };

        var trang = await messages.PageAsync(
            ht.Id,
            query.Before,
            toiOTrongDo.JoinedAtUtc,
            take,
            cancellationToken);

        var danhBa = await SoDanhBa.MoAsync(users, cancellationToken);

        /*
          Kho đọc lên MỚI-NHẤT-TRƯỚC, vì con trỏ cuộn ngược đi theo chiều đó. Cửa sổ chat
          thì vẽ từ trên xuống theo chiều thời gian, nên phải đảo.

          Đảo ở đây chứ không ở frontend: để bên kia thì mỗi màn dùng lại API này phải nhớ
          đảo, và có ngày một màn quên — mà một danh sách tin nhắn ngược thứ tự trông vẫn
          "có dữ liệu", nên nó qua được mọi phép kiểm trừ mắt người.
        */
        var items = trang.Items
            .Reverse()
            .Select(m => new MessageItem(m.Id, m.SenderUserId, danhBa[m.SenderUserId], m.Body, m.SentAtUtc))
            .ToList();

        return new MessagePageResponse(items, trang.HasMore);
    }
}
