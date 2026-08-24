using Luong.Kernel.Abstractions;
using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Domain;

namespace ONoOffice.Identity.Application.Me.GetProfile;

public sealed record GetMyProfileQuery : IQuery<MyProfile>;

/// <summary>
/// Hồ sơ của chính người đang đăng nhập.
///
/// <c>IsOwner</c> có ở đây vì giao diện cần nó để <b>ẩn bớt lựa chọn</b>: chủ sở hữu không
/// tự đổi vai trò của mình được, không tự vô hiệu hoá mình được. Hiện nút rồi báo lỗi khi
/// bấm là cách chắc chắn nhất làm người dùng bực.
/// </summary>
public sealed record MyProfile(
    Guid Id,
    Guid TenantId,
    string Email,
    string FullName,
    string RoleName,
    bool IsOwner,
    bool MustChangePassword);

/// <summary>
/// Thay cho <c>GET /api/auth/me</c> từng nợ trong HANDOFF.
///
/// <b>Vì sao cần khi phản hồi đăng nhập đã có tên và email:</b> frontend ghi chúng xuống
/// <c>localStorage</c> để mở lại tab là hiện ngay, nhưng bản ghi đó có thể cũ hàng tuần —
/// phòng Nhân sự đổi chức danh, quản trị viên đổi vai trò. Endpoint này là nguồn sự thật
/// để làm tươi lại.
/// </summary>
internal sealed class GetMyProfileQueryHandler(
    IUserRepository users,
    ICurrentUser currentUser) : IQueryHandler<GetMyProfileQuery, MyProfile>
{
    public async Task<Result<MyProfile>> Handle(GetMyProfileQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return IdentityErrors.Users.NotFound;
        }

        var profile = await users.GetProfileAsync(userId, cancellationToken);

        return profile is null ? IdentityErrors.Users.NotFound : profile;
    }
}
