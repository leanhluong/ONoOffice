using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.Application.Abstractions;

/// <summary>
/// Một vai trò trên màn <b>Vai trò &amp; quyền</b>.
///
/// <c>IsSystem</c> quyết định giao diện có khoá bảng quyền hay không: bốn vai hệ thống
/// dựng lại từ hằng số trong mã nguồn ở mọi workspace, nên sửa chúng sẽ bị lần nâng cấp
/// sau ghi đè mà không báo.
/// </summary>
public sealed record RoleListItem(
    Guid Id,
    string Name,
    bool IsSystem,
    IReadOnlyList<string> Permissions,
    int MemberCount);

public interface IRoleRepository
{
    /// <summary>
    /// Thêm cả bốn vai hệ thống một lượt. Chúng luôn được tạo cùng nhau lúc dựng
    /// workspace — không có ca nào tạo lẻ một vai hệ thống.
    /// </summary>
    void AddRange(IEnumerable<Role> roles);

    /// <summary>
    /// Nạp một vai trò để GÁN cho ai đó. Trả <c>null</c> khi không có.
    ///
    /// Bộ lọc theo tenant của EF đã chặn vai trò của workspace khác, nhưng nơi gọi vẫn
    /// phải tự kiểm: bộ lọc là lớp phòng thủ, không phải luật nghiệp vụ, và có những
    /// truy vấn cố tình bỏ qua nó.
    /// </summary>
    Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mọi vai trò của workspace hiện tại, kèm số người đang giữ.
    ///
    /// Đếm người ở phía database chứ không nạp cả danh sách tài khoản về rồi đếm: với
    /// workspace vài trăm người thì cách kia kéo về vài trăm dòng để trả ra bốn con số.
    /// </summary>
    Task<IReadOnlyList<RoleListItem>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Nạp một vai theo TÊN. Trả <c>null</c> khi không có.
    ///
    /// Chỉ dùng cho bốn vai HỆ THỐNG, nơi tên là hằng số trong mã nguồn
    /// (<c>SystemRoles</c>) chứ không phải chuỗi người dùng gõ. Chuyển nhượng quyền sở hữu
    /// cần đúng vai <c>Owner</c> và <c>Admin</c> của workspace hiện tại, mà mã của chúng
    /// thì mỗi workspace một khác — nên tra theo tên là cách duy nhất.
    ///
    /// ⚠️ ĐỪNG mở nó cho vai do người dùng tự đặt: tên vai không phải khoá, hai workspace
    /// đặt trùng tên là chuyện bình thường, và tra theo tên sẽ là một lỗi chờ sẵn.
    /// </summary>
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}
