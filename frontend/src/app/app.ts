import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { DemoBanner } from './core/demo/demo-banner';
import { demoDangBat } from './core/demo/demo.interceptor';

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
  imports: [RouterOutlet, DemoBanner],
  template: '<app-demo-banner /><router-outlet />',
})
export class App {
  constructor() {
    // Lớp trên <html> để `styles-demo.scss` trừ 26px chiều cao của `.khung` và `.qt`.
    // Gắn ở đây chứ không trong DemoBanner: nó phải có mặt TRƯỚC khung hình đầu tiên,
    // nếu không thì app nhấp một cái ở chiều cao sai rồi mới co lại.
    document.documentElement.classList.toggle('demo', demoDangBat() !== null);
  }
}
