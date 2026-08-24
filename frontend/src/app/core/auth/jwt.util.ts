import type { AccessTokenClaims } from '../models/auth.model';

/**
 * Giải mã phần payload của JWT.
 *
 * <b>Đây KHÔNG phải xác thực chữ ký.</b> Frontend không có khoá và cũng không nên có.
 * Đọc claim ở đây chỉ để vẽ giao diện — ẩn/hiện menu, chặn route sớm cho đỡ nhấp nháy.
 * Mọi quyết định bảo mật thật vẫn do backend làm: người dùng sửa tay localStorage thì
 * "thấy" thêm menu, nhưng bấm vào là 403 vì backend kiểm lại từ token đã ký.
 */
export function decodeJwtPayload(token: string): AccessTokenClaims | null {
  const parts = token.split('.');
  if (parts.length !== 3) {
    return null;
  }

  const payload = parts[1];
  if (!payload) {
    return null;
  }

  try {
    // JWT dùng base64url: đổi ký tự và bù `=` cho đủ bội số 4.
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
    // `atob` trả chuỗi byte; phải giải lại UTF-8 để đọc đúng tiếng Việt có dấu.
    const json = decodeURIComponent(
      Array.from(
        atob(padded),
        (char) => `%${char.charCodeAt(0).toString(16).padStart(2, '0')}`,
      ).join(''),
    );
    return JSON.parse(json) as AccessTokenClaims;
  } catch {
    // Token rác — coi như không có phiên, đừng để nó thành exception giữa lúc khởi động.
    return null;
  }
}

/** Gom claim `permission` về dạng mảng, bỏ trùng và bỏ chuỗi rỗng. */
export function readPermissions(claims: AccessTokenClaims): string[] {
  const raw = claims.permission;
  if (!raw) {
    return [];
  }
  const list = Array.isArray(raw) ? raw : [raw];
  return [...new Set(list.filter((item) => typeof item === 'string' && item.length > 0))];
}

/**
 * Hai claim BẮT BUỘC phải có thì token mới dùng được.
 *
 * Thiếu `tenant_id` mà vẫn nhận thì mọi truy vấn sau đó chạy không có workspace —
 * backend trả rỗng, và người dùng thấy một ứng dụng trống trơn không có lỗi nào.
 */
export function hasRequiredClaims(claims: AccessTokenClaims): boolean {
  return Boolean(claims.sub) && Boolean(claims.tenant_id);
}

/**
 * Thời điểm hết hạn, epoch milliseconds.
 *
 * Ưu tiên `exp` trong token vì nó do SERVER đặt. `expiresInSeconds` chỉ là phương án dự
 * phòng: nó cộng vào đồng hồ của máy client, mà đồng hồ đó có thể lệch hàng phút.
 */
export function readExpiry(claims: AccessTokenClaims, expiresInSeconds: number): number {
  return claims.exp ? claims.exp * 1000 : Date.now() + expiresInSeconds * 1000;
}
