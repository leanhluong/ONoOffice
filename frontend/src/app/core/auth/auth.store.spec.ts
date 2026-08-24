import { TestBed } from '@angular/core/testing';
import { AuthStore } from './auth.store';
import { TokenStorage } from './token.storage';
import type { LoginResponse, RefreshResponse } from '../models/auth.model';

/** Dựng một JWT giả — frontend không xác thực chữ ký, chỉ đọc payload để vẽ giao diện. */
function fakeJwt(payload: Record<string, unknown>): string {
  const encode = (value: object): string =>
    btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${encode({ alg: 'HS256', typ: 'JWT' })}.${encode(payload)}.chu-ky-gia`;
}

const USER_ID = '11111111-1111-1111-1111-111111111111';
const TENANT_ID = '22222222-2222-2222-2222-222222222222';

/** Đúng hình dạng backend trả về — xem docs/05-api/README.md. */
function loginResponse(overrides: Partial<LoginResponse> = {}): LoginResponse {
  return {
    accessToken: fakeJwt({
      sub: USER_ID,
      tenant_id: TENANT_ID,
      permission: ['employee.read', 'employee.write'],
      exp: Math.floor(Date.now() / 1000) + 900,
    }),
    refreshToken: 've-gia-han-1',
    expiresInSeconds: 900,
    user: {
      id: USER_ID,
      tenantId: TENANT_ID,
      email: 'chu@demo.vn',
      fullName: 'Chủ Workspace Demo',
    },
    ...overrides,
  };
}

describe('AuthStore', () => {
  let store: AuthStore;
  let storage: TokenStorage;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
    store = TestBed.inject(AuthStore);
    storage = TestBed.inject(TokenStorage);
  });

  afterEach(() => localStorage.clear());

  it('nhận đúng hình dạng response của backend', () => {
    expect(store.startSession(loginResponse())).toBe(true);

    expect(store.isAuthenticated()).toBe(true);
    expect(store.user()?.userId).toBe(USER_ID);
    expect(store.user()?.tenantId).toBe(TENANT_ID);
  });

  /**
   * Tên và email lấy từ THÂN phản hồi, không từ claim trong token.
   *
   * Backend cố ý không nhét chúng vào token: token đi kèm mọi request, mà tên người dùng
   * thì không phục vụ quyết định bảo mật nào — nhét vào chỉ làm mỗi request nặng thêm và
   * rò thông tin cá nhân vào mọi log có ghi header.
   *
   * Đọc nhầm chỗ thì không có lỗi nào cả: tên chỉ đơn giản là rỗng trên thanh điều hướng.
   */
  it('lấy tên và email từ thân phản hồi, không từ claim', () => {
    store.startSession(loginResponse());

    expect(store.user()?.email).toBe('chu@demo.vn');
    expect(store.user()?.displayName).toBe('Chủ Workspace Demo');
  });

  it('đọc quyền từ claim trong token', () => {
    store.startSession(loginResponse());

    expect(store.hasPermission('employee.read')).toBe(true);
    expect(store.hasPermission('employee.delete')).toBe(false);
  });

  // ── ADR-0004: hai nơi lưu khác nhau, có chủ đích ─────────────────────────

  /**
   * ⭐ Access token nằm TRONG BIẾN, không bao giờ chạm localStorage.
   *
   * Một lỗ hổng XSS đọc được localStorage. Refresh token nằm đó thì cũng nguy hiểm, nhưng
   * nó dùng được đúng một lần rồi bị xoay vòng, và lần dùng thứ hai sẽ kích hoạt phát
   * hiện trộm ở backend — thu hồi cả chuỗi. Access token thì ngược lại: cầm được là gọi
   * API tuỳ ý suốt 15 phút, không để lại dấu vết nào.
   */
  it('KHÔNG ghi access token vào localStorage', () => {
    const response = loginResponse();
    store.startSession(response);

    const raw = JSON.stringify(localStorage);

    expect(raw).not.toContain(response.accessToken);
  });

  it('ghi refresh token vào localStorage để mở lại tab vẫn còn phiên', () => {
    store.startSession(loginResponse());

    expect(storage.readRefreshToken()).toBe('ve-gia-han-1');
  });

  /**
   * Mở lại tab: chỉ khôi phục được refresh token, chưa có access token.
   *
   * `isAuthenticated` phải là FALSE ở thời điểm đó — nếu trả true thì guard cho vào
   * thẳng màn dashboard, request đầu tiên đi ra không có Bearer, dính 401, rồi mới bị
   * đá về đăng nhập. Người dùng thấy màn hình nhấp nháy một cái rồi văng ra.
   */
  it('mở lại tab thì chưa đăng nhập, nhưng khôi phục được được vé gia hạn', () => {
    store.startSession(loginResponse());

    // Giả lập mở lại tab: dựng một injector hoàn toàn mới, localStorage giữ nguyên.
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({});
    const moiTab = TestBed.inject(AuthStore);

    expect(moiTab.isAuthenticated()).toBe(false);
    expect(moiTab.canRestore()).toBe(true);
  });

  // ── Gia hạn phiên ────────────────────────────────────────────────────────

  /**
   * ⭐ `POST /api/auth/refresh` KHÔNG trả về `user` — nó chỉ trả cặp token mới.
   *
   * Nên khi gia hạn, thông tin người dùng phải được GIỮ LẠI từ phiên cũ. Xoá đi thì tên
   * trên thanh điều hướng biến mất sau 15 phút — đúng lúc người dùng đang làm việc, và
   * không có thao tác nào của họ giải thích được chuyện đó.
   */
  it('gia hạn phiên thì giữ nguyên thông tin người dùng', () => {
    store.startSession(loginResponse());

    const giaHan: RefreshResponse = {
      accessToken: fakeJwt({
        sub: USER_ID,
        tenant_id: TENANT_ID,
        permission: ['employee.read'],
        exp: Math.floor(Date.now() / 1000) + 900,
      }),
      refreshToken: 've-gia-han-2',
      expiresInSeconds: 900,
    };

    expect(store.renewSession(giaHan)).toBe(true);

    expect(store.user()?.displayName).toBe('Chủ Workspace Demo');
    expect(storage.readRefreshToken()).toBe('ve-gia-han-2');
  });

  /**
   * Quyền thì phải nạp LẠI từ token mới, không giữ của phiên cũ.
   *
   * Backend cố ý nạp lại quyền ở mỗi lần gia hạn (xem RefreshTokenCommandHandler) —
   * đó chính là chỗ việc thu hồi quyền có hiệu lực. Frontend giữ quyền cũ là làm hỏng
   * đúng cơ chế đó: nút vẫn hiện, bấm vào thì 403.
   */
  it('gia hạn phiên thì nạp LẠI quyền từ token mới', () => {
    store.startSession(loginResponse());
    expect(store.hasPermission('employee.write')).toBe(true);

    store.renewSession({
      accessToken: fakeJwt({
        sub: USER_ID,
        tenant_id: TENANT_ID,
        permission: ['employee.read'],
        exp: Math.floor(Date.now() / 1000) + 900,
      }),
      refreshToken: 've-gia-han-2',
      expiresInSeconds: 900,
    });

    expect(store.hasPermission('employee.write')).toBe(false);
    expect(store.hasPermission('employee.read')).toBe(true);
  });

  it('gia hạn khi chưa có phiên nào thì từ chối', () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({});
    const trong = TestBed.inject(AuthStore);

    const ketQua = trong.renewSession({
      accessToken: fakeJwt({ sub: USER_ID, tenant_id: TENANT_ID }),
      refreshToken: 've-la',
      expiresInSeconds: 900,
    });

    expect(ketQua).toBe(false);
  });

  // ── Từ chối token hỏng ───────────────────────────────────────────────────

  it('từ chối token không giải mã được', () => {
    expect(store.startSession(loginResponse({ accessToken: 'khong-phai-jwt' }))).toBe(false);
    expect(store.isAuthenticated()).toBe(false);
  });

  it('từ chối token thiếu claim tenant_id', () => {
    const thieuTenant = fakeJwt({ sub: USER_ID, exp: Math.floor(Date.now() / 1000) + 900 });

    expect(store.startSession(loginResponse({ accessToken: thieuTenant }))).toBe(false);
  });

  it('đăng xuất thì xoá sạch cả bộ nhớ lẫn localStorage', () => {
    store.startSession(loginResponse());

    store.clear();

    expect(store.isAuthenticated()).toBe(false);
    expect(storage.readRefreshToken()).toBeNull();
  });
});
