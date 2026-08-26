using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.Application.Abstractions;

public interface ITenantRepository
{
    /// <summary>
    /// Chỉ ghi vào bộ theo dõi thay đổi. Chốt xuống database là việc của
    /// <c>TransactionBehavior</c> — nhờ vậy nếu bước sau thất bại thì cả workspace, bốn
    /// vai trò và tài khoản chủ cùng biến mất, không để lại một công ty dựng dở.
    /// </summary>
    void Add(Tenant tenant);

    /// <summary>
    /// <b>Đây KHÔNG phải lớp bảo vệ cuối cùng.</b> Hai người đăng ký cùng một mã trong
    /// cùng một khoảnh khắc thì cả hai đều thấy "chưa ai dùng", và chỉ ràng buộc UNIQUE
    /// ở database mới chặn được đứa thứ hai.
    ///
    /// Phép kiểm này tồn tại để người dùng nhận một câu tiếng Việt dễ hiểu thay vì một
    /// lỗi 500. Ca đua nhau vẫn ra 500 — hiếm tới mức chấp nhận được ở lát 1, và đã ghi
    /// vào <c>docs/07-giao-dien/identity/dang-ky.md</c>.
    /// </summary>
    Task<bool> IsCodeTakenAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ai đang là chủ workspace hiện tại. <c>null</c> khi chưa gán ai.
    ///
    /// Chỉ trả về khoá chứ không trả cả <c>Tenant</c>: nơi gọi chỉ cần so một mã, và nạp
    /// cả gốc tổng hợp để so một trường là nạp thừa.
    /// </summary>
    Task<Guid?> GetOwnerUserIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Nạp workspace hiện tại để SỬA nó. Trả <c>null</c> khi phiên chưa đứng ở workspace nào.
    ///
    /// Khác <see cref="GetOwnerUserIdAsync"/> ở chỗ nó nạp cả gốc tổng hợp và có theo dõi
    /// thay đổi — chỉ dùng khi thật sự cần gọi một phương thức làm đổi trạng thái, hiện
    /// tại là <c>Tenant.TransferOwnership</c>. Đọc để SO thì vẫn dùng hàm kia, vì nạp cả
    /// gốc tổng hợp để so một trường là nạp thừa.
    /// </summary>
    Task<Tenant?> GetCurrentForUpdateAsync(CancellationToken cancellationToken = default);
}
