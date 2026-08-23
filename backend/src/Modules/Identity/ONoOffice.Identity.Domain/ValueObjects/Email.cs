using System.Net.Mail;
using Luong.Kernel.Primitives;

namespace ONoOffice.Identity.Domain.ValueObjects;

/// <summary>
/// Địa chỉ email đã được kiểm và chuẩn hoá.
///
/// <b>Vì sao không dùng thẳng <c>string</c>:</b> một chuỗi thì có thể rỗng, có thể là
/// "abc", có thể là "  An@Gmail.COM  ". Mỗi chỗ nhận nó lại phải tự kiểm lại, và chỉ
/// cần một chỗ quên là dữ liệu rác đi thẳng xuống database.
///
/// Kiểu này bảo đảm: <b>nếu bạn đang cầm một <c>Email</c>, nó chắc chắn hợp lệ và đã
/// chuẩn hoá.</b> Không có đường nào tạo ra một <c>Email</c> sai — hàm dựng là private,
/// lối vào duy nhất là <see cref="Create"/> và nó trả <see cref="Result{T}"/>.
///
/// Là <c>record</c> nên so sánh bằng NỘI DUNG — đúng bản chất của đối tượng giá trị:
/// hai email cùng địa chỉ là một, không quan tâm nằm ở ô nhớ nào.
/// </summary>
public sealed record Email
{
    /// <summary>Trần theo RFC 5321.</summary>
    private const int MaxLength = 254;

    private Email(string value) => Value = value;

    public string Value { get; }

    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return IdentityErrors.Emails.Empty;
        }

        // Chuẩn hoá TRƯỚC khi kiểm: "  An@Gmail.COM  " và "an@gmail.com" phải là một.
        // Không làm bước này thì cùng một người đăng ký được hai tài khoản, rồi lần
        // đăng nhập sau không biết mình là ai.
        string normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength)
        {
            return IdentityErrors.Emails.TooLong;
        }

        return IsValid(normalized) ? new Email(normalized) : IdentityErrors.Emails.Invalid;
    }

    private static bool IsValid(string value)
    {
        // Dùng bộ phân tích có sẵn của .NET thay vì tự viết biểu thức chính quy.
        // Biểu thức chính quy cho email đúng theo chuẩn dài hàng trăm ký tự, gần như
        // không ai đọc nổi, và bản chép trên mạng thường sai ở các ca hiếm.
        if (!MailAddress.TryCreate(value, out var address) || address.Address != value)
        {
            return false;
        }

        // Bộ phân tích của .NET chấp nhận "user@localhost" (đúng theo chuẩn, dùng được
        // trong mạng nội bộ). Ở đây CỐ Ý siết thêm: bắt buộc có dấu chấm ở phần tên
        // miền — vì đây là email công việc của người thật, và "an@localhost" gần như
        // chắc chắn là gõ nhầm chứ không phải chủ ý.
        int atIndex = value.LastIndexOf('@');
        string host = value[(atIndex + 1)..];

        return host.Contains('.', StringComparison.Ordinal)
            && !host.StartsWith('.')
            && !host.EndsWith('.');
    }

    public override string ToString() => Value;
}
