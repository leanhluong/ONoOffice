import { HttpErrorResponse, type HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthStore } from '../auth/auth.store';
import { isAuthEndpoint } from '../auth/auth.service';
import { CORRELATION_ID_HEADER } from './correlation-id.interceptor';
import { toAppError } from './problem-details.mapper';

/**
 * Biến mọi lỗi HTTP thành một kiểu duy nhất (`AppError`) và xử lý 401 tập trung.
 *
 * Vì sao gom về một kiểu: nếu không, mỗi component lại tự bóc
 * `err.error?.errors?.[0]?.description` theo một kiểu khác nhau, và chỉ cần
 * backend đổi format là hỏng khắp nơi. Sau interceptor này, component chỉ cần
 * `error.message` để hiện và `error.code` để rẽ nhánh.
 *
 * Riêng 401: xoá phiên rồi đá về màn đăng nhập, kèm `returnUrl` để đăng nhập
 * xong quay lại đúng chỗ đang dở. NGOẠI TRỪ chính request đăng nhập — sai mật
 * khẩu cũng trả 401, mà đá người dùng khỏi trang họ đang đứng thì vô lý;
 * lỗi đó phải hiện ngay trên form.
 *
 * Interceptor này phải đứng CUỐI chuỗi để bọc được lỗi của các interceptor trước.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const store = inject(AuthStore);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        // Lỗi không phải HTTP (bug trong operator phía sau) — để nguyên cho
        // global error handler, đừng giả vờ nó là lỗi API.
        return throwError(() => error);
      }

      const correlationId = req.headers.get(CORRELATION_ID_HEADER);
      const appError = toAppError(error, correlationId);

      if (appError.kind === 'unauthorized' && !isAuthEndpoint(req.url)) {
        store.clear();
        void router.navigate(['/login'], {
          queryParams: { returnUrl: router.url },
        });
      }

      return throwError(() => appError);
    }),
  );
};
