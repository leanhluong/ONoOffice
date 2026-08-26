using Luong.Kernel.Domain;

namespace ONoOffice.Comm.Domain.Entities;

/// <summary>
/// Một người trong một hội thoại.
///
/// Đây là thực thể con, KHÔNG phải gốc tổng hợp: nó chỉ tồn tại bên trong một
/// <see cref="Conversation"/> và chỉ đổi được qua các phương thức của hội thoại. Vì thế
/// mọi setter ở đây đều <c>private</c> và mọi phép sửa đều là <c>internal</c> — bên ngoài
/// module không có cửa nào tự tay đặt "đã đọc" cho người khác.
///
/// Nó giữ hai mốc thời gian, và cả hai đều là RANH GIỚI TẦM NHÌN chứ không phải dữ liệu
/// trang trí:
/// <list type="bullet">
///   <item><see cref="JoinedAtUtc"/> — chặn dưới: không thấy gì trước lúc mình vào.</item>
///   <item><see cref="LastReadAtUtc"/> — mốc đếm chưa đọc.</item>
/// </list>
/// </summary>
public sealed class Participant : Entity<Guid>
{
    private Participant(Guid id, Guid userId, DateTimeOffset joinedAtUtc) : base(id)
    {
        UserId = userId;
        JoinedAtUtc = joinedAtUtc;
    }

    /// <summary>Dành cho EF Core.</summary>
    private Participant()
    {
    }

    /// <summary>
    /// Chỉ giữ KHOÁ sang <c>identity.users</c>, không giữ tham chiếu.
    ///
    /// Luật 3: không JOIN chéo schema. Tên và ảnh của người này do tầng Application hỏi
    /// <c>IUserDirectory</c> rồi ghép vào lúc trả về — chứ không phải EF tự nối bảng.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Vào lúc nào — và vì thế, <b>thấy được từ lúc nào</b>.
    ///
    /// Thêm một người vào nhóm đang chạy mà không có mốc này thì họ đọc được toàn bộ lịch
    /// sử, kể cả những gì nói về chính họ trước khi họ vào.
    /// </summary>
    public DateTimeOffset JoinedAtUtc { get; private set; }

    /// <summary>
    /// Đã đọc tới đâu. <c>null</c> nghĩa là chưa mở hội thoại này lần nào.
    ///
    /// Lưu MỐC THỜI GIAN chứ không lưu số tin chưa đọc: số đếm là dữ liệu dư thừa, và một
    /// lần tăng/giảm hụt là nó lệch vĩnh viễn — huy hiệu đỏ hiện mãi cho một tin không tồn
    /// tại, và không có cách nào chữa ngoài việc đi sửa tay trong database.
    /// </summary>
    public DateTimeOffset? LastReadAtUtc { get; private set; }

    internal static Participant Them(Guid userId, DateTimeOffset luc) =>
        new(Guid.NewGuid(), userId, luc);

    /// <summary>
    /// Đẩy mốc đã đọc lên. <b>Chỉ tiến, không lùi.</b>
    ///
    /// Cuộn ngược lên đọc lại tin cũ là chuyện bình thường; nếu mốc lùi theo thì mọi tin
    /// mới bỗng dưng thành chưa đọc — huy hiệu đỏ hiện lại cho đúng thứ vừa đọc xong.
    /// </summary>
    internal void DaDocToi(DateTimeOffset luc)
    {
        if (LastReadAtUtc is null || luc > LastReadAtUtc)
        {
            LastReadAtUtc = luc;
        }
    }
}
