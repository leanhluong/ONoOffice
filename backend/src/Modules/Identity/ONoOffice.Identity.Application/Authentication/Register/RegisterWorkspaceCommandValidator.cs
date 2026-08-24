using FluentValidation;

namespace ONoOffice.Identity.Application.Authentication.Register;

/// <summary>
/// Kiểm dữ liệu gửi lên trước khi handler chạy — <c>ValidationBehavior</c> gọi hộ.
///
/// Chỉ chặn thứ chắc chắn vô nghĩa. Định dạng mã workspace và email do đối tượng giá trị
/// ở tầng Domain kiểm (<c>TenantCode</c>, <c>Email</c>) — chép lại ở đây là hai nguồn sự
/// thật cho cùng một luật, và sớm muộn chúng lệch nhau.
/// </summary>
internal sealed class RegisterWorkspaceCommandValidator : AbstractValidator<RegisterWorkspaceCommand>
{
    /// <summary>
    /// Ngưỡng DUY NHẤT về mật khẩu, và nó đo ĐỘ DÀI.
    ///
    /// Cố ý không bắt "phải có chữ hoa, số và ký tự đặc biệt". Luật đó đẻ ra toàn
    /// <c>Matkhau@123</c> — dài mà đoán được, và người dùng phải dán nó vào một file ghi
    /// chú vì không nhớ nổi. Một câu dài dễ nhớ an toàn hơn nhiều.
    /// </summary>
    private const int MinPasswordLength = 10;

    public RegisterWorkspaceCommandValidator()
    {
        RuleFor(c => c.CompanyName)
            .NotEmpty().WithMessage("Vui lòng nhập tên công ty.")
            .MaximumLength(200);

        RuleFor(c => c.WorkspaceCode)
            .NotEmpty().WithMessage("Vui lòng nhập mã workspace.");

        RuleFor(c => c.FullName)
            .NotEmpty().WithMessage("Vui lòng nhập họ tên.")
            .MaximumLength(200);

        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("Vui lòng nhập email.")
            .MaximumLength(254);

        RuleFor(c => c.Password)
            .NotEmpty().WithMessage("Vui lòng nhập mật khẩu.")
            .MinimumLength(MinPasswordLength)
                .WithMessage($"Mật khẩu phải có ít nhất {MinPasswordLength} ký tự.")

            // Trần 256 vì Argon2id băm chuỗi dài tốn thời gian tuyến tính — không chặn thì
            // một chuỗi 10MB gửi lên là một lần từ chối dịch vụ rẻ tiền.
            .MaximumLength(256).WithMessage("Mật khẩu quá dài.");
    }
}
