import type { AccessTokenClaims, AuthUser } from '../models/auth.model';

/**
 * Giải mã phần payload của JWT.
 *
 * LƯU Ý QUAN TRỌNG: đây KHÔNG phải là xác thực chữ ký. Frontend không có
 * public key và cũng không nên có. Việc đọc claim ở đây chỉ để vẽ giao diện
 * (hiện tên, ẩn/hiện menu). Mọi quyết định bảo mật thật vẫn do backend làm.
 * Người dùng hoàn toàn có thể sửa localStorage để "thấy" thêm menu, nhưng
 * API sẽ trả 403 vì backend tự kiểm lại permission từ token đã ký.
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
    // JWT dùng base64url: thay ký tự và bù `=` cho đủ bội số 4.
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
    // `atob` trả chuỗi byte; decodeURIComponent/escape để đọc đúng tiếng Việt có dấu.
    const json = decodeURIComponent(
      Array.from(
        atob(padded),
        (char) => `%${char.charCodeAt(0).toString(16).padStart(2, '0')}`,
      ).join(''),
    );
    return JSON.parse(json) as AccessTokenClaims;
  } catch {
    // Token rác (người dùng sửa tay localStorage chẳng hạn) — coi như không có phiên.
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

/** Dựng `AuthUser` từ claim. Trả null nếu thiếu claim bắt buộc (`sub`, `tenant_id`). */
export function readUser(claims: AccessTokenClaims): AuthUser | null {
  if (!claims.sub || !claims.tenant_id) {
    return null;
  }
  return {
    userId: claims.sub,
    tenantId: claims.tenant_id,
    email: claims.email ?? null,
    displayName: claims.name ?? claims.email ?? null,
  };
}
