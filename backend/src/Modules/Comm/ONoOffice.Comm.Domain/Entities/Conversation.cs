using Luong.Kernel.Domain;
using Luong.Kernel.Primitives;

namespace ONoOffice.Comm.Domain.Entities;

/// <summary>
/// Một cuộc trò chuyện, và danh sách người ở trong nó.
///
/// ═══════════════════════════════════════════════════════════════════════════
///  VÌ SAO NGƯỜI THAM GIA NẰM TRONG GỐC NÀY MÀ TIN NHẮN THÌ KHÔNG
/// ═══════════════════════════════════════════════════════════════════════════
///
/// Ranh giới của gốc tổng hợp là ranh giới của thứ phải đúng CÙNG NHAU trong một giao
/// dịch. Danh sách người tham gia đúng là như thế: "hội thoại riêng có đúng hai người" là
/// một luật, và luật đó chỉ kiểm được khi cả danh sách nằm trong tay. Danh sách này cũng
/// nhỏ — một nhóm vài chục người là cùng.
///
/// Tin nhắn thì ngược lại hoàn toàn: hàng chục nghìn dòng, và <b>không có luật nào bắt
/// chúng phải đúng cùng nhau</b> — không có "tối đa N tin", không có tổng nào phải khớp.
/// Ôm chúng vào đây nghĩa là gửi thêm một câu "ok" phải nạp toàn bộ lịch sử lên bộ nhớ.
/// Xem <see cref="Message"/>.
/// </summary>
public sealed class Conversation : AggregateRoot<Guid>, ITenantScoped, IAuditable
{
    private const int MaxNameLength = 120;

    private readonly List<Participant> _participants = [];

    private Conversation(
        Guid id,
        Guid tenantId,
        ConversationKind kind,
        string? name,
        string? pairKey,
        Guid createdByUserId) : base(id)
    {
        TenantId = tenantId;
        Kind = kind;
        Name = name;
        PairKey = pairKey;
        CreatedByUserId = createdByUserId;
    }

    /// <summary>Dành cho EF Core.</summary>
    private Conversation()
    {
    }

    public Guid TenantId { get; private set; }

    public ConversationKind Kind { get; private set; }

    /// <summary>
    /// Tên nhóm. Hội thoại riêng luôn <c>null</c>.
    ///
    /// Tên của một hội thoại riêng là tên NGƯỜI KIA, mà người kia thì khác nhau tuỳ ai
    /// đang nhìn. Lưu một cái tên cố định ở đây là lưu sẵn một câu sai cho một trong hai
    /// người — và nó sẽ còn sai thêm mỗi lần ai đó đổi tên hiển thị.
    /// </summary>
    public string? Name { get; private set; }

