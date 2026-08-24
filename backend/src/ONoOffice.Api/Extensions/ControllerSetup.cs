using Luong.Kernel.AspNetCore.Errors;
using Luong.Kernel.Primitives;
using Microsoft.AspNetCore.Mvc;
using ONoOffice.Api.Filters;

namespace ONoOffice.Api.Extensions;

public static class ControllerSetup
{
    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<LocalizeProblemDetailsFilter>();

            // ⭐ Tắt suy diễn [Required] từ kiểu tham chiếu không-null.
            //
            // Mặc định, một record `LoginCommand(string Email, string Password)` khiến
            // MVC tự coi cả hai là bắt buộc và tự trả 400 NGAY ở khâu ràng buộc dữ liệu —
            // trước khi FluentValidation kịp chạy. Hậu quả là hệ thống có HAI bộ kiểm dữ
            // liệu, mỗi bộ nói một câu khác nhau, và bộ nào chạy trước thì tuỳ vào việc
            // trường đó có phải kiểu tham chiếu hay không.
            //
            // Ở đây chỉ có MỘT nơi định nghĩa luật đầu vào: validator cạnh command.
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = TraLoiRangBuocHong;
        });

        return services;
    }

    /// <summary>
    /// Ca duy nhất còn lại mà MVC tự từ chối: dữ liệu <b>không đọc nổi</b> — JSON vỡ,
    /// hoặc gửi chuỗi vào một trường số. FluentValidation không bao giờ thấy những ca
    /// này, vì object còn chưa dựng được.
    ///
    /// Nếu để mặc định, MVC trả một khuôn riêng: <c>errors</c> là một TỪ ĐIỂN theo tên
    /// trường, khác hẳn mảng <c>errors[]</c> của mọi lỗi khác trong hệ. Frontend sẽ phải
    /// viết hai nhánh xử lý lỗi — và nhánh thứ hai chỉ lộ ra khi có người gửi JSON hỏng,
    /// tức là thường lộ ra ở môi trường thật.
    /// </summary>
    private static IActionResult TraLoiRangBuocHong(ActionContext context)
    {
        var chiTiet = context.ModelState
            .Where(muc => muc.Value?.Errors.Count > 0)
            .SelectMany(muc => muc.Value!.Errors.Select(loi => Error.Validation(
                // Khoá rỗng nghĩa là lỗi của cả thân request chứ không của trường nào.
                string.IsNullOrEmpty(muc.Key) ? "Request.Malformed" : muc.Key,
                MoTaAnToan(loi.ErrorMessage))))
            .ToArray();

        var problem = new ValidationError(chiTiet).ToProblemDetails();
        problem.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;

        return new ObjectResult(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { "application/problem+json" },
        };
    }

    /// <summary>
    /// JSON hỏng thì <c>ErrorMessage</c> rỗng và thông tin thật nằm trong exception của
    /// bộ đọc — dạng "'}' is invalid after a value. Path: $.email | LineNumber: 0...".
    ///
    /// Không đẩy chuỗi đó ra ngoài: nó là chi tiết nội bộ của bộ đọc JSON, người dùng
    /// không làm gì được với nó, mà nó lại nói cho người đang dò biết ta xử lý dữ liệu
    /// bằng gì.
    /// </summary>
    private static string MoTaAnToan(string? message) =>
        string.IsNullOrWhiteSpace(message) ? "Dữ liệu gửi lên không đọc được." : message;
}
