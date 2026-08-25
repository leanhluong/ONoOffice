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
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';
import { PopupHost } from '../../shared/ui/popup-host/popup-host';
import { Prefs } from '../../shared/ui/prefs/prefs';

/** Một app trên rail. */
interface RailApp {
  /** Khoá chọn hình vẽ trong `@switch` của template. */
  readonly key: string;

  /** Khoá dịch, KHÔNG phải câu chữ — nếu không thì menu vĩnh viễn một thứ tiếng. */
  readonly labelKey: string;

  /** Rỗng khi app chưa làm — lúc đó nó là `<button>`, không phải link. */
  readonly path: string;

  /** Quyền cần có để thấy app này. Rỗng = ai đăng nhập cũng thấy. */
  readonly permissions: readonly string[];

  /** Số việc đang chờ. Quá 9 thì template hiện "9+". */
  readonly badge?: number;

  /**
   * `true` thì hiện một CHẤM xám thay vì viên số màu nhấn.
   *
   * Dùng cho "có gì đó mới" chứ không phải "có việc phải làm". Cái gì cũng có số thì
   * không cái nào còn nghĩa là cần xử lý.
   */
  readonly quiet?: boolean;

  /** Chưa làm: bấm vào thì nói thẳng, không phải một link chết. */
  readonly soon?: boolean;
}

/**
 * Khung ngoài của phần app đã đăng nhập — <b>bản v4</b>: rail biểu tượng + vùng nội dung.
 *
 * <b>Vì sao đổi khỏi v3 (một cột chữ 212px):</b> cột chữ duy nhất chính là khuôn của một
 * TRANG QUẢN TRỊ. Nó chạy được với bốn màn, và chật ngay khi có sáu app — mỗi app lại còn
 * cần danh sách riêng của nó (hội thoại, tài liệu, phòng ban). Lark, Zalo PC và Slack đều
 * dùng rail + cột ngữ cảnh, và lý do là mỗi bên gánh một việc khác: rail để CHUYỂN app,
 * cột để làm việc BÊN TRONG app. Xem `docs/07-giao-dien/chung/_khung.css`.
 *
 * <b>Không có cột ngữ cảnh ở đây.</b> Mỗi app tự dựng cột của mình — màn Trao đổi dùng
 * danh sách hội thoại có ảnh và câu cuối, màn Hồ sơ không cần cột nào. Dựng một cột rỗng
 * ở khung ngoài thì mọi màn không cần cột phải đi gỡ nó.
 *
 * <b>Không có thanh ngang toàn chiều rộng.</b> Mỗi trang tự dựng tiêu đề riêng — tiêu đề
 * màn nhân sự nói về bộ lọc đang bật, tiêu đề màn chat nói về kênh đang mở. Một thanh
 * chung không nói được gì cụ thể mà vẫn ăn 56px chiều cao của mọi màn.
 *
 * Nguồn thiết kế: `docs/07-giao-dien/chung/_khung.css` + hàm `mountRail` trong `_shell.js`.
 * Toàn bộ CSS nằm ở `styles.scss` và được SINH từ đó, nên component này không có style riêng.
 */
@Component({
  selector: 'app-shell',
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
  templateUrl: './shell.html',
})
export class Shell {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly popups = inject(PopupService);
  private readonly translate = inject(TranslateService);
  private readonly host = inject(ElementRef<HTMLElement>);

  protected readonly store = inject(AuthStore);

  /** Menu đang mở: `'toi'`, `'caidat'`, hoặc `null`. */
  protected readonly openMenu = signal<'toi' | 'caidat' | null>(null);

  /**
   * Các app trên rail.
   *
   * Bốn app cuối để `soon: true` — chúng CHƯA có route lẫn backend. Cố ý giữ chúng trên
   * rail thay vì giấu đi: rail là danh tính của sản phẩm, và người dùng cần thấy trước
   * sản phẩm sẽ có những gì. Bấm vào thì `notBuiltYet()` nói thẳng, không im lặng.
   *
   * Thứ tự KHÔNG tuỳ tiện. Khi màn Trao đổi xong, nó sẽ lên đầu và thành app mặc định —
   * mở ONoOffice là vào thẳng chat, giống Lark và Zalo.
   */
  protected readonly apps: readonly RailApp[] = [
    { key: 'dashboard', labelKey: 'nav.dashboard', path: '/dashboard', permissions: [] },
    { key: 'chat', labelKey: 'nav.chat', path: '', permissions: [], soon: true },
    { key: 'calendar', labelKey: 'nav.calendar', path: '', permissions: [], soon: true },
    { key: 'docs', labelKey: 'nav.docs', path: '', permissions: [], soon: true },
    { key: 'approval', labelKey: 'nav.approvals', path: '', permissions: [], soon: true },
    { key: 'contacts', labelKey: 'nav.contacts', path: '', permissions: [], soon: true },
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

  protected closeMenu(): void {
    this.openMenu.set(null);
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
