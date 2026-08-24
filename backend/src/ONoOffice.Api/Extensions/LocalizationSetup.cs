using System.Globalization;
using System.Resources;
using Luong.Kernel.AspNetCore.Localization;
using Luong.Kernel.Localization;
using Microsoft.AspNetCore.Localization;

namespace ONoOffice.Api.Extensions;

public static class LocalizationSetup
{
    /// <summary>Ngôn ngữ gốc của sản phẩm. Không có bản dịch thì lùi về đây.</summary>
    public const string NgonNguMacDinh = "vi";

    private static readonly string[] NgonNguHoTro = [NgonNguMacDinh, "en"];

    /// <summary>
    /// Tên gốc của bộ tài nguyên. Phải khớp với đường dẫn file <c>.resx</c> tính từ
    /// namespace gốc: <c>Resources/Messages.{ngôn-ngữ}.resx</c>.
    /// </summary>
    private const string ResourceBaseName = "ONoOffice.Api.Resources.Messages";

    /// <summary>
    /// Một mã lỗi chắc chắn có bản dịch, dùng để thử bộ tài nguyên lúc khởi động.
    /// </summary>
    private const string MaLoiDeThu = "Auth.InvalidCredentials";

    public static IServiceCollection AddMessageCatalog(this IServiceCollection services)
    {
        services.AddSingleton(new ResourceManager(ResourceBaseName, typeof(LocalizationSetup).Assembly));

        // Đăng ký kiểu cụ thể rồi mới bắc cầu sang cổng: nhờ vậy lúc khởi động ta lấy
        // được đúng bản cài đặt để gọi AssertUsable, mà phần còn lại của hệ thống vẫn
        // chỉ nhìn thấy cổng IMessageCatalog.
        services.AddSingleton<ResxMessageCatalog>();
        services.AddSingleton<IMessageCatalog>(provider => provider.GetRequiredService<ResxMessageCatalog>());

        return services;
    }

    public static IServiceCollection AddConfiguredLocalization(this IServiceCollection services)
    {
        var cultures = NgonNguHoTro.Select(ten => new CultureInfo(ten)).ToArray();

        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new RequestCulture(NgonNguMacDinh);
            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;

            // Ngôn ngữ không nằm trong danh sách thì lùi về mặc định, không nổ. Người
            // dùng trình duyệt tiếng Nhật vẫn phải đọc được thông báo lỗi — bằng tiếng
            // Việt còn hơn không có gì.
            options.FallBackToParentCultures = true;
            options.FallBackToParentUICultures = true;
        });

        return services;
    }

    /// <summary>
    /// Thử đọc một khoá đã biết chắc là có, ngay lúc khởi động.
    ///
    /// Bản dịch nằm trong <b>satellite assembly</b> (<c>vi/ONoOffice.Api.resources.dll</c>),
    /// tức là những file RỜI đi kèm bản build. Chúng lạc mất lúc đóng gói là chuyện có
    /// thật, và khi đó hệ thống vẫn chạy hoàn toàn bình thường — chỉ là mọi thông báo
    /// đều rơi về câu tiếng Việt viết cứng trong code. Không ai phát hiện ra, cho tới
    /// khi có một khách hàng dùng tiếng Anh phàn nàn.
    /// </summary>
    public static void KiemTraBanDich(this IServiceProvider services)
    {
        services.GetRequiredService<ResxMessageCatalog>()
            .AssertUsable(MaLoiDeThu, new CultureInfo(NgonNguMacDinh));
    }
}
