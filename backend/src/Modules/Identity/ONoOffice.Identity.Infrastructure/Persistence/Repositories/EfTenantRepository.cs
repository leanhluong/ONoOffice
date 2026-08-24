using Microsoft.EntityFrameworkCore;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Domain.Entities;
using ONoOffice.Identity.Domain.ValueObjects;

namespace ONoOffice.Identity.Infrastructure.Persistence.Repositories;

internal sealed class EfTenantRepository(IdentityDbContext context) : ITenantRepository
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
}
