import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, finalize, map, of, shareReplay, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import type {
  LoginRequest,
  LoginResponse,
  RefreshResponse,
  RegisterWorkspaceRequest,
  RegisterWorkspaceResponse,
} from '../models/auth.model';
import type { AppError } from '../models/api-error.model';
import { AuthStore } from './auth.store';

/** Các endpoint xác thực. Interceptor cũng dùng hằng này nên phải export. */
export const AUTH_ENDPOINTS = {
  login: '/api/auth/login',
  registerWorkspace: '/api/auth/register-workspace',
  refresh: '/api/auth/refresh',
  logout: '/api/auth/logout',
} as const;

/** True nếu URL đang gọi là một endpoint xác thực (không gắn Bearer, không tự gia hạn). */
export function isAuthEndpoint(url: string): boolean {
  return Object.values(AUTH_ENDPOINTS).some((path) => url.includes(path));
}

/**
 * Cầu nối duy nhất tới các endpoint xác thực.
 *
 * Ranh giới trách nhiệm: service này gọi HTTP và ghi kết quả vào <c>AuthStore</c>. Nó
 * KHÔNG điều hướng trang — chuyển trang là việc của component và guard, để service còn
 * dùng lại được ở chỗ khác mà không kéo theo Router.
 *
 * Đã đối chiếu với backend chạy thật ngày 2026-08-24.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly store = inject(AuthStore);

  /**
   * Lời gọi gia hạn đang bay, nếu có.
   *
   * ⭐ <b>Đây là thứ chống "bão gia hạn".</b> Một màn hình mở ra thường bắn 5–6 request
   * cùng lúc. Access token vừa hết hạn thì cả 5–6 cùng nhận 401, và nếu mỗi cái tự gọi
   * <c>/refresh</c> thì có 5–6 lời gọi gia hạn chạy song song.
   *
   * Hậu quả không phải là chậm, mà là <b>mất phiên</b>: refresh token xoay vòng và chỉ
   * dùng được một lần. Lời gọi thứ nhất tiêu vé và nhận vé mới; lời gọi thứ hai vẫn cầm
   * vé cũ — backend thấy vé đã thu hồi được dùng lại, kết luận là bị trộm, và
   * <b>thu hồi toàn bộ chuỗi</b>. Người dùng bị đá ra vì chính app của mình.
   *
   * Giữ một Observable dùng chung là cách rẻ nhất để chỉ có đúng một lời gọi.
   */
  private inFlightRefresh: Observable<void> | null = null;

  login(credentials: LoginRequest): Observable<void> {
    return this.http.post<LoginResponse>(this.url(AUTH_ENDPOINTS.login), credentials).pipe(
      map((response) => {
        if (!this.store.startSession(response)) {
          throw this.invalidTokenError();
        }
      }),
    );
  }

  /**
   * Tạo workspace mới cùng tài khoản chủ sở hữu.
   *
   * Backend trả về CẢ cặp token, nên hàm này mở luôn phiên: người vừa đặt mật khẩu xong
   * không phải gõ lại nó ở màn đăng nhập.
   *
   * Trả về cả phản hồi chứ không phải <c>void</c> như <c>login</c> — màn đăng ký cần
   * <c>workspace.code</c> để hiện thẻ xác nhận, và đó là lần DUY NHẤT ta có nó: mã
   * workspace không nằm trong access token.
   */
  registerWorkspace(request: RegisterWorkspaceRequest): Observable<RegisterWorkspaceResponse> {
    return this.http
      .post<RegisterWorkspaceResponse>(this.url(AUTH_ENDPOINTS.registerWorkspace), request)
      .pipe(
        map((response) => {
          if (!this.store.startSession(response)) {
            throw this.invalidTokenError();
          }

          return response;
        }),
      );
  }

  /**
   * Đổi vé gia hạn lấy cặp token mới.
   *
   * Gọi nhiều lần cùng lúc chỉ sinh ra MỘT request — xem <c>inFlightRefresh</c>.
   */
  refresh(): Observable<void> {
    if (this.inFlightRefresh) {
      return this.inFlightRefresh;
    }

    const refreshToken = this.store.refreshToken();

    if (!refreshToken) {
      return throwError(() => this.noSessionError());
    }

    this.inFlightRefresh = this.http
      .post<RefreshResponse>(this.url(AUTH_ENDPOINTS.refresh), { refreshToken })
      .pipe(
        map((response) => {
          // Có phiên trong bộ nhớ thì giữ nguyên người dùng; không có (vừa mở lại tab)
          // thì lấy lại từ bản đã ghi cạnh vé gia hạn.
          const accepted = this.store.session()
            ? this.store.renewSession(response)
            : this.store.restoreSession(response);

          if (!accepted) {
            throw this.invalidTokenError();
          }
        }),

        // Dọn chỗ trước khi phát kết quả, để lời gọi TIẾP THEO bắt đầu một request mới
        // chứ không dùng lại kết quả cũ đã nguội.
        finalize(() => {
          this.inFlightRefresh = null;
        }),

        // refCount: false — người đến muộn vẫn nhận được kết quả (hoặc lỗi) đã có,
        // không kích hoạt thêm request nào.
        shareReplay({ bufferSize: 1, refCount: false }),
      );

    return this.inFlightRefresh;
  }

  /**
   * Đăng xuất.
   *
   * Xoá phiên ở client TRƯỚC rồi mới báo server: với người dùng thì "đã thoát" phải là
   * chắc chắn. Server lỗi cũng không được để họ kẹt lại trong trạng thái còn đăng nhập.
   */
  logout(): Observable<void> {
    const refreshToken = this.store.refreshToken();
    this.store.clear();

    if (!refreshToken) {
      return of(undefined);
    }

    return this.http.post<void>(this.url(AUTH_ENDPOINTS.logout), { refreshToken }).pipe(
      map(() => undefined),
      // Thu hồi vé thất bại cũng mặc kệ — client đã sạch rồi.
      catchError(() => of(undefined)),
    );
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
