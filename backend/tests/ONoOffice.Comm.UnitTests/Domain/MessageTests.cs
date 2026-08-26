using ONoOffice.Comm.Domain;
using ONoOffice.Comm.Domain.Entities;

namespace ONoOffice.Comm.UnitTests.Domain;

/// <summary>
/// Tin nhắn — gốc tổng hợp <b>RIÊNG</b>, không phải một collection con của
/// <c>Conversation</c>.
///
/// Sách giáo khoa sẽ bảo tin nhắn "thuộc về" hội thoại nên phải nằm trong nó. Nhưng một
/// hội thoại tích được hàng chục nghìn tin, và nếu tin nằm trong gốc thì <b>gửi thêm một
/// câu "ok" phải nạp toàn bộ lịch sử lên bộ nhớ</b> để EF theo dõi, rồi mới ghi một dòng.
///
/// Ranh giới của gốc tổng hợp là ranh giới của thứ phải đúng CÙNG NHAU trong một giao
/// dịch. Ở đây không có luật nào như thế: không có "hội thoại tối đa N tin", không có
/// tổng nào phải khớp. Nên tin nhắn đứng riêng, chỉ giữ <c>ConversationId</c>.
/// </summary>
public class MessageTests
{
    private static readonly Guid HoiThoai = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid An = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    private static readonly DateTimeOffset Luc = new(2026, 8, 26, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void GuiTin_ThiGiuNguoiGuiVaThoiDiem()
    {
        var tin = Message.Gui(Tenant, HoiThoai, An, "Chào cả nhà", Luc).Value;

        Assert.Equal(HoiThoai, tin.ConversationId);
        Assert.Equal(An, tin.SenderUserId);
        Assert.Equal("Chào cả nhà", tin.Body);
        Assert.Equal(Luc, tin.SentAtUtc);
    }

    [Fact]
    public void TinRong_ThiTuChoi()
    {
        Assert.Equal(
            CommErrors.Messages.Empty,
            Message.Gui(Tenant, HoiThoai, An, "   \n  ", Luc).Error);
    }

    /// <summary>
    /// Cắt khoảng trắng hai đầu, nhưng <b>GIỮ NGUYÊN xuống dòng ở giữa</b>.
    ///
    /// Người ta gõ danh sách gạch đầu dòng trong chat suốt ngày. Gộp xuống dòng thành dấu
    /// cách là sửa lời người khác nói.
    /// </summary>
    [Fact]
    public void GiuNguyenXuongDongOGiua()
    {
        var tin = Message.Gui(Tenant, HoiThoai, An, "  - một\n- hai  ", Luc).Value;

        Assert.Equal("- một\n- hai", tin.Body);
    }

    [Fact]
    public void TinQuaDai_ThiTuChoi()
    {
        Assert.Equal(
            CommErrors.Messages.TooLong,
            Message.Gui(Tenant, HoiThoai, An, new string('a', 4001), Luc).Error);
    }

    [Fact]
    public void KhongCoNguoiGui_ThiTuChoi()
    {
        Assert.Equal(
            CommErrors.Messages.SenderRequired,
            Message.Gui(Tenant, HoiThoai, Guid.Empty, "Chào", Luc).Error);
    }

    /// <summary>
    /// Tin nhắn KHÔNG ôm đối tượng <c>Conversation</c>, chỉ giữ khoá.
    ///
    /// Test này canh chính cái ranh giới nói ở đầu file: có ngày ai đó sẽ thấy tiện khi
    /// thêm <c>public Conversation Conversation { get; }</c> để lấy tên nhóm, và từ đó
    /// mỗi lần nạp một tin là kéo theo cả hội thoại.
    /// </summary>
    [Fact]
    public void TinNhan_KhongOmHoiThoai()
    {
        Assert.DoesNotContain(
            typeof(Message).GetProperties(),
            p => p.PropertyType == typeof(Conversation));
    }
}
