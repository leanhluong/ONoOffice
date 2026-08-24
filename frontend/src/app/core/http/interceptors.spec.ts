import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { AuthStore } from '../auth/auth.store';
import { isAppError, type AppError } from '../models/api-error.model';
import type { LoginResponse } from '../models/auth.model';
import { authInterceptor } from './auth.interceptor';
import { CORRELATION_ID_HEADER, correlationIdInterceptor } from './correlation-id.interceptor';
import { errorInterceptor } from './error.interceptor';
import { refreshInterceptor } from './refresh.interceptor';

/** Dựng một JWT giả — frontend không xác thực chữ ký. */
function fakeJwt(payload: Record<string, unknown>): string {
  const encode = (value: object): string =>
    btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${encode({ alg: 'HS256', typ: 'JWT' })}.${encode(payload)}.chu-ky-gia`;
}

/** AuthService gọi bằng URL tuyệt đối dựng từ apiBaseUrl — test phải khớp đúng nó. */
const REFRESH_URL = `${environment.apiBaseUrl}/api/auth/refresh`;

const USER_ID = '11111111-1111-1111-1111-111111111111';
const TENANT_ID = '22222222-2222-2222-2222-222222222222';

function token(permissions: string[] = ['employee.read']): string {
  return fakeJwt({
    sub: USER_ID,
    tenant_id: TENANT_ID,
    permission: permissions,
    exp: Math.floor(Date.now() / 1000) + 900,
  });
}

function login(accessToken: string, refreshToken = 've-1'): LoginResponse {
  return {
    accessToken,
    refreshToken,
    expiresInSeconds: 900,
    user: { id: USER_ID, tenantId: TENANT_ID, email: 'chu@demo.vn', fullName: 'Chủ Demo' },
  };
}

/** Thân lỗi đúng hình dạng backend trả về. */
function problem(code: string, description: string) {
  return { status: 401, title: 'Unauthorized', errors: [{ code, description }] };
}

describe('Chuỗi interceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let store: AuthStore;
  let router: Router;

  beforeEach(() => {
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(
          // ĐÚNG thứ tự của app thật. Test một thứ tự khác với thứ tự chạy thật thì
          // nó canh một hệ thống không tồn tại.
          withInterceptors([
            correlationIdInterceptor,
            refreshInterceptor,
            authInterceptor,
            errorInterceptor,
          ]),
        ),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    store = TestBed.inject(AuthStore);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
    TestBed.resetTestingModule();
  });

  // ── Mã lần vết ────────────────────────────────────────────────────────

  it('gắn X-Correlation-Id vào mọi request', () => {
    http.get('/api/ping').subscribe();

    const req = httpMock.expectOne('/api/ping');
    expect(req.request.headers.get(CORRELATION_ID_HEADER)).toBeTruthy();
    req.flush({});
  });

  // ── Gắn Bearer ────────────────────────────────────────────────────────

  it('không gắn Authorization khi chưa đăng nhập', () => {
    http.get('/api/ping').subscribe();

    const req = httpMock.expectOne('/api/ping');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('gắn Bearer khi đã có phiên', () => {
    const accessToken = token();
    store.startSession(login(accessToken));

    http.get('/api/ping').subscribe();

    const req = httpMock.expectOne('/api/ping');
    expect(req.request.headers.get('Authorization')).toBe(`Bearer ${accessToken}`);
    req.flush({});
  });

  it('KHÔNG gắn Bearer vào chính endpoint đăng nhập', () => {
    store.startSession(login(token()));

    http.post('/api/auth/login', {}).subscribe({ error: () => undefined });

    const req = httpMock.expectOne('/api/auth/login');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  // ── Chuẩn hoá lỗi ─────────────────────────────────────────────────────

  it('đổi Problem Details thành AppError giữ nguyên mã lỗi', async () => {
    const nhan = new Promise<AppError>((resolve) => {
      http.get('/api/ping').subscribe({ error: (e: AppError) => resolve(e) });
    });

    httpMock
      .expectOne('/api/ping')
      .flush(
        { status: 409, errors: [{ code: 'Tenant.AlreadyHasOwner', description: 'Đã có chủ.' }] },
        { status: 409, statusText: 'Conflict' },
      );

    const error = await nhan;

    expect(isAppError(error)).toBe(true);
    expect(error.kind).toBe('conflict');
    expect(error.code).toBe('Tenant.AlreadyHasOwner');
    expect(error.correlationId).toBeTruthy();
  });

  // ── Tự gia hạn khi 401 ────────────────────────────────────────────────

  /**
   * ⭐ Luồng chính: 401 → gia hạn → gửi lại → thành công. Người dùng không thấy gì cả.
   */
  it('gặp 401 thì tự gia hạn rồi gửi lại request', async () => {
    store.startSession(login(token(), 've-cu'));

    const ketQua = new Promise((resolve) => {
      http.get<{ ok: boolean }>('/api/employees').subscribe({ next: resolve });
    });

    // Lần một: token hết hạn.
    httpMock
      .expectOne('/api/employees')
      .flush(problem('Auth.InvalidCredentials', 'Hết hạn'), {
        status: 401,
        statusText: 'Unauthorized',
      });

    // Interceptor tự gọi gia hạn, gửi đúng vé đang giữ.
    const giaHan = httpMock.expectOne(REFRESH_URL);
    expect(giaHan.request.body).toEqual({ refreshToken: 've-cu' });

    const tokenMoi = token(['employee.read', 'employee.write']);
    giaHan.flush({ accessToken: tokenMoi, refreshToken: 've-moi', expiresInSeconds: 900 });

    // Lần hai: cùng URL, nhưng phải mang token MỚI.
    const lanHai = httpMock.expectOne('/api/employees');
    expect(lanHai.request.headers.get('Authorization')).toBe(`Bearer ${tokenMoi}`);
    lanHai.flush({ ok: true });

    expect(await ketQua).toEqual({ ok: true });
    expect(store.hasPermission('employee.write')).toBe(true);
  });

  /**
   * ⭐⭐ Nhiều request cùng dính 401 chỉ được sinh ra MỘT lời gọi gia hạn.
   *
   * Đây không phải chuyện tối ưu tốc độ mà là chuyện MẤT PHIÊN: vé gia hạn dùng một lần
   * rồi xoay vòng. Hai lời gọi song song thì cái thứ hai cầm vé đã tiêu — backend coi là
   * bị trộm và thu hồi CẢ CHUỖI. App tự đá người dùng ra.
   */
  it('nhiều request cùng dính 401 chỉ gọi gia hạn MỘT lần', async () => {
    store.startSession(login(token(), 've-cu'));

    const xong = Promise.all([
      new Promise((r) => http.get('/api/a').subscribe({ next: r })),
      new Promise((r) => http.get('/api/b').subscribe({ next: r })),
      new Promise((r) => http.get('/api/c').subscribe({ next: r })),
    ]);

    for (const url of ['/api/a', '/api/b', '/api/c']) {
      httpMock
        .expectOne(url)
        .flush(problem('Auth.InvalidCredentials', 'Hết hạn'), {
          status: 401,
          statusText: 'Unauthorized',
        });
    }

    // expectOne sẽ NÉM nếu có hai lời gọi gia hạn — đó chính là phép kiểm.
    const giaHan = httpMock.expectOne(REFRESH_URL);
    giaHan.flush({ accessToken: token(), refreshToken: 've-moi', expiresInSeconds: 900 });

    for (const url of ['/api/a', '/api/b', '/api/c']) {
      httpMock.expectOne(url).flush({ url });
    }

    expect(await xong).toHaveLength(3);
  });

  /**
   * Gửi lại đúng MỘT lần. Token mới mà vẫn 401 thì vấn đề không phải hết hạn — thử tiếp
   * chỉ tạo ra một vòng lặp gọi API vô tận.
   */
  it('token mới mà vẫn 401 thì KHÔNG thử lại nữa, và đá về màn đăng nhập', async () => {
    store.startSession(login(token(), 've-cu'));

    const dieuHuong = new Promise<unknown[]>((resolve) => {
      vi.spyOn(router, 'navigate').mockImplementation((commands) => {
        resolve(commands as unknown[]);
        return Promise.resolve(true);
      });
    });

    http.get('/api/employees').subscribe({ error: () => undefined });

    httpMock
      .expectOne('/api/employees')
      .flush(problem('Auth.InvalidCredentials', 'Hết hạn'), {
        status: 401,
        statusText: 'Unauthorized',
      });

    httpMock
      .expectOne(REFRESH_URL)
      .flush({ accessToken: token(), refreshToken: 've-moi', expiresInSeconds: 900 });

    httpMock
      .expectOne('/api/employees')
      .flush(problem('Auth.InvalidCredentials', 'Vẫn hết hạn'), {
        status: 401,
        statusText: 'Unauthorized',
      });

    expect(await dieuHuong).toEqual(['/login']);
    expect(store.isAuthenticated()).toBe(false);
  });

  /**
   * Vé gia hạn cũng chết → phiên chấm dứt thật. Xoá sạch và đưa về màn đăng nhập.
   */
  it('gia hạn thất bại thì xoá phiên và về màn đăng nhập', async () => {
    store.startSession(login(token(), 've-cu'));

    const dieuHuong = new Promise<unknown[]>((resolve) => {
      vi.spyOn(router, 'navigate').mockImplementation((commands) => {
        resolve(commands as unknown[]);
        return Promise.resolve(true);
      });
    });

    http.get('/api/employees').subscribe({ error: () => undefined });

    httpMock
      .expectOne('/api/employees')
      .flush(problem('Auth.InvalidCredentials', 'Hết hạn'), {
        status: 401,
        statusText: 'Unauthorized',
      });

    httpMock
      .expectOne(REFRESH_URL)
      .flush(problem('Auth.InvalidRefreshToken', 'Vé chết'), {
        status: 401,
        statusText: 'Unauthorized',
      });

    expect(await dieuHuong).toEqual(['/login']);
    expect(store.isAuthenticated()).toBe(false);
    expect(localStorage.getItem('onooffice.refresh-token')).toBeNull();
  });

  /**
   * ⭐ Sai mật khẩu cũng là 401 — nhưng KHÔNG được gia hạn, và KHÔNG được đá đi đâu cả.
   *
   * Lỗi đó phải hiện ngay trên form. Đá người dùng khỏi trang họ đang đứng vì gõ sai
   * mật khẩu là vô lý, mà gia hạn ở đó thì lại càng vô nghĩa.
   */
  it('sai mật khẩu ở màn đăng nhập KHÔNG kích hoạt gia hạn', async () => {
    const nhan = new Promise<AppError>((resolve) => {
      http.post('/api/auth/login', {}).subscribe({ error: (e: AppError) => resolve(e) });
    });

    httpMock
      .expectOne('/api/auth/login')
      .flush(problem('Auth.InvalidCredentials', 'Email hoặc mật khẩu không đúng.'), {
        status: 401,
        statusText: 'Unauthorized',
      });

    const error = await nhan;

    expect(error.code).toBe('Auth.InvalidCredentials');
    // httpMock.verify() ở afterEach sẽ đỏ nếu có lời gọi /refresh nào lọt ra.
  });

  it('chưa từng đăng nhập mà dính 401 thì về thẳng màn đăng nhập, không gọi gia hạn', async () => {
    const dieuHuong = new Promise<unknown[]>((resolve) => {
      vi.spyOn(router, 'navigate').mockImplementation((commands) => {
        resolve(commands as unknown[]);
        return Promise.resolve(true);
      });
    });

    http.get('/api/employees').subscribe({ error: () => undefined });

    httpMock
      .expectOne('/api/employees')
      .flush(problem('Auth.InvalidCredentials', 'Chưa đăng nhập'), {
        status: 401,
        statusText: 'Unauthorized',
      });

    expect(await dieuHuong).toEqual(['/login']);
  });
});
