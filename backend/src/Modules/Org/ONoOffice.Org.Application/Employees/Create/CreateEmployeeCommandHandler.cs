using Luong.Kernel.Abstractions;
using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Org.Application.Abstractions;
using ONoOffice.Org.Domain;
using ONoOffice.Org.Domain.Entities;

namespace ONoOffice.Org.Application.Employees.Create;

public sealed record CreateEmployeeCommand(
    string Code,
    string FullName,
    string? JobTitle,
    string? WorkEmail,
    string? Phone,
    Guid? DepartmentId) : ICommand<CreateEmployeeResponse>;

public sealed record CreateEmployeeResponse(Guid Id, string Code, string FullName);

/// <summary>
/// Thêm một hồ sơ nhân sự.
///
/// <b>Hồ sơ, KHÔNG phải tài khoản đăng nhập.</b> Hai khái niệm khác nhau và chúng đổi vì
/// những lý do khác nhau — xem chú thích đầu <c>Employee.cs</c>. Người mới có hồ sơ trước,
/// tài khoản sau; và tài khoản bot thì có tài khoản mà không có hồ sơ.
///
/// Thứ tự bốn phép kiểm là thứ tự người dùng SỬA ĐƯỢC: workspace → dữ liệu trong hồ sơ →
/// mã trùng → phòng ban tồn tại. Hỏi database chỉ sau khi mọi thứ kiểm được tại chỗ đã
/// xong, để mỗi vòng mạng đều đáng.
/// </summary>
internal sealed class CreateEmployeeCommandHandler(
    IEmployeeRepository employees,
    IDepartmentRepository departments,
    ICurrentTenant currentTenant) : ICommandHandler<CreateEmployeeCommand, CreateEmployeeResponse>
{
    public async Task<Result<CreateEmployeeResponse>> Handle(
        CreateEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        // Tenant lấy từ PHIÊN, không bao giờ từ thân request.
        if (currentTenant.TenantId is not { } tenantId)
        {
            return OrgErrors.Employees.TenantRequired;
        }

        var nguoi = Employee.Create(
            tenantId,
            command.Code,
            command.FullName,
            command.WorkEmail,
            command.Phone);

        if (nguoi.IsFailure)
        {
            return nguoi.Error;
        }

        var chucDanh = nguoi.Value.ChangeJobTitle(command.JobTitle);

        if (chucDanh.IsFailure)
        {
            return chucDanh.Error;
        }

        /*
          Hỏi database bằng mã ĐÃ CHUẨN HOÁ (`nguoi.Value.Code`), không bằng chuỗi thô
          người dùng gõ.

          `Employee.Create` cắt khoảng trắng và VIẾT HOA — `  nv001  ` thành `NV001`. Hỏi
          bằng chuỗi thô thì phép kiểm trùng chạy trên một giá trị KHÁC với giá trị sắp
          lưu: tạo được cả `nv001` lẫn `NV001`, và ràng buộc UNIQUE của Postgres mới nổ,
          bằng một lỗi 500 mà người dùng không hiểu gì.
        */
        if (await employees.CodeTakenAsync(nguoi.Value.Code, null, cancellationToken))
        {
            return OrgErrors.Employees.CodeTaken;
        }

        if (command.DepartmentId is { } phongId)
        {
            if (await departments.GetAsync(phongId, cancellationToken) is null)
            {
                return OrgErrors.Departments.NotFound;
            }

            var chuyen = nguoi.Value.TransferTo(phongId);

            if (chuyen.IsFailure)
            {
                return chuyen.Error;
            }
        }

        // Không kèm phòng ban là chuyện BÌNH THƯỜNG của người mới, không phải thiếu dữ liệu.

        employees.Add(nguoi.Value);

        return new CreateEmployeeResponse(nguoi.Value.Id, nguoi.Value.Code, nguoi.Value.FullName);
    }
}
