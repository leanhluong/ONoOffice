import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { PopupService } from '../../../core/ui/popup.service';
import { UserService } from '../../../core/users/user.service';
import { UserStatusFilter } from '../../../core/models/user.model';

/** Bốn con số ở đầu màn. `null` = chưa nạp xong. */
interface Stats {
  readonly total: number;
  readonly pending: number;
  readonly disabled: number;
  readonly roles: number;
}

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

  /** Ngưỡng tô cảnh báo cho thanh hạn ngạch. 80 chứ không phải 100: lúc chạm trần thì
   *  người dùng đã bị chặn tạo tài khoản và không hiểu vì sao. */
  protected readonly canhNguong = 80;

  constructor() {
    const count = (status?: UserStatusFilter) =>
      this.users.list({ status, pageSize: 1 }).pipe(map((page) => page.totalCount));

    forkJoin({
      total: count(),
      pending: count(UserStatusFilter.PendingFirstLogin),
      disabled: count(UserStatusFilter.Disabled),
      roles: this.users.roles().pipe(map((list) => list.length)),
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
}
