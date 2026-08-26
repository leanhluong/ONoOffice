/**
 * Hợp đồng dữ liệu với `/api/conversations` — module Comm.
 *
 * Nguồn sự thật là `CommViews.cs` trong `ONoOffice.Comm.Application`. Có
 * `chat-contract.spec.ts` đọc thẳng file C# ra rồi đối chiếu tên trường, cùng cách
 * `org-contract.spec.ts` canh module Org.
 */

/**
 * Hai kiểu hội thoại. Số phải khớp `ConversationKind` bên C# — nó đi qua dây mạng dưới
 * dạng số, nên đổi số ở một bên là hai bên nói hai chuyện khác nhau mà không ai báo.
 */
export enum ConversationKind {
  Rieng = 1,
  Nhom = 2,
}

/**
 * Một dòng trong cột trái.
 *
 * `displayName` do MÁY CHỦ tính, không phải frontend ghép. Với hội thoại riêng nó là tên
 * người kia — mà "người kia" thì khác nhau tuỳ ai đang đăng nhập, nên nó không nằm trong
 * database và không thể nằm ở đó.
 */
export interface ConversationSummary {
  id: string;
  kind: ConversationKind;
  displayName: string;
  otherUserId: string | null;
  participantCount: number;
  lastMessageBody: string | null;
  lastMessageSenderName: string | null;
  lastMessageAtUtc: string | null;
  unreadCount: number;
}

export interface MessageItem {
  id: string;
  senderUserId: string;
  senderName: string;
  body: string;
  sentAtUtc: string;
}

/** `hasMore` nghĩa là còn tin CŨ HƠN nữa ở phía trên. */
export interface MessagePageResponse {
  items: MessageItem[];
  hasMore: boolean;
}

/** Thân của `POST /api/conversations/direct`. */
export interface OpenDirectRequest {
  otherUserId: string;
}

/** Thân của `POST /api/conversations/group`. */
export interface CreateGroupRequest {
  name: string;
  memberUserIds: string[];
}

/** Thân của `POST /api/conversations/{id}/messages`. Mã hội thoại nằm ở đường dẫn. */
export interface SendMessageRequest {
  body: string;
}

/**
 * Một tin ĐANG hiển thị trên màn — không hẳn là một tin đã có trên máy chủ.
 *
 * Câu vừa gõ được vẽ ra NGAY, trước khi máy chủ trả lời, vì chờ một vòng mạng rồi mới
 * thấy chữ của mình là cảm giác ứng dụng bị đơ. `trangThai` là chỗ nói thật về sự khác
 * nhau đó: `dang` thì mờ đi, `hong` thì viền đỏ kèm nút thử lại.
 *
 * `id` của tin lạc quan là mã tạm do client sinh — nó bị thay bằng mã thật khi máy chủ
 * trả về. Đó cũng là lý do không dùng nó làm con trỏ cuộn.
 */
export interface MessageOnScreen extends MessageItem {
  trangThai?: 'dang' | 'hong';
}
