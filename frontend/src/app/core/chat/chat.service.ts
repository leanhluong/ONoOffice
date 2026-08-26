import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import type {
  ConversationSummary,
  CreateGroupRequest,
  MessageItem,
  MessagePageResponse,
} from '../models/chat.model';

/**
 * Cầu nối tới module Comm: `/api/conversations`.
 *
 * Cùng ranh giới trách nhiệm với `OrgService` và `UserService`: gọi HTTP, không điều
 * hướng, không giữ trạng thái màn hình. Hội thoại đang mở và ô đang gõ dở thuộc về
 * component.
 */
@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);

  /** Cột trái. Không phân trang — máy chủ trả hết, mới nhất trước. */
  conversations(): Observable<ConversationSummary[]> {
    return this.http.get<ConversationSummary[]>(this.url('/api/conversations'));
  }

  /**
   * Mở hội thoại riêng — trả lại đúng hội thoại cũ nếu đã có.
   *
   * Gọi mười lần thì mười lần ra cùng một hội thoại, nên bấm nhầm hai lần vào một cái tên
   * không tạo ra hai phòng.
   */
  openDirect(otherUserId: string): Observable<ConversationSummary> {
    return this.http.post<ConversationSummary>(this.url('/api/conversations/direct'), {
      otherUserId,
    });
  }

  createGroup(request: CreateGroupRequest): Observable<ConversationSummary> {
    return this.http.post<ConversationSummary>(this.url('/api/conversations/group'), request);
  }

  /**
   * Một trang tin, theo thứ tự cũ → mới.
   *
   * `before` là MÃ MỘT TIN, không phải mốc thời gian — xem chú thích cùng tên ở
   * `IMessageRepository` bên backend. Bỏ trống nghĩa là "trang mới nhất".
   */
  messages(conversationId: string, before?: string): Observable<MessagePageResponse> {
    const params = before ? new HttpParams().set('before', before) : undefined;

    return this.http.get<MessagePageResponse>(
      this.url(`/api/conversations/${conversationId}/messages`),
      { params },
    );
  }

  send(conversationId: string, body: string): Observable<MessageItem> {
    return this.http.post<MessageItem>(this.url(`/api/conversations/${conversationId}/messages`), {
      body,
    });
  }

  /** Không có thân: máy chủ dùng "lúc này", không nhận mốc từ client. */
  markRead(conversationId: string): Observable<void> {
    return this.http.post<void>(this.url(`/api/conversations/${conversationId}/read`), {});
  }

  private url(path: string): string {
    return `${environment.apiBaseUrl}${path}`;
  }
}
