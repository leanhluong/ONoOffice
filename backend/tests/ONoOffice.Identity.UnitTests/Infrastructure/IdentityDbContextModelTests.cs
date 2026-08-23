using Luong.Kernel.Abstractions;
using Microsoft.EntityFrameworkCore;
using ONoOffice.Identity.Domain.Entities;
using ONoOffice.Identity.Infrastructure.Persistence;

namespace ONoOffice.Identity.UnitTests.Infrastructure;

/// <summary>
/// Dựng THẬT mô hình EF rồi soi nó.
///
/// Cấu hình EF sai thì code vẫn BIÊN DỊCH ĐƯỢC — nó chỉ nổ lúc chạy, ở lần đầu ai đó
/// đụng tới database. Mấy test này kéo thời điểm phát hiện về ngay lúc chạy test.
/// </summary>
public class IdentityDbContextModelTests
{
    private sealed class NoTenant : ICurrentTenant
    {
        public Guid? TenantId => null;
    }

    private static IdentityDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseNpgsql("Host=localhost;Database=khong-ket-noi-that")
                .Options,
            new NoTenant());

    [Fact]
    public void MoHinh_DungDuocKhongLoi()
    {
        using var context = CreateContext();

        Assert.NotNull(context.Model);
    }

    [Fact]
    public void MoiBang_NamTrongSchemaIdentity()
    {
        using var context = CreateContext();

        foreach (var entityType in context.Model.GetEntityTypes())
        {
            Assert.Equal(IdentityDbContext.Schema, entityType.GetSchema());
        }
    }

    [Fact]
    public void TenBangVaCot_LaSnakeCase()
    {
        using var context = CreateContext();
        var user = context.Model.FindEntityType(typeof(User))!;

        Assert.Equal("users", user.GetTableName());
        Assert.Equal("password_hash", user.FindProperty(nameof(User.PasswordHash))!.GetColumnName());
        Assert.Equal("tenant_id", user.FindProperty(nameof(User.TenantId))!.GetColumnName());
    }

    // Email unique TOÀN HỆ THỐNG — ép ở tầng database, không chỉ kiểm trong code.
    // Hai request đồng thời cùng đăng ký một email sẽ qua được bước kiểm trong code;
    // chỉ ràng buộc UNIQUE mới chặn được đứa thứ hai.
    [Fact]
    public void Email_CoRangBuocUniqueToanHeThong()
    {
        using var context = CreateContext();
        var user = context.Model.FindEntityType(typeof(User))!;

        var index = user.GetIndexes().Single(i =>
            i.Properties.Count == 1 && i.Properties[0].Name == nameof(User.Email));

        Assert.True(index.IsUnique);
    }

    [Fact]
    public void TenVaiTro_KhongTrungTrongCungMotWorkspace()
    {
        using var context = CreateContext();
        var role = context.Model.FindEntityType(typeof(Role))!;

        var index = role.GetIndexes().Single(i => i.Properties.Count == 2);

        Assert.True(index.IsUnique);
        Assert.Contains(index.Properties, p => p.Name == nameof(Role.TenantId));
        Assert.Contains(index.Properties, p => p.Name == nameof(Role.Name));
    }

    // Bốn thực thể có tenant đều phải bị bộ lọc chặn. Sót một cái là rò rỉ dữ liệu.
    [Theory]
    [InlineData(typeof(User))]
    [InlineData(typeof(Role))]
    [InlineData(typeof(RefreshToken))]
    public void ThucTheCoTenant_DeuBiLocTuDong(Type clrType)
    {
        using var context = CreateContext();

        Assert.NotEmpty(context.Model.FindEntityType(clrType)!.GetDeclaredQueryFilters());
    }

    // Tenant KHÔNG bị lọc theo tenant — chính nó LÀ tenant. Lọc nó là không ai
    // đọc được workspace của mình, kể cả sau khi đã đăng nhập.
    [Fact]
    public void BangTenant_KhongBiLocTheoTenant()
    {
        using var context = CreateContext();

        Assert.Empty(context.Model.FindEntityType(typeof(Tenant))!.GetDeclaredQueryFilters());
    }

    [Fact]
    public void BangOutboxVaInbox_DuocKhaiBao()
    {
        using var context = CreateContext();

        Assert.NotNull(context.Model.FindEntityType(typeof(Luong.Kernel.Outbox.OutboxMessage)));
        Assert.NotNull(context.Model.FindEntityType(typeof(Luong.Kernel.Inbox.InboxMessage)));
    }
}
