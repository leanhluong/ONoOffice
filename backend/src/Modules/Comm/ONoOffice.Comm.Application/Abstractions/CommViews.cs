using ONoOffice.Comm.Domain.Entities;

namespace ONoOffice.Comm.Application.Abstractions;

/// <summary>
/// Một hội thoại như người dùng NHÌN THẤY nó — đã ghép tên từ module Identity.
///
/// Khác <see cref="ConversationRow"/> đúng ở chỗ đó: <c>ConversationRow</c> là thứ schema
/// <c>comm</c> biết, còn cái này là thứ màn hình cần. Giữ hai kiểu riêng vì Luật 3 buộc
/// phép ghép tên phải xảy ra ở handler; gộp làm một thì repository sẽ dần bị nhét thêm
/// trường của module khác, và một ngày nào đó có người thêm câu JOIN cho tiện.
/// </summary>
public sealed record ConversationSummary(
    Guid Id,
    ConversationKind Kind,
    /// <summary>Tên nhóm, hoặc tên người kia với hội thoại riêng.</summary>
    string DisplayName,
    Guid? OtherUserId,
    int ParticipantCount,
    string? LastMessageBody,
    string? LastMessageSenderName,
    DateTimeOffset? LastMessageAtUtc,
    int UnreadCount);

/// <summary>Một tin nhắn như người dùng nhìn thấy nó.</summary>
public sealed record MessageItem(
    Guid Id,
    Guid SenderUserId,
    string SenderName,
    string Body,
    DateTimeOffset SentAtUtc);

/// <summary>
/// Một trang tin, theo thứ tự <b>cũ → mới</b> để cửa sổ chat vẽ thẳng từ trên xuống.
///
/// <paramref name="HasMore"/> nghĩa là còn tin CŨ HƠN nữa ở phía trên.
/// </summary>
public sealed record MessagePageResponse(IReadOnlyList<MessageItem> Items, bool HasMore);
