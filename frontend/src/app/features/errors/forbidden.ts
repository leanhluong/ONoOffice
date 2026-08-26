import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

/**
 * Màn "không đủ quyền" — nơi `permissionGuard` đưa người dùng tới.
 *
 * Cố ý hiện luôn tên quyền còn thiếu: người dùng nội bộ sẽ copy đúng chuỗi đó
 * gửi cho admin, thay vì mô tả vòng vo "tôi không vào được trang nhân sự".
 * Đây là app nội bộ nên việc lộ tên quyền không phải rủi ro.
 */
@Component({
  selector: 'app-forbidden',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <div class="noidung">
      <div class="trang">
        <section class="forbidden">
      <h1>Bạn không có quyền vào trang này</h1>
      @if (required(); as permissions) {
        <p>
          Trang này yêu cầu quyền: <code>{{ permissions }}</code
          >. Liên hệ quản trị viên để được cấp.
        </p>
      } @else {
        <p>Liên hệ quản trị viên nếu bạn cho rằng đây là nhầm lẫn.</p>
      }
      <a routerLink="/dashboard">← Về bảng điều khiển</a>
        </section>
      </div>
    </div>
  `,
  styles: `
    .forbidden {
      max-width: 36rem;

      h1 {
        margin: 0 0 0.5rem;
        font-size: 1.3rem;
      }

      p {
        color: var(--ink-soft);
        font-size: 0.9rem;
        line-height: 1.6;
      }

      a {
        color: var(--accent);
        font-size: 0.9rem;
      }
    }
  `,
})
export class Forbidden {
  private readonly route = inject(ActivatedRoute);

  protected required(): string | null {
    return this.route.snapshot.queryParamMap.get('required');
  }
}
