using ONoOffice.Comm.Domain.Entities;

namespace ONoOffice.Comm.Application.Abstractions;

/// <summary>Một tin nhắn thô, chưa ghép tên người gửi.</summary>
public sealed record MessageRow(
    Guid Id,
    Guid SenderUserId,
    string Body,
    DateTimeOffset SentAtUtc);

/// <summary>
/// Một trang tin nhắn, kèm câu trả lời cho "còn nữa không".
///
/// Không dùng <c>PagedList</c> như các màn khác: <c>PagedList</c> mang <c>TotalCount</c>
/// để vẽ "Trang 2/17", mà cửa sổ chat không có số trang — nó chỉ cuộn lên. Đếm tổng số
/// tin của một hội thoại là một câu <c>COUNT(*)</c> trên bảng lớn nhất hệ thống, chạy mỗi
/// lần cuộn, để hiển thị một con số không ai nhìn.
/// </summary>
public sealed record MessagePage(IReadOnlyList<MessageRow> Items, bool HasMore);

public interface IMessageRepository
{
    void Add(Message message);

    /// <summary>
    /// Lấy <paramref name="take"/> tin CŨ HƠN con trỏ, mới nhất trước.
    ///
    /// <b>Con trỏ là một mã tin, không phải một mốc thời gian.</b> Dùng thời gian thì hai
    /// tin trùng đúng một micro-giây sẽ làm một tin bị nhảy cóc mất vĩnh viễn — người
    /// dùng cuộn qua chỗ đó và câu ấy đơn giản là không còn ở đâu cả. Với mã tin thì so
    /// được cả cặp <c>(sent_at, id)</c>, và cặp đó thì không thể trùng.
    ///
    /// <paramref name="notBefore"/> là chặn dưới theo <c>JoinedAtUtc</c> của người đang
    /// xem: người mới vào nhóm không thấy những gì nói trước khi họ vào.
    /// </summary>
    Task<MessagePage> PageAsync(
        Guid conversationId,
        Guid? beforeMessageId,
        DateTimeOffset notBefore,
        int take,
        CancellationToken cancellationToken);
}
