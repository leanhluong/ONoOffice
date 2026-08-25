using Luong.Kernel.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ONoOffice.Identity.Infrastructure.Persistence;
using ONoOffice.Org.Infrastructure.Persistence;

namespace ONoOffice.Api.IntegrationTests;

/// <summary>
/// <c>IUnitOfWork</c> phải chốt <b>cả hai</b> DbContext.
///
/// ═══════════════════════════════════════════════════════════════════════
///  BỘ CANH NÀY SINH RA TỪ MỘT LỖI SUÝT LỌT
/// ═══════════════════════════════════════════════════════════════════════
///
/// <c>TransactionBehavior</c> của kernel phân giải MỘT <c>IUnitOfWork</c> rồi gọi
/// <c>SaveChangesAsync</c> sau mỗi mệnh lệnh thành công. <c>AddIdentityModule</c> đăng ký
/// nó trỏ vào <c>IdentityDbContext</c>.
///
/// Thêm module Org mà để nguyên như vậy thì mọi mệnh lệnh của Org gọi <c>SaveChanges</c>
/// trên context <b>không theo dõi thực thể nào của Org</b>. EF không thấy gì thay đổi nên
/// không sinh câu SQL nào, không ném lỗi, trả về 0. Handler trả <c>Result.Success</c>, API
/// trả 200, giao diện hiện "đã lưu" — và database trống trơn. Không log nào ghi lại.
///
/// Đây đúng loại lỗi mà mọi bộ test khác đều bỏ qua: test đơn vị dùng repository giả nên
/// không có <c>SaveChanges</c> nào; test tích hợp không chạm database; test database thì
/// gọi repository trực tiếp chứ không đi qua MediatR.
///
/// Nên phép kiểm phải nhắm vào chính DÂY NỐI, và nó không cần database nào.
/// </summary>
public sealed class UnitOfWorkWiringTests
{
    [Fact]
    public void UnitOfWork_KhongDuocLaMotDbContextDonLe()
    {
        using var factory = new ApiFactory();
        using var scope = factory.Services.CreateScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Trỏ thẳng vào một context nghĩa là module còn lại ghi vào hư không.
        Assert.IsNotType<IdentityDbContext>(unitOfWork);
        Assert.IsNotType<OrgDbContext>(unitOfWork);
    }

    /// <summary>
    /// Chốt xong thì <b>cả hai</b> context phải hết thay đổi đang chờ.
    ///
    /// Đây mới là phép kiểm thật sự: nó không quan tâm lớp nào cài <c>IUnitOfWork</c>, nó
    /// hỏi đúng câu người dùng quan tâm — "tôi bấm lưu thì dữ liệu của tôi có được ghi
    /// không". Ai đó thay <c>CompositeUnitOfWork</c> bằng thứ khác mà vẫn đúng thì test
    /// này vẫn xanh, đúng như nên thế.
    ///
    /// Không cần Postgres: cả hai context ở đây đều KHÔNG có gì thay đổi, nên
    /// <c>SaveChanges</c> là phép rỗng và không mở kết nối nào. Chuỗi kết nối của
    /// <c>ApiFactory</c> trỏ vào cổng 1 — nếu lớp này lỡ chạm database thì test hỏng NGAY
    /// chứ không treo.
    /// </summary>
    [Fact]
    public async Task ChotXong_ThiCaHaiContextDeuSachThayDoi()
    {
        using var factory = new ApiFactory();
        using var scope = factory.Services.CreateScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var org = scope.ServiceProvider.GetRequiredService<OrgDbContext>();

        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        Assert.False(identity.ChangeTracker.HasChanges());
        Assert.False(org.ChangeTracker.HasChanges());
    }

    /// <summary>
    /// Hai module, hai schema — luật số 2 của kiến trúc.
    ///
    /// Kiểm ở đây chứ không ở test kiến trúc vì tên schema là một hằng số lúc CHẠY, không
    /// phải một quan hệ giữa các assembly. Trùng schema thì hai module ghi đè bảng của
    /// nhau, và migration của cái này xoá bảng của cái kia.
    /// </summary>
    [Fact]
    public void HaiModule_DungHaiSchemaKhacNhau()
    {
        Assert.NotEqual(IdentityDbContext.Schema, OrgDbContext.Schema);
    }
}
