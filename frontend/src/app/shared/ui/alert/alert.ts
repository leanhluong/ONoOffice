import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type AlertTone = 'error' | 'warning' | 'info' | 'success';

/**
 * Khối thông báo dùng chung (lỗi API, cảnh báo, xác nhận thành công).
 *
 * Vì sao tách thành component riêng: mọi màn hình đều phải hiện lỗi, và nếu
 * mỗi màn tự viết một cái `<div class="error">` thì màu sắc, khoảng cách và
 * thuộc tính accessibility sẽ mỗi nơi một kiểu. Gom lại đây thì sửa một lần
 * là cả app đổi theo.
 *
 * `role="alert"` khiến trình đọc màn hình đọc ngay khi nội dung xuất hiện —
 * quan trọng vì người dùng bàn phím không nhìn thấy khối lỗi đỏ vừa hiện ra.
 */
@Component({
  selector: 'app-alert',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './alert.scss',
  template: `
    <div class="alert" [class]="'alert--' + tone()" role="alert">
      @if (title()) {
        <p class="alert__title">{{ title() }}</p>
      }
      <p class="alert__message"><ng-content /></p>
      @if (code()) {
        <p class="alert__code">Mã lỗi: {{ code() }}</p>
      }
    </div>
  `,
})
export class Alert {
  readonly tone = input<AlertTone>('error');
  readonly title = input<string | null>(null);
  /** Mã lỗi kỹ thuật, hiện nhỏ bên dưới để người dùng đọc cho bộ phận hỗ trợ. */
  readonly code = input<string | null>(null);
}
