import { Injectable } from '@angular/core';
import type { AuthUser } from '../models/auth.model';

const REFRESH_TOKEN_KEY = 'onooffice.refresh-token';
const USER_KEY = 'onooffice.user';

/**
 * Nơi DUY NHẤT trong app đụng tới `localStorage` cho việc đăng nhập.
 *
 * <b>Chỉ giữ refresh token. Access token KHÔNG bao giờ được ghi xuống đây</b> — luật của
 * `ADR-0004`, và lý do rất cụ thể chứ không phải nguyên tắc suông:
 *
 * <ul>
 * <li><b>Access token</b> cầm được là gọi API tuỳ ý suốt 15 phút, không để lại dấu vết
 * nào ở phía server. Nó sống trong biến của <c>AuthStore</c> và chết theo tab.</li>
 * <li><b>Refresh token</b> cũng nguy hiểm nếu bị đọc, nhưng nó <b>dùng được đúng một
 * lần</b>: backend xoay vòng nó ở mỗi lần gia hạn, và lần dùng thứ hai kích hoạt phát
 * hiện trộm — thu hồi cả chuỗi. Kẻ trộm dùng nó là tự tố cáo, và nạn nhân bị đăng xuất
 * chứ không bị chiếm phiên im lặng.</li>
 * </ul>
 *
 * Cái giá của việc access token chết theo tab: mở lại tab thì phải gia hạn một lần trước
 * khi vào được. Đó là một request thêm, đổi lấy việc thứ nguy hiểm nhất không nằm trên đĩa.
 *
 * Vì sao gói vào một service thay vì gọi thẳng: hôm nào FE và API về chung một tên miền
 * thì chuyển sang cookie HttpOnly chỉ phải sửa đúng file này.
 */
@Injectable({ providedIn: 'root' })
export class TokenStorage {
  readRefreshToken(): string | null {
    try {
      return localStorage.getItem(REFRESH_TOKEN_KEY);
    } catch {
      // Chế độ ẩn danh của một số trình duyệt ném lỗi ngay ở getItem.
      return null;
    }
  }

  writeRefreshToken(token: string): void {
    try {
      localStorage.setItem(REFRESH_TOKEN_KEY, token);
    } catch {
      // Hết quota hoặc bị chặn: phiên vẫn chạy được, chỉ là đóng tab thì mất.
    }
  }

  /**
   * Tên và email của chính người đang dùng máy này — <b>không phải bí mật</b>.
   *
   * Ghi xuống để mở lại tab là thanh điều hướng hiện đúng tên ngay, thay vì trống một
   * lúc rồi mới có. Cần nó vì access token cố ý KHÔNG mang tên với email (xem
   * <c>LoginUser</c>), và lát 1 chưa có <c>GET /api/auth/me</c> để hỏi lại.
   *
   * Vì sao ghi cái này mà không ghi access token: đây là dữ liệu người dùng vốn đã nhìn
   * thấy trên màn hình của họ. Đọc được nó không cho ai làm được gì — trong khi access
   * token thì cho gọi mọi API suốt 15 phút. Hai thứ khác hẳn nhau về hậu quả.
   */
  readUser(): AuthUser | null {
    try {
      const raw = localStorage.getItem(USER_KEY);
      return raw ? (JSON.parse(raw) as AuthUser) : null;
    } catch {
      // JSON hỏng (người dùng sửa tay) — coi như không có, đừng làm hỏng lúc khởi động.
      return null;
    }
  }

  writeUser(user: AuthUser): void {
    try {
      localStorage.setItem(USER_KEY, JSON.stringify(user));
    } catch {
      // Không ghi được thì tên chỉ trống một lúc, không ảnh hưởng chức năng.
    }
  }

  clear(): void {
    try {
      localStorage.removeItem(REFRESH_TOKEN_KEY);
      localStorage.removeItem(USER_KEY);

      // Dọn khoá của bản cũ, khi access token còn bị ghi chung vào đây. Không dọn thì
      // nó nằm lại trên máy người dùng vô thời hạn — một access token cũ thì vô hại,
      // nhưng để rác bảo mật nằm đó là thói quen xấu.
      localStorage.removeItem('onooffice.session');
    } catch {
      // Đọc lại sẽ tự trả null.
    }
  }
}
