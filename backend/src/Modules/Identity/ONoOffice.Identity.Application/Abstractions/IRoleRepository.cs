using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.Application.Abstractions;

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
}
