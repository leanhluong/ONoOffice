import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/**
 * Component gốc. Cố ý để trống ngoài `<router-outlet />`.
 *
 * Lý do: màn đăng nhập và phần app đã đăng nhập có khung hoàn toàn khác nhau
 * (một cái căn giữa màn hình, một cái có sidebar). Nếu nhét sidebar vào đây
 * thì màn login cũng dính theo. Khung của phần đã đăng nhập nằm ở
 * `layout/shell` và được gắn qua route cha.
 */
@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet],
  template: '<router-outlet />',
})
export class App {}
