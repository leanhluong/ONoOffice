import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthStore } from '../../core/auth/auth.store';
import type { MemberListItem } from '../../core/models/org.model';
import { OrgService } from '../../core/org/org.service';

/** Một lối tắt trên bảng điều khiển. `quyen = null` nghĩa là ai đăng nhập cũng vào được. */
interface Shortcut {
  readonly ma: string;
  readonly duong: string;
  readonly quyen: string | null;
}

/**
 * Bảng điều khiển — màn đầu tiên sau khi đăng nhập.
 *
 * ═══════════════════════════════════════════════════════════════════════
 *  MÀN NÀY DỄ LÀM SAI THEO ĐÚNG MỘT KIỂU
 * ═══════════════════════════════════════════════════════════════════════
 *
 * Bảng điều khiển là chỗ cám dỗ nhất để bịa: biểu đồ, dòng thời gian hoạt động, "5 tin
 * nhắn chưa đọc", "3 đơn chờ duyệt". Không thứ nào có dữ liệu — chưa có module Trao đổi,
 * chưa có đơn từ, chưa có nhật ký. Vẽ chúng rồi để số 0 đứng yên vĩnh viễn thì tệ hơn để
 * trống: người dùng kết luận công ty mình không có việc gì, chứ không kết luận là tính
 * năng chưa tới.
 *
 * Nên màn này chỉ có thứ ĐẾM ĐƯỢC HÔM NAY, và mỗi con số là một liên kết đi thẳng tới danh
 * sách đã lọc sẵn — một con số không bấm được thì chỉ là trang trí.
 *
 * ═══════════════════════════════════════════════════════════════════════
 *  KHÔNG ĐẾM ĐƯỢC THÌ GIẤU, KHÔNG HIỆN SỐ 0
 * ═══════════════════════════════════════════════════════════════════════
 *
 * <c>/api/members</c> đòi CẢ HAI quyền <c>user.read</c> và <c>employee.read</c>. Vai Member
 * chỉ có quyền thứ hai, nên với họ khối "cần xử lý" <b>không tồn tại</b> — và ta cũng
 * không gọi endpoint đó, vì một popup lỗi đỏ ngay khi vừa đăng nhập là ấn tượng đầu tiên
 * tệ nhất có thể tạo ra.
 *
 * Nguồn thiết kế: `docs/07-giao-dien/khung/bang-dieu-khien.html`.
 */
@Component({
  selector: 'app-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslatePipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  private readonly org = inject(OrgService);
  protected readonly store = inject(AuthStore);

  private readonly members = signal<readonly MemberListItem[] | null>(null);

  /**
   * Bốn lối tắt, lọc theo quyền.
   *
   * Một thẻ dẫn tới màn sẽ trả về "bạn không có quyền" là mời người dùng đi vào ngõ cụt —
   * và họ không biết là do quyền hay do app hỏng.
   */
  private static readonly TAT: readonly Shortcut[] = [
    { ma: 'contacts', duong: '/contacts', quyen: 'employee.read' },
    { ma: 'profile', duong: '/me', quyen: null },
    { ma: 'members', duong: '/admin/users', quyen: 'user.read' },
    { ma: 'help', duong: '/huong-dan', quyen: null },
  ];

  protected readonly shortcuts = computed(() =>
    Dashboard.TAT.filter((s) => s.quyen === null || this.store.hasPermission(s.quyen)),
  );

  /**
   * Có đếm được việc không.
   *
   * `null` = chưa có dữ liệu, vì chưa gọi (thiếu quyền) hoặc vì gọi hỏng. Cả hai ca đều
   * GIẤU hẳn khối, không hiện ba số 0 — ba số 0 là một câu trả lời SAI ("workspace của
   * bạn không có việc gì"), không phải một câu trả lời thiếu.
   */
  protected readonly showTasks = computed(() => this.members() !== null);

  /** Chỉ đếm người CÒN ĐANG LÀM: người đã nghỉ không phải việc phải xử lý. */
  private readonly dangLam = computed(() => (this.members() ?? []).filter((m) => m.isActive));

  protected readonly pendingPassword = computed(
    () => this.dangLam().filter((m) => m.mustChangePassword).length,
  );

  protected readonly noAccount = computed(
    () => this.dangLam().filter((m) => m.userId === null).length,
  );

  protected readonly noProfile = computed(
    () => this.dangLam().filter((m) => m.employeeId === null).length,
  );

  /**
   * Workspace vừa dựng, còn đúng một người.
   *
   * Lúc đó mọi con số đều bằng 0 và màn hình rỗng không nói cho họ biết việc tiếp theo là
   * gì. Đếm trên danh sách GỘP nên tài khoản máy cũng tính — đúng: nếu đã có một con bot
   * thì workspace này không còn "vừa dựng" nữa.
   */
  protected readonly isNewWorkspace = computed(() => this.members()?.length === 1);

  /**
   * Lời chào theo buổi.
   *
   * Đọc đồng hồ MÁY chứ không hỏi server: múi giờ của người dùng là thứ server không biết,
   * và "chào buổi sáng" lúc 8 giờ tối là kiểu sai làm người ta thấy app cẩu thả.
   */
  protected readonly greetingKey = computed(() => {
    const gio = new Date().getHours();

    if (gio < 11) {
      return 'dashboard.morning';
    }

    return gio < 18 ? 'dashboard.afternoon' : 'dashboard.evening';
  });

  /** Tên gọi — lấy TỪ CUỐI, kiểu Việt Nam: "Lê Anh Lượng" → "Lượng". */
  protected readonly firstName = computed(() => {
    const ten = this.store.user()?.displayName?.trim() ?? '';
    const tu = ten.split(/\s+/).filter(Boolean);

    return tu.length === 0 ? '' : tu[tu.length - 1];
  });

  constructor() {
    // KHÔNG gọi khi thiếu quyền: `/api/members` đòi cả `user.read` lẫn `employee.read`,
    // nên với vai Member đây là một request chắc chắn nhận 403.
    if (!this.store.hasPermission('user.read') || !this.store.hasPermission('employee.read')) {
      return;
    }

    this.org.members().subscribe({
      next: (ds) => this.members.set(ds),

      // Nuốt lỗi là CÓ CHỦ Ý. Bảng điều khiển không phải chỗ báo hỏng: người dùng vừa đăng
      // nhập và chưa yêu cầu gì cả. Khối việc biến mất, lối tắt vẫn còn, và họ đi tiếp
      // được — màn nào thật sự cần dữ liệu đó sẽ tự báo khi họ mở nó.
      error: () => this.members.set(null),
    });
  }
}
