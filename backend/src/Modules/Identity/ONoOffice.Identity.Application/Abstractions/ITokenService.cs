namespace ONoOffice.Identity.Application.Abstractions;

public sealed record AccessToken(string Value, TimeSpan Lifetime);

/// <summary>
/// Cặp chuỗi của một refresh token: <paramref name="Raw"/> gửi cho client,
/// <paramref name="Hash"/> lưu database.
///
/// Tách làm hai để không có chỗ nào lỡ tay lưu nhầm chuỗi thô. Server KHÔNG BAO GIỜ
/// giữ lại <paramref name="Raw"/> — nó rời khỏi tiến trình ngay trong phản hồi HTTP.
/// </summary>
public sealed record RefreshTokenPair(string Raw, string Hash);

public interface ITokenService
{
    AccessToken IssueAccessToken(Guid userId, Guid tenantId, IReadOnlySet<string> permissions);

    RefreshTokenPair IssueRefreshToken();

    /// <summary>Băm chuỗi thô client gửi lên, để tra trong bảng. Cùng thuật toán với lúc phát.</summary>
    string HashRefreshToken(string rawToken);
}
