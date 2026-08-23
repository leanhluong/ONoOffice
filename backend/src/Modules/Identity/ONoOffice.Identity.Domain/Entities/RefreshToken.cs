using Luong.Kernel.Domain;
using Luong.Kernel.Primitives;

namespace ONoOffice.Identity.Domain.Entities;

/// <summary>
/// Vé gia hạn phiên đăng nhập.
///
/// <b>Vì sao cần nó:</b> access token cố tình chỉ sống 15 phút, vì token đã phát ra thì
/// không thu hồi được — khoá tài khoản lúc 10h00 mà token sống 24 giờ thì người đó vẫn
/// dùng được tới 10h00 hôm sau. Nhưng bắt người dùng đăng nhập lại mỗi 15 phút thì không
/// ai chịu nổi. Refresh token giải quyết: nó sống 30 ngày, đổi lấy access token mới, và
/// KHÁC access token ở chỗ nó <b>có thể thu hồi</b> vì được lưu trong database.
///
/// <b>Lưu chuỗi băm, không lưu token thô.</b> Bảng này chứa chìa khoá vào mọi tài khoản
/// suốt 30 ngày. Lộ bảng mà token nằm dạng thô thì kẻ tấn công đăng nhập được vào tất cả.
/// Cùng lý do không bao giờ lưu mật khẩu thô.
///
/// <b>Không giữ đồng hồ bên trong.</b> Mọi phương thức nhận <c>now</c> từ ngoài — nhờ vậy
/// test kiểm được "hết hạn sau 30 ngày" mà không phải chờ 30 ngày.
/// </summary>
public sealed class RefreshToken : Entity<Guid>, ITenantScoped
{
    private RefreshToken(
        Guid id,
        Guid userId,
        Guid tenantId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc) : base(id)
    {
        UserId = userId;
        TenantId = tenantId;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    /// <summary>Dành cho EF Core.</summary>
    private RefreshToken() => TokenHash = null!;

    public Guid UserId { get; private set; }

    public Guid TenantId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    /// <summary>
    /// Token nào đã thay thế cái này khi xoay vòng. Có nó thì lần ngược được cả chuỗi —
    /// cần khi phát hiện trộm và phải thu hồi toàn bộ chuỗi.
    /// </summary>
    public Guid? ReplacedByTokenId { get; private set; }

    public static Result<RefreshToken> Create(
        Guid userId,
        Guid tenantId,
        string? tokenHash,
        DateTimeOffset createdAtUtc,
        TimeSpan lifetime)
    {
        if (userId == Guid.Empty || tenantId == Guid.Empty)
        {
            return IdentityErrors.RefreshTokens.OwnerRequired;
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return IdentityErrors.RefreshTokens.HashRequired;
        }

        if (lifetime <= TimeSpan.Zero)
        {
            // Thời hạn bằng 0 hoặc âm nghĩa là token chết ngay khi sinh ra. Chặn ở đây
            // vì lỗi kiểu này thường do cấu hình thiếu, và nếu để lọt thì biểu hiện là
            // "ai đăng nhập cũng bị đá ra ngay" — rất tốn thời gian mới lần ra nguyên nhân.
            return IdentityErrors.RefreshTokens.InvalidLifetime;
        }

        return new RefreshToken(Guid.NewGuid(), userId, tenantId, tokenHash, createdAtUtc, createdAtUtc + lifetime);
    }

    public bool IsActiveAt(DateTimeOffset now) => RevokedAtUtc is null && now < ExpiresAtUtc;

    /// <summary>
    /// Thu hồi. Token đã HẾT HẠN vẫn thu hồi được — đây là hành động bảo mật, khi nghi ngờ
    /// bị lộ thì người ta thu hồi tất cả, và bắt phải lọc ra "cái nào còn hạn" chỉ tạo
    /// thêm một chỗ để sót.
    /// </summary>
    public Result Revoke(DateTimeOffset now)
    {
        if (RevokedAtUtc is not null)
        {
            return IdentityErrors.RefreshTokens.AlreadyRevoked;
        }

        RevokedAtUtc = now;

        return Result.Success();
    }

    /// <summary>
    /// Xoay vòng: thu hồi vé này và ghi lại vé đã thay thế nó.
    ///
    /// <b>Mỗi refresh token chỉ dùng được ĐÚNG MỘT LẦN.</b> Nếu một token đã xoay rồi lại
    /// được dùng tiếp, chỉ có một cách giải thích: <b>hai bên đang cùng giữ nó</b> — tức
    /// là đã bị đánh cắp.
    ///
    /// Tầng Application bắt được thất bại này thì phải thu hồi <b>toàn bộ chuỗi token</b>
    /// của người đó (lần theo <see cref="ReplacedByTokenId"/>), không phải chỉ mỗi cái vừa
    /// bị dùng lại. Không làm vậy thì kẻ trộm cứ thế gia hạn phiên mãi mãi, còn người dùng
    /// thật không hề hay biết.
    /// </summary>
    public Result RotateTo(Guid newTokenId, DateTimeOffset now)
    {
        if (newTokenId == Id || newTokenId == Guid.Empty)
        {
            return IdentityErrors.RefreshTokens.InvalidReplacement;
        }

        if (!IsActiveAt(now))
        {
            // Gộp hai ca "đã thu hồi" và "đã hết hạn" vào một thông báo là CỐ Ý.
            // Nói rõ cho người gọi biết token của họ hỏng vì lý do gì là giúp kẻ tấn công
            // dò ra token nào từng tồn tại. Chi tiết thật nằm ở log phía server.
            return IdentityErrors.RefreshTokens.NotActive;
        }

        RevokedAtUtc = now;
        ReplacedByTokenId = newTokenId;

        return Result.Success();
    }
}
