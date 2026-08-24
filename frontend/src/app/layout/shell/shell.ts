import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  inject,
  signal,
} from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';
import { AuthStore } from '../../core/auth/auth.store';
import { PopupService } from '../../core/ui/popup.service';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';
import { PopupHost } from '../../shared/ui/popup-host/popup-host';
import { Prefs } from '../../shared/ui/prefs/prefs';

/** Một mục trên cột điều hướng. */
interface NavItem {
  /** Khoá dịch, KHÔNG phải câu chữ — nếu không thì menu vĩnh viễn một thứ tiếng. */
  readonly labelKey: string;
  readonly path: string;

  /** Quyền cần có để thấy mục này. Rỗng = ai đăng nhập cũng thấy. */
  readonly permissions: readonly string[];

  /** Số việc đang chờ. `null` = không có gì để đếm. */
  readonly badge?: number;

  /**
   * `true` thì số hiện bằng chữ xám thay vì viên màu nhấn.
   *
   * Dùng cho số ĐO ĐẠC (38 nhân viên) chứ không phải số VIỆC PHẢI LÀM (4 tin chưa đọc).
   * Cái gì cũng tô màu nhấn thì không cái nào còn nghĩa là "cần xử lý".
   */
  readonly quiet?: boolean;
}

/**
 * Khung ngoài của phần app đã đăng nhập: cột điều hướng + vùng nội dung.
 *
 * Vì sao là một route cha có `children` chứ không phải component bọc trong từng màn: làm
 * thế này thì chuyển trang chỉ vẽ lại phần `<router-outlet>` bên trong, cột điều hướng giữ
 * nguyên trạng thái. Nếu mỗi màn tự nhúng shell thì cả cột bị dựng lại mỗi lần.
 *
 * <b>KHÔNG có thanh ngang toàn chiều rộng.</b> Mỗi trang tự dựng tiêu đề riêng — tiêu đề
 * màn nhân sự nói về bộ lọc đang bật, tiêu đề màn chat nói về kênh đang mở. Một thanh
 * chung thì không nói được gì cụ thể, mà vẫn ăn 52px chiều cao của mọi màn.
 *
 * Nguồn thiết kế: `docs/07-giao-dien/chung/_khung.css`. Toàn bộ CSS của khung nằm ở
 * `styles.scss` và được SINH từ file đó, nên component này không có style riêng.
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
    PopupHost,
  ],
  templateUrl: './shell.html',
})
export class Shell {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly popups = inject(PopupService);
  private readonly translate = inject(TranslateService);
  private readonly host = inject(ElementRef<HTMLElement>);

  protected readonly store = inject(AuthStore);

  /** Thu gọn cột còn bề rộng biểu tượng. */
  protected readonly collapsed = signal(false);

  /** Menu đang mở: `'toi'`, `'caidat'`, hoặc `null`. */
  protected readonly openMenu = signal<'toi' | 'caidat' | null>(null);

  protected readonly navGroups: readonly {
    readonly labelKey: string;
    readonly items: readonly NavItem[];
  }[] = [
    {
      labelKey: 'nav.groupWork',
      items: [{ labelKey: 'nav.dashboard', path: '/dashboard', permissions: [] }],
    },
    {
      labelKey: 'nav.groupOrg',
      items: [
        {
          labelKey: 'nav.employees',
          path: '/employees',
          permissions: ['employee.read'],
          quiet: true,
        },
      ],
    },
  ];

  /**
   * Chữ cái đầu làm ảnh đại diện tạm: chữ đầu của TỪ ĐẦU và TỪ CUỐI.
   *
   * Không cắt hai ký tự đầu chuỗi — tên Việt "Lê Anh Lượng" sẽ ra "Lê", tức là cả họ, mà
   * họ thì trùng nhau đầy công ty. "LL" phân biệt được nhiều hơn.
   */
  protected initials(name: string, email: string): string {
    const words = name.trim().split(/\s+/).filter(Boolean);

    if (words.length === 0) {
      return (email[0] ?? '?').toUpperCase();
    }

    return (words[0][0] + words[words.length - 1][0]).toUpperCase();
  }

  protected toggleCollapsed(): void {
    this.collapsed.update((value) => !value);
  }

  protected toggleMenu(menu: 'toi' | 'caidat', event: Event): void {
    // Chặn nổi bọt, nếu không cú bấm này chạm luôn vào bộ đóng-khi-bấm-ra-ngoài bên dưới
    // và menu mở rồi đóng ngay trong cùng một cú bấm.
    event.stopPropagation();
    this.openMenu.update((current) => (current === menu ? null : menu));
  }

  /**
   * Bấm ra ngoài thì đóng. Thiếu chỗ này là kiểu lỗi ai cũng gặp: menu xổ ra rồi nằm lì
   * trên màn hình cho tới khi đổi trang.
   */
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
    // Điều hướng ngay, không chờ server trả lời: AuthService đã xoá phiên ở client trước
    // rồi, nên giữ người dùng lại chờ mạng là vô nghĩa.
    this.auth.logout().subscribe();
    void this.router.navigate(['/login']);
  }
}
