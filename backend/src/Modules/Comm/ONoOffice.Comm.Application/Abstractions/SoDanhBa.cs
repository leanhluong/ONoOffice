using ONoOffice.Identity.Contracts;

namespace ONoOffice.Comm.Application.Abstractions;

/// <summary>
/// Sổ tra tên, dựng một lần cho mỗi request.
///
/// Ba handler đều cần cùng một việc: đổi <c>Guid</c> thành tên người. Gom về đây vì phần
/// nguy hiểm của việc đó không phải phép tra, mà là <b>ca không tra được</b> — và mỗi
/// handler tự xử một kiểu thì sẽ có kiểu nổ.
///
/// Chuyện đã xảy ra thật ở màn Thành viên: một <c>ToDictionary</c> gặp khoá trùng làm cả
/// màn trả 500. Ở đây rủi ro đối xứng lại: khoá THIẾU. Tài khoản bị vô hiệu hoá, hoặc hai
/// nguồn lệch nhau một nhịp, và <c>map[id]</c> ném <c>KeyNotFoundException</c> — một hàng
/// hỏng làm trắng xoá toàn bộ màn Trao đổi.
///
/// Nên chỗ này KHÔNG bao giờ ném. Không tra được thì trả một câu người đọc hiểu, và phần
/// còn lại của danh sách vẫn hiện.
/// </summary>
internal sealed class SoDanhBa
{
    /// <summary>
    /// Câu thay thế khi không còn tra được tên.
    ///
    /// Cố ý KHÔNG hiện mã tài khoản: người dùng không đọc được <c>Guid</c>, và nó cũng
    /// chẳng phải thứ họ cần biết. Câu này nói đúng thứ đang xảy ra.
    /// </summary>
    private const string KhongRo = "Người dùng không xác định";

    private readonly Dictionary<Guid, string> _ten;

    private SoDanhBa(Dictionary<Guid, string> ten) => _ten = ten;

    public static async Task<SoDanhBa> MoAsync(
        IUserDirectory users,
        CancellationToken cancellationToken)
    {
        var tatCa = await users.GetAllAsync(cancellationToken);
        var ten = new Dictionary<Guid, string>(tatCa.Count);

        // Vòng lặp tay chứ không `ToDictionary`: `ToDictionary` ném khi gặp khoá trùng, và
        // một mã tài khoản xuất hiện hai lần là chuyện của dữ liệu, không phải của màn
        // hình đang hiển thị nó.
        foreach (var u in tatCa)
        {
            ten[u.Id] = u.FullName;
        }

        return new SoDanhBa(ten);
    }

    public string this[Guid id] => _ten.GetValueOrDefault(id, KhongRo);

    public string? Cua(Guid? id) => id is { } co ? this[co] : null;
}
