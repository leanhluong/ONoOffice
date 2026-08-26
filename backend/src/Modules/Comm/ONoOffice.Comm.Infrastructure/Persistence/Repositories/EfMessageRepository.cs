using Microsoft.EntityFrameworkCore;
using ONoOffice.Comm.Application.Abstractions;
using ONoOffice.Comm.Domain.Entities;
using ONoOffice.Comm.Infrastructure.Persistence.Configurations;

namespace ONoOffice.Comm.Infrastructure.Persistence.Repositories;

internal sealed class EfMessageRepository(CommDbContext context) : IMessageRepository
{
    public void Add(Message message) => context.Messages.Add(message);

    /// <summary>
    /// Một trang tin, mới nhất trước, cũ hơn con trỏ.
    ///
    /// Con trỏ là một MÃ TIN, và ở đây nó được đổi sang số thứ tự <c>Seq</c> — lý do đầy
    /// đủ nằm ở <c>MessageConfiguration.Seq</c>. Tóm lại: sắp bằng thời gian thì hai tin
    /// trùng đúng một micro-giây làm một câu biến mất vĩnh viễn.
    ///
    /// Con trỏ trỏ vào một tin không còn (bị xoá, hoặc client giữ mã cũ) thì coi như KHÔNG
    /// có con trỏ — trả về trang mới nhất. Ném lỗi ở đây thì cửa sổ chat trắng xoá vì một
    /// chuyện chỉ cần cuộn lại là xong.
    /// </summary>
    public async Task<MessagePage> PageAsync(
        Guid conversationId,
        Guid? beforeMessageId,
        DateTimeOffset notBefore,
        int take,
        CancellationToken cancellationToken)
    {
        var q = context.Messages
            .Where(m => m.ConversationId == conversationId && m.SentAtUtc >= notBefore);

        if (beforeMessageId is { } conTro)
        {
            long? moc = await context.Messages
                .Where(m => m.Id == conTro)
                .Select(m => (long?)EF.Property<long>(m, MessageConfiguration.Seq))
                .FirstOrDefaultAsync(cancellationToken);

            if (moc is { } so)
            {
                q = q.Where(m => EF.Property<long>(m, MessageConfiguration.Seq) < so);
            }
        }

        /*
          Lấy DƯ MỘT tin để trả lời "còn nữa không".

          Cách khác là chạy thêm một câu `COUNT(*)` trên bảng lớn nhất hệ thống, mỗi lần
          cuộn, để so với số đã lấy — đắt hơn nhiều lần cho cùng một câu trả lời đúng/sai.
        */
        var lay = await q
            .OrderByDescending(m => EF.Property<long>(m, MessageConfiguration.Seq))
            .Take(take + 1)
            .Select(m => new MessageRow(m.Id, m.SenderUserId, m.Body, m.SentAtUtc))
            .ToListAsync(cancellationToken);

        bool conNua = lay.Count > take;

        return new MessagePage(conNua ? lay[..take] : lay, conNua);
    }
}
