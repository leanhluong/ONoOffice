using FluentValidation;

namespace ONoOffice.Identity.Application.Authentication.Login;

/// <summary>
/// Kiểm dữ liệu gửi lên trước khi handler chạy — <c>ValidationBehavior</c> gọi hộ.
///
/// CỐ Ý kiểm rất nhẹ: chỉ chặn thứ chắc chắn vô nghĩa (rỗng, dài bất thường).
/// KHÔNG kiểm định dạng email chặt ở đây, và KHÔNG kiểm độ mạnh mật khẩu — hai chuyện đó
/// thuộc màn ĐĂNG KÝ. Ở màn đăng nhập, mọi thứ sai đều phải quy về cùng một câu trả lời
/// "email hoặc mật khẩu không đúng"; báo riêng "email sai định dạng" là lại mở đường
/// dò tài khoản.
/// </summary>
internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("Vui lòng nhập email.")
            .MaximumLength(254);

        RuleFor(c => c.Password)
            .NotEmpty().WithMessage("Vui lòng nhập mật khẩu.")
            .MaximumLength(256).WithMessage("Mật khẩu quá dài.");
    }
}
