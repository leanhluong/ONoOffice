import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthStore } from '../auth/auth.store';
import { isAppError, type AppError } from '../models/api-error.model';
import { authInterceptor } from './auth.interceptor';
import { CORRELATION_ID_HEADER, correlationIdInterceptor } from './correlation-id.interceptor';
import { errorInterceptor } from './error.interceptor';

/** Dựng một JWT giả (chữ ký rác — frontend không xác thực chữ ký). */
function fakeJwt(payload: Record<string, unknown>): string {
  const encode = (value: object): string =>
    btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${encode({ alg: 'RS256', typ: 'JWT' })}.${encode(payload)}.chu-ky-gia`;
}

describe('Chuỗi interceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let store: AuthStore;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(
          withInterceptors([correlationIdInterceptor, authInterceptor, errorInterceptor]),
        ),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    store = TestBed.inject(AuthStore);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('gắn X-Correlation-Id vào mọi request', () => {
    http.get('/api/ping').subscribe();
    const req = httpMock.expectOne('/api/ping');
    expect(req.request.headers.get(CORRELATION_ID_HEADER)).toBeTruthy();
    req.flush({});
  });

  it('không gắn Authorization khi chưa đăng nhập', () => {
    http.get('/api/ping').subscribe();
    const req = httpMock.expectOne('/api/ping');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('gắn Bearer token khi đã có phiên', () => {
    const token = fakeJwt({
      sub: 'u-1',
      tenant_id: 't-1',
      permission: ['employee.read'],
      exp: Math.floor(Date.now() / 1000) + 900,
    });
    expect(
      store.setSessionFromResponse({ accessToken: token, refreshToken: 'r', expiresIn: 900 }),
    ).toBe(true);

    http.get('/api/ping').subscribe();
    const req = httpMock.expectOne('/api/ping');
    expect(req.request.headers.get('Authorization')).toBe(`Bearer ${token}`);
    req.flush({});
  });

  it('KHÔNG gắn Bearer vào endpoint đăng nhập', () => {
    store.setSessionFromResponse({
      accessToken: fakeJwt({ sub: 'u-1', tenant_id: 't-1' }),
      refreshToken: 'r',
      expiresIn: 900,
    });

    http.post('/api/auth/login', {}).subscribe({ error: () => undefined });
    const req = httpMock.expectOne('/api/auth/login');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({}, { status: 401, statusText: 'Unauthorized' });
  });

  it('chuyển Problem Details thành AppError', () => {
    let captured: AppError | null = null;
    http.post('/api/employees', {}).subscribe({
      error: (err: unknown) => {
        captured = isAppError(err) ? err : null;
      },
    });

    httpMock.expectOne('/api/employees').flush(
      {
        type: 'https://tools.ietf.org/html/rfc7231#section-6.5.8',
        title: 'Conflict',
        status: 409,
        errors: [{ code: 'Employee.EmailTaken', description: 'Email đã có người dùng.' }],
      },
      { status: 409, statusText: 'Conflict' },
    );

    const error = captured as AppError | null;
    expect(error).not.toBeNull();
    expect(error?.kind).toBe('conflict');
    expect(error?.code).toBe('Employee.EmailTaken');
    expect(error?.message).toBe('Email đã có người dùng.');
    expect(error?.correlationId).toBeTruthy();
  });

  it('đọc được dictionary ModelState thành fieldErrors', () => {
    let captured: AppError | null = null;
    http.post('/api/employees', {}).subscribe({
      error: (err: unknown) => {
        captured = isAppError(err) ? err : null;
      },
    });

    httpMock
      .expectOne('/api/employees')
      .flush(
        { title: 'Bad Request', status: 400, errors: { Email: ['Email không hợp lệ.'] } },
        { status: 400, statusText: 'Bad Request' },
      );

    const error = captured as AppError | null;
    expect(error?.kind).toBe('validation');
    expect(error?.fieldErrors['email']).toEqual(['Email không hợp lệ.']);
  });

  it('401 ngoài endpoint auth thì xoá phiên và đá về /login', () => {
    store.setSessionFromResponse({
      accessToken: fakeJwt({ sub: 'u-1', tenant_id: 't-1' }),
      refreshToken: 'r',
      expiresIn: 900,
    });
    expect(store.isAuthenticated()).toBe(true);

    // Theo dõi lời gọi navigate thay vì đọc router.url: TestBed chỉ dựng router
    // rỗng nên điều hướng tới /login sẽ không khớp route nào.
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    http.get('/api/employees').subscribe({ error: () => undefined });
    httpMock.expectOne('/api/employees').flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(store.isAuthenticated()).toBe(false);
    expect(navigate).toHaveBeenCalledWith(['/login'], expect.anything());
  });
});
