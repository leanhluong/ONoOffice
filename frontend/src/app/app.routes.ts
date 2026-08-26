import type { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/guards/auth.guard';
import { permissionGuard } from './core/guards/permission.guard';

/**
 * Bản đồ route của app.
 *
 * ═══════════════════════════════════════════════════════════════════════
 *  BA VÙNG, VÀ VÌ SAO CHÚNG TÁCH NHAU
 * ═══════════════════════════════════════════════════════════════════════
 *
 * <pre>
 *   người ngoài   /login · /dang-ky        không khung nào, mỗi màn tự dựng
 *   🟢 khung A     /  ·  /me                rail biểu tượng + vùng nội dung
 *   🔴 khung B     /admin/*                 thanh ngang + sidebar 240px
 * </pre>
 *
 * Luật ranh giới giữa A và B, một câu, kiểm được: <b>màn thao tác lên NGƯỜI KHÁC hoặc lên
 * CẤU HÌNH WORKSPACE thì thuộc khung B; màn thao tác lên CHÍNH MÌNH hoặc lên dữ liệu công
 * việc hằng ngày thì thuộc khung A.</b> Nên `/me` (hồ sơ của tôi) ở A, còn `/admin/users`
 * (tạo tài khoản cho người khác) ở B — dù cả hai đều sửa một bản ghi `User`.
 *
 * Chi tiết: `docs/07-giao-dien/chung/khung-man-hinh.md`.
 *
 * ═══════════════════════════════════════════════════════════════════════
 *  BỐN QUYẾT ĐỊNH VỀ ĐƯỜNG DẪN
 * ═══════════════════════════════════════════════════════════════════════
 *
 * 1. <b>Mọi màn đều `loadComponent` (lazy).</b> Người chưa đăng nhập chỉ tải đúng bundle
 *    màn login. Với app nhiều module thì khác biệt này lớn dần theo thời gian.
 *
 * 2. <b>Guard nằm ở route CHA.</b> Thêm màn mới chỉ cần thêm một `children` — không thể
 *    quên gắn guard. Chống lỗi bằng cấu trúc, không bằng kỷ luật.
 *
 * 3. <b>Đường dẫn bên trong app viết bằng TIẾNG ANH</b>, khớp tên module. Chỉ hai màn
 *    người NGOÀI công ty nhìn thấy mới dùng tiếng Việt (`/dang-ky`) — họ là khách, và
 *    thanh địa chỉ là thứ đầu tiên họ đọc. Luật này từng được ghi ra rồi bị phá ngay
 *    (`/nhan-su`, `/tai-khoan`, `/vai-tro`); nay đã dọn.
 *
 * 4. <b>`/me` chứ không `/account`</b> — khớp thẳng endpoint `GET /api/me` đang có.
 */
export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    // Đường dẫn tiếng Việt vì đây là màn người NGOÀI công ty nhìn thấy đầu tiên.
    path: 'dang-ky',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/register/register').then((m) => m.Register),
  },

  // ══════════════════════════════════════════════════════════════════
  // 🔴 KHUNG B — vùng quản trị
  //
  // Khai TRƯỚC khung A vì Angular khớp route theo thứ tự, và khung A có một route cha
  // rỗng (`path: ''`) ôm cả `**`. Đặt sau thì `/admin` bị khung A nuốt và rơi vào màn
  // "không tìm thấy trang" — hỏng im lặng, vì URL vẫn đúng còn màn hình thì sai.
  // ══════════════════════════════════════════════════════════════════
  {
    path: 'admin',
    canActivate: [
      authGuard,

      // Chế độ mặc định của `permissionGuard` là `any`: có MỘT trong hai quyền là vào
      // được vùng quản trị. Người vào bằng `role.read` sẽ thấy sidebar chỉ còn nhánh
      // "Vai trò & quyền" — `*appHasPermission` lo phần đó.
      permissionGuard('user.read', 'role.read'),
    ],
    loadComponent: () => import('./layout/admin-shell/admin-shell').then((m) => m.AdminShell),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/admin/overview/overview').then((m) => m.AdminOverview),
      },
      {
        // Quyền là `user.read` chứ không phải `employee.read`: màn này quản lý TÀI KHOẢN
        // đăng nhập, thuộc module Identity. Hồ sơ nhân sự (chức danh, phòng ban) là của
        // module Org và sẽ có màn riêng.
        path: 'users',
        canActivate: [permissionGuard('user.read')],
        loadComponent: () => import('./features/users/user-list').then((m) => m.UserList),
      },
      {
        path: 'roles',
        canActivate: [permissionGuard('role.read')],
        loadComponent: () => import('./features/roles/role-list').then((m) => m.RoleList),
      },
      {
        // SỬA cây tổ chức → khung quản trị. Xem cây thì ở `/contacts`, khung app.
        path: 'departments',
        canActivate: [permissionGuard('department.read')],
        loadComponent: () =>
          import('./features/departments/department-tree').then((m) => m.DepartmentTree),
      },
    ],
  },

  // ══════════════════════════════════════════════════════════════════
  // 🟢 KHUNG A — không gian làm việc
  // ══════════════════════════════════════════════════════════════════
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell/shell').then((m) => m.Shell),
    children: [
      // Hôm nay là Bảng điều khiển. Khi màn Trao đổi xong thì đây đổi thành `/chat` —
      // mở ONoOffice là vào thẳng chat, giống Lark và Zalo.
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard),
      },
      {
        // Hồ sơ của CHÍNH mình — không đòi quyền gì ngoài việc đã đăng nhập.
        path: 'me',
        loadComponent: () => import('./features/account/account').then((m) => m.Account),
      },
      {
        // Danh bạ: TRA CỨU đồng nghiệp. `employee.read` là quyền cả bốn vai hệ thống đều
        // có — đây là màn nhân viên mở hằng ngày, không phải công cụ quản trị.
        path: 'contacts',
        canActivate: [permissionGuard('employee.read')],
        loadComponent: () => import('./features/contacts/contact-list').then((m) => m.ContactList),
      },
      /**
       * Trung tâm trợ giúp. KHÔNG đòi quyền gì ngoài việc đã đăng nhập — người cần hướng
       * dẫn nhất thường là người có ít quyền nhất.
       *
       * Hai đường: `/huong-dan` là trang chủ, `/huong-dan/:ma` là một bài. Mã bài nằm
       * trên URL vì người ta gửi link hướng dẫn cho nhau — một trung tâm trợ giúp không
       * chia sẻ được từng bài thì mất một nửa công dụng.
       */
      {
        path: 'huong-dan',
        loadComponent: () => import('./features/help/help').then((m) => m.Help),
      },
      {
        path: 'huong-dan/:ma',
        loadComponent: () => import('./features/help/help').then((m) => m.Help),
      },
      {
        path: 'forbidden',
        loadComponent: () => import('./features/errors/forbidden').then((m) => m.Forbidden),
      },

      // ── Đường dẫn cũ ────────────────────────────────────────────
      // Giữ lại vì chúng đã nằm trong ảnh chụp màn hình, trong tài liệu, và có thể trong
      // bookmark của chính người đang phát triển. Một `redirectTo` rẻ hơn nhiều so với
      // một màn "không tìm thấy trang" mà không ai đoán được vì sao.
      { path: 'tai-khoan', redirectTo: 'me', pathMatch: 'full' },
      { path: 'nhan-su', redirectTo: '/admin/users', pathMatch: 'full' },
      { path: 'vai-tro', redirectTo: '/admin/roles', pathMatch: 'full' },

      {
        path: '**',
        loadComponent: () => import('./features/errors/not-found').then((m) => m.NotFound),
      },
    ],
  },
];
