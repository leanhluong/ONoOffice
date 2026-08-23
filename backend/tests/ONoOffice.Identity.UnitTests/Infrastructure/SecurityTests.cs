using Luong.Kernel.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using ONoOffice.Identity.Infrastructure.Security;

namespace ONoOffice.Identity.UnitTests.Infrastructure;

public class Argon2PasswordHasherTests
{
    private readonly Argon2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_RoiVerify_ThiKhop()
    {
        string hash = _hasher.Hash("MatKhauCuaToi123!");

        Assert.True(_hasher.Verify("MatKhauCuaToi123!", hash));
    }

    [Fact]
    public void Verify_TuChoiMatKhauSai()
    {
        string hash = _hasher.Hash("MatKhauCuaToi123!");

        Assert.False(_hasher.Verify("MatKhauKhac123!", hash));
    }

    // Cùng một mật khẩu băm hai lần phải ra HAI chuỗi khác nhau — vì mỗi lần dùng một
    // muối ngẫu nhiên riêng. Ra giống nhau nghĩa là không có muối, và lúc đó kẻ tấn công
    // dựng sẵn bảng tra ngược là phá được hàng loạt tài khoản cùng lúc.
    [Fact]
    public void BamHaiLan_RaHaiChuoiKhacNhau()
    {
        string a = _hasher.Hash("MatKhauCuaToi123!");
        string b = _hasher.Hash("MatKhauCuaToi123!");

        Assert.NotEqual(a, b);
        Assert.True(_hasher.Verify("MatKhauCuaToi123!", a));
        Assert.True(_hasher.Verify("MatKhauCuaToi123!", b));
    }

    // Chuỗi băm hỏng (dữ liệu cũ, bị sửa tay) thì coi như sai mật khẩu, KHÔNG để
    // exception bay lên thành lỗi 500 — 500 sẽ tố giác rằng tài khoản này có gì bất thường.
    [Fact]
    public void Verify_ChuoiBamHong_ThiTraFalseChuKhongNem()
    {
        Assert.False(_hasher.Verify("bat-ky", "day-khong-phai-chuoi-bam"));
    }
}

public class JwtTokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 23, 10, 0, 0, TimeSpan.Zero);

    private sealed class FrozenClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }

    private static JwtTokenService CreateService() =>
        new(
            Options.Create(new JwtOptions
            {
                SecretKey = "day-la-khoa-bi-mat-du-dai-cho-hs256-nhe",
                Issuer = "onooffice",
                Audience = "onooffice-web",
                AccessTokenLifetimeMinutes = 15,
            }),
            new FrozenClock());

    [Fact]
    public void AccessToken_MangDuSubTenantVaPermission()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var token = CreateService().IssueAccessToken(
            userId, tenantId, new HashSet<string> { "employee.read", "employee.write" });

        var parsed = new JsonWebTokenHandler().ReadJsonWebToken(token.Value);

        Assert.Equal(userId.ToString(), parsed.GetClaim("sub").Value);
        Assert.Equal(tenantId.ToString(), parsed.GetClaim("tenant_id").Value);
        Assert.Equal(2, parsed.Claims.Count(c => c.Type == "permission"));
    }

    [Fact]
    public void AccessToken_HetHanDungTheoCauHinh()
    {
        var token = CreateService().IssueAccessToken(Guid.NewGuid(), Guid.NewGuid(), new HashSet<string>());

        Assert.Equal(TimeSpan.FromMinutes(15), token.Lifetime);

        var parsed = new JsonWebTokenHandler().ReadJsonWebToken(token.Value);
        Assert.Equal(Now.AddMinutes(15).UtcDateTime, parsed.ValidTo, TimeSpan.FromSeconds(1));
    }

    // ⭐ Chuỗi thô gửi cho client PHẢI khác chuỗi băm lưu database.
    [Fact]
    public void RefreshToken_ChuoiThoKhacChuoiBam()
    {
        var pair = CreateService().IssueRefreshToken();

        Assert.NotEqual(pair.Raw, pair.Hash);
        Assert.NotEmpty(pair.Raw);
        Assert.Equal(64, pair.Hash.Length);   // SHA-256 dạng hex
    }

    // Hai lần phát phải ra hai token khác nhau — nếu trùng thì hai người dùng chung phiên.
    [Fact]
    public void RefreshToken_MoiLanPhatMotChuoiKhacNhau()
    {
        var service = CreateService();

        Assert.NotEqual(service.IssueRefreshToken().Raw, service.IssueRefreshToken().Raw);
    }

    [Theory]
    [InlineData("qua-ngan")]
    [InlineData("")]
    public void JwtOptions_TuChoiKhoaQuaNgan(string secret)
    {
        var options = new JwtOptions { SecretKey = secret, Issuer = "a", Audience = "b" };

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void JwtOptions_TuChoiThieuIssuerHoacAudience()
    {
        var options = new JwtOptions
        {
            SecretKey = "day-la-khoa-bi-mat-du-dai-cho-hs256-nhe",
            Issuer = "",
            Audience = "b",
        };

        Assert.Throws<InvalidOperationException>(options.Validate);
    }
}
