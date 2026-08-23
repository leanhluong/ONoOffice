using System.Text.RegularExpressions;
using Luong.Kernel.Primitives;

namespace ONoOffice.Identity.Domain.ValueObjects;

/// <summary>
/// Mã định danh ngắn của một workspace: <c>acme</c>, <c>cong-ty-abc</c>.
///
/// Người dùng nhìn thấy mã này (trên URL, trong lời mời), nên nó phải gõ được, đọc được
/// qua điện thoại, và <b>ổn định</b> — đổi mã là gãy mọi đường dẫn đã chia sẻ.
///
/// Luật đặt mã theo đúng chuẩn của một nhãn tên miền, vì sau này rất có thể mã này thành
/// tên miền con <c>acme.onooffice.com</c>. Chọn luật chặt ngay từ đầu thì ngày đó không
/// phải đi sửa dữ liệu cũ.
/// </summary>
public sealed partial record TenantCode
{
    private const int MinLength = 3;
    private const int MaxLength = 30;

    private TenantCode(string value) => Value = value;

    public string Value { get; }

    public static Result<TenantCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return IdentityErrors.TenantCodes.Empty;
        }

        string normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length is < MinLength or > MaxLength)
        {
            return IdentityErrors.TenantCodes.WrongLength;
        }

        return Pattern().IsMatch(normalized)
            ? new TenantCode(normalized)
            : IdentityErrors.TenantCodes.Invalid;
    }

    /// <summary>
    /// Bắt đầu bằng chữ cái · giữa là chữ/số/gạch nối · kết thúc bằng chữ hoặc số
    /// · không có hai gạch nối liền nhau.
    ///
    /// Bốn luật này đều có lý do: bắt đầu bằng số thì trông như một con số; kết thúc
    /// bằng gạch nối thì nhìn như bị cắt cụt; hai gạch liền nhau là dấu hiệu gõ nhầm.
    /// </summary>
    [GeneratedRegex("^[a-z](?:[a-z0-9]|-(?=[a-z0-9]))*[a-z0-9]$")]
    private static partial Regex Pattern();

    public override string ToString() => Value;
}
