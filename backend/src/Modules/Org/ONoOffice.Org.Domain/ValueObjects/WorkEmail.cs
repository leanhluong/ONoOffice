using System.Net.Mail;
using Luong.Kernel.Primitives;

namespace ONoOffice.Org.Domain.ValueObjects;

/// <summary>
/// Email liên hệ trên danh bạ.
///
/// <b>Vì sao Org có kiểu riêng thay vì dùng lại <c>Email</c> của Identity</b> — đây là
/// câu hỏi đúng và câu trả lời không phải "quên":
///
/// <list type="number">
/// <item><b>Luật 1 cấm.</b> Module chỉ được thấy <c>Contracts</c> của module khác;
/// <c>Identity.Domain</c> nằm ngoài tầm với, và test kiến trúc sẽ đỏ nếu tham chiếu.</item>
///
/// <item><b>Hai thứ khác nhau về bản chất.</b> <c>Identity.Email</c> là <b>danh tính</b>:
/// bắt buộc phải có, unique toàn hệ thống, là thứ người ta gõ để vào hệ thống.
/// <c>WorkEmail</c> là <b>thông tin danh bạ</b>: được phép bỏ trống (công nhân xưởng có
/// thể không có email công ty), không unique, và chỉ để đồng nghiệp liên lạc. Chúng
/// thường trùng nhau, nhưng trùng nhau không có nghĩa là một.</item>
/// </list>
///
/// Cái giá phải trả, nói thẳng: phép kiểm định dạng bị lặp ở hai module. Đó là <b>giá
/// của tính độc lập</b> — chính thứ khiến sau này cắt <c>Org</c> ra thành dịch vụ riêng
/// chỉ cần đổi chuỗi kết nối. Ngưỡng xem lại: khi có module thứ ba cũng cần nó, thì đẩy
/// một kiểu <c>EmailAddress</c> chung xuống <c>Luong.Kernel.Primitives</c> — nó đủ chung
/// để không mang một chữ nghiệp vụ nào.
/// </summary>
public sealed record WorkEmail
{
    /// <summary>Trần theo RFC 5321.</summary>
    private const int MaxLength = 254;

    private WorkEmail(string value) => Value = value;

    public string Value { get; }

    /// <summary>
    /// Dựng từ một chuỗi <b>đã biết chắc là không rỗng</b>.
    ///
    /// Cố ý KHÔNG nhận chuỗi rỗng rồi trả về <c>Result</c> thành công mang giá trị
    /// <c>null</c>. <c>Luong.Kernel</c> cấm chuyện đó — <c>Result.Success(null)</c> ném
    /// lỗi ngay — và cấm là đúng: "thành công nhưng không có giá trị" là một trạng thái
    /// mập mờ, người gọi phải nhớ kiểm null sau mỗi lần dùng, và chỉ cần một chỗ quên là
    /// có <c>NullReferenceException</c>.
    ///
    /// Việc "được phép bỏ trống" thuộc về <c>Employee.UpdateContact</c>, nơi biết rằng
    /// một hồ sơ có thể không có email. Kiểu này chỉ trả lời đúng một câu: chuỗi đó có
    /// phải email hợp lệ không.
    /// </summary>
    public static Result<WorkEmail> Create(string value)
    {
        // Chuẩn hoá TRƯỚC khi kiểm: "  An@Congty.VN  " và "an@congty.vn" là một.
        string normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength)
        {
            return OrgErrors.WorkEmails.TooLong;
        }

        // Dùng bộ phân tích có sẵn của .NET thay vì tự viết biểu thức chính quy — bản
        // chép trên mạng gần như luôn sai ở các ca hiếm, và không ai đọc nổi để sửa.
        if (!MailAddress.TryCreate(normalized, out var address) || address.Address != normalized)
        {
            return OrgErrors.WorkEmails.Invalid;
        }

        return new WorkEmail(normalized);
    }

    public override string ToString() => Value;
}
