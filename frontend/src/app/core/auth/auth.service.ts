import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import type { LoginRequest, LoginResponse } from '../models/auth.model';
import type { AppError } from '../models/api-error.model';
import { AuthStore } from './auth.store';

/** Các endpoint xác thực. Interceptor cũng dùng hằng này nên phải export. */
export const AUTH_ENDPOINTS = {
  login: '/api/auth/login',
  refresh: '/api/auth/refresh',
  logout: '/api/auth/logout',
} as const;

/** True nếu URL đang gọi là một endpoint xác thực (không cần gắn Bearer). */
export function isAuthEndpoint(url: string): boolean {
  return Object.values(AUTH_ENDPOINTS).some((path) => url.includes(path));
}

/**
 * Cầu nối duy nhất tới các endpoint xác thực của backend.
 *
 * Ranh giới trách nhiệm: service này gọi HTTP và ghi kết quả vào AuthStore.
 * Nó KHÔNG điều hướng trang — chuyển trang là việc của component/guard,
 * để service còn dùng lại được ở chỗ khác mà không kéo theo Router.
 *
 * !!! CHƯA KIỂM CHỨNG VỚI BACKEND THẬT !!!
 * Tại thời điểm viết, backend .NET chưa có `POST /api/auth/login`.
 * URL, tên trường request/response ở đây dựng theo mô tả hợp đồng API.
 * Khi backend lên, phải chạy lại luồng đăng nhập thật và đối chiếu:
 *   - tên trường: accessToken / refreshToken / expiresIn
 *   - tên claim trong token: sub / tenant_id / permission
 *   - mã lỗi khi sai mật khẩu (dự kiến 401 kèm Problem Details)
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly store = inject(AuthStore);

  /**
   * Đăng nhập bằng email + mật khẩu.
   * Lỗi phát ra từ đây đã được `error.interceptor` chuẩn hoá thành `AppError`.
   */
  login(credentials: LoginRequest): Observable<void> {
    return this.http
      .post<LoginResponse>(this.url(AUTH_ENDPOINTS.login), credentials)
      .pipe(map((response) => this.applySession(response)));
  }

  /**
   * Đổi refresh token lấy access token mới.
   * CHƯA ĐƯỢC GỌI Ở ĐÂU: việc tự động refresh khi gặp 401 sẽ làm sau, khi đã
   * chốt được hành vi thật của backend (mã lỗi, có xoay vòng refresh token
   * hay không). Viết sẵn để chỗ nối vào đã có sẵn.
   */
  refresh(): Observable<void> {
    const refreshToken = this.store.refreshToken();
    if (!refreshToken) {
      return throwError(() => this.noSessionError());
    }
    return this.http
      .post<LoginResponse>(this.url(AUTH_ENDPOINTS.refresh), { refreshToken })
      .pipe(map((response) => this.applySession(response)));
  }

  /**
   * Đăng xuất. Xoá phiên ở client TRƯỚC rồi mới báo server, vì với người dùng
   * thì "đã thoát" phải là chắc chắn — server có lỗi cũng không được kẹt lại
   * trong trạng thái còn đăng nhập.
   */
  logout(): Observable<void> {
    const refreshToken = this.store.refreshToken();
    this.store.clear();
    if (!refreshToken) {
      return of(undefined);
    }
    return this.http.post<void>(this.url(AUTH_ENDPOINTS.logout), { refreshToken }).pipe(
      map(() => undefined),
      // Server thu hồi token thất bại cũng mặc kệ: client đã sạch rồi.
      catchError(() => of(undefined)),
    );
  }

  private applySession(response: LoginResponse): void {
    const accepted = this.store.setSessionFromResponse(response);
    if (!accepted) {
      // Token trả về không giải mã được hoặc thiếu `sub`/`tenant_id`.
      // Đây là lỗi hợp đồng giữa FE và BE, không phải lỗi người dùng.
      throw this.invalidTokenError();
    }
  }

  private url(path: string): string {
    return `${environment.apiBaseUrl}${path}`;
  }

  private invalidTokenError(): AppError {
    return {
      kind: 'server',
      status: 0,
      code: 'Auth.InvalidTokenPayload',
      message: 'Máy chủ trả về token không hợp lệ. Vui lòng liên hệ quản trị viên.',
      details: [],
      fieldErrors: {},
      correlationId: null,
    };
  }

  private noSessionError(): AppError {
    return {
      kind: 'unauthorized',
      status: 401,
      code: 'Auth.NoRefreshToken',
      message: 'Phiên làm việc đã kết thúc. Vui lòng đăng nhập lại.',
      details: [],
      fieldErrors: {},
      correlationId: null,
    };
  }
}
