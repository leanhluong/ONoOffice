import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { PopupService } from '../../../core/ui/popup.service';
import { UserService } from '../../../core/users/user.service';
import { UserStatusFilter, type UserListItem } from '../../../core/models/user.model';

/** Bốn con số ở đầu màn. `null` = chưa nạp xong. */
interface Stats {
  readonly total: number;
  readonly pending: number;
  readonly disabled: number;
  readonly roles: number;
}

/** Số người hiện trong thẻ "Cần bạn xử lý" trước khi phải bấm "xem cả …". */
const CAN_XU_LY = 3;

/**
 * Tổng quan tổ chức — trang gốc của vùng quản trị.
 *
 * <b>KHÔNG có endpoint thống kê nào, và cố ý không thêm.</b> Bốn con số lấy từ ba lời gọi
 * đã chạy thật, mỗi lời gọi xin <c>pageSize=1</c> rồi chỉ đọc <c>totalCount</c>:
 *
 * <code>
 *   GET /api/users?pageSize=1                          → tổng tài khoản
 *   GET /api/users?status=2&amp;pageSize=1             → còn mật khẩu tạm
 *   GET /api/users?status=3&amp;pageSize=1             → đang bị vô hiệu
 *   GET /api/roles                                     → đếm mảng
 * </code>
 *
 * Đánh đổi: bốn request thay vì một. Chấp nhận được vì màn này mở mỗi tháng vài lần, và
 * đổi lại là <b>không phải thêm bảng, thêm endpoint, thêm test cho một con số</b>. Khi nào
 * cần biểu đồ theo ngày thì mới đáng làm endpoint riêng — lúc đó nó có việc thật để làm.
 *
 * <b>Gói cước và hạn ngạch chưa nối gì.</b> Ba khái niệm Plan / Quota / Usage chưa có một
 * bảng nào ở backend. Các khối đó đeo nhãn "chưa nối" ngay trên màn hình, đúng như bản
 * dựng đã duyệt — bản dựng hứa thứ không có là lỗi đã bị cắn hai lần trong dự án này.
 */
@Component({
  selector: 'app-admin-overview',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslatePipe],
  templateUrl: './overview.html',
  styleUrl: './overview.scss',
})
export class AdminOverview {
  private readonly users = inject(UserService);
  private readonly popups = inject(PopupService);
  private readonly translate = inject(TranslateService);

  protected readonly stats = signal<Stats | null>(null);
  protected readonly failed = signal(false);

  /**
   * Vài người đầu tiên còn mật khẩu tạm.
   *
   * Lấy KÈM trong cùng lời gọi đếm: `?status=PendingFirstLogin&pageSize=3` vừa cho
   * `totalCount` để hiện con số, vừa cho ba dòng đầu để hiện danh sách. Gọi hai lần cho
   * cùng một bộ lọc là trả tiền hai lần cho một câu hỏi.
   */
  protected readonly canXuLy = signal<readonly UserListItem[]>([]);

  /** Ngưỡng tô cảnh báo cho thanh hạn ngạch. 80 chứ không phải 100: lúc chạm trần thì
   *  người dùng đã bị chặn tạo tài khoản và không hiểu vì sao. */
  protected readonly canhNguong = 80;

  constructor() {
    const count = (status?: UserStatusFilter) =>
      this.users.list({ status, pageSize: 1 }).pipe(map((page) => page.totalCount));

    forkJoin({
      total: count(),
      disabled: count(UserStatusFilter.Disabled),
      roles: this.users.roles().pipe(map((list) => list.length)),

      // Một lời gọi, hai câu trả lời: `totalCount` cho ô số, `items` cho danh sách bên
      // cột phụ. Đây là bộ lọc DUY NHẤT màn này cần cả hai.
      pending: this.users
        .list({ status: UserStatusFilter.PendingFirstLogin, pageSize: CAN_XU_LY })
        .pipe(
          map((page) => {
            this.canXuLy.set(page.items);

            return page.totalCount;
          }),
        ),
    })
      .pipe(
        catchError(() => {
          this.failed.set(true);

          return of(null);
        }),
      )
      .subscribe((value) => {
        if (value) {
          this.stats.set(value);
        }
      });
  }

  /** Nút chưa làm: nói thẳng, đừng im lặng. */
  protected notBuiltYet(event: Event, labelKey: string): void {
    event.preventDefault();

    const label = this.translate.instant(labelKey) as string;
    const suffix = this.translate.instant('login.comingSoon') as string;

    this.popups.show(`${label} — ${suffix}`);
  }

  protected readonly filter = UserStatusFilter;

  /**
   * Chữ cái đầu làm ảnh đại diện tạm: chữ đầu của TỪ ĐẦU và TỪ CUỐI.
   *
   * Không cắt hai ký tự đầu chuỗi — tên Việt "Đỗ Ngọc Hà" sẽ ra "Đỗ", tức là cả họ, mà
   * họ thì trùng nhau đầy công ty.
   */
  protected initials(name: string): string {
    const words = name.trim().split(/\s+/).filter(Boolean);

    if (words.length === 0) {
      return '?';
    }

    return (words[0][0] + words[words.length - 1][0]).toUpperCase();
  }

  /**
   * Số ngày kể từ lúc tạo tài khoản.
   *
   * Để càng lâu thì mật khẩu tạm càng nguy: nó đã đi qua Zalo, qua lời nói, và có thể còn
   * nằm trong lịch sử tin nhắn của ai đó. Quá một tuần thì tô cảnh báo.
   */
  protected daysOld(createdAtUtc: string): number {
    const ms = Date.now() - new Date(createdAtUtc).getTime();

    return Math.max(0, Math.floor(ms / 86_400_000));
  }
}
