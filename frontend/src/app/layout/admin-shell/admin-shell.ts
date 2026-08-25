import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  inject,
  signal,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';
import { AuthStore } from '../../core/auth/auth.store';
import { PopupService } from '../../core/ui/popup.service';
import { UserService } from '../../core/users/user.service';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';
import { PopupHost } from '../../shared/ui/popup-host/popup-host';
import { Prefs } from '../../shared/ui/prefs/prefs';

/** Một trang con trong sidebar quản trị. */
interface AdminChild {
  readonly labelKey: string;

  /** Rỗng = chưa làm; lúc đó nó là `<button>` và bấm vào thì nói thẳng. */
  readonly path: string;

  readonly permissions: readonly string[];
}

/** Một nhóm trong sidebar. Không có `children` thì nó tự là một trang. */
interface AdminGroup {
  /** Khoá chọn hình vẽ trong `@switch` của template. */
  readonly key: string;

  readonly labelKey: string;
  readonly path: string;
  readonly permissions: readonly string[];
  readonly children: readonly AdminChild[];
}

/**
 * Khung ngoài của vùng quản trị — <b>khuôn B</b>: thanh ngang + sidebar 240px + nội dung.
 *
 * <b>Vì sao đây là một khung RIÊNG, không phải một app trên rail của khung A:</b>
 *
 * <list type="number">
 * <item>Điều hướng quản trị <b>sâu</b>, không rộng. Khung A có 6 app ngang hàng nên rail
 * 56px là vừa. Ở đây là nhiều nhóm, mỗi nhóm vài trang con — cây hai cấp gập được thì
 * bắt buộc phải có chữ, và phải có 240px để chứa chữ đó. Không có chữ thì "Tuân thủ" và
 * "Bảo mật" là hai biểu tượng khiên gần giống hệt nhau.</item>
 * <item>Ở đây người ta <b>đọc</b>, không lướt. Thanh ngang ăn 56px chiều cao: đắt với màn
 * chat, rẻ với một trang cuộn dọc.</item>
 * <item>Thanh ngang là chỗ <b>duy nhất</b> đặt được thứ chỉ quản trị mới có: tên tổ chức,
 * gói đang dùng, và lối ra về lại app.</item>
 * </list>
 *
 * <b>Đánh đổi phải trả:</b> rời khung A là mất rail, tức mất đường tắt sang Trao đổi. Bù
 * bằng "Về không gian làm việc" đứng ngay ĐẦU sidebar.
 *
 * Nguồn thiết kế: `docs/07-giao-dien/chung/_khung-quantri.css` + `mountQuanTri` trong
 * `_shell.js`. CSS sinh tự động, nên component này không có style riêng.
 */
@Component({
  selector: 'app-admin-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    NgTemplateOutlet,
    HasPermissionDirective,
    TranslatePipe,
    Prefs,
    PopupHost,
  ],
  templateUrl: './admin-shell.html',
})
export class AdminShell {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly popups = inject(PopupService);
  private readonly translate = inject(TranslateService);
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly users = inject(UserService);

  protected readonly store = inject(AuthStore);

  constructor() {
    // Lỗi thì BỎ QUA, không hiện popup: vai trò dưới tên là thông tin phụ. Bắn một thông
    // báo đỏ vì không lấy được nó sẽ che mất lỗi thật của trang bên dưới.
    this.users.myProfile().subscribe({
      next: (profile) => this.roleLabel.set(profile.roleName),
      error: () => this.roleLabel.set(''),
    });
  }

  /** Ẩn hẳn sidebar. Không thu nhỏ — một sidebar 56px chỉ có biểu tượng thì mất hết chữ,
   *  mà chữ chính là lý do khuôn này tồn tại. */
  protected readonly collapsed = signal(false);

  protected readonly openMenu = signal<'toi' | 'caidat' | null>(null);

  /**
   * Nhóm nào đang mở.
   *
   * Mặc định mở "Tổ chức" vì đó là nhóm chứa hai trang duy nhất đã chạy thật. Gập hết lại
   * rồi bắt người dùng tự tìm là cách chắc chắn nhất làm họ lạc trong một cây nhiều nhánh.
   */
  private readonly openGroups = signal<ReadonlySet<string>>(new Set(['org']));

