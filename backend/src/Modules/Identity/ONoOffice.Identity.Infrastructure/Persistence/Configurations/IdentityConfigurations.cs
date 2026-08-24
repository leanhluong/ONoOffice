using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ONoOffice.Identity.Domain.Entities;
using ONoOffice.Identity.Domain.ValueObjects;

namespace ONoOffice.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// Ánh xạ <c>Tenant</c> sang bảng.
///
/// Cả file này KHÔNG có một quyết định nghiệp vụ nào — luật đã nằm hết ở tầng Domain.
/// Ở đây chỉ còn: tên bảng, độ dài cột, chỉ mục, và cách đọc/ghi đối tượng giá trị.
/// </summary>
internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(t => t.Id);

        // TenantCode là đối tượng giá trị. Lưu xuống chỉ là một chuỗi, nên dùng bộ chuyển
        // đổi thay vì tạo hẳn một bảng con — nó không có danh tính riêng để cần bảng riêng.
        builder.Property(t => t.Code)
            .HasConversion(code => code.Value, value => TenantCode.Create(value).Value)
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(t => t.Code).IsUnique();

        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();

        builder.Ignore(t => t.DomainEvents);
    }
}

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .HasConversion(email => email.Value, value => Email.Create(value).Value)
            .HasMaxLength(254)
            .IsRequired();

        // UNIQUE TOÀN HỆ THỐNG, không phải unique trong một tenant.
        //
        // Đây là hệ quả bắt buộc của "mỗi người thuộc đúng một workspace" (ADR-0002):
        // nếu email chỉ unique trong một công ty thì đăng nhập bằng email + mật khẩu là
        // mơ hồ — hai công ty cùng có an@gmail.com thì hệ thống không biết là ai.
        //
        // Ép ở tầng database chứ không chỉ kiểm trong code: hai request đồng thời cùng
        // đăng ký một email sẽ qua được bước kiểm trong code, và chỉ có ràng buộc UNIQUE
        // mới chặn được đứa thứ hai.
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
        builder.Property(u => u.FullName).HasMaxLength(200).IsRequired();

        // Vai trò lưu thành MỘT CỘT MẢNG uuid[] của Postgres, không phải bảng nối riêng.
        //
        // Vì sao: User và Role là HAI gốc tổng hợp khác nhau. Một gốc không giữ tham chiếu
        // trực tiếp tới gốc khác — chỉ giữ khoá. Domain khai `List<Guid>`, nên ánh xạ
        // thẳng thành mảng là cách trung thực nhất; dựng bảng nối sẽ phải đẻ thêm một
        // kiểu trung gian chỉ để làm vui lòng EF.
        //
        // ĐÁNH ĐỔI, nói thẳng: mất ràng buộc khoá ngoại — xoá một Role thì các Guid trong
        // mảng thành mồ côi. Chấp nhận được vì xoá vai trò là việc hiếm và sẽ đi qua một
        // use case riêng (phải gỡ khỏi mọi user trước). Ngưỡng phải xem lại: khi cần truy
        // vấn ngược "vai trò này đang gán cho ai" thường xuyên — lúc đó thêm chỉ mục GIN
        // hoặc chuyển sang bảng nối.
        builder.PrimitiveCollection<List<Guid>>("_roleIds")
            .HasColumnName("role_ids")
            .HasColumnType("uuid[]");

        builder.Ignore(u => u.RoleIds);
        builder.Ignore(u => u.DomainEvents);
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    private const string PermissionsField = "_permissions";

    /// <summary>
    /// Dạy EF cách so sánh hai tập quyền — và đây <b>không</b> phải chi tiết vụn vặt.
    ///
    /// Với một thuộc tính có phép chuyển đổi, mặc định EF so bằng <b>tham chiếu</b>. Mà
    /// <c>Grant()</c> sửa tại chỗ chính cái <c>HashSet</c> đang có, nên tham chiếu không
    /// đổi → EF kết luận "không có gì thay đổi" → <c>SaveChanges</c> không sinh câu
    /// <c>UPDATE</c> nào. Cấp quyền xong, không có lỗi nào, và quyền <b>không được lưu</b>.
    ///
    /// Băm phải KHÔNG phụ thuộc thứ tự (dùng XOR, không dùng <c>HashCode.Combine</c> nối
    /// tiếp): hai tập cùng phần tử mà duyệt ra khác thứ tự phải cho cùng một mã băm, nếu
    /// không EF lại tưởng có thay đổi ở mọi lần đọc lên.
    ///
    /// Bản sao chép phải là bản sao THẬT — EF giữ nó làm ảnh chụp để đối chiếu. Trả về
    /// chính đối tượng cũ thì ảnh chụp thay đổi theo bản gốc, và không bao giờ khác nó.
    /// </summary>
    private static readonly ValueComparer<HashSet<string>> PermissionsComparer = new(
        (a, b) => a != null && b != null && a.SetEquals(b),
        tap => tap.Aggregate(0, (bam, quyen) => bam ^ StringComparer.OrdinalIgnoreCase.GetHashCode(quyen)),
        tap => new HashSet<string>(tap, StringComparer.OrdinalIgnoreCase));

    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();

        // Không cho trùng tên vai trò TRONG CÙNG một công ty. Khác công ty thì trùng
        // thoải mái — "Trưởng phòng" của A và của B là hai vai trò khác nhau.
        builder.HasIndex(r => new { r.TenantId, r.Name }).IsUnique();

        // Tập quyền lưu thành MỘT CỘT text[] của Postgres.
        //
        // Không dùng được PrimitiveCollection như User._roleIds: cái đó cần một mảng hoặc
        // IList, mà tập quyền là HashSet (xem lý do ở Role._permissions). Nên phải tự khai
        // phép chuyển đổi HashSet ↔ string[].
        builder.Property<HashSet<string>>(PermissionsField)
            .HasColumnName("permissions")
            .HasColumnType("text[]")
            .HasConversion(
                tap => tap.ToArray(),
                mang => new HashSet<string>(mang, StringComparer.OrdinalIgnoreCase),
                PermissionsComparer)
            .IsRequired();

        builder.Ignore(r => r.Permissions);
        builder.Ignore(r => r.DomainEvents);
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();

        // Tra cứu lúc gia hạn phiên đi qua chỉ mục này.
        builder.HasIndex(t => t.TokenHash).IsUnique();

        // Phục vụ việc thu hồi CẢ CHUỖI khi phát hiện token bị dùng lại (dấu hiệu bị trộm).
        builder.HasIndex(t => new { t.UserId, t.RevokedAtUtc });
    }
}
