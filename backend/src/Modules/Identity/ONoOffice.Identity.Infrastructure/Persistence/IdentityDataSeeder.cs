using Luong.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Domain;
using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.Infrastructure.Persistence;

/// <summary>
/// Dựng một workspace dùng được ngay: chính workspace đó, bốn vai hệ thống, và một chủ
/// sở hữu đăng nhập được.
///
/// <b>Thứ tự không đảo được:</b> phải có <c>Tenant</c> trước (mọi thứ khác mang
/// <c>tenant_id</c> của nó), rồi <c>Role</c> (để có cái mà gán), rồi <c>User</c>, rồi mới
/// gán chủ. Đảo lại thì hoặc vi phạm ràng buộc, hoặc tệ hơn — tạo ra một workspace không
/// có ai vào được, mà cũng không có lỗi nào báo.
///
/// <b>Chạy được nhiều lần:</b> thấy workspace đã tồn tại thì đi ra. Không có tính chất
/// này thì mỗi lần khởi động lại ứng dụng là một lần đâm vào ràng buộc UNIQUE, và app
/// không lên được.
/// </summary>
public sealed class IdentityDataSeeder(
    IdentityDbContext context,
    IPasswordHasher passwordHasher,
    IOptions<SeedOptions> options,
    ILogger<IdentityDataSeeder> logger)
{
    private readonly SeedOptions _options = options.Value;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        _options.Validate();

        if (_options.ApplyMigrations)
        {
            await context.Database.MigrateAsync(cancellationToken);
        }

        // IgnoreQueryFilters không cần cho Tenants (nó không thuộc workspace nào), nhưng
        // toàn bộ phần dưới chạy NGOÀI một request — ICurrentTenant trả "không có ai" —
        // nên mọi truy vấn có lọc tenant ở đây sẽ trắng. Chỉ chạm Tenants là có chủ đích.
        bool daCo = await context.Tenants.AnyAsync(
            t => t.Code == Domain.ValueObjects.TenantCode.Create(_options.WorkspaceCode).Value,
            cancellationToken);

        if (daCo)
        {
            logger.LogInformation(
                "Bỏ qua gieo dữ liệu: workspace '{Ma}' đã tồn tại.", _options.WorkspaceCode);

            return;
        }

        var tenant = Bat(Tenant.Create(_options.WorkspaceCode, _options.WorkspaceName));
        context.Tenants.Add(tenant);

        // Bốn vai hệ thống, đúng định nghĩa ở ADR-0002. Owner phải là vai đầu tiên vì
        // ngay bên dưới ta gán nó cho người tạo workspace.
        var vaiTro = SystemRoles.All.ToDictionary(
            dinhNghia => dinhNghia.Name,
            dinhNghia => Bat(dinhNghia.CreateFor(tenant.Id)),
            StringComparer.OrdinalIgnoreCase);

        context.Roles.AddRange(vaiTro.Values);

        // Băm ở đây, không phải hằng số trong git. Argon2id mất khoảng 100ms — chỉ một
        // lần, ở lần khởi động đầu tiên của một database trống.
        string bam = passwordHasher.Hash(_options.OwnerPassword);

        var chu = Bat(User.Create(tenant.Id, _options.OwnerEmail, bam, _options.OwnerFullName));
        Bat(chu.AssignRole(vaiTro[SystemRoles.Owner.Name].Id));
        context.Users.Add(chu);

        // Gán chủ SAU khi đã có user. Tenant.AssignOwner chỉ nhận lần đầu — lần sau phải
        // đi đường chuyển nhượng, nên không có chuyện gán đè im lặng ở đây.
        Bat(tenant.AssignOwner(chu.Id));

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Đã gieo workspace '{Ma}' với chủ sở hữu {Email} và {SoVai} vai trò hệ thống.",
            _options.WorkspaceCode,
            _options.OwnerEmail,
            vaiTro.Count);
    }

    /// <summary>
    /// Thất bại ở đây là lỗi CẤU HÌNH hoặc lỗi lập trình, không phải thất bại nghiệp vụ
    /// mà người dùng cần đọc — nên ném exception, để nó chết ngay lúc khởi động thay vì
    /// để lại một workspace dựng dở.
    /// </summary>
    private static T Bat<T>(Result<T> result) => result.IsSuccess
        ? result.Value
        : throw new InvalidOperationException($"Gieo dữ liệu thất bại: {result.Error.Code} — {result.Error.Description}");

    private static void Bat(Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Gieo dữ liệu thất bại: {result.Error.Code} — {result.Error.Description}");
        }
    }
}
