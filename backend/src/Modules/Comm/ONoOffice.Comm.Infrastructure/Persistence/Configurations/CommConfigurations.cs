using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ONoOffice.Comm.Domain.Entities;

namespace ONoOffice.Comm.Infrastructure.Persistence.Configurations;

internal sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(120);

        // Enum lưu bằng SỐ (mặc định của EF). Đổi sang chuỗi thì đọc database dễ hơn,
        // nhưng đổi tên một hằng trong code sẽ làm dữ liệu cũ không đọc lại được.
        builder.Property(c => c.Kind).IsRequired();

        /*
          ĐÂY là ràng buộc quan trọng nhất của cả module.

          Khoá cặp chặn ca đua: An bấm "nhắn Bình" đúng lúc Bình bấm "nhắn An", cả hai
          truy vấn đều thấy "chưa có", cả hai đều ghi. Không có UNIQUE thì kết quả là hai
          hội thoại song song, mỗi người nói vào một cái, KHÔNG AI THẤY TIN CỦA AI và cũng
          chẳng có lỗi nào. Kiểm trong handler không cứu được: giữa lúc kiểm và lúc ghi
          vẫn còn một khe.

          Kèm `TenantId` vì hai công ty khác nhau có thể có hai người trùng mã — không
          xảy ra với `Guid`, nhưng chỉ mục này cũng là chỉ mục dùng để TRA, và mọi truy
          vấn đều đã lọc tenant sẵn.

          Nhóm để `PairKey` là NULL, và Postgres không ép UNIQUE lên NULL — nên một cột
          duy nhất phục vụ được cả hai kiểu hội thoại.
        */
        builder.Property(c => c.PairKey).HasMaxLength(73);
        builder.HasIndex(c => new { c.TenantId, c.PairKey }).IsUnique();

        /*
          Người tham gia là thực thể CON, nạp qua chính hội thoại.

          `PropertyAccessMode.Field` vì `Participants` chỉ đọc — EF phải ghi thẳng vào
          `_participants`. Thiếu dòng này thì EF cố gọi setter không có và nổ lúc khởi
          động model, đúng lúc đáng nổ.
        */
        builder.HasMany(c => c.Participants)
            .WithOne()
            .HasForeignKey("ConversationId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Conversation.Participants))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(c => c.DomainEvents);
    }
}

internal sealed class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
        builder.ToTable("Participants");
        builder.HasKey(p => p.Id);

        // "Mọi hội thoại của tôi" là truy vấn chạy mỗi lần mở app, và nó lọc đúng cột này.
        builder.HasIndex(p => p.UserId);

        // Một người chỉ có một chỗ trong một hội thoại. Gốc tổng hợp đã canh
        // (`AlreadyIn`), nhưng luật này rẻ để ép ở đây và đắt để phát hiện nếu lọt: hai
        // hàng thì người đó nhận hai lần thông báo và số chưa đọc nhân đôi.
        builder.HasIndex("ConversationId", nameof(Participant.UserId)).IsUnique();
    }
}

internal sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    /// <summary>
    /// Số thứ tự do database phát, dùng để SẮP XẾP và làm con trỏ cuộn.
    ///
    /// Vì sao không sắp bằng <c>SentAtUtc</c>: hai tin trùng nhau đúng một micro-giây thì
    /// câu lọc <c>WHERE sent_at &lt; con_trỏ</c> nhảy cóc mất một tin — người dùng cuộn
    /// qua chỗ đó và câu ấy đơn giản là không còn ở đâu cả, vĩnh viễn, không lỗi nào báo.
    /// Ghép cặp <c>(sent_at, id)</c> thì đúng, nhưng so sánh <c>uuid</c> bằng &lt; trong
    /// LINQ không có gì đảm bảo dịch được sang SQL.
    ///
    /// Một cột <c>bigint</c> tự tăng thì vừa đúng vừa dịch được, và nó cũng là thứ tự THẬT
    /// của phép ghi.
    ///
    /// Để ở dạng thuộc tính BÓNG (shadow property), không phải thuộc tính của
    /// <c>Message</c>: đây là chuyện của cách lưu, không phải của nghiệp vụ. Tầng Domain
    /// không nên biết database đánh số cho nó.
    /// </summary>
    internal const string Seq = "Seq";

    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Body).HasMaxLength(4000).IsRequired();

        builder.Property<long>(Seq).ValueGeneratedOnAdd();

        /*
          Chỉ mục phục vụ ĐÚNG một truy vấn, và là truy vấn chạy nhiều nhất hệ thống:
          "lấy N tin gần nhất của hội thoại này, cũ hơn con trỏ".

          `(conversation_id, seq DESC)` cho Postgres đọc thẳng N hàng đầu rồi dừng, không
          sắp xếp gì cả. Thiếu nó thì mỗi lần cuộn là một lượt quét cả bảng lớn nhất hệ
          thống.
        */
        builder.HasIndex("ConversationId", Seq).IsDescending(false, true);

        // KHÔNG khai khoá ngoại sang `Conversations`. Tin nhắn là gốc tổng hợp RIÊNG —
        // xem chú thích đầu `Message.cs`. Khai khoá ngoại thì EF dựng navigation ngược và
        // ranh giới đó bị xoá nhoà ngay lần đầu ai đó viết `.Include(...)` cho tiện.
        builder.HasIndex(m => m.SenderUserId);

        builder.Ignore(m => m.DomainEvents);
    }
}
