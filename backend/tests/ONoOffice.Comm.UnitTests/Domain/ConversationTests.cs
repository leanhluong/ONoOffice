using ONoOffice.Comm.Domain;
using ONoOffice.Comm.Domain.Entities;

namespace ONoOffice.Comm.UnitTests.Domain;

/// <summary>
/// Hội thoại — gốc tổng hợp của module Trao đổi.
///
/// ═══════════════════════════════════════════════════════════════════════
///  HAI KIỂU HỘI THOẠI, VÀ CHÚNG KHÔNG CÙNG LUẬT
/// ═══════════════════════════════════════════════════════════════════════
///
/// <b>Riêng (1-1)</b> — đúng hai người, cố định mãi mãi. Không thêm ai vào được: thêm
/// người thứ ba nghĩa là hai người kia bỗng dưng có một khán giả đọc được toàn bộ lịch sử
/// mà họ đã nói khi tưởng chỉ có hai.
///
/// <b>Nhóm</b> — có tên, thêm bớt người được, và người mới chỉ thấy từ lúc họ vào.
/// </summary>
public class ConversationTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid An = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Binh = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid Chi = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003");

    private static readonly DateTimeOffset Luc = new(2026, 8, 26, 9, 0, 0, TimeSpan.Zero);

    // ══════════════════════════════════════════════════════════════════
    // Hội thoại RIÊNG
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Khoá cặp KHÔNG phụ thuộc thứ tự người mở.
    ///
    /// An mở chat với Bình, và Bình mở chat với An — phải ra cùng một hội thoại. Không có
    /// khoá này thì hai người bấm cùng lúc tạo ra hai hội thoại song song, mỗi người nói
    /// vào một cái, và <b>không ai thấy tin của ai</b> mà cũng chẳng có lỗi nào.
    ///
    /// Sắp xếp hai mã rồi ghép: chỉ số học, không cần hỏi database, nên nó dùng được cả ở
    /// ràng buộc UNIQUE lẫn ở phép tra trước khi tạo.
    /// </summary>
    [Fact]
    public void KhoaCap_KhongPhuThuocThuTu()
    {
        var a = Conversation.MoRieng(Tenant, An, Binh, Luc).Value;
        var b = Conversation.MoRieng(Tenant, Binh, An, Luc).Value;

        Assert.Equal(a.PairKey, b.PairKey);
        Assert.NotNull(a.PairKey);
    }

    [Fact]
    public void HoiThoaiRieng_CoDungHaiNguoi()
    {
        var ht = Conversation.MoRieng(Tenant, An, Binh, Luc).Value;

        Assert.Equal(ConversationKind.Rieng, ht.Kind);
        Assert.Equal([An, Binh], ht.Participants.Select(p => p.UserId).Order());

        // Hội thoại riêng KHÔNG có tên: tên của nó là tên người kia, và người kia thì khác
        // nhau tuỳ ai đang nhìn. Lưu một cái tên cố định là lưu một câu sai cho một trong
        // hai người.
        Assert.Null(ht.Name);
    }

    [Fact]
    public void TuNhanTinChoChinhMinh_ThiTuChoi()
    {
        var kq = Conversation.MoRieng(Tenant, An, An, Luc);

        Assert.True(kq.IsFailure);
        Assert.Equal(CommErrors.Conversations.CannotChatWithSelf, kq.Error);
    }

    /// <summary>
    /// KHÔNG thêm người vào hội thoại riêng.
    ///
    /// Người thứ ba sẽ đọc được toàn bộ lịch sử mà hai người kia đã nói khi tưởng chỉ có
    /// hai. Muốn thêm người thì mở một nhóm mới — một bước cố ý, và hai người kia nhìn
    /// thấy nó xảy ra.
    /// </summary>
    [Fact]
    public void ThemNguoiVaoHoiThoaiRieng_ThiTuChoi()
    {
        var ht = Conversation.MoRieng(Tenant, An, Binh, Luc).Value;

        var kq = ht.ThemNguoi(Chi, Luc);

        Assert.True(kq.IsFailure);
        Assert.Equal(CommErrors.Conversations.DirectIsFixed, kq.Error);
        Assert.Equal(2, ht.Participants.Count);
    }

    // ══════════════════════════════════════════════════════════════════
    // Nhóm
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void MoNhom_ThiNguoiTaoLaMotThanhVien()
    {
        var ht = Conversation.MoNhom(Tenant, "Khối Kỹ thuật", An, [Binh], Luc).Value;

        Assert.Equal(ConversationKind.Nhom, ht.Kind);
        Assert.Equal("Khối Kỹ thuật", ht.Name);

        // Người tạo tự động là thành viên. Không thêm thì họ mở một nhóm rồi không vào
        // được chính nó — và nhóm đó không ai xoá được vì không ai ở trong.
        Assert.Contains(An, ht.Participants.Select(p => p.UserId));
        Assert.Equal(2, ht.Participants.Count);
    }

    [Fact]
    public void NhomKhongCoTen_ThiTuChoi()
    {
        Assert.Equal(
            CommErrors.Conversations.NameEmpty,
            Conversation.MoNhom(Tenant, "   ", An, [Binh], Luc).Error);
    }

    /// <summary>
    /// Nhóm KHÔNG có khoá cặp.
    ///
    /// Một công ty có thể có mười nhóm cùng tên "Dự án A" và cả mười đều hợp lệ — chúng là
    /// mười cuộc trò chuyện khác nhau. Gán khoá cặp cho nhóm thì nhóm thứ hai bị ràng buộc
    /// UNIQUE chặn, và người dùng nhận một lỗi 500 không giải thích được.
    /// </summary>
    [Fact]
    public void Nhom_KhongCoKhoaCap()
    {
        Assert.Null(Conversation.MoNhom(Tenant, "Dự án A", An, [Binh], Luc).Value.PairKey);
    }

    [Fact]
    public void ThemNguoiDaCoTrongNhom_ThiTuChoi()
    {
        var ht = Conversation.MoNhom(Tenant, "Nhóm", An, [Binh], Luc).Value;

        Assert.Equal(CommErrors.Conversations.AlreadyIn, ht.ThemNguoi(Binh, Luc).Error);
    }

    /// <summary>
    /// Người mới chỉ thấy tin nhắn TỪ LÚC HỌ VÀO.
    ///
    /// `JoinedAtUtc` là mốc cắt. Không có nó thì thêm một người vào nhóm nghĩa là trao cho
    /// họ toàn bộ lịch sử — kể cả những gì nói về chính họ trước khi họ vào.
    /// </summary>
    [Fact]
    public void NguoiMoiVaoNhom_MangMocThoiGianRIENG()
    {
        var ht = Conversation.MoNhom(Tenant, "Nhóm", An, [Binh], Luc).Value;
        var sau = Luc.AddDays(3);

        ht.ThemNguoi(Chi, sau);

        Assert.Equal(sau, ht.Participants.Single(p => p.UserId == Chi).JoinedAtUtc);
        Assert.Equal(Luc, ht.Participants.Single(p => p.UserId == An).JoinedAtUtc);
    }

    [Fact]
    public void RoiNhom_ThiKhongConLaThanhVien()
    {
        var ht = Conversation.MoNhom(Tenant, "Nhóm", An, [Binh], Luc).Value;

        Assert.True(ht.RoiDi(Binh).IsSuccess);
        Assert.False(ht.CoThanhVien(Binh));
    }

    [Fact]
    public void RoiHoiThoaiRieng_ThiTuChoi()
    {
        var ht = Conversation.MoRieng(Tenant, An, Binh, Luc).Value;

        Assert.Equal(CommErrors.Conversations.DirectIsFixed, ht.RoiDi(An).Error);
    }

    // ══════════════════════════════════════════════════════════════════
    // Đã đọc tới đâu
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Mốc "đã đọc" chỉ TIẾN, không lùi.
    ///
    /// Người dùng cuộn ngược lên đọc lại tin cũ là chuyện bình thường, và nếu mốc đó lùi
    /// theo thì mọi tin mới bỗng dưng thành chưa đọc — huy hiệu đỏ hiện lại cho thứ họ
    /// vừa đọc xong.
    /// </summary>
    [Fact]
    public void MocDaDoc_ChiTIEN_khong_lui()
    {
        var ht = Conversation.MoRieng(Tenant, An, Binh, Luc).Value;
        var muon = Luc.AddHours(2);

        ht.DanhDauDaDoc(An, muon);
        ht.DanhDauDaDoc(An, Luc);

        Assert.Equal(muon, ht.Participants.Single(p => p.UserId == An).LastReadAtUtc);
    }

    [Fact]
    public void NguoiNgoaiDanhDauDaDoc_ThiTuChoi()
    {
        var ht = Conversation.MoRieng(Tenant, An, Binh, Luc).Value;

        Assert.Equal(CommErrors.Conversations.NotAParticipant, ht.DanhDauDaDoc(Chi, Luc).Error);
    }
}
