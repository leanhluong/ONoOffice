using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.Application.Abstractions;

public interface IRoleRepository
{
    /// <summary>
    /// Thêm cả bốn vai hệ thống một lượt. Chúng luôn được tạo cùng nhau lúc dựng
    /// workspace — không có ca nào tạo lẻ một vai hệ thống.
    /// </summary>
    void AddRange(IEnumerable<Role> roles);
}
