namespace ONoOffice.Api.Middleware;

/// <summary>
/// Gắn mấy header nói cho trình duyệt biết <b>đừng</b> làm gì với phản hồi của API này.
///
/// Ba header, ba sự cố khác nhau:
///
/// <list type="number">
/// <item><b><c>X-Content-Type-Options: nosniff</c></b> — cấm trình duyệt "đoán" kiểu nội
/// dung. Không có nó, một phản hồi JSON chứa dữ liệu do người dùng nhập có thể bị đoán
/// thành HTML và chạy như HTML; đoạn <c>&lt;script&gt;</c> ai đó lưu vào hồ sơ nhân viên
/// sẽ chạy trong trình duyệt người khác.</item>
///
/// <item><b><c>X-Frame-Options: DENY</c></b> — cấm nhúng vào iframe. Chặn clickjacking:
/// kẻ tấn công phủ trang thật dưới một lớp trong suốt, người dùng tưởng đang bấm nút
/// của trang giả nhưng thật ra bấm nút của trang thật, với phiên đăng nhập thật.</item>
///
/// <item><b><c>Referrer-Policy: no-referrer</c></b> — không gửi kèm URL hiện tại khi
/// chuyển sang trang khác. URL của API hay mang mã bản ghi (<c>/api/employees/{id}</c>);
/// rò sang bên thứ ba là rò dữ liệu nội bộ vào log của họ.</item>
/// </list>
///
/// Đây là những thứ mà bật thì mất ba dòng, còn quên thì tới lúc kiểm định an ninh mới
/// biết — và lúc đó phải sửa gấp.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        // Đặt qua OnStarting chứ không gán thẳng: khi có exception lọt lưới,
        // ExceptionHandlingMiddleware gọi Response.Clear() và xoá sạch header đã gán.
        // OnStarting chạy ngay trước byte đầu tiên, tức là SAU lần dọn đó — nên phản hồi
        // lỗi cũng được bảo vệ như phản hồi thành công.
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";

            return Task.CompletedTask;
        });

        return next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
}
