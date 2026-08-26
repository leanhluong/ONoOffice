using Luong.Kernel.Abstractions;
using Microsoft.EntityFrameworkCore;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Domain.Entities;
using ONoOffice.Identity.Domain.ValueObjects;

namespace ONoOffice.Identity.Infrastructure.Persistence.Repositories;

internal sealed class EfTenantRepository(IdentityDbContext context, ICurrentTenant currentTenant)
    : ITenantRepository
{
    public void Add(Tenant tenant) => context.Tenants.Add(tenant);

    public Task<bool> IsCodeTakenAsync(string code, CancellationToken cancellationToken = default)
    {
        var parsed = TenantCode.Create(code);

        // Mã sai định dạng thì không thể trùng với ai — khỏi hỏi database.
        if (parsed.IsFailure)
        {
            return Task.FromResult(false);
        }

        // Tenant KHÔNG phải ITenantScoped nên không có bộ lọc tenant nào để bỏ qua —
        // đây là bảng đứng trên mọi workspace. Đăng ký cũng chạy khi chưa có phiên nào.
        return context.Tenants.AnyAsync(t => t.Code == parsed.Value, cancellationToken);
    }

    /// <summary>
    /// Ai đang là chủ workspace hiện tại.
    ///
    /// ⚠️ Phải nêu ĐÍCH DANH mã workspace. Bình luận đầu tiên ở đây từng nói "bộ lọc theo
    /// tenant giới hạn sẵn rồi" — <b>sai</b>: <c>Tenant</c> không cài <c>ITenantScoped</c>,
    /// vì nó CHÍNH LÀ workspace chứ không thuộc về workspace nào. Bảng này không có bộ lọc
    /// nào cả.
    ///
    /// Hậu quả nếu thiếu điều kiện: truy vấn trả về chủ sở hữu của một workspace bất kỳ,
    /// nên hai luật "không khoá chủ sở hữu" và "không đổi vai chủ sở hữu" sẽ bảo vệ nhầm
    /// người — và bỏ trống đúng người cần bảo vệ. Một test trên database thật đã bắt được
    /// chuyện này.
    /// </summary>
    public async Task<Guid?> GetOwnerUserIdAsync(CancellationToken cancellationToken = default)
    {
        if (currentTenant.TenantId is not { } tenantId)
        {
            return null;
        }

        return await context.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.Id == tenantId)
            .Select(tenant => tenant.OwnerUserId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// KHÔNG có <c>AsNoTracking</c> — cố ý, và đây là khác biệt duy nhất so với hàm trên.
    ///
    /// Nơi gọi sẽ đổi trạng thái gốc tổng hợp (<c>Tenant.TransferOwnership</c>), nên EF
    /// phải theo dõi nó thì câu <c>UPDATE</c> mới được sinh ra lúc chốt giao dịch. Thêm
    /// <c>AsNoTracking</c> ở đây thì lệnh chuyển nhượng chạy êm ru và <b>không lưu gì</b>.
    /// </summary>
    public async Task<Tenant?> GetCurrentForUpdateAsync(CancellationToken cancellationToken = default)
    {
        if (currentTenant.TenantId is not { } tenantId)
        {
            return null;
        }

        return await context.Tenants.FirstOrDefaultAsync(
            tenant => tenant.Id == tenantId,
            cancellationToken);
    }
}
