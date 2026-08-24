using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using ONoOffice.Api.Authorization;
using ONoOffice.Identity.Infrastructure.Security;

namespace ONoOffice.Api.Extensions;

public static class AuthenticationSetup
{
    /// <summary>
    /// Cấu hình bên XÁC MINH token. Bên PHÁT token là <c>JwtTokenService</c> ở
    /// <c>Identity.Infrastructure</c> — hai bên phải khớp nhau từng chi tiết, và đó là
    /// lý do cả hai cùng đọc một khối cấu hình <see cref="JwtOptions"/>.
    ///
    /// Lệch một trường thôi — issuer chẳng hạn — thì triệu chứng là "đăng nhập thành
    /// công nhưng gọi API nào cũng 401", một trong những lỗi tốn thời gian nhất vì
    /// nhìn đâu cũng thấy đúng.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException($"Thiếu khối cấu hình '{JwtOptions.SectionName}'.");

        // Chết ngay lúc khởi động nếu cấu hình thiếu hoặc khoá quá ngắn. Để lọt thì API
        // vẫn lên, và chỉ hỏng ở lần đầu có người đăng nhập — muộn hơn nhiều so với chỗ
        // đáng hỏng, mà lúc đó đã có người dùng thật đang nhìn màn hình.
        jwt.Validate();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Không giữ token trong AuthenticationProperties: ta không cần đọc lại
                // chuỗi thô ở đâu cả, mà giữ thì nó lọt vào log chẩn đoán.
                options.SaveToken = false;

                // Chỉ bật ở môi trường phát triển: thông báo lỗi chi tiết của thư viện
                // token rất hữu ích khi dò, nhưng nó nói cho người gọi biết token của họ
                // sai ở CHÍNH XÁC chỗ nào — tức là chỉ đường cho người đang thử.
                options.IncludeErrorDetails = false;

                // Giữ nguyên tên claim như lúc phát.
                //
                // Mặc định .NET "dịch" tên claim sang mấy URI dài của WS-Federation:
                // "sub" thành "http://schemas.xmlsoap.org/.../nameidentifier". Khi đó
                // code đi tìm claim "sub" sẽ không thấy gì, mà token thì rõ ràng có "sub".
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),

                    ValidateLifetime = true,

                    // Mặc định của thư viện là 5 PHÚT khoan dung. Nghĩa là access token
                    // đời 15 phút thật ra sống 20 phút — dài hơn một phần ba so với con
                    // số đã ghi trong ADR. Khoan dung đó sinh ra cho những hệ mà máy phát
                    // và máy xác minh là hai máy khác nhau, lệch giờ nhau. Ở đây chỉ có
                    // MỘT tiến trình, dùng chung một đồng hồ, nên không có gì để khoan dung.
                    ClockSkew = TimeSpan.Zero,

                    // Tên claim chứa mã người dùng, để User.Identity.Name và các tiện ích
                    // của ASP.NET trỏ đúng chỗ thay vì rỗng.
                    NameClaimType = "sub",
                };
            });

        return services;
    }

    /// <summary>
    /// Bật phân quyền theo <c>permission</c>, với policy sinh lúc chạy.
    /// </summary>
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization();

        // Cả hai đều không giữ trạng thái theo request -> Singleton.
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
