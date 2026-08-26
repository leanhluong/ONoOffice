using ONoOffice.Comm.Application.Abstractions;
using ONoOffice.Comm.Application.Conversations.CreateGroup;
using ONoOffice.Comm.Application.Conversations.GetList;
using ONoOffice.Comm.Application.Conversations.MarkRead;
using ONoOffice.Comm.Application.Conversations.OpenDirect;
using ONoOffice.Comm.Domain;
using ONoOffice.Comm.Domain.Entities;
using ONoOffice.Comm.UnitTests.Fakes;
using ONoOffice.Identity.Contracts;

namespace ONoOffice.Comm.UnitTests.Conversations;

public class ConversationHandlerTests
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
        TaiKhoan(Chi, "Lê Chi"),
    ];

    private static OpenDirectConversationCommandHandler MoRieng(
        FakeConversationRepository kho,
        Guid? toi = null,
        params UserSummary[] danhBa) =>
        new(kho,
            new FakeUserDirectory(danhBa.Length == 0 ? CaCongTy : danhBa),
            new FakeCurrentTenant(Tenant),
            new FakeCurrentUser(toi ?? An),
            new FakeClock(Luc));

    // ══════════════════════════════════════════════════════════════════════
    // Mở hội thoại riêng
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Bấm "nhắn tin" lần thứ hai phải mở lại đúng hội thoại cũ, không tạo cái mới.
    ///
    /// Đây là cả điểm mấu chốt của <c>PairKey</c> nhìn từ tầng này: người dùng không có
    /// khái niệm "tạo hội thoại", họ chỉ bấm vào một cái tên. Mỗi lần bấm mà đẻ ra một
    /// phòng mới thì lịch sử vỡ thành từng mảnh và không mảnh nào tìm lại được.
    /// </summary>
    [Fact]
    public async Task MoLaiHoiThoaiDaCo_ThiKhongTaoMoi()
    {
        var daCo = Conversation.MoRieng(Tenant, An, Binh, Luc).Value;
        var kho = new CoSanHoiThoai(daCo);

        var kq = await MoRieng(kho).Handle(new OpenDirectConversationCommand(Binh), default);

        Assert.Equal(daCo.Id, kq.Value.Id);
        Assert.Empty(kho.Added);
    }

    [Fact]
    public async Task MoHoiThoaiRiengLanDau_ThiTao()
    {
        var kho = new FakeConversationRepository();

        var kq = await MoRieng(kho).Handle(new OpenDirectConversationCommand(Binh), default);

        Assert.True(kq.IsSuccess);
        var moi = Assert.Single(kho.Added);
        Assert.Equal(ConversationKind.Rieng, moi.Kind);
        Assert.Equal([An, Binh], moi.Participants.Select(p => p.UserId).Order());
    }

    /// <summary>
    /// Tên hiển thị của hội thoại riêng là tên NGƯỜI KIA.
    ///
    /// Nó không nằm trong bảng <c>comm.conversations</c> và không thể nằm ở đó: cùng một
    /// hàng, An nhìn thấy "Trần Bình" còn Bình nhìn thấy "Nguyễn An". Handler ghép vào
    /// bằng <c>IUserDirectory</c> — đây là chỗ Luật 3 (cấm JOIN chéo schema) phải trả giá,
    /// và cái giá đó là một lượt hỏi thêm.
    /// </summary>
    [Fact]
    public async Task TenHienThiCuaHoiThoaiRieng_LaTenNguoiKIA()
    {
        var kq = await MoRieng(new FakeConversationRepository())
            .Handle(new OpenDirectConversationCommand(Binh), default);

        Assert.Equal("Trần Bình", kq.Value.DisplayName);
    }

    /// <summary>
    /// Không tin con số client gửi lên.
    ///
    /// <c>UserId</c> ở schema <c>comm</c> là <c>Guid</c> trần, không phải khoá ngoại —
    /// Luật 3 cấm ràng buộc xuyên schema. Nên nếu handler không hỏi, một mã bịa sẽ tạo ra
    /// một hội thoại với người không tồn tại, và không lớp nào phía dưới bắt được.
    /// </summary>
    [Fact]
    public async Task MoVoiNguoiKhongCoThat_ThiTuChoi()
    {
        var kq = await MoRieng(new FakeConversationRepository())
            .Handle(new OpenDirectConversationCommand(Guid.NewGuid()), default);

        Assert.True(kq.IsFailure);
        Assert.Equal("User.NotFound", kq.Error.Code);
    }

    [Fact]
    public async Task MoVoiChinhMinh_ThiTuChoi()
    {
        var kq = await MoRieng(new FakeConversationRepository())
            .Handle(new OpenDirectConversationCommand(An), default);

        Assert.Equal(CommErrors.Conversations.CannotChatWithSelf, kq.Error);
    }

    [Fact]
    public async Task ChuaDangNhap_ThiTuChoi()
    {
        var handler = new OpenDirectConversationCommandHandler(
            new FakeConversationRepository(),
            new FakeUserDirectory(CaCongTy),
            new FakeCurrentTenant(Tenant),
            new FakeCurrentUser(null),
            new FakeClock(Luc));

        Assert.True((await handler.Handle(new OpenDirectConversationCommand(Binh), default)).IsFailure);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Tạo nhóm
    // ══════════════════════════════════════════════════════════════════════

    private CreateGroupConversationCommandHandler TaoNhom(FakeConversationRepository kho) =>
        new(kho,
            new FakeUserDirectory(CaCongTy),
            new FakeCurrentTenant(Tenant),
            new FakeCurrentUser(An),
            new FakeClock(Luc));

    [Fact]
    public async Task TaoNhom_ThiNguoiTaoCungONhomDo()
    {
        var kho = new FakeConversationRepository();

        var kq = await TaoNhom(kho)
            .Handle(new CreateGroupConversationCommand("Khối Kỹ thuật", [Binh, Chi]), default);

        Assert.True(kq.IsSuccess);
        Assert.Equal("Khối Kỹ thuật", kq.Value.DisplayName);
        Assert.Equal([An, Binh, Chi], kho.Added.Single().Participants.Select(p => p.UserId).Order());
    }

    /// <summary>
    /// Một mã bịa trong danh sách mời làm hỏng CẢ nhóm, không phải bỏ qua riêng nó.
    ///
    /// Bỏ qua âm thầm thì người tạo thấy nhóm dựng xong và tưởng đủ người; họ nói chuyện
    /// vào đó suốt một tuần rồi mới phát hiện một người chưa bao giờ ở trong.
    /// </summary>
    [Fact]
    public async Task MoiNguoiKhongCoThat_ThiHongCaNhom()
    {
        var kho = new FakeConversationRepository();

        var kq = await TaoNhom(kho)
            .Handle(new CreateGroupConversationCommand("Nhóm", [Binh, Guid.NewGuid()]), default);

        Assert.True(kq.IsFailure);
        Assert.Empty(kho.Added);
    }

    /// <summary>
    /// Nhóm phải có ít nhất một người khác.
    ///
    /// Về mặt kỹ thuật một nhóm chỉ có mình mình chạy được, và nhiều ứng dụng dùng nó làm
    /// chỗ ghi chú riêng. Nhưng thứ đó phải là một tính năng CỐ Ý có tên và có chỗ đứng
    /// trên giao diện, không phải sản phẩm phụ của một luật bị quên.
    /// </summary>
    [Fact]
    public async Task TaoNhomMotMinh_ThiTuChoi()
    {
        var kq = await TaoNhom(new FakeConversationRepository())
            .Handle(new CreateGroupConversationCommand("Nhóm", []), default);

        Assert.Equal(CommErrors.Conversations.GroupNeedsSomeone, kq.Error);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Danh sách hội thoại
    // ══════════════════════════════════════════════════════════════════════

    private sealed class KhoCoDanhSach(params ConversationRow[] rows) : FakeConversationRepository
    {
        public override Task<IReadOnlyList<ConversationRow>> ListForUserAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ConversationRow>>(rows);
    }

    private GetConversationsQueryHandler DanhSach(params ConversationRow[] rows) =>
        new(new KhoCoDanhSach(rows),
            new FakeUserDirectory(CaCongTy),
            new FakeCurrentUser(An));

    private static ConversationRow Dong(
        ConversationKind kind,
        string? name,
        Guid? otherUserId,
        Guid? senderId = null) =>
        new(Guid.NewGuid(), kind, name, otherUserId, 2, "Xong rồi nhé", senderId, Luc, 3);

    [Fact]
    public async Task DanhSach_HoiThoaiRiengLayTenNguoiKia()
    {
        var kq = await DanhSach(Dong(ConversationKind.Rieng, null, Chi, Chi))
            .Handle(new GetConversationsQuery(), default);

        var dong = Assert.Single(kq.Value);
        Assert.Equal("Lê Chi", dong.DisplayName);
        Assert.Equal("Lê Chi", dong.LastMessageSenderName);
        Assert.Equal(3, dong.UnreadCount);
    }

    [Fact]
    public async Task DanhSach_NhomLayTenNhom()
    {
        var kq = await DanhSach(Dong(ConversationKind.Nhom, "Dự án A", null, Binh))
            .Handle(new GetConversationsQuery(), default);

        Assert.Equal("Dự án A", Assert.Single(kq.Value).DisplayName);
    }

    /// <summary>
    /// Người đã biến khỏi danh bạ vẫn phải hiện ra được.
    ///
    /// Tài khoản bị vô hiệu hoá, hoặc chỉ đơn giản là hai nguồn dữ liệu lệch nhau một
    /// nhịp — và nếu handler tra tên bằng <c>Single()</c> hay <c>[key]</c> thì MỘT hàng
    /// hỏng làm nổ cả danh sách, tức là <b>toàn bộ màn Trao đổi trắng xoá</b>. Đúng kiểu
    /// hỏng đã xảy ra thật ở màn Thành viên (xem nhật ký 26/8).
    /// </summary>
    [Fact]
    public async Task NguoiKhongConTrongDanhBa_ThiVanHienDuoc()
    {
        var kq = await DanhSach(Dong(ConversationKind.Rieng, null, Guid.NewGuid()))
            .Handle(new GetConversationsQuery(), default);

        Assert.True(kq.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(Assert.Single(kq.Value).DisplayName));
    }

    // ══════════════════════════════════════════════════════════════════════
    // Đánh dấu đã đọc
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DanhDauDaDoc_ThiDayMocLen()
    {
        var ht = Conversation.MoRieng(Tenant, An, Binh, Luc).Value;
        var bayGio = Luc.AddHours(5);
        var handler = new MarkConversationReadCommandHandler(
            new CoSanHoiThoai(ht),
            new FakeCurrentUser(An),
            new FakeClock(bayGio));

        var kq = await handler.Handle(new MarkConversationReadCommand(ht.Id), default);

        Assert.True(kq.IsSuccess);
        Assert.Equal(bayGio, ht.Participants.Single(p => p.UserId == An).LastReadAtUtc);
    }

    [Fact]
    public async Task NguoiNgoaiDanhDauDaDoc_ThiTuChoi()
    {
        var ht = Conversation.MoRieng(Tenant, An, Binh, Luc).Value;
        var handler = new MarkConversationReadCommandHandler(
            new CoSanHoiThoai(ht),
            new FakeCurrentUser(Chi),
            new FakeClock(Luc));

        Assert.Equal(
            CommErrors.Conversations.NotAParticipant,
            (await handler.Handle(new MarkConversationReadCommand(ht.Id), default)).Error);
    }

    [Fact]
    public async Task DanhDauHoiThoaiKhongCoThat_ThiTuChoi()
    {
        var handler = new MarkConversationReadCommandHandler(
            new FakeConversationRepository(),
            new FakeCurrentUser(An),
            new FakeClock(Luc));

        Assert.Equal(
            CommErrors.Conversations.NotFound,
            (await handler.Handle(new MarkConversationReadCommand(Guid.NewGuid()), default)).Error);
    }
}
