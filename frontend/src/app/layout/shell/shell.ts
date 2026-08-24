import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { AuthStore } from '../../core/auth/auth.store';
import { TranslatePipe } from '@ngx-translate/core';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';
import { Prefs } from '../../shared/ui/prefs/prefs';

/** Một mục trên thanh điều hướng trái. */
interface NavItem {
  /** Khoá dịch, KHÔNG phải câu chữ — nếu không thì menu vĩnh viễn một thứ tiếng. */
  readonly labelKey: string;
  readonly path: string;
  readonly icon: string;
  /** Quyền cần có để thấy mục này. Rỗng = ai đăng nhập cũng thấy. */
  readonly permissions: readonly string[];
}

/**
 * Khung ngoài của phần app đã đăng nhập: topbar + sidebar + vùng nội dung.
 *
 * Vì sao là một route cha có `children` chứ không phải component bọc trong
 * từng màn: làm thế này thì chuyển trang chỉ vẽ lại phần `<router-outlet>`
 * bên trong, sidebar giữ nguyên trạng thái (đang cuộn tới đâu, đang mở nhóm
 * nào). Nếu mỗi màn tự nhúng shell thì cả sidebar bị dựng lại mỗi lần.
 *
 * Danh sách menu khai báo bằng dữ liệu (`navItems`) thay vì viết tay từng thẻ
 * `<a>`: thêm module mới chỉ cần thêm một dòng, và quyền của mục nằm ngay
 * cạnh đường dẫn nên khó quên.
 */
@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    HasPermissionDirective,
    TranslatePipe,
    Prefs,
  ],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  protected readonly store = inject(AuthStore);

  protected readonly sidebarOpen = signal(true);

  protected readonly navItems: readonly NavItem[] = [
    { labelKey: 'nav.dashboard', path: '/dashboard', icon: '▦', permissions: [] },
    { labelKey: 'nav.employees', path: '/employees', icon: '☺', permissions: ['employee.read'] },
  ];

  protected toggleSidebar(): void {
    this.sidebarOpen.update((open) => !open);
  }

  protected logout(): void {
    // Điều hướng ngay, không chờ server trả lời: AuthService đã xoá phiên ở
    // client trước rồi, nên giữ người dùng lại chờ mạng là vô nghĩa.
    this.auth.logout().subscribe();
    void this.router.navigate(['/login']);
  }
}
