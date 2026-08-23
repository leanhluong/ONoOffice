import type { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import { AuthStore } from '../auth/auth.store';
import { isAuthEndpoint } from '../auth/auth.service';

/**
 * Gắn `Authorization: Bearer <token>` cho các request đi tới backend của mình.
 *
 * Ba trường hợp CỐ Ý bỏ qua:
 * 1. Request đã tự đặt Authorization — người gọi biết rõ họ đang làm gì.
 * 2. Endpoint đăng nhập/refresh — gửi token cũ (có thể đã hết hạn) tới đó
 *    chỉ gây nhiễu, thậm chí khiến backend từ chối sớm.
 * 3. URL trỏ ra ngoài backend của mình (CDN, dịch vụ bên thứ ba) — rò token
 *    ra domain lạ là lỗi bảo mật thật sự, không phải chuyện nhỏ.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const store = inject(AuthStore);
  const token = store.accessToken();

  if (!token || req.headers.has('Authorization') || isAuthEndpoint(req.url)) {
    return next(req);
  }

  if (!isOwnBackend(req.url)) {
    return next(req);
  }

  return next(
    req.clone({
      setHeaders: { Authorization: `Bearer ${token}` },
    }),
  );
};

/** URL tương đối, hoặc URL tuyệt đối trùng gốc với `apiBaseUrl`. */
function isOwnBackend(url: string): boolean {
  if (!/^https?:\/\//i.test(url)) {
    return true;
  }
  const base = environment.apiBaseUrl;
  if (base.length > 0) {
    return url.startsWith(base);
  }
  // apiBaseUrl rỗng = cùng origin với trang đang chạy.
  return typeof location !== 'undefined' && url.startsWith(location.origin);
}
