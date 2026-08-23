import { inject } from '@angular/core';
import { Router, type CanActivateFn } from '@angular/router';
import { AuthStore } from '../auth/auth.store';

/**
 * Chặn route khi chưa đăng nhập.
 *
 * Trả về `UrlTree` chứ không gọi `router.navigate`: Angular sẽ huỷ điều hướng
 * hiện tại và chuyển hướng trong cùng một chu kỳ, không để lộ nửa giây màn
 * hình trắng rồi mới nhảy sang trang khác.
 *
 * `returnUrl` được đính kèm để đăng nhập xong quay lại đúng trang đang muốn vào.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const store = inject(AuthStore);
  const router = inject(Router);

  if (store.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url },
  });
};

/**
 * Ngược lại với `authGuard`: đã đăng nhập rồi thì không vào màn login nữa.
 * Tránh cảnh người dùng bấm nút Back rồi thấy lại form đăng nhập trống.
 */
export const guestGuard: CanActivateFn = () => {
  const store = inject(AuthStore);
  const router = inject(Router);

  return store.isAuthenticated() ? router.createUrlTree(['/dashboard']) : true;
};
