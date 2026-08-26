namespace ONoOffice.Comm.Domain.Entities;

/// <summary>
/// Hai kiểu hội thoại — và chúng KHÔNG cùng luật.
///
/// Lưu xuống database bằng SỐ, không bằng tên: đổi tên hằng trong code là chuyện của
/// code, còn dữ liệu cũ thì nằm im. Vì vậy các số dưới đây <b>không bao giờ được đổi</b>.
/// </summary>
public enum ConversationKind
{
    /// <summary>Đúng hai người, cố định mãi mãi. Không tên, không thêm bớt ai.</summary>
    Rieng = 1,

    /// <summary>Có tên, thêm bớt người được, người mới chỉ thấy từ lúc họ vào.</summary>
    Nhom = 2,
}
