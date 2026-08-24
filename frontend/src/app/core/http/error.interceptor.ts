import { HttpErrorResponse, type HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { CORRELATION_ID_HEADER } from './correlation-id.interceptor';
import { toAppError } from './problem-details.mapper';

/**
 * Biến mọi lỗi HTTP thành một kiểu duy nhất: <c>AppError</c>.
 *
 * Vì sao gom về một kiểu: nếu không, mỗi component lại tự bóc
 * <c>err.error?.errors?.[0]?.description</c> theo một kiểu khác nhau, và chỉ cần backend
 * đổi hình dạng là hỏng khắp nơi. Sau interceptor này, component chỉ cần <c>error.code</c>
 * để rẽ nhánh và <c>error.message</c> để dự phòng.
 *
 * <b>Nó KHÔNG xử lý 401.</b> Trước đây nó tự xoá phiên và đá về màn đăng nhập, nhưng như
 * vậy thì không còn chỗ nào để thử gia hạn — người dùng bị đá ra đúng 15 phút một lần.
 * Việc đó nay thuộc về <c>refreshInterceptor</c>, nơi biết cách gia hạn rồi gửi lại.
 * Ở đây chỉ dịch lỗi sang một hình dạng chung.
 *
 * Đứng CUỐI chuỗi (gần mạng nhất) để mọi lỗi đi lên đều đã được chuẩn hoá trước khi các
 * interceptor bên ngoài nhìn thấy.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        // Lỗi không phải HTTP (bug trong operator phía sau) — để nguyên cho global error
        // handler, đừng giả vờ nó là lỗi API.
        return throwError(() => error);
      }

      return throwError(() => toAppError(error, req.headers.get(CORRELATION_ID_HEADER)));
    }),
  );
