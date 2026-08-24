using System.Security.Cryptography;
using ONoOffice.Identity.Application.Abstractions;

namespace ONoOffice.Identity.Infrastructure.Security;

/// <summary>
/// Sinh mật khẩu tạm cho tài khoản do quản trị viên tạo hộ.
///
/// Mật khẩu này có một ràng buộc mà mật khẩu bình thường không có: <b>người tạo phải đọc
/// được nó qua điện thoại, và người nhận phải gõ lại đúng ngay lần đầu.</b> Lát này chưa
/// có dịch vụ gửi email, nên nó đi qua Zalo, qua lời nói, hoặc qua một mẩu giấy.
///
/// Ba quyết định đến từ đúng ràng buộc đó:
///
/// <list type="number">
/// <item><b>Bảng chữ bỏ ký tự dễ đọc nhầm</b> — không có <c>0/O</c>, <c>1/l/I</c>. Chúng
/// làm người nhận gõ sai, thử ba lần rồi gọi lại hỏi.</item>
/// <item><b>Chia cụm bằng dấu nối</b> — <c>k7np-2wqx-hs4m</c> đọc dễ hơn hẳn
/// <c>k7np2wqxhs4m</c> dù cùng số ký tự. Người đọc không mất dấu giữa chừng.</item>
/// <item><b>Không dùng ký tự đặc biệt</b> — chúng nằm ở chỗ khác nhau trên bàn phím điện
/// thoại, và mật khẩu này sống đúng một lần đăng nhập.</item>
/// </list>
///
/// <b>Về độ mạnh:</b> 12 ký tự từ bảng 31 ký tự cho khoảng 59 bit. Ít hơn một chuỗi ngẫu
/// nhiên đầy đủ, nhưng nó chỉ sống tới lần đăng nhập đầu tiên — <c>MustChangePassword</c>
/// bắt đổi ngay. Đánh đổi có chủ ý: đọc được quan trọng hơn ở đây.
///
/// Dùng <c>RandomNumberGenerator</c> chứ KHÔNG dùng <c>Random</c>: <c>Random</c> gieo từ
/// đồng hồ, nên hai tài khoản tạo trong cùng một mili-giây có thể nhận cùng một mật khẩu.
/// </summary>
internal sealed class TemporaryPasswordGenerator : ITemporaryPasswordGenerator
{
    /// <summary>Không có <c>0 O 1 l I</c> — xem phần tài liệu ở trên.</summary>
    private const string Alphabet = "abcdefghijkmnpqrstuvwxyz23456789";

    private const int GroupCount = 3;
    private const int GroupLength = 4;

    public string Generate()
    {
        var groups = new string[GroupCount];

        for (var i = 0; i < GroupCount; i++)
        {
            groups[i] = new string(RandomNumberGenerator.GetItems<char>(Alphabet, GroupLength));
        }

        return string.Join('-', groups);
    }
}