  protected readonly groups: readonly AdminGroup[] = [
    {
      key: 'overview',
      labelKey: 'admin.overview',
      path: '/admin',
      permissions: [],
      children: [],
    },
    {
      key: 'org',
      labelKey: 'admin.org',
      path: '',
      permissions: ['user.read', 'role.read', 'department.read'],
      children: [
        { labelKey: 'admin.members', path: '/admin/users', permissions: ['user.read'] },
        {
          labelKey: 'admin.departments',
          path: '/admin/departments',
          permissions: ['department.read'],
        },
        { labelKey: 'nav.roles', path: '/admin/roles', permissions: ['role.read'] },
      ],
    },
    {
      key: 'billing',
      labelKey: 'admin.billing',
      path: '',
      permissions: [],
      children: [
        { labelKey: 'admin.plan', path: '', permissions: [] },
        { labelKey: 'admin.invoices', path: '', permissions: [] },
      ],
    },
    {
      key: 'storage',
      labelKey: 'admin.storage',
      path: '',
      permissions: [],
      children: [{ labelKey: 'admin.quota', path: '', permissions: [] }],
    },
    {
      key: 'security',
      labelKey: 'admin.security',
      path: '',
      permissions: [],
      children: [
        { labelKey: 'admin.sessions', path: '', permissions: [] },
        { labelKey: 'admin.passwordPolicy', path: '', permissions: [] },
      ],
    },
    { key: 'audit', labelKey: 'admin.audit', path: '', permissions: [], children: [] },
    { key: 'settings', labelKey: 'admin.settings', path: '', permissions: [], children: [] },
  ];

  /**
   * Vai trò hiện dưới tên ở thanh ngang.
   *
   * Phải gọi `GET /api/me` mới có: `AuthUser` trong phiên chỉ giữ id, email và tên hiển
   * thị — backend cố ý KHÔNG nhét vai trò vào token, vì token sống 15 phút còn vai trò
   * đổi được bất cứ lúc nào. Đọc từ token thì người vừa bị hạ vai vẫn thấy mình là Owner
   * suốt 15 phút, ngay tại màn hình mà điều đó quan trọng nhất.
   *
   * Rỗng cho tới khi có dữ liệu thật. Không đặt giá trị tạm nào — một chữ "Owner" đoán
   * bừa ở đây tệ hơn hẳn một khoảng trắng.
   */
  protected readonly roleLabel = signal('');

  /**
   * Chữ cái đầu làm ảnh đại diện tạm: chữ đầu của TỪ ĐẦU và TỪ CUỐI.
   *
   * Không cắt hai ký tự đầu chuỗi — tên Việt "Lê Anh Lượng" sẽ ra "Lê", tức là cả họ, mà
   * họ thì trùng nhau đầy công ty.
   */
  protected initials(name: string, email: string): string {
    const words = name.trim().split(/\s+/).filter(Boolean);

    if (words.length === 0) {
      return (email[0] ?? '?').toUpperCase();
    }

    return (words[0][0] + words[words.length - 1][0]).toUpperCase();
  }

  protected isOpen(key: string): boolean {
    return this.openGroups().has(key);
  }

  protected toggleGroup(key: string): void {
    this.openGroups.update((current) => {
      const next = new Set(current);

      if (!next.delete(key)) {
        next.add(key);
      }

      return next;
    });
  }

  protected toggleCollapsed(): void {
    this.collapsed.update((value) => !value);
  }

  protected closeMenu(): void {
    this.openMenu.set(null);
  }

  protected toggleMenu(menu: 'toi' | 'caidat', event: Event): void {
    // Chặn nổi bọt, nếu không cú bấm này chạm luôn vào bộ đóng-khi-bấm-ra-ngoài bên dưới
    // và menu mở rồi đóng ngay trong cùng một cú bấm.
    event.stopPropagation();
    this.openMenu.update((current) => (current === menu ? null : menu));
  }

  @HostListener('document:click', ['$event'])
  protected onDocumentClick(event: MouseEvent): void {
    if (this.openMenu() && !this.host.nativeElement.contains(event.target as Node)) {
      this.openMenu.set(null);
    }
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    this.openMenu.set(null);
  }

  /** Nút chưa làm: nói thẳng, đừng im lặng. Im lặng thì người dùng tưởng app hỏng. */
  protected notBuiltYet(event: Event, labelKey: string): void {
    event.preventDefault();
    this.openMenu.set(null);

    const label = this.translate.instant(labelKey) as string;
    const suffix = this.translate.instant('login.comingSoon') as string;

    this.popups.show(`${label} — ${suffix}`);
  }

  protected logout(): void {
    this.auth.logout().subscribe();
    void this.router.navigate(['/login']);
  }
}
