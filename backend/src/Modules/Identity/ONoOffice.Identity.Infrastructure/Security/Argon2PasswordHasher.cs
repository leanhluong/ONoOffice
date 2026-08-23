using Isopoh.Cryptography.Argon2;
using ONoOffice.Identity.Application.Abstractions;

namespace ONoOffice.Identity.Infrastructure.Security;

/// <summary>
/// Băm mật khẩu bằng Argon2id.
///
/// <b>Vì sao không dùng SHA-256:</b> hàm băm thường được thiết kế để chạy NHANH — đó
/// chính xác là điều KHÔNG muốn cho mật khẩu. Nhanh nghĩa là card đồ hoạ dò được hàng tỉ
/// tổ hợp mỗi giây. Argon2id cố tình chậm, và quan trọng hơn: nó ngốn BỘ NHỚ.
///
/// Ngốn bộ nhớ mới là điểm mấu chốt. Máy đào GPU/ASIC mạnh vì chạy hàng nghìn nhân song
/// song, nhưng mỗi nhân lại rất ít RAM. Bắt mỗi phép băm chiếm 19 MiB là cắt phăng lợi
/// thế đó — thứ mà PBKDF2 hay bcrypt (chỉ ngốn CPU) không làm được.
/// </summary>
internal sealed class Argon2PasswordHasher : IPasswordHasher
{
    // Mức OWASP khuyến nghị tại thời điểm viết. NÊN kiểm lại định kỳ — khuyến nghị
    // tăng dần theo tốc độ phần cứng.
    private const int MemoryCostKib = 19456;   // 19 MiB
    private const int TimeCost = 2;            // số vòng
    private const int Parallelism = 1;

    public string Hash(string password) =>
        Argon2.Hash(
            password: password,
            timeCost: TimeCost,
            memoryCost: MemoryCostKib,
            parallelism: Parallelism,
            type: Argon2Type.HybridAddressing);   // HybridAddressing = Argon2id

    /// <summary>
    /// Chuỗi băm mang theo cả muối và tham số, nên xác minh không cần đọc gì thêm —
    /// và nhờ vậy đổi tham số về sau không làm hỏng mật khẩu cũ.
    /// </summary>
    public bool Verify(string password, string passwordHash)
    {
        try
        {
            return Argon2.Verify(passwordHash, password);
        }
        catch (Exception)
        {
            // Chuỗi băm hỏng (dữ liệu cũ, bị sửa tay) thì coi như SAI mật khẩu, không
            // để exception bay lên thành lỗi 500. Người dùng chỉ cần biết "không đăng
            // nhập được"; còn 500 thì tố giác rằng tài khoản này có gì đó bất thường.
            return false;
        }
    }
}
