import { Injectable, computed, inject, signal } from '@angular/core';
import type { AuthSession, AuthUser, LoginResponse, RefreshResponse } from '../models/auth.model';
import { decodeJwtPayload, hasRequiredClaims, readExpiry, readPermissions } from './jwt.util';
import { TokenStorage } from './token.storage';

/**
 * Trạng thái đăng nhập của toàn app, viết bằng signal.
 *
 * <b>Phiên sống TRONG BỘ NHỚ.</b> Thứ duy nhất chạm đĩa là refresh token — xem
 * <c>TokenStorage</c> và <c>ADR-0004</c>. Hệ quả trực tiếp: mở lại tab thì
 * <c>isAuthenticated()</c> là <c>false</c> cho tới khi gia hạn xong. Đó là hành vi ĐÚNG,
 * và <c>canRestore()</c> là thứ phân biệt "chưa từng đăng nhập" với "có vé, gia hạn được".
 *
 * Vì sao signal mà không phải NgRx: cả state chỉ có đúng MỘT nguồn sự thật (phiên hiện
 * tại), mọi thứ khác suy ra bằng <c>computed</c>. Dựng action/reducer/effect cho một
 * object là chi phí không đổi lại được gì.
 */
@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly storage = inject(TokenStorage);

  /** Nguồn sự thật duy nhất. */
  private readonly sessionState = signal<AuthSession | null>(null);

  readonly session = this.sessionState.asReadonly();

  readonly user = computed(() => this.sessionState()?.user ?? null);

  readonly accessToken = computed(() => this.sessionState()?.accessToken ?? null);

  readonly refreshToken = computed(
    () => this.sessionState()?.refreshToken ?? this.storage.readRefreshToken(),
  );

  /** Set thay mảng: kiểm tra quyền là thao tác chạy rất nhiều lần. */
  readonly permissions = computed<ReadonlySet<string>>(
    () => new Set(this.sessionState()?.permissions ?? []),
  );

  readonly permissionList = computed(() => [...this.permissions()].sort());

  readonly tenantId = computed(() => this.sessionState()?.user.tenantId ?? null);

  readonly isAuthenticated = computed(() => this.sessionState() !== null);

  /**
   * True khi access token đã quá hạn theo đồng hồ máy này.
   *
   * Backend mới là bên quyết định thật; biết trước chỉ để chủ động gia hạn thay vì để
   * người dùng dính một lần 401 rồi mới xử lý.
   */
  readonly isAccessTokenExpired = computed(() => {
    const session = this.sessionState();
    return session === null ? true : session.expiresAt <= Date.now();
  });

  /**
   * Chưa đăng nhập, nhưng còn vé gia hạn trên đĩa — tức là <b>có thể</b> khôi phục phiên
   * mà không bắt gõ lại mật khẩu. Đây là ca của "mở lại tab sáng hôm sau".
   */
  canRestore(): boolean {
    return !this.isAuthenticated() && this.storage.readRefreshToken() !== null;
  }

  hasPermission(permission: string): boolean {
    return this.permissions().has(permission);
  }

  /** Có ít nhất một trong các quyền. Danh sách rỗng nghĩa là không đòi hỏi gì. */
  hasAnyPermission(permissions: readonly string[]): boolean {
    if (permissions.length === 0) {
      return true;
    }
    const granted = this.permissions();
    return permissions.some((permission) => granted.has(permission));
  }

  hasAllPermissions(permissions: readonly string[]): boolean {
    const granted = this.permissions();
    return permissions.every((permission) => granted.has(permission));
  }

  /**
   * Mở phiên mới từ phản hồi đăng nhập.
   *
   * Người dùng lấy từ THÂN phản hồi, quyền lấy từ TOKEN — hai nguồn khác nhau vì backend
   * cố ý để chúng ở hai chỗ khác nhau (xem <c>LoginUser</c>).
   *
   * Trả <c>false</c> nếu token không dùng được. Khi đó KHÔNG ghi gì cả — nửa vời còn tệ
   * hơn không có, vì guard sẽ cho vào rồi mọi request đều hỏng.
   */
  startSession(response: LoginResponse): boolean {
    const claims = decodeJwtPayload(response.accessToken);

    if (!claims || !hasRequiredClaims(claims)) {
      return false;
    }

    const user: AuthUser = {
      userId: response.user.id,
      tenantId: response.user.tenantId,
      email: response.user.email,
      displayName: response.user.fullName,
    };

    this.apply({
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
      expiresAt: readExpiry(claims, response.expiresInSeconds),
      user,
      permissions: readPermissions(claims),
    });

    return true;
  }

  /**
   * Gia hạn phiên bằng cặp token mới.
   *
   * <b>Giữ nguyên người dùng, nạp LẠI quyền.</b> Bất đối xứng đó là có chủ đích và khớp
   * với backend:
   *
   * <ul>
   * <li><c>/refresh</c> không trả về <c>user</c>, nên xoá đi thì tên trên thanh điều
   * hướng biến mất sau 15 phút — không thao tác nào của người dùng giải thích được.</li>
   * <li>Backend <b>nạp lại quyền</b> ở mỗi lần gia hạn (xem
   * <c>RefreshTokenCommandHandler</c>) — đó chính là chỗ việc thu hồi quyền có hiệu lực.
   * Frontend giữ quyền cũ là làm hỏng đúng cơ chế đó: nút vẫn hiện, bấm vào thì 403.</li>
   * </ul>
   *
   * Trả <c>false</c> khi chưa có phiên nào để gia hạn. Ca "mở lại tab" không đi qua đây
   * mà đi qua <c>restoreSession</c>.
   */
  renewSession(response: RefreshResponse): boolean {
    const current = this.sessionState();

    if (current === null) {
      return false;
    }

    const claims = decodeJwtPayload(response.accessToken);

    if (!claims || !hasRequiredClaims(claims)) {
      return false;
    }

    this.apply({
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
      expiresAt: readExpiry(claims, response.expiresInSeconds),
      user: current.user,
      permissions: readPermissions(claims),
    });

    return true;
  }

  /**
   * Khôi phục phiên sau khi mở lại tab, khi chưa có <c>user</c> nào trong bộ nhớ.
   *
   * Dựng người dùng từ claim là <b>không thể</b> — token cố ý không mang tên với email.
   * Nên lấy lại từ bản đã ghi cùng vé gia hạn. Không có (người dùng dọn localStorage một
   * nửa) thì để trống chỗ tên — thà trống còn hơn bịa.
   *
   * Ngày có <c>GET /api/auth/me</c> thì thay chỗ này bằng một lời gọi thật.
   */
  restoreSession(response: RefreshResponse): boolean {
    const claims = decodeJwtPayload(response.accessToken);

    if (!claims || !hasRequiredClaims(claims)) {
      return false;
    }

    this.apply({
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
      expiresAt: readExpiry(claims, response.expiresInSeconds),
      user: this.sessionState()?.user ??
        this.storage.readUser() ?? {
          userId: claims.sub,
          tenantId: claims.tenant_id,
          email: '',
          displayName: '',
        },
      permissions: readPermissions(claims),
    });

    return true;
  }

  /** Xoá phiên khỏi cả bộ nhớ lẫn đĩa. Dùng khi đăng xuất và khi gia hạn thất bại. */
  clear(): void {
    this.sessionState.set(null);
    this.storage.clear();
  }

  private apply(session: AuthSession): void {
    this.sessionState.set(session);

    // Chạm đĩa đúng hai thứ: vé gia hạn, và tên người dùng để mở lại tab không bị trống.
    // Access token thì KHÔNG BAO GIỜ — xem lý do dài ở TokenStorage.
    this.storage.writeRefreshToken(session.refreshToken);
    this.storage.writeUser(session.user);
  }
}
