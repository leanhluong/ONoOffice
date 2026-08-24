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
    // Đường dẫn tiếng Việt vì đây là màn người NGOÀI công ty nhìn thấy đầu tiên; phần
    // bên trong app vẫn dùng đường dẫn tiếng Anh cho khớp tên module.
    path: 'dang-ky',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/register/register').then((m) => m.Register),
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
        //
        // Quyền là user.read chứ không phải employee.read: màn này quản lý TÀI KHOẢN
        // đăng nhập, thuộc module Identity. Hồ sơ nhân sự (chức danh, phòng ban) là của
        // module Org và sẽ có màn riêng.
        path: 'nhan-su',
        canActivate: [permissionGuard('user.read')],
        loadComponent: () => import('./features/users/user-list').then((m) => m.UserList),
      },
      {
        // Hồ sơ của CHÍNH mình — không đòi quyền gì ngoài việc đã đăng nhập.
        path: 'tai-khoan',
        loadComponent: () => import('./features/account/account').then((m) => m.Account),
      },
      {
        path: 'vai-tro',
        canActivate: [permissionGuard('role.read')],
        loadComponent: () => import('./features/roles/role-list').then((m) => m.RoleList),
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
