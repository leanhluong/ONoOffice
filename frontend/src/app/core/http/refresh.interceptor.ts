import type { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { EMPTY, catchError, switchMap, throwError } from 'rxjs';
import { AuthService, isAuthEndpoint } from '../auth/auth.service';
import { AuthStore } from '../auth/auth.store';
import { isAppError } from '../models/api-error.model';

/**
 * Gặp 401 thì tự gia hạn phiên rồi gửi lại request — người dùng không thấy gì cả.
 *
 * <b>Vì sao cần:</b> access token sống 15 phút. Không có lớp này thì cứ 15 phút một lần,
 * thao tác đang làm dở bị cắt ngang và người dùng bị đá về màn đăng nhập. Với app dùng
 * cả ngày, đó là hàng chục lần gõ mật khẩu mỗi ngày mà chẳng bảo vệ thêm được gì —
 * họ vẫn là chính họ, chỉ là cái vé hết hạn.
 *
 * <b>Vị trí trong chuỗi rất quan trọng.</b> Nó phải nằm TRƯỚC <c>authInterceptor</c>:
 *
 * <pre>
 *   correlationId → refresh → auth → error → [mạng]
 * </pre>
 *
 * Vì lần gửi lại phải đi qua <c>auth</c> một lần nữa để gắn token MỚI. Đặt sau <c>auth</c>
 * thì request được gửi lại vẫn mang đúng cái token vừa hết hạn — 401 lần nữa, và lần này
 * không ai cứu.
 *
 * <b>Ba chỗ cố ý không đụng vào:</b>
 * <ol>
 * <li><b>Endpoint xác thực.</b> <c>/login</c> trả 401 khi sai mật khẩu — gia hạn ở đó là
 * vô nghĩa. <c>/refresh</c> trả 401 khi vé chết — gia hạn lại chính nó là vòng lặp.</li>
 * <li><b>Đã thử một lần rồi.</b> Chỉ gửi lại đúng MỘT lần. Token mới mà vẫn 401 thì vấn
 * đề không phải hết hạn, và thử tiếp chỉ tạo ra một vòng lặp gọi API vô tận.</li>
 * <li><b>Không có vé gia hạn.</b> Chưa từng đăng nhập — đá thẳng về màn đăng nhập.</li>
 * </ol>
 */
export const refreshInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const store = inject(AuthStore);
  const router = inject(Router);

  if (isAuthEndpoint(req.url)) {
    return next(req);
  }

  // Cờ cục bộ cho ĐÚNG request này. Mỗi request chạy qua interceptor một lần nên mỗi
  // request có cờ riêng — không có chuyện hai request giẫm lên cờ của nhau.
  let daThuGiaHan = false;

  return next(req).pipe(
    catchError((error: unknown) => {
      const laHetPhien = isAppError(error) && error.kind === 'unauthorized';

      if (!laHetPhien || daThuGiaHan) {
        return throwError(() => error);
      }

      daThuGiaHan = true;

      if (!store.refreshToken()) {
        return dangXuatVaVeManDangNhap();
      }

      return auth.refresh().pipe(
        // Gửi lại CHÍNH request cũ. Giữ nguyên cả X-Correlation-Id, nên trong log của
        // backend hai lần gọi này nối liền thành một hành trình — đúng như nó vốn là.
        switchMap(() => next(req)),
        catchError(() => dangXuatVaVeManDangNhap()),
      );
    }),
  );

  /**
   * Gia hạn thất bại nghĩa là phiên đã chết thật — vé hết hạn, bị thu hồi, hoặc backend
   * phát hiện nó bị dùng lại và huỷ cả chuỗi.
   *
   * Trả <c>EMPTY</c> chứ không phát lại lỗi: người gọi sắp bị chuyển trang rồi, ném thêm
   * một lỗi vào mặt họ chỉ khiến màn hình đang thoát hiện thêm một thông báo vô nghĩa.
   */
  function dangXuatVaVeManDangNhap() {
    store.clear();

    void router.navigate(['/login'], {
      queryParams: { returnUrl: router.url, lyDo: 'het-phien' },
    });

    return EMPTY;
  }
};
