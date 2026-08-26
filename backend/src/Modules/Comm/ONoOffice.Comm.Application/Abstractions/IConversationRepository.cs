using ONoOffice.Comm.Domain.Entities;

namespace ONoOffice.Comm.Application.Abstractions;

/// <summary>
/// Một dòng trong danh sách hội thoại bên trái màn Trao đổi — <b>đã tính theo góc nhìn
/// của MỘT người</b>.
///
/// Hai trường cuối là chỗ chữ "theo góc nhìn" có nghĩa thật: số chưa đọc và tin cuối
/// khác nhau tuỳ ai đang xem. Chúng phải được tính trong cùng câu truy vấn lấy danh sách,
/// vì tính rời từng hội thoại là N+1 lượt hỏi database cho một màn mở ra là thấy.
/// </summary>
public sealed record ConversationRow(
    Guid Id,
    ConversationKind Kind,
    string? Name,
    /// <summary>
    /// Người kia, với hội thoại riêng. Nhóm thì <c>null</c>.
    ///
    /// Repository KHÔNG tra tên: tên nằm ở schema <c>identity</c>, và Luật 3 cấm JOIN
    /// sang đó. Handler ghép tên vào sau, qua <c>IUserDirectory</c>.
    /// </summary>
    Guid? OtherUserId,
    int ParticipantCount,
    string? LastMessageBody,
    Guid? LastMessageSenderUserId,
    DateTimeOffset? LastMessageAtUtc,
    int UnreadCount);

public interface IConversationRepository
{
    void Add(Conversation conversation);

    /// <summary>Kèm cả danh sách người tham gia — mọi luật của gốc này đều cần nó.</summary>
    Task<Conversation?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Tìm hội thoại riêng đã có bằng khoá cặp.
    ///
    /// Đây là nửa "kiểm trước" của phép mở hội thoại 1-1. Nửa còn lại là ràng buộc UNIQUE
    /// dưới database — xem chú thích <c>Conversation.PairKey</c> về vì sao chỉ mình phép
    /// kiểm này không đủ.
    /// </summary>
    Task<Conversation?> GetDirectAsync(string pairKey, CancellationToken cancellationToken);

    /// <summary>
    /// Danh sách hội thoại của một người, mới nhất trước.
    ///
    /// <b>Chặn dưới theo <c>JoinedAtUtc</c> phải áp ở đây nữa, không chỉ ở màn tin nhắn.</b>
    /// Bỏ sót thì người vừa được thêm vào nhóm thấy dòng xem trước của một tin họ mở ra sẽ
    /// không đọc được — một lời hứa hụt, và là kiểu rò rỉ khó chịu nhất vì nó rò đúng một
    /// câu.
    /// </summary>
    Task<IReadOnlyList<ConversationRow>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
