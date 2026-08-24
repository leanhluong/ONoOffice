import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Dấu <b>?</b> cạnh nhãn, mở ra một bong bóng giải thích.
 *
 * Thay cho những dòng chú thích nằm cố định dưới ô nhập. Một dòng chữ xám dưới mỗi ô làm
 * biểu mẫu dài gấp rưỡi và người dùng đọc lướt qua hết — chỗ nào cũng có chữ thì không
 * chỗ nào đáng đọc. Cất vào dấu ? thì ai cần mới mở.
 *
 * <c>tabindex="0"</c> để mở được bằng bàn phím: chỉ hiện khi rê chuột nghĩa là người dùng
 * bàn phím không bao giờ đọc được nội dung đó.
 *
 * Style nằm ở `styles.scss` toàn cục (sinh từ bản dựng), không có style riêng ở đây.
 */
@Component({
  selector: 'app-tip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: `
    :host {
      display: contents;
    }
  `,
  template: `
    <span class="tip" tabindex="0" role="note" [attr.aria-label]="text()">
      ?
      <span class="tip__bubble"><ng-content /></span>
    </span>
  `,
})
export class Tip {
  /** Bản chữ thuần cho trình đọc màn hình — nội dung nhìn thấy đi qua `<ng-content>`. */
  readonly text = input.required<string>();
}
