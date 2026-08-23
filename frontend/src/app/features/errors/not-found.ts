import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/** Màn 404 cho các đường dẫn không khớp route nào. */
@Component({
  selector: 'app-not-found',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <section class="not-found">
      <h1>404 — Không tìm thấy trang</h1>
      <p>Đường dẫn bạn mở không tồn tại hoặc đã bị đổi.</p>
      <a routerLink="/dashboard">← Về bảng điều khiển</a>
    </section>
  `,
  styles: `
    .not-found {
      max-width: 36rem;

      h1 {
        margin: 0 0 0.5rem;
        font-size: 1.3rem;
      }

      p {
        color: var(--color-text-muted);
        font-size: 0.9rem;
      }

      a {
        color: var(--color-accent);
        font-size: 0.9rem;
      }
    }
  `,
})
export class NotFound {}
