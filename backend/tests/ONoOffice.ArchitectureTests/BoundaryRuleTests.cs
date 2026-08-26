namespace ONoOffice.ArchitectureTests;

/// <summary>
/// Bốn luật ranh giới ghi ở docs/02-kien-truc/README.md.
///
/// Có mấy test này thì luật không còn là điều phải nhớ lúc review — vi phạm là ĐỎ ngay,
/// kèm đúng tên project và đúng thứ nó không được phép tham chiếu.
/// </summary>
public class BoundaryRuleTests
{
    private static readonly string[] InfrastructurePackages =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Npgsql",
        "StackExchange.Redis",
        "RabbitMQ.Client",
        "Hangfire",
        "Luong.Kernel.EntityFrameworkCore",
        "Luong.Kernel.AspNetCore",
    ];

    private static IReadOnlyList<string> Modules => SolutionGraph.Modules;

    /// <summary>
    /// Bẫy tự thân: phép đọc module phải TÌM THẤY chúng.
    ///
    /// Trượt hết thì <c>Module_ChiDuocThayContractsCuaModuleKhac</c> bỏ qua mọi project
    /// và vẫn xanh — đúng cái cách nó đã bỏ qua module <c>Comm</c> suốt, hồi danh sách
    /// còn viết cứng là <c>["Identity", "Org"]</c>. Con số này phải TĂNG theo số module;
    /// để <c>&gt;= 1</c> thì một phép đọc chỉ còn thấy Identity vẫn qua được.
    /// </summary>
    [Fact]
    public void PhepDocModule_TimThayDuBaModule()
    {
        Assert.True(
            Modules.Count >= 3,
            $"Chỉ đọc ra {Modules.Count} module ({string.Join(", ", Modules)}), "
                + "trong khi solution có ít nhất 3.");
    }

    // ── LUẬT 4 ───────────────────────────────────────────────────────────────
    // Domain là trái tim: nó giữ luật nghiệp vụ và KHÔNG được biết ngoài kia có
    // database hay có HTTP. Biết rồi thì không test được nghiệp vụ nếu không dựng
    // hạ tầng, và nghiệp vụ bắt đầu bị uốn theo thứ hạ tầng làm được.
    [Fact]
    public void Domain_KhongThamChieuBatKyHaTangNao()
    {
        foreach (var project in SolutionGraph.InLayer("Domain"))
        {
            var viPham = project.AllReferences
                .Where(r => InfrastructurePackages.Any(p => r.StartsWith(p, StringComparison.Ordinal)))
                .ToList();

            Assert.True(
                viPham.Count == 0,
                $"{project.Name} đang tham chiếu hạ tầng: {string.Join(", ", viPham)}. "
                    + "Domain chỉ được tham chiếu Luong.Kernel.");
        }
    }

    [Fact]
    public void Domain_KhongThamChieuProjectNaoKhac()
    {
        foreach (var project in SolutionGraph.InLayer("Domain"))
        {
            Assert.True(
                project.ProjectReferences.Count == 0,
                $"{project.Name} tham chiếu {string.Join(", ", project.ProjectReferences)}. "
                    + "Domain phải đứng một mình.");
        }
    }

    // ── Mũi tên chỉ đi VÀO TRONG ─────────────────────────────────────────────
    // Application điều phối use case; nó khai ra các CỔNG và để Infrastructure cài.
    // Nếu Application biết Infrastructure thì mũi tên quay ngược, tầng trong lại phụ
    // thuộc tầng ngoài — đúng thứ Clean Architecture cấm.
    [Fact]
    public void Application_KhongThamChieuInfrastructure()
    {
        foreach (var project in SolutionGraph.InLayer("Application"))
        {
            var viPham = project.AllReferences
                .Where(r => r.EndsWith(".Infrastructure", StringComparison.Ordinal))
                .ToList();

            Assert.True(
                viPham.Count == 0,
                $"{project.Name} tham chiếu {string.Join(", ", viPham)}. "
                    + "Infrastructure biết Application, không có chiều ngược lại.");
        }
    }

    // ── LUẬT 1 ───────────────────────────────────────────────────────────────
    // Module chỉ được thấy MẶT TIỀN (Contracts) của module khác. Thấy Domain của nhau
    // là hai module dính chặt, và ngày muốn tách một cái ra thành dịch vụ riêng sẽ là
    // ngày viết lại.
    [Fact]
    public void Module_ChiDuocThayContractsCuaModuleKhac()
    {
        foreach (var project in SolutionGraph.Projects)
        {
            string? chuSoHuu = Modules.FirstOrDefault(m =>
                project.Name.StartsWith($"ONoOffice.{m}.", StringComparison.Ordinal));

            if (chuSoHuu is null)
            {
                continue; // ONoOffice.Api được phép ráp mọi module lại
            }

            var viPham = project.ProjectReferences
                .Where(r => Modules.Any(m =>
                    m != chuSoHuu && r.StartsWith($"ONoOffice.{m}.", StringComparison.Ordinal)))
                .Where(r => !r.EndsWith(".Contracts", StringComparison.Ordinal))
                .ToList();

            Assert.True(
                viPham.Count == 0,
                $"{project.Name} chạm vào ruột module khác: {string.Join(", ", viPham)}. "
                    + "Chỉ được tham chiếu project .Contracts của module khác.");
        }
    }

    // ── Contracts phải nhẹ ───────────────────────────────────────────────────
    // Mặt tiền mà kéo theo Domain thì mọi module gọi tới cũng kéo theo Domain, và
    // "chỉ thấy mặt tiền" thành vô nghĩa.
    [Fact]
    public void Contracts_KhongKeoTheoProjectNaoKhac()
    {
        foreach (var project in SolutionGraph.InLayer("Contracts"))
        {
            Assert.True(
                project.ProjectReferences.Count == 0,
                $"{project.Name} tham chiếu {string.Join(", ", project.ProjectReferences)}. "
                    + "Contracts chỉ được chứa interface + DTO, không kéo theo gì cả.");
        }
    }

    // Bẫy tự thân: nếu đường dẫn hỏng thì mọi test trên đều xanh vì không có gì để duyệt.
    // Test này bảo đảm bộ đọc thật sự tìm thấy project.
    [Fact]
    public void BoDocTimThayDuMoiProject()
    {
        Assert.True(
            SolutionGraph.Projects.Count >= 9,
            $"Chỉ đọc được {SolutionGraph.Projects.Count} project — nhiều khả năng đường dẫn hỏng.");
    }
}
