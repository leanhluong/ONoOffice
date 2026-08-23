import { inject } from '@angular/core';
import { Router, type CanActivateFn } from '@angular/router';
import { AuthStore } from '../auth/auth.store';

/**
 * Chặn route theo PERMISSION (`employee.read`), không theo role.
 *
 * Vì sao không kiểm role: role chỉ là một cái tên gộp nhiều quyền lại.
 * Hôm nay "Trưởng phòng" được xem lương, mai công ty đổi ý — nếu code viết
 * `if (role === 'Manager')` thì phải sửa và build lại frontend. Kiểm theo
 * permission thì admin chỉ cần gỡ quyền trong trang phân quyền là xong.
 * Backend cũng kiểm theo permission, hai bên nói cùng một ngôn ngữ.
 *
 * Nhắc lại: guard chỉ để giao diện đỡ khó chịu, KHÔNG phải hàng rào bảo mật.
 * Người dùng sửa localStorage vẫn vào được route, nhưng API sẽ trả 403.
 *
 * Hai cách dùng:
 *
 * 1. Truyền thẳng (khuyến nghị — TypeScript kiểm được, đọc route là thấy quyền):
 *    { path: 'employees', canActivate: [permissionGuard('employee.read')], ... }
 *
 * 2. Khai trong `data` (khi cần cấu hình động):
 *    { path: 'employees', canActivate: [permissionGuard()],
 *      data: { permissions: ['employee.read'], permissionMode: 'all' } }
 */
export function permissionGuard(...required: string[]): CanActivateFn {
  return (route, state) => {
    const store = inject(AuthStore);
    const router = inject(Router);

    // `noPropertyAccessFromIndexSignature` bật nên phải truy cập bằng ngoặc vuông.
    const fromData = route.data['permissions'] as string[] | string | undefined;
    const mode = (route.data['permissionMode'] as 'any' | 'all' | undefined) ?? 'any';

    const permissions =
      required.length > 0 ? required : fromData ? [fromData].flat() : ([] as string[]);

    // Chưa đăng nhập thì không phải chuyện thiếu quyền — đưa về màn đăng nhập.
    if (!store.isAuthenticated()) {
      return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
    }

    if (permissions.length === 0) {
      return true;
    }

    const allowed =
      mode === 'all' ? store.hasAllPermissions(permissions) : store.hasAnyPermission(permissions);

    // Đã đăng nhập mà thiếu quyền: đưa sang màn "không có quyền", KHÔNG đưa về
    // login. Bắt đăng nhập lại chẳng giải quyết được gì, chỉ làm người dùng rối.
    return allowed
      ? true
      : router.createUrlTree(['/forbidden'], {
          queryParams: { required: permissions.join(',') },
        });
  };
}
