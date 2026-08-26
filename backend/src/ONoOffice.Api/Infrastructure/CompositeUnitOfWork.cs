using Luong.Kernel.Abstractions;
using ONoOffice.Comm.Infrastructure.Persistence;
using ONoOffice.Identity.Infrastructure.Persistence;
using ONoOffice.Org.Infrastructure.Persistence;

namespace ONoOffice.Api.Infrastructure;

/// <summary>
/// Một <see cref="IUnitOfWork"/> chốt <b>cả ba</b> DbContext.
///
/// ═══════════════════════════════════════════════════════════════════════
///  BỊT MỘT LỖ HỎNG IM LẶNG
/// ═══════════════════════════════════════════════════════════════════════
///
/// <c>TransactionBehavior</c> của kernel phân giải <b>một</b> <c>IUnitOfWork</c> rồi gọi
/// <c>SaveChangesAsync</c> sau mỗi mệnh lệnh thành công. Nhưng modular monolith này có
/// BA context, mỗi module một cái.
///
/// Nếu để mỗi module tự đăng ký <c>IUnitOfWork</c> trỏ vào context của mình thì cái đăng
/// ký SAU thắng, và hỏng theo kiểu tệ nhất: mệnh lệnh của module thua sẽ gọi
/// <c>SaveChanges</c> trên một context <b>không theo dõi thực thể nào của nó</b>. EF thấy
/// không có gì thay đổi nên không sinh câu SQL nào, không ném lỗi, và trả về 0. Handler
/// trả <c>Result.Success</c>, API trả 200, giao diện hiện "đã lưu" — và trong database
/// không có gì. Không log nào ghi lại chuyện đó.
///
/// Nên chỗ đăng ký phải là <b>gốc kết hợp</b> (<c>Program.cs</c>), nơi duy nhất biết hệ
/// thống có bao nhiêu module.
///
/// ⚠️ <b>Thêm module thứ tư thì phải sửa ở đây.</b> Quên thì mọi mệnh lệnh của nó trả 200
/// và không ghi gì — đúng cái lỗ hỏng đoạn trên vừa mô tả. <c>UnitOfWorkWiringTests</c>
/// canh chỗ này, và khi thêm module Comm nó đã đỏ đúng như thiết kế.
///
/// ═══════════════════════════════════════════════════════════════════════
///  ⚠️ GIỚI HẠN: HAI LẦN GHI, KHÔNG PHẢI MỘT TRANSACTION
/// ═══════════════════════════════════════════════════════════════════════
///
/// Ba <c>DbContext</c> mở ba kết nối riêng, nên đây là ba transaction nối tiếp nhau,
/// không phải một. Ghi xong Identity mà Org nổ thì phần Identity <b>đã nằm trong
/// database</b>.
///
/// Hôm nay điều đó chưa gây hại vì <b>mọi mệnh lệnh đều chỉ chạm đúng MỘT module</b> —
/// context còn lại không có gì thay đổi nên <c>SaveChanges</c> của nó là một phép rỗng,
/// không đi tới database. Ngày nào có một mệnh lệnh ghi vào cả hai (ví dụ: tạo tài khoản
/// và tạo luôn hồ sơ nhân sự) thì <b>chỗ này phải đổi trước</b>: hoặc dùng chung một
/// <c>DbConnection</c> và một transaction, hoặc tách thành hai mệnh lệnh nối nhau qua
/// outbox. Đừng để lớp này im lặng gánh một thứ nó không gánh được.
/// </summary>
internal sealed class CompositeUnitOfWork(
    IdentityDbContext identity,
    OrgDbContext org,
    CommDbContext comm) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // `ChangeTracker.HasChanges()` để không mở kết nối một cách vô ích. EF cũng tự bỏ
        // qua khi không có gì thay đổi, nhưng gọi tường minh thì đọc code là thấy ngay
        // rằng lớp này KHÔNG chạm database cho module đứng ngoài mệnh lệnh.
        int daGhi = 0;

        if (identity.ChangeTracker.HasChanges())
        {
            daGhi += await identity.SaveChangesAsync(cancellationToken);
        }

        if (org.ChangeTracker.HasChanges())
        {
            daGhi += await org.SaveChangesAsync(cancellationToken);
        }

        if (comm.ChangeTracker.HasChanges())
        {
            daGhi += await comm.SaveChangesAsync(cancellationToken);
        }

        return daGhi;
    }
}
