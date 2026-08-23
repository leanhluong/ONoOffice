using System.Xml.Linq;

namespace ONoOffice.ArchitectureTests;

/// <summary>Một project trong solution, cùng danh sách nó tham chiếu tới.</summary>
internal sealed record ProjectInfo(
    string Name,
    IReadOnlyList<string> ProjectReferences,
    IReadOnlyList<string> PackageReferences)
{
    public IEnumerable<string> AllReferences => ProjectReferences.Concat(PackageReferences);
}

/// <summary>
/// Đọc thẳng các file .csproj để dựng lại đồ thị phụ thuộc.
///
/// Vì sao đọc .csproj chứ không soi assembly đã biên dịch: ta muốn bắt lỗi ở đúng chỗ
/// nó được TẠO RA — dòng ProjectReference mà ai đó vừa thêm — chứ không phải ở một hệ
/// quả xa xôi. Thông báo lỗi vì thế chỉ thẳng vào file cần sửa.
/// </summary>
internal static class SolutionGraph
{
    private static readonly Lazy<IReadOnlyList<ProjectInfo>> Cache = new(Load);

    public static IReadOnlyList<ProjectInfo> Projects => Cache.Value;

    public static IEnumerable<ProjectInfo> InLayer(string layer) =>
        Projects.Where(p => p.Name.EndsWith("." + layer, StringComparison.Ordinal));

    private static IReadOnlyList<ProjectInfo> Load()
    {
        string root = FindSolutionRoot();

        return [.. Directory
            .EnumerateFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Select(Parse)];
    }

    private static ProjectInfo Parse(string path)
    {
        var document = XDocument.Load(path);

        var projectReferences = document.Descendants("ProjectReference")
            .Select(e => Path.GetFileNameWithoutExtension(e.Attribute("Include")!.Value.Replace('\\', '/')))
            .ToList();

        // KernelReference là item riêng của repo này (xem Directory.Build.targets). Nó
        // trở thành ProjectReference hay PackageReference tuỳ công tắc, nên xét luật
        // ranh giới thì coi nó như một package.
        var packageReferences = document.Descendants("PackageReference")
            .Concat(document.Descendants("KernelReference"))
            .Select(e => e.Attribute("Include")!.Value)
            .ToList();

        return new ProjectInfo(Path.GetFileNameWithoutExtension(path), projectReferences, packageReferences);
    }

    private static string FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ONoOffice.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Không tìm được thư mục gốc solution (ONoOffice.slnx).");
    }
}
