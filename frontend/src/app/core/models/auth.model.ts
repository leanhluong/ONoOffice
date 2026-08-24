/**
 * Hợp đồng dữ liệu với `POST /api/auth/{login,refresh,logout}`.
 *
 * Nguồn sự thật là `docs/05-api/README.md`. Đã đối chiếu với backend đang chạy thật
 * ngày 2026-08-24 — không còn chỗ nào là phỏng đoán.
 */

/** Thân request đăng nhập. */
export interface LoginRequest {
  email: string;
  password: string;
}

/**
 * Người dùng, đúng như backend trả trong thân phản hồi đăng nhập.
 *
 * Cố ý KHÔNG nằm trong access token: token đi kèm mọi request, mà tên với email thì
 * không phục vụ quyết định bảo mật nào — nhét vào chỉ làm mỗi request nặng thêm và rò
 * thông tin cá nhân vào mọi log có ghi header.
 */
export interface LoginUser {
  id: string;
  tenantId: string;
  email: string;
  fullName: string;
}

/** `POST /api/auth/login` → 200. */
export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  /** Backend đặt tên đúng như vậy — KHÔNG phải `expiresIn`. Hiện là 900 (15 phút). */
  expiresInSeconds: number;
  user: LoginUser;
}

/**
 * `POST /api/auth/refresh` → 200.
 *
 * <b>Không có `user`.</b> Gia hạn phiên chỉ đổi cặp token; thông tin người dùng phải
 * được giữ lại từ phiên cũ — xem `AuthStore.renewSession`.
 */
export interface RefreshResponse {
  accessToken: string;
  refreshToken: string;
  expiresInSeconds: number;
}

/**
 * Các claim backend thật sự nhét vào access token.
 *
 * Chỉ có ba, và cả ba đều phục vụ một quyết định bảo mật:
 * `sub` là ai, `tenant_id` thuộc workspace nào, `permission` được làm gì.
 */
export interface AccessTokenClaims {
  sub: string;
  tenant_id: string;
  /**
   * Danh sách quyền dạng `employee.read`.
   * JWT gom claim lặp lại thành chuỗi đơn khi chỉ có một phần tử, nên phải nhận cả hai kiểu.
   */
  permission?: string | string[];
  /** Thời điểm hết hạn, giây kể từ epoch. */
  exp?: number;
}

/** Thông tin người dùng mà giao diện cần hiển thị. */
export interface AuthUser {
  readonly userId: string;
  readonly tenantId: string;
  readonly email: string;
  readonly displayName: string;
}

/**
 * Phiên đăng nhập đang sống.
 *
 * ⚠️ Object này **chỉ tồn tại trong bộ nhớ**. Thứ duy nhất được ghi xuống đĩa là
 * `refreshToken` — xem `TokenStorage` và `ADR-0004`.
 */
export interface AuthSession {
  readonly accessToken: string;
  readonly refreshToken: string;
  /** Thời điểm access token hết hạn, epoch milliseconds. */
  readonly expiresAt: number;
  readonly user: AuthUser;
  readonly permissions: readonly string[];
}

/**
 * Thân request `POST /api/auth/register-workspace`.
 *
 * Tên trường khớp <c>RegisterWorkspaceCommand</c> ở backend (camelCase do
 * System.Text.Json đổi). Đây là lời gọi DUY NHẤT tạo ra một workspace mới.
 */
export interface RegisterWorkspaceRequest {
  companyName: string;
  workspaceCode: string;
  fullName: string;
  email: string;
  password: string;
}

/** Workspace vừa được tạo, đúng như backend trả về. */
export interface RegisteredWorkspace {
  id: string;
  code: string;
  name: string;
}

/**
 * `POST /api/auth/register-workspace` → 200.
 *
 * <b>Kèm luôn cặp token.</b> Đăng ký xong là đã đăng nhập — bắt người vừa tạo workspace
 * gõ lại chính mật khẩu họ vừa đặt là một bước thừa, và là chỗ dễ bỏ dở nhất của luồng.
 *
 * Vì vậy nó là một <c>LoginResponse</c> có thêm `workspace`, và dùng lại được nguyên
 * <c>AuthStore.startSession</c>.
 */
export interface RegisterWorkspaceResponse extends LoginResponse {
  workspace: RegisteredWorkspace;
}
