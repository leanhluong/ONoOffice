using Luong.Kernel.Abstractions;
using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Comm.Application.Abstractions;
using ONoOffice.Comm.Domain;
using ONoOffice.Comm.Domain.Entities;
using ONoOffice.Identity.Contracts;

namespace ONoOffice.Comm.Application.Conversations.GetList;

public sealed record GetConversationsQuery : IQuery<IReadOnlyList<ConversationSummary>>;

/// <summary>
/// Cột trái của màn Trao đổi: mọi hội thoại của tôi, mới nhất trước.
///
/// <b>Không phân trang, cố ý.</b> Cột này là thứ người dùng quét mắt để tìm một cái tên,
/// và một danh sách hội thoại có nút "trang sau" thì cái tên cần tìm nằm ở trang nào là
/// câu hỏi không ai trả lời được. Số hội thoại của một người trong một công ty là hàng
/// chục, không phải hàng nghìn. Đến ngày nó thành hàng nghìn thì thứ cần thêm là ô tìm
/// kiếm và mục "đã lưu trữ", không phải phân trang.
///
/// Handler mỏng: lọc, sắp xếp, đếm chưa đọc, lấy tin cuối — tất cả là việc của một câu
/// truy vấn duy nhất. Việc DUY NHẤT ở đây là ghép tên từ module Identity, thứ mà Luật 3
/// không cho câu truy vấn đó tự làm.
/// </summary>
internal sealed class GetConversationsQueryHandler(
    IConversationRepository conversations,
    IUserDirectory users,
    ICurrentUser currentUser) : IQueryHandler<GetConversationsQuery, IReadOnlyList<ConversationSummary>>
{
    public async Task<Result<IReadOnlyList<ConversationSummary>>> Handle(
        GetConversationsQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } toi)
        {
            return CommErrors.Conversations.NotAParticipant;
        }

        var rows = await conversations.ListForUserAsync(toi, cancellationToken);

        if (rows.Count == 0)
        {
            // Về sớm: không có hội thoại nào thì không cần hỏi danh bạ. Workspace mới lập
            // đi qua nhánh này mỗi lần mở app, và đây là màn mặc định.
            return Array.Empty<ConversationSummary>();
        }

        var danhBa = await SoDanhBa.MoAsync(users, cancellationToken);

        return rows.Select(r => new ConversationSummary(
            r.Id,
            r.Kind,
            TenHienThi(r, danhBa),
            r.OtherUserId,
            r.ParticipantCount,
            r.LastMessageBody,
            danhBa.Cua(r.LastMessageSenderUserId),
            r.LastMessageAtUtc,
            r.UnreadCount)).ToList();
    }

    /// <summary>
    /// Nhóm dùng tên của nhóm; hội thoại riêng dùng tên NGƯỜI KIA.
    ///
    /// Một hàng, hai câu trả lời khác nhau tuỳ ai đang xem — đó là lý do tên này không thể
    /// nằm trong bảng, và phải tính ở đây mỗi lần.
    /// </summary>
    private static string TenHienThi(ConversationRow r, SoDanhBa danhBa) =>
        r.Kind == ConversationKind.Nhom
            ? r.Name ?? string.Empty
            : danhBa.Cua(r.OtherUserId) ?? string.Empty;
}
