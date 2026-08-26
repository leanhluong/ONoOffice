using Microsoft.EntityFrameworkCore;
using ONoOffice.Comm.Application.Abstractions;
using ONoOffice.Comm.Domain.Entities;
using ONoOffice.Comm.Infrastructure.Persistence.Configurations;

namespace ONoOffice.Comm.Infrastructure.Persistence.Repositories;

internal sealed class EfConversationRepository(CommDbContext context) : IConversationRepository
{
    public void Add(Conversation conversation) => context.Conversations.Add(conversation);

    /// <summary>
    /// Nạp KÈM người tham gia, luôn luôn.
    ///
    /// Mọi luật của gốc này đều đọc danh sách đó — "có phải người tham gia không", "vào
    /// lúc nào", "đã đọc tới đâu". Để bên gọi tự nhớ <c>Include</c> thì chỗ nào quên sẽ
    /// nhận một danh sách RỖNG và kết luận là người dùng không ở trong hội thoại. Một lỗi
    /// phân quyền hiện ra dưới dạng "bạn không ở trong hội thoại này" là loại lỗi người ta
    /// đi tìm rất lâu.
    /// </summary>
    public Task<Conversation?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        context.Conversations
            .Include(c => c.Participants)
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Conversation?> GetDirectAsync(string pairKey, CancellationToken cancellationToken) =>
        context.Conversations
            .Include(c => c.Participants)
            .SingleOrDefaultAsync(c => c.PairKey == pairKey, cancellationToken);

    /// <summary>
    /// Cột trái của màn Trao đổi, trong MỘT lượt hỏi database.
    ///
    /// Câu này nặng — nó mang theo hai truy vấn con vào bảng lớn nhất hệ thống. Nhưng lựa
    /// chọn thay thế nặng hơn nhiều: lấy danh sách hội thoại rồi lặp từng cái để hỏi tin
    /// cuối và đếm chưa đọc là 1 + 2N lượt đi về, cho một màn mở ra là thấy. Với 40 hội
    /// thoại thì đó là 81 vòng mạng.
    ///
    /// Cả hai truy vấn con đều lọc theo <c>JoinedAtUtc</c> của CHÍNH người đang xem. Bỏ
    /// sót ở dòng xem trước thì người vừa được thêm vào nhóm nhìn thấy một câu họ mở ra sẽ
    /// không đọc được — rò rỉ đúng một dòng, và là kiểu khó chịu nhất vì nó vẫn "chạy".
    /// </summary>
    public async Task<IReadOnlyList<ConversationRow>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var rows = await context.Conversations
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .Select(c => new
            {
                c.Id,
                c.Kind,
                c.Name,
                c.CreatedAtUtc,
                Toi = c.Participants.Single(p => p.UserId == userId),
                SoNguoi = c.Participants.Count,
                NguoiKia = c.Participants
                    .Where(p => p.UserId != userId)
                    .Select(p => (Guid?)p.UserId)
                    .FirstOrDefault(),
            })
            .Select(x => new
            {
                x.Id,
                x.Kind,
                x.Name,
                x.CreatedAtUtc,
                x.SoNguoi,
                x.NguoiKia,
                TinCuoi = context.Messages
                    .Where(m => m.ConversationId == x.Id && m.SentAtUtc >= x.Toi.JoinedAtUtc)
                    .OrderByDescending(m => EF.Property<long>(m, MessageConfiguration.Seq))
                    .Select(m => new { m.Body, m.SenderUserId, m.SentAtUtc })
                    .FirstOrDefault(),

                // Tin của CHÍNH MÌNH không bao giờ tính là chưa đọc. Handler cũng đẩy mốc
                // "đã đọc" lên mỗi lần gửi, nên đây là lớp thứ hai — cố ý, vì một mốc
                // đứng yên trong lúc người ta đang nói chuyện là thứ không ai đi kiểm cho
                // tới khi nó sai.
                ChuaDoc = context.Messages.Count(m =>
                    m.ConversationId == x.Id
                    && m.SentAtUtc >= x.Toi.JoinedAtUtc
                    && m.SenderUserId != userId
                    && (x.Toi.LastReadAtUtc == null || m.SentAtUtc > x.Toi.LastReadAtUtc)),
            })
            // Hội thoại chưa có tin nào sắp theo NGÀY TẠO, không rơi xuống đáy. Để `null`
            // sắp thẳng thì Postgres xếp NULL lên đầu ở chiều giảm dần — nhóm vừa tạo và
            // nhóm bỏ hoang ba tháng nằm cạnh nhau ở đỉnh danh sách.
            .OrderByDescending(x => x.TinCuoi == null ? x.CreatedAtUtc : x.TinCuoi.SentAtUtc)
            .ToListAsync(cancellationToken);

        return [.. rows.Select(x => new ConversationRow(
            x.Id,
            x.Kind,
            x.Name,
            x.NguoiKia,
            x.SoNguoi,
            x.TinCuoi?.Body,
            x.TinCuoi?.SenderUserId,
            x.TinCuoi?.SentAtUtc,
            x.ChuaDoc))];
    }
}
