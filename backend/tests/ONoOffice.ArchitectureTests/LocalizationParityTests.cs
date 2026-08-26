using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ONoOffice.ArchitectureTests;

/// <summary>
/// Đối chiếu ba nguồn phải luôn khớp nhau:
///
/// <code>
///   IdentityErrors.cs   ↔   Messages.vi.resx   ↔   Messages.en.resx
/// </code>
///
/// <b>Vì sao đáng viết:</b> NextX — hệ đang chạy thật — có <b>129 khoá khai trong code
/// nhưng 18 khoá không có bản dịch nào cả</b>. Chuỗi dự phòng của họ kết thúc bằng
/// <c>?? key</c>, nên khi những lỗi đó xảy ra, người dùng nhìn thấy đúng chuỗi
/// <c>dms.checklist.template_not_found</c> trên màn hình.
///
/// Đó không phải lỗi của định dạng file — JSON cũng lệch y hệt. Đó là lỗi của việc
/// <b>không có test này</b>.
/// </summary>
public partial class LocalizationParityTests
{
    private static readonly string[] Languages = ["vi", "en"];

    /// <summary>
    /// Mã lỗi sinh ra từ <c>Luong.Kernel</c>, không phải từ module nghiệp vụ.
    ///
    /// Liệt kê tay chứ không đọc mã nguồn kernel, vì khi CI build với
    /// <c>-p:UseLocalKernel=false</c> thì kernel là một gói NuGet — không còn file
    /// <c>.cs</c> nào để đọc.
    ///
    /// Danh sách này ngắn và ổn định. Kernel thêm mã mới mà quên cập nhật ở đây thì test
    /// đỏ — và đỏ là ĐÚNG: mã mới đó cũng cần bản dịch.
    /// </summary>
    private static readonly string[] KernelCodes =
    [
        "Server.Unexpected",      // ExceptionHandlingMiddleware — lỗi lọt lưới
        "Validation.Multiple",    // ValidationError — gói nhiều lỗi con
    ];

    [Fact]
    public void MoiMaLoiKhaiTrongCode_DeuCoBanDich()
    {
        var codes = ErrorCodesFromSource();
        var thieu = new List<string>();

        foreach (string lang in Languages)
        {
            var keys = KeysFromResx(lang);
            thieu.AddRange(codes.Except(keys).Select(code => $"{lang}: {code}"));
        }

        Assert.True(
            thieu.Count == 0,
            $"Có {thieu.Count} mã lỗi khai trong code nhưng thiếu bản dịch — "
                + "người dùng sẽ nhìn thấy câu mặc định thay vì đúng ngôn ngữ của họ:\n  "
                + string.Join("\n  ", thieu));
    }

    // Chiều ngược lại cũng phải canh: bản dịch còn sót lại của mã đã xoá khỏi code là
    // rác — không ai dám xoá vì không biết còn ai dùng, và nó cứ nằm đó mãi.
    [Fact]
    public void MoiBanDich_DeuUngVoiMotMaLoiCoThat()
    {
        var codes = ErrorCodesFromSource();
        var thua = new List<string>();

        foreach (string lang in Languages)
        {
            thua.AddRange(KeysFromResx(lang).Except(codes).Select(key => $"{lang}: {key}"));
        }

        Assert.True(
            thua.Count == 0,
            $"Có {thua.Count} bản dịch không ứng với mã lỗi nào trong code:\n  " + string.Join("\n  ", thua));
    }

    [Fact]
    public void HaiNgonNgu_CoDungCungBoKhoa()
    {
        var vi = KeysFromResx("vi");
        var en = KeysFromResx("en");

        Assert.Equal(vi.OrderBy(k => k), en.OrderBy(k => k));
    }

    // Bẫy tự thân: đường dẫn hỏng thì mọi test trên đều xanh vì không có gì để đối chiếu.
    [Fact]
    public void BoDoc_TimThayDuNguon()
    {
        Assert.True(ErrorCodesFromSource().Count >= 30);
        Assert.True(KeysFromResx("vi").Count >= 30);
    }

    // ── Đọc nguồn ────────────────────────────────────────────────────────────

