using System.Text.RegularExpressions;

namespace ONoOffice.ArchitectureTests;

/// <summary>
/// Canh luật <b>"action chỉ được một dòng"</b> — luật ghi ở <c>HANDOFF.md</c> và ở
/// đầu <c>AuthController</c>.
///
/// <b>Vì sao luật này cần một cái test canh, chứ không phải chỉ cần nhớ:</b> cái bẫy của
/// Controller không phải hiệu năng — mà là nó <i>quá tiện</i> để nhét logic vào. Ai cũng
/// biết không nên, và ai cũng sẽ nhét đúng một lần, với lý do "chỉ chỗ này thôi, có mỗi
/// một câu if". Chỗ thứ hai thì đã có tiền lệ để viện. Sáu tháng sau nghiệp vụ nằm rải
/// ở hai mươi controller, và không test đơn vị nào chạm tới được — muốn chạy chúng thì
/// phải dựng cả một máy chủ HTTP.
///
/// Luật vì thế phải là thứ ĐỎ ngay lúc build, không phải thứ trông chờ vào người review
/// nhìn thấy.
/// </summary>
public partial class ControllerRuleTests
{
    /// <summary>
    /// Từ khoá điều khiển luồng. Có mặt một trong số này ở Controller nghĩa là đang có
    /// một quyết định được đưa ra ở đây — mà mọi quyết định đều thuộc về Application.
    /// </summary>
    private static readonly string[] TuKhoaCam = ["if", "else", "for", "foreach", "while", "switch", "try", "catch"];

    [Fact]
    public void MoiAction_DeuLaMotBieuThucDuyNhat()
    {
        var viPham = new List<string>();

        foreach (var (ten, noiDung) in DocControllers())
        {
            foreach (Match match in ThanAction().Matches(noiDung))
            {
                if (match.Groups[1].Value != "=>")
                {
                    viPham.Add($"{ten}: {RutGon(match.Value)}");
                }
            }
        }

        Assert.True(
            viPham.Count == 0,
            "Có action dùng thân dạng khối { } thay vì một biểu thức. Khối là chỗ để nhét "
                + "thêm câu lệnh, và nó sẽ được nhét:\n  " + string.Join("\n  ", viPham));
    }

    [Fact]
    public void Controller_KhongChuaMotCauDieuKhienNao()
    {
        var viPham = new List<string>();

        foreach (var (ten, noiDung) in DocControllers())
        {
            viPham.AddRange(TuKhoaCam
                .Where(tuKhoa => Regex.IsMatch(noiDung, $@"\b{tuKhoa}\b\s*[\(\{{]"))
                .Select(tuKhoa => $"{ten}: '{tuKhoa}'"));
        }

        Assert.True(
            viPham.Count == 0,
            "Controller đang tự quyết định điều gì đó. Quyết định thuộc về Application — "
                + "ở đó nó test được mà không cần dựng máy chủ HTTP:\n  " + string.Join("\n  ", viPham));
    }

    /// <summary>
    /// Controller không được tự chọn mã HTTP.
    ///
    /// Mã HTTP phải suy ra từ <c>ErrorType</c> của kết quả, qua <c>ToActionResult()</c>.
    /// Để mỗi action tự chọn thì cùng một loại thất bại sẽ ra 400 ở chỗ này, 409 ở chỗ
    /// kia, tuỳ người viết — và frontend phải chiều từng endpoint một.
    /// </summary>
    [Fact]
    public void Controller_KhongTuChonMaHttp()
    {
        string[] camGoi = ["StatusCode(", "BadRequest(", "NotFound(", "Conflict(", "Unauthorized(", "Forbid("];

        var viPham = (from tep in DocControllers()
                      from goi in camGoi
                      where tep.NoiDung.Contains(goi, StringComparison.Ordinal)
                      select $"{tep.Ten}: {goi})").ToList();

        Assert.True(
            viPham.Count == 0,
            "Controller đang tự chọn mã HTTP thay vì để ToActionResult() suy ra từ loại lỗi:\n  "
                + string.Join("\n  ", viPham));
    }

    // Bẫy tự thân: đường dẫn hỏng thì ba test trên đều xanh vì không có gì để duyệt.
    [Fact]
    public void BoDoc_TimThayControllerVaAction()
    {
        var tep = DocControllers();

        Assert.NotEmpty(tep);

        int soAction = tep.Sum(t => ThanAction().Matches(t.NoiDung).Count);

        Assert.True(soAction >= 3, $"Chỉ đọc được {soAction} action — nhiều khả năng biểu thức tìm kiếm hỏng.");
    }

    // ── Đọc nguồn ────────────────────────────────────────────────────────────

    /// <summary>
    /// Đọc mã nguồn chứ không dùng phản chiếu: phản chiếu thấy được chữ ký của phương
    /// thức nhưng KHÔNG thấy được bên trong nó có gì. Mà luật ở đây nói về chính cái
    /// bên trong đó.
    /// </summary>
    private static List<(string Ten, string NoiDung)> DocControllers()
    {
        string thuMuc = Path.Combine(SolutionRoot(), "src", "ONoOffice.Api", "Controllers");

        if (!Directory.Exists(thuMuc))
        {
            return [];
        }

        return [.. Directory
            .EnumerateFiles(thuMuc, "*.cs", SearchOption.AllDirectories)
            .Select(duongDan => (Path.GetFileName(duongDan), BoChuThich(File.ReadAllText(duongDan))))];
    }

    /// <summary>
    /// Bỏ chú thích trước khi soi.
    ///
    /// Không bỏ thì chính đoạn tài liệu giải thích luật — "không <c>if</c>, không
    /// <c>try/catch</c>" — sẽ làm test đỏ. Test đỏ vì câu văn mô tả luật là kiểu sai
    /// khiến người ta mất niềm tin vào cả bộ test.
    /// </summary>
    private static string BoChuThich(string nguon) =>
        Regex.Replace(Regex.Replace(nguon, @"/\*[\s\S]*?\*/", " "), @"//[^\n]*", " ");

    private static string RutGon(string doan) =>
        Regex.Replace(doan, @"\s+", " ").Trim() is var gon && gon.Length > 90 ? gon[..90] + "…" : gon;

    private static string SolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ONoOffice.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Không tìm được gốc solution.");
    }

    /// <summary>
    /// Bắt từ thuộc tính <c>[Http...]</c> tới ký tự mở thân phương thức, và giữ lại
    /// chính ký tự đó: <c>=&gt;</c> là một biểu thức, <c>{</c> là một khối.
    /// </summary>
    [GeneratedRegex(@"\[Http\w+[^\]]*\]\s*(?:\[[^\]]*\]\s*)*public\s+[^{;=]*\)\s*(=>|\{)")]
    private static partial Regex ThanAction();
}
