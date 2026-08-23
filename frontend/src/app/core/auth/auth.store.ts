import { Injectable, computed, inject, signal } from '@angular/core';
import type { AuthSession, LoginResponse } from '../models/auth.model';
import { decodeJwtPayload, readPermissions, readUser } from './jwt.util';
import { TokenStorage } from './token.storage';

/**
 * Trạng thái đăng nhập của toàn app, viết bằng signal của Angular.
 *
 * Vì sao signal mà không phải NgRx: state ở đây chỉ có đúng MỘT nguồn sự thật
 * (access token) và mọi thứ khác đều suy ra được từ nó bằng `computed`.
 * Dựng cả bộ action/reducer/effect cho một object là chi phí không đổi lại
 * được gì. Signal còn cho phép template đọc trực tiếp mà không cần async pipe.
 *
 * Vì sao là service `providedIn: 'root'` chứ không phải biến toàn cục:
 * để guard, interceptor và component đều lấy được qua `inject()`, và để
 * test có thể thay bằng bản giả.
 */
@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly storage = inject(TokenStorage);

  /** Nguồn sự thật duy nhất. Mọi thứ bên dưới đều suy ra từ đây. */
  private readonly sessionState = signal<AuthSession | null>(this.storage.read());

  readonly session = this.sessionState.asReadonly();

  readonly user = computed(() => this.sessionState()?.user ?? null);

  readonly accessToken = computed(() => this.sessionState()?.accessToken ?? null);

  readonly refreshToken = computed(() => this.sessionState()?.refreshToken ?? null);

  /** Dùng Set thay mảng: kiểm tra permission là thao tác chạy rất nhiều lần. */
  readonly permissions = computed<ReadonlySet<string>>(
    () => new Set(this.sessionState()?.permissions ?? []),
  );

  /** Mảng permission đã sắp xếp — tiện cho màn debug / hiển thị. */
  readonly permissionList = computed(() => [...this.permissions()].sort());

  readonly tenantId = computed(() => this.sessionState()?.user.tenantId ?? null);

  readonly isAuthenticated = computed(() => this.sessionState() !== null);

  /**
   * True khi access token đã quá hạn. Backend mới là bên quyết định thật,
   * nhưng biết trước giúp app chủ động gọi refresh thay vì để dính 401.
   */
  readonly isAccessTokenExpired = computed(() => {
    const session = this.sessionState();
    return session === null ? true : session.expiresAt <= Date.now();
  });

  /** Kiểm tra một permission cụ thể, ví dụ `employee.read`. */
  hasPermission(permission: string): boolean {
    return this.permissions().has(permission);
  }

  /** Có ít nhất một trong các permission (điều kiện HOẶC). */
  hasAnyPermission(permissions: readonly string[]): boolean {
    if (permissions.length === 0) {
      return true;
    }
    const granted = this.permissions();
    return permissions.some((permission) => granted.has(permission));
  }

  /** Có đủ tất cả permission (điều kiện VÀ). */
  hasAllPermissions(permissions: readonly string[]): boolean {
    const granted = this.permissions();
    return permissions.every((permission) => granted.has(permission));
  }

  /**
   * Ghi phiên mới từ response đăng nhập / làm mới token.
   * Trả về false nếu token không giải mã được hoặc thiếu claim bắt buộc —
   * lúc đó coi như đăng nhập thất bại, không ghi gì cả.
   */
  setSessionFromResponse(response: LoginResponse): boolean {
    const claims = decodeJwtPayload(response.accessToken);
    if (!claims) {
      return false;
    }

    const user = readUser(claims);
    if (!user) {
      return false;
    }

    // Ưu tiên `exp` trong token; `expiresIn` chỉ là phương án dự phòng vì
    // đồng hồ máy client có thể lệch so với server.
    const expiresAt = claims.exp ? claims.exp * 1000 : Date.now() + response.expiresIn * 1000;

    const session: AuthSession = {
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
      expiresAt,
      user,
      permissions: readPermissions(claims),
    };

    this.sessionState.set(session);
    this.storage.write(session);
    return true;
  }

  /** Xoá phiên khỏi cả bộ nhớ lẫn storage. Dùng khi đăng xuất hoặc dính 401. */
  clear(): void {
    this.sessionState.set(null);
    this.storage.clear();
  }
}
