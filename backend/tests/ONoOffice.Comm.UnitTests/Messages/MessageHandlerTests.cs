using ONoOffice.Comm.Application.Abstractions;
using ONoOffice.Comm.Application.Messages.GetList;
using ONoOffice.Comm.Application.Messages.Send;
using ONoOffice.Comm.Domain;
using ONoOffice.Comm.Domain.Entities;
using ONoOffice.Comm.UnitTests.Fakes;
using ONoOffice.Identity.Contracts;

namespace ONoOffice.Comm.UnitTests.Messages;

public class MessageHandlerTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid An = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Binh = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid Chi = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003");

    private static readonly DateTimeOffset Luc = new(2026, 8, 26, 9, 0, 0, TimeSpan.Zero);

    private static UserSummary TaiKhoan(Guid id, string ten) =>
        new(id, $"{id:N}@congty.vn", ten, "Member", IsActive: true, MustChangePassword: false, Luc);

    private static readonly UserSummary[] CaCongTy =
    [
        TaiKhoan(An, "Nguyễn An"),
        TaiKhoan(Binh, "Trần Bình"),
    ];

    // ══════════════════════════════════════════════════════════════════════
    // Gửi tin
    // ══════════════════════════════════════════════════════════════════════

    private static SendMessageCommandHandler Gui(
        Conversation ht,
        Guid nguoiGui,
        FakeMessageRepository tin,
        DateTimeOffset? bayGio = null) =>
        new(new CoSanHoiThoai(ht),
            tin,
            new FakeUserDirectory(CaCongTy),
            new FakeCurrentTenant(Tenant),
            new FakeCurrentUser(nguoiGui),
            new FakeClock(bayGio ?? Luc));

    [Fact]
    public async Task GuiTin_ThiLuuLaiVaTraVeKemTenNguoiGui()
    {
        var ht = Conversation.MoRieng(Tenant, An, Binh, Luc).Value;
        var tin = new FakeMessageRepository();

        var kq = await Gui(ht, An, tin).Handle(new SendMessageCommand(ht.Id, "Chào Bình"), default);

        Assert.True(kq.IsSuccess);
        Assert.Equal("Chào Bình", kq.Value.Body);
        Assert.Equal("Nguyễn An", kq.Value.SenderName);
        Assert.Equal(ht.Id, tin.Added.Single().ConversationId);
    }

    /// <summary>
    /// Phân quyền của module này là <b>tư cách tham gia</b>, không phải một quyền trong
    /// bảng vai trò.
    ///
    /// Không có phép kiểm này thì một tài khoản hợp lệ bất kỳ chỉ cần đoán ra mã hội thoại
    /// là nói được vào cuộc trò chuyện riêng của hai người khác — và <c>[Authorize]</c> ở
    /// controller vẫn thấy mọi thứ hoàn toàn bình thường.
    /// </summary>
    [Fact]
    public async Task NguoiNgoaiGuiTinVao_ThiTuChoi()
    {
        var ht = Conversation.MoRieng(Tenant, An, Binh, Luc).Value;
        var tin = new FakeMessageRepository();

        var kq = await Gui(ht, Chi, tin).Handle(new SendMessageCommand(ht.Id, "Xin chào"), default);

        Assert.Equal(CommErrors.Conversations.NotAParticipant, kq.Error);
        Assert.Empty(tin.Added);
    }

    [Fact]
    public async Task GuiVaoHoiThoaiKhongCoThat_ThiTuChoi()
    {
        var handler = new SendMessageCommandHandler(
            new FakeConversationRepository(),
            new FakeMessageRepository(),
            new FakeUserDirectory(CaCongTy),
            new FakeCurrentTenant(Tenant),
            new FakeCurrentUser(An),
            new FakeClock(Luc));

        Assert.Equal(
            CommErrors.Conversations.NotFound,
            (await handler.Handle(new SendMessageCommand(Guid.NewGuid(), "Chào"), default)).Error);
    }

    [Fact]
    public async Task TinRong_ThiTuChoi()
    {
        var ht = Conversation.MoRieng(Tenant, An, Binh, Luc).Value;

        Assert.Equal(
            CommErrors.Messages.Empty,
            (await Gui(ht, An, new FakeMessageRepository())
                .Handle(new SendMessageCommand(ht.Id, "  "), default)).Error);
    }

    /// <summary>
    /// Gửi xong thì chính mình coi như đã đọc tới đó.
    ///
    /// Thiếu bước này thì vừa bấm Gửi là huy hiệu đỏ nhảy lên một — cho đúng câu mình vừa
    /// gõ. Số chưa đọc ở tầng dữ liệu cũng đã loại tin của chính mình ra, nên đây là lớp
    /// thứ hai; hai lớp là cố ý, vì một mốc "đã đọc" đứng yên khi người ta đang nói chuyện
    /// là thứ không ai đi kiểm cho tới khi nó sai.
    /// </summary>
    [Fact]
    public async Task GuiTinXong_ThiMocDaDocCuaChinhMinhTienTheo()
    {
        var ht = Conversation.MoRieng(Tenant, An, Binh, Luc).Value;
        var bayGio = Luc.AddHours(2);

        await Gui(ht, An, new FakeMessageRepository(), bayGio)
            .Handle(new SendMessageCommand(ht.Id, "Chào"), default);

        Assert.Equal(bayGio, ht.Participants.Single(p => p.UserId == An).LastReadAtUtc);
        Assert.Null(ht.Participants.Single(p => p.UserId == Binh).LastReadAtUtc);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Đọc tin
    // ══════════════════════════════════════════════════════════════════════

    private static GetMessagesQueryHandler Doc(
        Conversation ht,
        Guid nguoiXem,
        FakeMessageRepository tin) =>
        new(new CoSanHoiThoai(ht), tin, new FakeUserDirectory(CaCongTy), new FakeCurrentUser(nguoiXem));

    /// <summary>
    /// Chặn dưới của phép đọc là <c>JoinedAtUtc</c> của CHÍNH người đang xem.
    ///
    /// Đây là thứ dễ quên nhất của cả module, vì bỏ quên nó thì mọi thứ trông vẫn đúng —
    /// chỉ là người vừa được thêm vào nhóm đọc được cả năm trước, kể cả những gì nói về
    /// họ trước khi họ vào.
    /// </summary>
    [Fact]
    public async Task DocTin_ChanDuoiLaLucNGUOIDOVaoNhom()
    {
        var ht = Conversation.MoNhom(Tenant, "Nhóm", An, [Binh], Luc).Value;
        var vaoSau = Luc.AddDays(30);
        ht.ThemNguoi(Chi, vaoSau);

        var tin = new GhiLaiThamSo();
        await Doc(ht, Chi, tin).Handle(new GetMessagesQuery(ht.Id), default);

        Assert.Equal(vaoSau, tin.NotBefore);
    }

    [Fact]
    public async Task NguoiNgoaiDocTin_ThiTuChoi()
    {
        var ht = Conversation.MoRieng(Tenant, An, Binh, Luc).Value;

        Assert.Equal(
            CommErrors.Conversations.NotAParticipant,
            (await Doc(ht, Chi, new FakeMessageRepository())
                .Handle(new GetMessagesQuery(ht.Id), default)).Error);
    }

    /// <summary>
    /// Trần cứng cho số tin mỗi lượt. Không có nó thì <c>?take=1000000</c> kéo cả bảng
    /// lớn nhất hệ thống lên bộ nhớ trong một request — rẻ để gửi, đắt để phục vụ.
    /// </summary>
    [Fact]
    public async Task SoTinMoiLuot_CoTranCung()
    {
        var ht = Conversation.MoRieng(Tenant, An, Binh, Luc).Value;
        var tin = new GhiLaiThamSo();

        await Doc(ht, An, tin).Handle(new GetMessagesQuery(ht.Id, Take: 1_000_000), default);

        Assert.True(tin.Take <= 100, $"Trần bị hở: handler xin {tin.Take} tin một lượt.");
    }

    /// <summary>
    /// Trả về theo thứ tự CŨ → MỚI, dù kho đọc lên theo chiều ngược lại.
    ///
    /// Kho phải đọc mới-nhất-trước vì con trỏ cuộn ngược đi theo chiều đó. Nhưng cửa sổ
    /// chat vẽ từ trên xuống theo chiều thời gian, nên chỗ đảo phải nằm ở đây — để ở
    /// frontend thì mỗi màn dùng lại API này phải nhớ đảo, và có ngày một màn quên.
    /// </summary>
    [Fact]
    public async Task TraVeTheoThuTuCuTruocMoiSau()
    {
        var ht = Conversation.MoRieng(Tenant, An, Binh, Luc).Value;
        var tin = new GhiLaiThamSo(
            new MessageRow(Guid.NewGuid(), An, "câu mới", Luc.AddMinutes(5)),
            new MessageRow(Guid.NewGuid(), Binh, "câu cũ", Luc));

        var kq = await Doc(ht, An, tin).Handle(new GetMessagesQuery(ht.Id), default);

        Assert.Equal(["câu cũ", "câu mới"], kq.Value.Items.Select(m => m.Body));
        Assert.Equal(["Trần Bình", "Nguyễn An"], kq.Value.Items.Select(m => m.SenderName));
    }
}
