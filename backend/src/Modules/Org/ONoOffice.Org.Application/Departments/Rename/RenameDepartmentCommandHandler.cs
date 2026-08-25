using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Org.Application.Abstractions;
using ONoOffice.Org.Domain;

namespace ONoOffice.Org.Application.Departments.Rename;

public sealed record RenameDepartmentCommand(Guid Id, string Name) : ICommand;

/// <summary>
/// Đổi tên một phòng ban.
///
/// Phép kiểm trùng tên phải LOẠI TRỪ chính nó (<c>exceptId</c>). Thiếu chỗ đó thì đổi
/// "Kỹ thuật" thành "Kỹ thuật" — hoặc chỉ sửa hoa thường, hoặc bỏ một dấu cách thừa — sẽ
/// bị từ chối vì trùng với chính mình, và thông báo lỗi đọc như một lời nói dối.
/// </summary>
internal sealed class RenameDepartmentCommandHandler(IDepartmentRepository departments)
    : ICommandHandler<RenameDepartmentCommand>
{
    public async Task<Result> Handle(
        RenameDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        var phong = await departments.GetAsync(command.Id, cancellationToken);

        if (phong is null)
        {
            return OrgErrors.Departments.NotFound;
        }

        var doiTen = phong.Rename(command.Name);

        if (doiTen.IsFailure)
        {
            return doiTen.Error;
        }

        if (await departments.NameTakenAsync(phong.Name, command.Id, cancellationToken))
        {
            return OrgErrors.Departments.NameTaken;
        }

        return Result.Success();
    }
}