    /// <summary>
    /// Khoá cặp của hội thoại riêng: hai mã người, <b>sắp xếp rồi ghép</b>. Nhóm luôn
    /// <c>null</c>.
    ///
    /// Đây là chỗ chặn ca đua: An bấm "nhắn Bình" đúng lúc Bình bấm "nhắn An", cả hai
    /// truy vấn đều thấy "chưa có hội thoại nào", cả hai đều tạo. Kết quả là hai hội thoại
    /// song song, mỗi người nói vào một cái, <b>không ai thấy tin của ai — và không có lỗi
    /// nào cả</b>. Kiểm trước khi ghi không cứu được: giữa lúc kiểm và lúc ghi vẫn có khe.
    ///
    /// Chỉ ràng buộc UNIQUE trên cột này mới thật sự đóng khe đó. Và nó chỉ đóng được nếu
    /// khoá KHÔNG phụ thuộc thứ tự — nên phải sắp xếp.
    ///
    /// Nhóm để <c>null</c>, không phải vì lười: một công ty có thể có mười nhóm cùng tên
    /// "Dự án A" và cả mười đều hợp lệ. Trong Postgres, <c>NULL</c> không đụng ràng buộc
    /// UNIQUE, nên một cột duy nhất phục vụ được cả hai kiểu.
    /// </summary>
    public string? PairKey { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public IReadOnlyList<Participant> Participants => _participants.AsReadOnly();

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    // ══════════════════════════════════════════════════════════════════════
    // Mở hội thoại
    // ══════════════════════════════════════════════════════════════════════

    public static Result<Conversation> MoRieng(
        Guid tenantId,
        Guid nguoiMo,
        Guid nguoiKia,
        DateTimeOffset luc)
    {
        if (tenantId == Guid.Empty)
        {
            return CommErrors.Conversations.TenantRequired;
        }

        if (nguoiMo == nguoiKia)
        {
            return CommErrors.Conversations.CannotChatWithSelf;
        }

        var ht = new Conversation(
            Guid.NewGuid(),
            tenantId,
            ConversationKind.Rieng,
            name: null,
            pairKey: TaoKhoaCap(nguoiMo, nguoiKia),
            createdByUserId: nguoiMo);

        ht._participants.Add(Participant.Them(nguoiMo, luc));
        ht._participants.Add(Participant.Them(nguoiKia, luc));

        return ht;
    }

    public static Result<Conversation> MoNhom(
        Guid tenantId,
        string? ten,
        Guid nguoiTao,
        IEnumerable<Guid> nhungNguoiKhac,
        DateTimeOffset luc)
    {
        if (tenantId == Guid.Empty)
        {
            return CommErrors.Conversations.TenantRequired;
        }

        var daKiem = KiemTen(ten);

        if (daKiem.IsFailure)
        {
            return daKiem.Error;
        }

        var ht = new Conversation(
            Guid.NewGuid(),
            tenantId,
            ConversationKind.Nhom,
            daKiem.Value,
            pairKey: null,
            createdByUserId: nguoiTao);

        // Người tạo vào trước, rồi mới tới danh sách mời — và `Distinct` chịu trách nhiệm
        // cho ca người tạo tự mời chính mình. Thiếu bước này thì họ mở một nhóm rồi không
        // vào được chính nó, và nhóm đó không ai xoá được vì không ai ở trong.
        foreach (var ai in nhungNguoiKhac.Prepend(nguoiTao).Distinct())
        {
            ht._participants.Add(Participant.Them(ai, luc));
        }

        return ht;
    }

    /// <summary>
    /// Hai mã người, sắp xếp rồi ghép — nên An↔Bình và Bình↔An ra cùng một chuỗi.
    ///
    /// So sánh theo chuỗi <c>"D"</c> chứ không theo <c>Guid.CompareTo</c>: thứ tự byte của
    /// <c>Guid</c> trong .NET không giống thứ tự chữ mà database nhìn thấy, và khoá này
    /// phải khớp với thứ database đang giữ UNIQUE, không phải với thứ .NET nghĩ.
    /// </summary>
    private static string TaoKhoaCap(Guid a, Guid b)
    {
        string x = a.ToString("D");
        string y = b.ToString("D");

        return string.CompareOrdinal(x, y) <= 0 ? $"{x}:{y}" : $"{y}:{x}";
    }

    // ══════════════════════════════════════════════════════════════════════
    // Người ra người vào
    // ══════════════════════════════════════════════════════════════════════

    public bool CoThanhVien(Guid userId) => _participants.Any(p => p.UserId == userId);

    /// <summary>
    /// Thêm một người vào NHÓM. Hội thoại riêng thì không.
    ///
    /// <paramref name="luc"/> thành mốc tầm nhìn của riêng người này: họ chỉ thấy tin từ
    /// giây phút đó trở đi.
    /// </summary>
    public Result ThemNguoi(Guid userId, DateTimeOffset luc)
    {
        if (Kind == ConversationKind.Rieng)
        {
            return CommErrors.Conversations.DirectIsFixed;
        }

        if (CoThanhVien(userId))
        {
            return CommErrors.Conversations.AlreadyIn;
        }

        _participants.Add(Participant.Them(userId, luc));

        return Result.Success();
    }

    /// <summary>
    /// Rời một nhóm.
    ///
    /// Không chặn ca "người cuối cùng rời đi": một nhóm rỗng là chuyện bình thường, nó chỉ
    /// đơn giản không hiện trong danh sách của ai nữa. Bắt phải có người ở lại nghĩa là có
    /// người bị kẹt trong một nhóm họ không muốn ở.
    /// </summary>
    public Result RoiDi(Guid userId)
    {
        if (Kind == ConversationKind.Rieng)
        {
            return CommErrors.Conversations.DirectIsFixed;
        }

        var ai = _participants.SingleOrDefault(p => p.UserId == userId);

        if (ai is null)
        {
            return CommErrors.Conversations.NotAParticipant;
        }

        _participants.Remove(ai);

        return Result.Success();
    }

    // ══════════════════════════════════════════════════════════════════════
    // Đã đọc tới đâu
    // ══════════════════════════════════════════════════════════════════════

    public Result DanhDauDaDoc(Guid userId, DateTimeOffset luc)
    {
        var ai = _participants.SingleOrDefault(p => p.UserId == userId);

        if (ai is null)
        {
            return CommErrors.Conversations.NotAParticipant;
        }

        ai.DaDocToi(luc);

        return Result.Success();
    }

    private static Result<string> KiemTen(string? ten)
    {
        if (string.IsNullOrWhiteSpace(ten))
        {
            return CommErrors.Conversations.NameEmpty;
        }

        string trimmed = ten.Trim();

        return trimmed.Length > MaxNameLength
            ? CommErrors.Conversations.NameTooLong
            : trimmed;
    }
}
