namespace ONoOffice.Identity.Infrastructure.Persistence;

/// <summary>
/// Cấu hình gieo dữ liệu mồi.
///
/// <b>Vì sao gieo bằng code chạy lúc khởi động, chứ không bằng <c>HasData</c> của EF:</b>
/// <c>HasData</c> nhúng dữ liệu thẳng vào file migration, nên chuỗi băm mật khẩu phải là
/// một <b>hằng số nằm trong git</b>. Nghĩa là mọi môi trường — kể cả máy chủ thật — sinh
/// ra với sẵn một tài khoản mà bất kỳ ai đọc repo đều biết mật khẩu. Không có cách nào
/// gỡ chuyện đó ra khỏi lịch sử git, và cũng không có cách nào để nó chỉ áp cho môi
/// trường phát triển.
///
/// Ở đây thì ngược lại: mặc định <see cref="Enabled"/> là <c>false</c>. Máy chủ thật
/// không bật thì không có tài khoản nào được tạo cả.
/// </summary>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>
    /// Mặc định TẮT. Bật ở <c>appsettings.Development.json</c> và trong test.
    ///
    /// Mặc định phải là "không làm gì": người quên cấu hình thì nhận một hệ thống trống,
    /// còn hơn nhận một hệ thống có cửa sau.
    /// </summary>
    public bool Enabled { get; set; }

    public string WorkspaceCode { get; set; } = "demo";

    public string WorkspaceName { get; set; } = "Workspace Demo";

    public string OwnerEmail { get; set; } = string.Empty;

    public string OwnerPassword { get; set; } = string.Empty;

    public string OwnerFullName { get; set; } = string.Empty;

    /// <summary>
    /// Chạy migration trước khi gieo.
    ///
    /// Tách thành công tắc riêng vì hai việc này hỏng theo hai kiểu khác nhau: gieo nhầm
    /// chỉ tạo dữ liệu thừa, còn migration tự chạy ở máy chủ thật là chuyện khác hẳn —
    /// hai instance khởi động cùng lúc sẽ chạy song song vào cùng một database, và một
    /// migration hỏng làm cả ứng dụng không lên được.
    /// </summary>
    public bool ApplyMigrations { get; set; } = true;

    /// <summary>Chết sớm với thông báo nói rõ thiếu gì, thay vì gieo ra một tài khoản hỏng.</summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(OwnerEmail) || string.IsNullOrWhiteSpace(OwnerPassword))
        {
            throw new InvalidOperationException(
                $"Đã bật '{SectionName}:Enabled' nhưng thiếu OwnerEmail hoặc OwnerPassword.");
        }

        if (string.IsNullOrWhiteSpace(OwnerFullName))
        {
            throw new InvalidOperationException($"Đã bật '{SectionName}:Enabled' nhưng thiếu OwnerFullName.");
        }
    }
}
