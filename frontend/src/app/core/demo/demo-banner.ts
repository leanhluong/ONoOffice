import { ChangeDetectionStrategy, Component } from '@angular/core';
import { demoDangBat } from './demo.interceptor';

/**
 * Dải báo "đang ở CHẾ ĐỘ DEMO", chạy suốt bề ngang, <b>không có nút tắt</b>.
 *
 * Không cho tắt là chủ ý. Một dải cảnh báo tắt được thì người ta tắt ngay lần đầu, rồi
 * ba ngày sau chụp màn hình gửi cho khách và không ai biết những con số đó là bịa. Chi
 * phí là 26px chiều cao ở một chế độ vốn không dành cho người dùng thật.
 *
 * Nó cũng là chỗ DUY NHẤT nói cho người thử biết cách thoát ra (`?demo=0`) và cách đổi
 * vai (`?demo=member`) — không viết ở đây thì hai tham số đó chỉ tồn tại trong mã nguồn.
 */
@Component({
  selector: 'app-demo-banner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (vai(); as v) {
      <div class="demobar" role="status">
        <b>CHẾ ĐỘ DEMO</b>
        <span>Dữ liệu là giả, không có backend. Đang đóng vai <b>{{ v }}</b>.</span>
        <span class="demobar__gioi">
          <code>?demo=owner</code> · <code>?demo=member</code> · <code>?demo=0</code> để thoát
        </span>
      </div>
    }
  `,
  styles: `
    .demobar {
      position: fixed;
      inset: 0 0 auto 0;
      z-index: 200;
      display: flex;
      align-items: center;
      gap: 10px;
      flex-wrap: wrap;
      height: 26px;
      padding: 0 12px;
      background: repeating-linear-gradient(
        135deg,
        var(--canh) 0 12px,
        color-mix(in srgb, var(--canh) 76%, #000) 12px 24px
      );
      color: #1a1408;
      font-size: 11.5px;
      line-height: 26px;
      white-space: nowrap;
      overflow: hidden;
    }

    .demobar b {
      font-weight: 700;
      letter-spacing: 0.06em;
    }

    .demobar code {
      font-family: 'JetBrains Mono', ui-monospace, monospace;
      font-size: 10.5px;
    }

    .demobar__gioi {
      margin-left: auto;
      opacity: 0.85;
    }

    /*
      Phần ĐẨY CẢ APP XUỐNG 26px không nằm ở đây mà ở src/styles-demo.scss.

      Style của component bị bó phạm vi, nên nó không với tới .khung và .qt — hai thứ cao
      đúng 100vh. Không trừ đi thì đáy của chúng bị dải này đẩy ra ngoài màn hình, và
      thanh phân trang của màn Thành viên biến mất — đúng chỗ người thử cần bấm nhất.

      (Chú thích ở đây KHÔNG được dùng dấu huyền bao quanh tên file: cả khối styles này
      là một template literal, nên một dấu huyền lạc vào là chuỗi đứt giữa chừng và
      TypeScript báo hàng chục lỗi chẳng liên quan gì tới nguyên nhân.)
    */
  `,
})
export class DemoBanner {
  protected readonly vai = () => demoDangBat();
}
