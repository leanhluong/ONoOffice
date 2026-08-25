using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ONoOffice.Org.Domain.Entities;
using ONoOffice.Org.Domain.ValueObjects;

namespace ONoOffice.Org.Infrastructure.Persistence.Configurations;

/// <summary>
/// Ánh xạ <c>Department</c> sang bảng.
///
/// Cả file này KHÔNG có một quyết định nghiệp vụ nào — luật đã nằm hết ở tầng Domain.
/// Ở đây chỉ còn: tên bảng, độ dài cột, chỉ mục, và cách đọc/ghi đối tượng giá trị.
/// </summary>
internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name).HasMaxLength(100).IsRequired();

        /*
          UNIQUE trong MỘT workspace, không phải toàn hệ thống.

          Ngược với `Users.Email` (unique toàn hệ thống vì đăng nhập bằng email phải không
          mơ hồ). Ở đây thì hai công ty cùng có phòng "Kỹ thuật" là hoàn toàn bình thường,
          và ép unique toàn cục sẽ khiến công ty thứ hai không đặt được tên phòng của mình.

          Ép ở tầng database chứ không chỉ kiểm trong handler: hai request đồng thời cùng
          tạo "Kỹ thuật" thì cả hai đều thấy "chưa có" rồi cùng ghi.
        */
        builder.HasIndex(d => new { d.TenantId, d.Name }).IsUnique();

        // Chỉ mục trên ParentId: mọi truy vấn dựng cây và mọi phép kiểm "còn phòng con
        // không" đều lọc theo cột này.
        builder.HasIndex(d => d.ParentId);

        /*
          KHÔNG khai khoá ngoại tự trỏ vào chính bảng (`ParentId` → `Id`).

          Nghe hợp lý, nhưng nó khoá tay ở đúng chỗ cần mềm: xoá một phòng gốc sẽ bị
          database từ chối vì còn tham chiếu, TRƯỚC KHI handler kịp trả về câu
          `Department.HasChildren` mà giao diện hiểu được. Người dùng nhận một lỗi 500 kèm
          tên ràng buộc thay vì "phòng ban còn phòng con".

          Luật đó đã có ở `DeleteDepartmentCommandHandler`, và nó nói được bằng tiếng người.
        */

        builder.Ignore(d => d.DomainEvents);
    }
}

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).HasMaxLength(30).IsRequired();

        // Mã nhân viên do công ty đặt → unique trong workspace. Domain đã VIẾT HOA và cắt
        // khoảng trắng trước khi tới đây, nên chỉ mục này so đúng thứ người dùng gõ.
        builder.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();

        builder.Property(e => e.FullName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.JobTitle).HasMaxLength(100);
        builder.Property(e => e.Phone).HasMaxLength(30);

        /*
          `WorkEmail` là đối tượng giá trị và ĐƯỢC PHÉP rỗng.

          Bộ chuyển đổi chỉ chạy khi giá trị khác null — EF lo phần đó. Nhưng phép chuyển
          ngược `WorkEmail.Create(value).Value` sẽ NỔ nếu trong database có một chuỗi không
          hợp lệ (dữ liệu cũ, ai đó sửa tay). Chấp nhận: thà hỏng to lúc đọc còn hơn im
          lặng nuốt một email sai rồi hiện nó lên danh bạ.
        */
        builder.Property(e => e.WorkEmail)
            .HasConversion(email => email!.Value, value => WorkEmail.Create(value).Value)
            .HasMaxLength(254);

        builder.HasIndex(e => e.DepartmentId);

        // Tra "hồ sơ nào ứng với tài khoản này" là truy vấn thường xuyên khi người dùng mở
        // hồ sơ của chính mình. Không unique: một tài khoản chỉ nối được một hồ sơ, nhưng
        // luật đó do `Employee.LinkAccount` canh, và ép unique ở đây sẽ chặn cả những hàng
        // `UserId = null` trên Postgres theo cách khó đoán.
        builder.HasIndex(e => e.UserId);

        builder.Ignore(e => e.DomainEvents);
    }
}
