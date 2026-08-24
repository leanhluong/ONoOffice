using Luong.Kernel.Abstractions;
using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Domain;
using ONoOffice.Identity.Domain.Entities;
using ONoOffice.Identity.Domain.ValueObjects;

namespace ONoOffice.Identity.Application.Users.Create;

public sealed record CreateUserCommand(
    string FullName,
    string Email,
    Guid RoleId,
    bool MustChangePassword) : ICommand<CreateUserResponse>;

/// <summary>
/// <c>TemporaryPassword</c> là lần DUY NHẤT mật khẩu thô tồn tại ngoài đầu người tạo.
/// Nó không được ghi log, không được lưu, và không có endpoint nào đọc lại được nó.
/// </summary>
public sealed record CreateUserResponse(
    Guid Id,
    string Email,
    string FullName,
    string RoleName,
    string TemporaryPassword);

/// <summary>
/// Quản trị viên tạo tài khoản HỘ một đồng nghiệp.
///
/// Khác đăng ký workspace ở một điểm quyết định mọi thứ còn lại: <b>người tạo và người
/// dùng là hai người khác nhau</b>. Người dùng không có mặt lúc tạo, nên họ không chọn
/// được mật khẩu của mình. Hệ thống sinh hộ một mật khẩu tạm, trả về đúng một lần cho
/// người tạo đưa tận tay, và đánh dấu buộc đổi ở lần đăng nhập đầu.
///
/// <b>Vì sao không gửi email lời mời:</b> lát này chưa nối dịch vụ gửi mail. Làm một luồng
/// "đã gửi lời mời" mà thật ra không gửi gì là kiểu nói dối tệ nhất — quản trị viên ngồi
/// chờ, đồng nghiệp không nhận được gì, và không có chỗ nào báo lỗi.
/// </summary>
internal sealed class CreateUserCommandHandler(
    IUserRepository users,
    IRoleRepository roles,
    IPasswordHasher passwordHasher,
    ITemporaryPasswordGenerator passwordGenerator,
    ICurrentTenant currentTenant) : ICommandHandler<CreateUserCommand, CreateUserResponse>
{
    public async Task<Result<CreateUserResponse>> Handle(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        // Tenant lấy từ PHIÊN, không bao giờ từ thân request. Nhận từ ngoài vào thì một
        // quản trị viên gõ tay mã workspace của công ty khác là tạo được tài khoản trong
        // công ty đó — và không lớp nào phía dưới ngăn được, vì mọi thứ dưới đây tin rằng
        // tenant đã đúng.
        if (currentTenant.TenantId is not { } tenantId)
        {
            return IdentityErrors.Users.TenantRequired;
        }

        // Kiểm định dạng TRƯỚC khi hỏi database: email sai định dạng thì không thể trùng
        // với ai, nên hỏi là một vòng đi về thừa — và là một cách đo xem email nào có thật.
        var email = Email.Create(command.Email);

        if (email.IsFailure)
        {
            return email.Error;
        }

        var role = await roles.GetByIdAsync(command.RoleId, cancellationToken);

        // So cả TenantId chứ không chỉ tin bộ lọc của EF. Bộ lọc là lớp phòng thủ, không
        // phải luật nghiệp vụ — và có truy vấn cố tình bỏ qua nó.
        if (role is null || role.TenantId != tenantId)
        {
            return IdentityErrors.Roles.NotFound;
        }

        if (await users.IsEmailTakenAsync(email.Value.Value, cancellationToken))
        {
            return IdentityErrors.Emails.Taken;
        }

        var temporaryPassword = passwordGenerator.Generate();

        var user = User.Create(
            tenantId,
            email.Value.Value,
            passwordHasher.Hash(temporaryPassword),
            command.FullName);

        if (user.IsFailure)
        {
            return user.Error;
        }

        var assigned = user.Value.AssignRole(role.Id);

        if (assigned.IsFailure)
        {
            return assigned.Error;
        }

        if (command.MustChangePassword)
        {
            user.Value.RequirePasswordChange();
        }

        users.Add(user.Value);

        return new CreateUserResponse(
            user.Value.Id,
            user.Value.Email.Value,
            user.Value.FullName,
            role.Name,
            temporaryPassword);
    }
}
