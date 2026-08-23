/**
 * Hợp đồng dữ liệu với `POST /api/auth/login` và nội dung access token.
 * Đặt ở `core/models` vì cả AuthService, AuthStore lẫn TokenStorage đều dùng.
 */

/** Thân request đăng nhập. */
export interface LoginRequest {
  email: string;
  password: string;
}

/** Thân response đăng nhập do backend trả về. */
export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  /** Số giây còn sống của access token (backend đang đặt 900 = 15 phút). */
  expiresIn: number;
}

/** Các claim mà backend nhét vào access token. */
export interface AccessTokenClaims {
  /** Id nhân viên đang đăng nhập. */
  sub: string;
  /** Tenant hiện tại — app là multi-tenant nên claim này bắt buộc phải có. */
  tenant_id: string;
  /**
   * Danh sách permission dạng `employee.read`.
   * JWT chuẩn cho phép claim lặp lại bị gom thành chuỗi đơn khi chỉ có 1 phần tử,
   * nên kiểu phải nhận cả `string` lẫn `string[]`.
   */
  permission?: string | string[];
  /** Thời điểm hết hạn, tính bằng giây kể từ epoch. */
  exp?: number;
  email?: string;
  name?: string;
}

/** Thông tin người dùng mà UI cần hiển thị. */
export interface AuthUser {
  readonly userId: string;
  readonly tenantId: string;
  readonly email: string | null;
  readonly displayName: string | null;
}

/** Phiên đăng nhập đầy đủ, lưu trong AuthStore và TokenStorage. */
export interface AuthSession {
  readonly accessToken: string;
  readonly refreshToken: string;
  /** Thời điểm access token hết hạn, epoch milliseconds. */
  readonly expiresAt: number;
  readonly user: AuthUser;
  readonly permissions: readonly string[];
}
