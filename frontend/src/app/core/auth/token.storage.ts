import { Injectable } from '@angular/core';
import type { AuthSession } from '../models/auth.model';

const STORAGE_KEY = 'onooffice.session';

/**
 * Nơi DUY NHẤT trong app đụng tới `localStorage`.
 *
 * Vì sao tách riêng: nếu sau này đổi sang `sessionStorage`, sang cookie
 * HttpOnly, hay sang IndexedDB thì chỉ sửa đúng file này. AuthStore và
 * AuthService không biết token nằm ở đâu.
 *
 * Vì sao localStorage: refresh token sống 30 ngày và yêu cầu là mở lại tab
 * vẫn còn đăng nhập. Đánh đổi là token đọc được bằng JavaScript (rủi ro XSS).
 * Cách chặn XSS triệt để là dùng cookie HttpOnly, nhưng việc đó cần backend
 * đổi cách trả token — chưa nằm trong phạm vi hiện tại.
 */
@Injectable({ providedIn: 'root' })
export class TokenStorage {
  read(): AuthSession | null {
    const raw = this.safeGet();
    if (!raw) {
      return null;
    }
    try {
      return JSON.parse(raw) as AuthSession;
    } catch {
      // Dữ liệu hỏng thì dọn luôn cho sạch, tránh lặp lại lỗi mỗi lần khởi động.
      this.clear();
      return null;
    }
  }

  write(session: AuthSession): void {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    } catch {
      // Chế độ ẩn danh hoặc hết quota: bỏ qua, phiên chỉ sống trong bộ nhớ.
    }
  }

  clear(): void {
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {
      // Không có gì để làm — đọc lại sẽ tự trả null.
    }
  }

  private safeGet(): string | null {
    try {
      return localStorage.getItem(STORAGE_KEY);
    } catch {
      return null;
    }
  }
}
