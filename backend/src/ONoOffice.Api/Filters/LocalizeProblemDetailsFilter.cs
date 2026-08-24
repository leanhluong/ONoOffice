using Luong.Kernel.AspNetCore.Errors;
using Luong.Kernel.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ONoOffice.Api.Filters;

/// <summary>
/// Dịch mọi thông báo lỗi sang ngôn ngữ người gọi yêu cầu — <b>đúng một chỗ duy nhất</b>.
///
/// <b>Vì sao không dịch ngay trong <c>ToActionResult()</c>:</b> hàm đó nằm ở
/// <c>Luong.Kernel.AspNetCore</c>, một thư viện dùng chung. Nó không được biết ONoOffice
/// có file <c>.resx</c> nào, và cũng không được ép mọi sản phẩm dùng nó phải có bản dịch.
/// Nên kernel chỉ CUNG CẤP phép dịch (<c>ProblemDetails.Localize</c>), còn quyết định
/// gọi hay không là của từng sản phẩm.
///
/// <b>Vì sao không dịch trong từng controller:</b> vì rồi sẽ có endpoint quên gọi. Và
/// endpoint quên gọi thì vẫn chạy đúng, chỉ là luôn trả tiếng Việt — không ai phát hiện
/// ra cho tới khi có khách hàng dùng tiếng Anh.
///
/// <b>Vì sao là <see cref="IAlwaysRunResultFilter"/> chứ không phải
/// <c>IResultFilter</c>:</b> lỗi kiểm dữ liệu đầu vào bị chặn SỚM, ở tầng lọc, và cắt
/// ngang chuỗi filter thông thường. <c>IResultFilter</c> sẽ không chạy cho những ca đó —
/// mà đó lại là những lỗi người dùng gặp nhiều nhất.
/// </summary>
internal sealed class LocalizeProblemDetailsFilter(IMessageCatalog catalog) : IAlwaysRunResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        // Chỉ đụng vào phản hồi lỗi. Phản hồi thành công không có gì để dịch.
        if (context.Result is ObjectResult { Value: ProblemDetails problem })
        {
            // Không truyền culture: Localize tự đọc CultureInfo.CurrentUICulture, mà
            // UseRequestLocalization đã đặt sẵn theo header Accept-Language của request này.
            problem.Localize(catalog);
        }
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
        // Không có gì để làm sau khi phản hồi đã gửi đi.
    }
}
