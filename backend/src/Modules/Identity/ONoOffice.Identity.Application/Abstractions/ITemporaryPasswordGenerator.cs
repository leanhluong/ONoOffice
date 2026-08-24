namespace ONoOffice.Identity.Application.Abstractions;

/// <summary>
/// Sinh mật khẩu tạm cho tài khoản do quản trị viên tạo hộ.
///
/// <b>Vì sao là một cổng chứ không phải một hàm tiện ích:</b> bản cài thật phải dùng bộ
/// sinh số ngẫu nhiên AN TOÀN MẬT MÃ. <c>Random</c> thường gieo từ đồng hồ, nên hai tài
/// khoản tạo cùng một giây có thể nhận cùng một mật khẩu — và ai đoán được thời điểm tạo
/// thì đoán được mật khẩu. Tách ra thành cổng để chỗ đó có một cái tên, và để test không
/// phải đoán một chuỗi ngẫu nhiên.
/// </summary>
public interface ITemporaryPasswordGenerator
{
    /// <summary>Chuỗi thô. Người gọi băm ngay và <b>không được lưu lại bản thô</b>.</summary>
    string Generate();
}
