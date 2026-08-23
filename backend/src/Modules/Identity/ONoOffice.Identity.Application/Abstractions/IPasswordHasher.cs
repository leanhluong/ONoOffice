namespace ONoOffice.Identity.Application.Abstractions;

/// <summary>
/// Cổng băm mật khẩu. Bản cài đặt (Argon2id) nằm ở <c>Infrastructure</c>.
///
/// Tầng này KHÔNG biết thuật toán là gì — và đó là điểm mấu chốt: hôm nào Argon2id bị
/// coi là yếu, đổi bản cài đặt là xong, không đụng một dòng nghiệp vụ nào.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}
