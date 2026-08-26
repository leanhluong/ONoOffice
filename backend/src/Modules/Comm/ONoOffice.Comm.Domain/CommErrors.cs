using Luong.Kernel.Primitives;

namespace ONoOffice.Comm.Domain;

/// <summary>
/// Mã lỗi của module Trao đổi.
///
/// Cùng quy ước với <c>IdentityErrors</c> và <c>OrgErrors</c>: mã dạng <c>Vùng.ChuyệnGì</c>,
/// và mã đó CHÍNH LÀ khoá tra bản dịch ở <c>Messages.*.resx</c>. Trùng khít như vậy thì
/// frontend không cần bảng ánh xạ trung gian, và không có chỗ nào để lệch.
///
/// ⚠️ Thêm mã ở đây mà quên bản dịch thì <c>LocalizationParityTests</c> đỏ ngay.
/// </summary>
public static class CommErrors
{
    public static class Conversations
    {
        public static readonly Error NotFound =
            Error.NotFound("Conversation.NotFound", "Không tìm thấy hội thoại này.");

        public static readonly Error TenantRequired =
            Error.Validation("Conversation.TenantRequired", "Hội thoại phải thuộc về một workspace.");

        public static readonly Error NameEmpty =
            Error.Validation("Conversation.NameEmpty", "Tên nhóm không được để trống.");

        public static readonly Error NameTooLong =
            Error.Validation("Conversation.NameTooLong", "Tên nhóm không được dài quá 120 ký tự.");

        public static readonly Error CannotChatWithSelf = Error.Validation(
            "Conversation.CannotChatWithSelf",
            "Không thể mở hội thoại riêng với chính mình.");

        /// <summary>
        /// Hội thoại riêng có đúng hai người, cố định mãi mãi.
        ///
        /// Thêm người thứ ba nghĩa là hai người kia bỗng dưng có một khán giả đọc được
        /// toàn bộ những gì họ đã nói khi tưởng chỉ có hai. Muốn thêm thì mở một nhóm mới —
        /// một bước cố ý, và cả hai nhìn thấy nó xảy ra.
        /// </summary>
        public static readonly Error DirectIsFixed = Error.Conflict(
            "Conversation.DirectIsFixed",
            "Hội thoại riêng chỉ có hai người và không đổi được. Hãy mở một nhóm mới.");

        public static readonly Error AlreadyIn =
            Error.Conflict("Conversation.AlreadyIn", "Người này đã ở trong hội thoại.");

        /// <summary>
        /// Nhóm phải có ít nhất một người khác.
        ///
        /// Về mặt kỹ thuật một nhóm chỉ có mình mình chạy được, và nhiều ứng dụng dùng nó
        /// làm chỗ ghi chú riêng. Nhưng thứ đó phải là một tính năng CỐ Ý — có tên, có chỗ
        /// đứng trên giao diện — chứ không phải sản phẩm phụ của một luật bị quên.
        /// </summary>
        public static readonly Error GroupNeedsSomeone = Error.Validation(
            "Conversation.GroupNeedsSomeone",
            "Nhóm phải có ít nhất một người khác ngoài bạn.");

        /// <summary>
        /// Phân quyền của module này là <b>tư cách THAM GIA</b>, không phải một quyền trong
        /// bảng vai trò.
        ///
        /// Một quyền mà cả bốn vai hệ thống đều có thì không phải quyền, nó là nhiễu — và
        /// nó trả lời sai câu hỏi: chuyện không phải "bạn có được chat không" mà là "bạn có
        /// ở trong hội thoại này không".
        /// </summary>
        public static readonly Error NotAParticipant = Error.Forbidden(
            "Conversation.NotAParticipant",
            "Bạn không ở trong hội thoại này.");
    }

    public static class Messages
    {
        public static readonly Error Empty =
            Error.Validation("Message.Empty", "Tin nhắn không được để trống.");

        public static readonly Error TooLong =
            Error.Validation("Message.TooLong", "Tin nhắn không được dài quá 4000 ký tự.");

        public static readonly Error SenderRequired =
            Error.Validation("Message.SenderRequired", "Tin nhắn phải có người gửi.");
    }
}
