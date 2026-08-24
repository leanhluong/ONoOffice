using Luong.Kernel.AspNetCore.Middleware;

namespace ONoOffice.Api.Extensions;

/// <summary>Danh sách origin được phép gọi API, đọc từ cấu hình.</summary>
public sealed class SpaCorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = [];
}

public static class CorsSetup
{
    public const string PolicyName = "onooffice-spa";

    /// <summary>
    /// CORS không phải là thứ bảo vệ máy chủ — bất kỳ ai cũng gọi được API bằng curl.
    /// Nó bảo vệ <b>người dùng</b>: nó quyết định trang web nào được phép dùng trình
    /// duyệt CỦA HỌ để gọi API này và đọc kết quả.
    ///
    /// Vì vậy danh sách origin phải nêu đích danh, không dùng <c>AllowAnyOrigin</c>.
    /// </summary>
    public static IServiceCollection AddConfiguredCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(SpaCorsOptions.SectionName).Get<SpaCorsOptions>()
            ?? new SpaCorsOptions();

        if (options.AllowedOrigins.Length == 0)
        {
            // Chết ngay lúc khởi động. Để trống thì API vẫn lên và Postman vẫn gọi ngon
            // lành — chỉ có trình duyệt là im lặng từ chối mọi phản hồi. Người phát hiện
            // sẽ là người dùng đầu tiên, và họ chỉ mô tả được là "trang trắng".
            throw new InvalidOperationException(
                $"Thiếu '{SpaCorsOptions.SectionName}:AllowedOrigins'. Phải nêu đích danh origin của frontend.");
        }

        services.AddCors(cors => cors.AddPolicy(PolicyName, policy => policy
            .WithOrigins(options.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()

            // Mặc định trình duyệt chỉ cho JavaScript đọc vài header cơ bản của phản hồi.
            // Không khai ở đây thì frontend không đọc được mã lần vết, và người dùng
            // không có gì để đọc cho bộ phận hỗ trợ khi gặp lỗi.
            .WithExposedHeaders(CorrelationIdMiddleware.HeaderName)));

        // CỐ Ý không gọi AllowCredentials(). Hệ này không dùng cookie — token đi trong
        // thân phản hồi và do frontend tự gắn vào header (xem ADR-0004). Bật
        // AllowCredentials khi không cần tới là mở rộng bề mặt tấn công cho không: nó
        // cho phép trình duyệt tự động đính kèm cookie vào request xuyên origin.
        return services;
    }
}
