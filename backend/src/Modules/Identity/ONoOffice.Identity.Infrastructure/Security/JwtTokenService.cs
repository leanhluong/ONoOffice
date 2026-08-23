using System.Security.Cryptography;
using System.Text;
using Luong.Kernel.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ONoOffice.Identity.Application.Abstractions;

namespace ONoOffice.Identity.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Ký bằng HS256 nên đây vừa là khoá ký vừa là khoá xác minh. Tối thiểu 32 ký tự.</summary>
    public string SecretKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    /// <summary>
    /// Kiểm ngay lúc khởi động.
    ///
    /// Thà chết ngay với thông báo nói rõ thiếu gì, còn hơn khởi động thành công rồi phát
    /// ra token ký bằng chuỗi rỗng — lúc đó ai cũng tự làm được token hợp lệ.
    /// </summary>
    public void Validate()
    {
        // 32 byte là độ dài đầu ra của SHA-256, tức là kích thước khoá tối thiểu để HS256
        // không bị yếu đi. Khoá ngắn hơn thì thư viện sẽ từ chối, nhưng chặn ở đây cho
        // thông báo dễ hiểu hơn.
        if (SecretKey.Length < 32)
        {
            throw new InvalidOperationException(
                $"Jwt:SecretKey phải dài ít nhất 32 ký tự (hiện tại {SecretKey.Length}).");
        }

        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("Jwt:Issuer và Jwt:Audience là bắt buộc.");
        }

        if (AccessTokenLifetimeMinutes <= 0)
        {
            throw new InvalidOperationException("Jwt:AccessTokenLifetimeMinutes phải lớn hơn 0.");
        }
    }
}

/// <summary>
/// Phát access token (JWT) và refresh token.
///
/// <b>Ký bằng HS256, không phải RS256</b> — xem <c>ADR-0003</c>. ONoOffice là MỘT tiến
/// trình: chính nó phát, chính nó xác minh. RS256 chỉ cần khi có nhiều dịch vụ cùng xác
/// minh token do một dịch vụ phát; lúc đó khoá công khai chia cho mọi người mà không ai
/// giả mạo được. Ở đây thì thêm khoá công khai chỉ thêm việc mà không thêm gì.
/// </summary>
internal sealed class JwtTokenService(IOptions<JwtOptions> options, IDateTimeProvider dateTimeProvider)
    : ITokenService
{
    private readonly JwtOptions _options = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken IssueAccessToken(Guid userId, Guid tenantId, IReadOnlySet<string> permissions)
    {
        var lifetime = TimeSpan.FromMinutes(_options.AccessTokenLifetimeMinutes);
        var now = dateTimeProvider.UtcNow;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = now.Add(lifetime).UtcDateTime,

            Claims = new Dictionary<string, object>
            {
                ["sub"] = userId.ToString(),

                // Workspace nằm TRONG token đã ký. Đây là luật sống còn của multi-tenant:
                // nhận tenant từ header hay body do client gửi nghĩa là ai cũng đổi được
                // một con số rồi đọc dữ liệu công ty khác.
                ["tenant_id"] = tenantId.ToString(),

                // Quyền nhét sẵn vào token nên kiểm quyền không phải tra database.
                // Đánh đổi đã ghi ở ADR-0002: đổi quyền chờ tối đa một vòng đời token
                // mới có hiệu lực.
                ["permission"] = permissions.ToArray(),
            },

            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey)),
                SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(_handler.CreateToken(descriptor), lifetime);
    }

    public RefreshTokenPair IssueRefreshToken()
    {
        // 32 byte ngẫu nhiên từ nguồn dùng cho mật mã. KHÔNG dùng Random hay Guid:
        // Guid.NewGuid không phải là ngẫu nhiên an toàn, và refresh token là thứ đoán
        // được thì chiếm được phiên của người khác.
        byte[] raw = RandomNumberGenerator.GetBytes(32);
        string rawToken = Base64UrlEncoder.Encode(raw);

        // Băm bằng SHA-256, KHÔNG dùng Argon2id — và đây không phải là bất cẩn.
        //
        // Argon2id cố tình chậm để chống dò MẬT KHẨU, vốn là chuỗi ngắn do người nghĩ ra
        // nên đoán được. Refresh token là 32 byte ngẫu nhiên: không gian tìm kiếm lớn tới
        // mức dò là chuyện bất khả thi, nên làm chậm chẳng bảo vệ thêm gì — chỉ khiến MỌI
        // lần gia hạn phiên tốn thêm 100ms.
        //
        // Băm ở đây phục vụ một mục đích khác: lộ bảng database thì kẻ đọc được cũng
        // không dùng được, vì họ chỉ có bản băm.
        return new RefreshTokenPair(rawToken, HashRefreshToken(rawToken));
    }

    /// <summary>
    /// Băm CHUỖI mà client cầm, không phải mảng byte gốc — để lúc gia hạn phiên, chuỗi
    /// client gửi lên băm ra đúng giá trị đang nằm trong bảng.
    ///
    /// Băm hai bên bằng hai cách khác nhau là lỗi im lặng kinh điển: mọi lần gia hạn đều
    /// "không tìm thấy token", và trông y hệt như token đã hết hạn.
    /// </summary>
    public string HashRefreshToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
