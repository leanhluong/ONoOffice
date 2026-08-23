import type { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/guards/auth.guard';
import { permissionGuard } from './core/guards/permission.guard';

/**
 * Bản đồ route của app.
 *
 * Hai quyết định đáng nói:
 *
 * 1. Mọi màn hình đều `loadComponent` (lazy). Nhờ vậy người chưa đăng nhập chỉ
 *    tải đúng bundle màn login, không kéo theo toàn bộ app. Với app nội bộ
 *    nhiều module thì khác biệt này lớn dần theo thời gian.
 *
 * 2. Phần đã đăng nhập nằm dưới MỘT route cha rỗng gắn `Shell` + `authGuard`.
 *    Thêm màn mới chỉ cần thêm một `children` — không thể quên gắn guard, vì
 *    guard nằm ở cha. Đây là cách chống lỗi bằng cấu trúc, không bằng kỷ luật.
 */
export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell/shell').then((m) => m.Shell),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard),
      },
      {
        // Ví dụ thật của chặn route theo permission (không theo role).
        path: 'employees',
        canActivate: [permissionGuard('employee.read')],
        loadComponent: () =>
          import('./features/employees/employee-list').then((m) => m.EmployeeList),
      },
      {
        path: 'forbidden',
        loadComponent: () => import('./features/errors/forbidden').then((m) => m.Forbidden),
      },
      {
        path: '**',
        loadComponent: () => import('./features/errors/not-found').then((m) => m.NotFound),
      },
    ],
  },
];
