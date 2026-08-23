import type { HttpInterceptorFn } from '@angular/common/http';

/** Tên header phải khớp đúng với thứ backend .NET đọc. */
export const CORRELATION_ID_HEADER = 'X-Correlation-Id';

/**
 * Sinh một correlation id mới.
 * `crypto.randomUUID` chỉ tồn tại trong secure context (https hoặc localhost);
 * khi chạy http trên IP nội bộ thì không có, nên phải có phương án dự phòng.
 */
export function newCorrelationId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }
  // Dự phòng: không cần bảo mật, chỉ cần đủ khác nhau để tra log.
  const random = Math.random().toString(16).slice(2, 10);
  return `${Date.now().toString(16)}-${random}`;
}

/**
 * Gắn `X-Correlation-Id` vào mọi request.
 *
 * Vì sao cần: khi người dùng báo "bấm nút thì lỗi", ta hỏi họ mã lỗi hiển thị
 * trên màn hình rồi grep đúng một chuỗi đó trong log backend là ra toàn bộ
 * đường đi của request qua các service. Không có nó thì phải mò theo thời gian.
 *
 * Nếu request đã tự đặt sẵn header thì giữ nguyên — quy ước này khớp với
 * backend: có sẵn thì dùng lại, không có thì tự sinh.
 */
export const correlationIdInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.headers.has(CORRELATION_ID_HEADER)) {
    return next(req);
  }
  return next(
    req.clone({
      setHeaders: { [CORRELATION_ID_HEADER]: newCorrelationId() },
    }),
  );
};
