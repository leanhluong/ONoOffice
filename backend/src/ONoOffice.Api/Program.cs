using Luong.Kernel.Abstractions;
using Luong.Kernel.Application;
using Luong.Kernel.AspNetCore.Middleware;
using Luong.Kernel.AspNetCore.Security;
using ONoOffice.Api.Extensions;
using ONoOffice.Api.Middleware;
using ONoOffice.Identity.Application.Authentication.Login;
using ONoOffice.Identity.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ══════════════════════════════════════════════════════════════════════════════
//  ĐĂNG KÝ DỊCH VỤ
// ══════════════════════════════════════════════════════════════════════════════

builder.Services
    .AddApiControllers()
    .AddMessageCatalog()
    .AddConfiguredLocalization()
    .AddConfiguredCors(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddPermissionAuthorization();

// ICurrentUser và ICurrentTenant do CÙNG một lớp cài — cùng đọc từ một ClaimsPrincipal.
// Đăng ký lớp cụ thể một lần rồi bắc cầu hai cổng vào nó, để hai cổng trong cùng một
// request không bao giờ nhìn thấy hai bản khác nhau.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<HttpContextCurrentUser>();
builder.Services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<HttpContextCurrentUser>());
builder.Services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<HttpContextCurrentUser>());

// Không giữ trạng thái gì, và job nền cũng cần -> Singleton.
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

// MediatR + 3 behavior (log → kiểm dữ liệu → transaction), quét handler trong assembly
// Application của từng module. Org chưa có handler nào nên chưa nêu ở đây.
builder.Services.AddApplicationLayer(typeof(LoginCommand).Assembly);

builder.Services.AddIdentityModule(builder.Configuration);

var app = builder.Build();

// Thử bộ tài nguyên dịch NGAY, trước khi nhận request đầu tiên. Thiếu satellite assembly
// là lỗi đóng gói im lặng: hệ thống vẫn chạy đúng, chỉ là không còn dịch được gì.
app.Services.KiemTraBanDich();

// ══════════════════════════════════════════════════════════════════════════════
//  ĐƯỜNG ĐI CỦA MỘT REQUEST
//
//  Thứ tự dưới đây KHÔNG phải sở thích. Mỗi dòng nằm ở đúng chỗ của nó vì một lý
//  do, và đảo đi thì hỏng theo một kiểu riêng — chú thích từng dòng.
// ══════════════════════════════════════════════════════════════════════════════

// ① Sớm nhất có thể: mọi dòng log sinh ra sau đây đều mang mã lần vết. Đặt muộn hơn
//    thì đúng những log của mấy tầng ngoài cùng lại là log không lần vết được.
app.UseCorrelationId();

// ② Bọc ngoài mọi thứ còn lại. Exception lọt tới đây thành 500 rỗng ruột + một dòng
//    log đầy đủ. Đặt vào trong thì exception của các middleware ngoài nó không ai bắt,
//    và người dùng nhận một trang lỗi mặc định của máy chủ, kèm stack trace.
app.UseProblemDetailsExceptionHandler();

// ③ Header an toàn. Đặt trong ② nhưng gắn header qua OnStarting, nên phản hồi lỗi do
//    ② dựng ra cũng được bảo vệ.
app.UseSecurityHeaders();

// ④ Đặt CultureInfo cho request theo Accept-Language. Phải TRƯỚC MVC, vì bộ lọc dịch
//    thông báo lỗi đọc CultureInfo.CurrentUICulture lúc dựng phản hồi.
app.UseRequestLocalization();

// ⑤ ⭐ TRƯỚC ⑦, và đây là lý do THẬT — đã kiểm chứng bằng test, không phải nghe kể.
//
//    Lời giải thích quen thuộc là "preflight OPTIONS không mang token nên sẽ bị 401".
//    Ở cấu hình này điều đó KHÔNG xảy ra: OPTIONS không khớp endpoint nào (định tuyến
//    theo thuộc tính chỉ map GET/POST), nên ⑦ không có policy nào để áp và cứ thế cho qua.
//
//    Chuyện thật sự hỏng là ở request BÌNH THƯỜNG bị từ chối: ⑦ cắt ngang và trả 401
//    ngay tại chỗ nó đứng. Nếu ⑤ nằm sau ⑦ thì middleware CORS không bao giờ chạy cho
//    phản hồi đó, và 401 đi ra KHÔNG có Access-Control-Allow-Origin. Trình duyệt vì thế
//    không cho JavaScript đọc phản hồi — kể cả mã trạng thái. Frontend không phân biệt
//    được "phiên hết hạn" với "máy chủ hỏng", nên không biết phải đưa người dùng về màn
//    đăng nhập; console thì hiện lỗi CORS, và người ta đi sửa CORS trong khi chuyện thật
//    chỉ là token hết hạn.
//
//    Đặt trước thì middleware CORS gắn header qua OnStarting, nên header dính vào cả
//    những phản hồi do tầng dưới cắt ngang.
app.UseCors(CorsSetup.PolicyName);

// ⑥ "Anh là ai" — đọc và xác minh token, dựng ClaimsPrincipal.
app.UseAuthentication();

// ⑦ "Anh được làm gì" — bắt buộc SAU ⑥. Đảo lại thì lúc kiểm quyền chưa có ai để kiểm,
//    và mọi endpoint có [Authorize] đều trả 401 kể cả khi token hoàn toàn hợp lệ.
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// Mở <c>Program</c> ra cho assembly test nhìn thấy.
///
/// Chương trình viết bằng câu lệnh cấp cao nhất vẫn sinh ra một lớp <c>Program</c>, nhưng
/// nó là <c>internal</c> — mà <c>WebApplicationFactory&lt;T&gt;</c> đòi một kiểu công khai
/// để biết phải khởi động cái gì. Một dòng này là toàn bộ cái giá phải trả để test dựng
/// được đúng máy chủ thật thay vì một bản dựng lại gần giống.
/// </summary>
public partial class Program;
