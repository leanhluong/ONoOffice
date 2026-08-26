using Luong.Kernel.Domain;
using Luong.Kernel.Primitives;

namespace ONoOffice.Comm.Domain.Entities;

/// <summary>
/// Một câu ai đó đã nói.
///
/// Gốc tổng hợp <b>RIÊNG</b>, không phải collection con của <see cref="Conversation"/> —
/// lý do đầy đủ nằm ở phần đầu file <c>Conversation.cs</c>. Tóm lại: hàng chục nghìn dòng
/// và không có luật nào bắt chúng phải đúng cùng nhau.
///
/// Nó chỉ giữ <see cref="ConversationId"/>. Không có thuộc tính điều hướng sang hội thoại,
/// và đó là chủ ý: thêm một cái cho tiện thì mỗi lần nạp một tin là kéo theo cả hội thoại
/// cùng danh sách người tham gia của nó.
/// </summary>
public sealed class Message : AggregateRoot<Guid>, ITenantScoped
{
    private const int MaxBodyLength = 4000;

    private Message(
        Guid id,
        Guid tenantId,
        Guid conversationId,
        Guid senderUserId,
        string body,
        DateTimeOffset sentAtUtc) : base(id)
    {
        TenantId = tenantId;
        ConversationId = conversationId;
        SenderUserId = senderUserId;
        Body = body;
        SentAtUtc = sentAtUtc;
    }

    /// <summary>Dành cho EF Core.</summary>
    private Message() => Body = null!;

    public Guid TenantId { get; private set; }

    public Guid ConversationId { get; private set; }

    public Guid SenderUserId { get; private set; }

    public string Body { get; private set; }

    /// <summary>
    /// Thời điểm gửi — do tầng trên truyền vào, không phải <c>DateTimeOffset.UtcNow</c>
    /// gọi ở đây.
    ///
    /// Đây cũng là thứ mà <c>LastReadAtUtc</c> đem ra so, nên nó không chỉ để hiển thị:
    /// lấy giờ ở hai chỗ khác nhau là đủ để một tin vừa gửi rơi vào phía "đã đọc" của
    /// chính người chưa mở nó.
    /// </summary>
    public DateTimeOffset SentAtUtc { get; private set; }

    public static Result<Message> Gui(
        Guid tenantId,
        Guid conversationId,
        Guid senderUserId,
        string? body,
        DateTimeOffset luc)
    {
        if (tenantId == Guid.Empty)
        {
            return CommErrors.Conversations.TenantRequired;
        }

        if (senderUserId == Guid.Empty)
        {
            return CommErrors.Messages.SenderRequired;
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return CommErrors.Messages.Empty;
        }

        // Cắt hai đầu, GIỮ NGUYÊN xuống dòng ở giữa. Người ta gõ danh sách gạch đầu dòng
        // trong chat suốt ngày; gộp xuống dòng thành dấu cách là sửa lời người khác nói.
        string trimmed = body.Trim();

        return trimmed.Length > MaxBodyLength
            ? CommErrors.Messages.TooLong
            : new Message(Guid.NewGuid(), tenantId, conversationId, senderUserId, trimmed, luc);
    }
}