    /// <summary>
    /// Mọi file <c>*Errors.cs</c> của MỌI module.
    ///
    /// <b>Bản đầu chỉ đọc <c>IdentityErrors.cs</c>, và đó là một lỗ hổng thật:</b> khi
    /// module Org ra đời, 22 mã lỗi của nó không có một bản dịch nào — cả ba test trên
    /// vẫn xanh, vì chúng không biết file đó tồn tại. Người dùng tiếng Anh sẽ nhận câu
    /// tiếng Việt viết cứng trong <c>Error.Conflict(...)</c>.
    ///
    /// <b>Và bản thứ hai — danh sách khai tay — cũng là một lỗ hổng thật:</b> chú thích
    /// trên đây đã hứa "quét theo mẫu tên", nhưng code bên dưới lại là một mảng cứng hai
    /// dòng. Khi module Comm ra đời, 12 mã lỗi của nó lặng lẽ không được đối chiếu — đúng
    /// y cái cách module Org đã lọt lưới, chỉ khác là lần này chú thích còn nói dối rằng
    /// chuyện đó không thể xảy ra.
    ///
    /// Nay nó QUÉT thật. Thêm module thứ tư mà quên dịch thì test đỏ ngay, không cần ai
    /// nhớ sửa chỗ này.
    /// </summary>
    private static string[] ErrorSources() =>
        [.. Directory
            .EnumerateFiles(
                Path.Combine(SolutionRoot(), "src", "Modules"),
                "*Errors.cs",
                SearchOption.AllDirectories)
            // Chỉ lấy tầng Domain: mã lỗi sống ở đó, còn `bin`/`obj` thì chứa bản sao của
            // chính những file này và sẽ làm mỗi mã bị đếm hai lần.
            .Where(p => p.Contains($"{Path.DirectorySeparatorChar}ONoOffice.", StringComparison.Ordinal)
                && p.Contains(".Domain", StringComparison.Ordinal)
                && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)];

    /// <summary>
    /// Đọc THẲNG mã nguồn thay vì dùng phản chiếu.
    ///
    /// Phản chiếu chỉ thấy được những mã đã biên dịch vào assembly, nên nếu ai đó khai
    /// mã ở một file khác thì test này im lặng bỏ qua. Đọc mã nguồn thì thấy đúng thứ
    /// lập trình viên vừa gõ.
    /// </summary>
    private static HashSet<string> ErrorCodesFromSource()
    {
        var codes = ErrorSources()
            .SelectMany(path => ErrorCall().Matches(File.ReadAllText(path)))
            .Select(m => m.Groups[1].Value);

        return codes.Concat(KernelCodes).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// Bẫy tự thân số hai: mỗi file nguồn phải THẬT SỰ đọc được.
    ///
    /// Phép quét trượt hết thì <c>SelectMany</c> ở trên chỉ đơn giản là đóng góp 0 mã, và
    /// cả ba test đối chiếu vẫn xanh — đúng cái cách module Org lọt lưới suốt.
    ///
    /// Con số dưới đây phải TĂNG theo số module. Để nó là <c>>= 1</c> thì một phép quét
    /// chỉ còn tìm thấy đúng <c>IdentityErrors.cs</c> vẫn qua được, và ta lại trở về đúng
    /// chỗ cũ.
    /// </summary>
    [Fact]
    public void MoiFileMaLoi_DeuDocDuoc()
    {
        var sources = ErrorSources();

        Assert.True(
            sources.Length >= 3,
            "Phép quét chỉ tìm thấy "
                + $"{sources.Length} file *Errors.cs, trong khi có ít nhất 3 module: "
                + string.Join(", ", sources));

        foreach (string path in sources)
        {
            Assert.True(
                ErrorCall().Matches(File.ReadAllText(path)).Count >= 5,
                $"File {Path.GetFileName(path)} khai quá ít mã lỗi — "
                    + "nhiều khả năng biểu thức đọc đã hỏng.");
        }
    }

    private static HashSet<string> KeysFromResx(string language)
    {
        string path = Path.Combine(SolutionRoot(), "src", "ONoOffice.Api", "Resources", $"Messages.{language}.resx");

        return XDocument.Load(path)
            .Root!
            .Elements("data")
            .Select(e => e.Attribute("name")!.Value)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string SolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ONoOffice.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Không tìm được gốc solution.");
    }

    [GeneratedRegex("""Error\.\w+\(\s*"([^"]+)"\s*,""")]
    private static partial Regex ErrorCall();
}
