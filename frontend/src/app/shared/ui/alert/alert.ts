import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

export type AlertTone = 'error' | 'warning' | 'info' | 'success';

/**
 * Khối thông báo dùng chung (lỗi API, cảnh báo, xác nhận thành công).
 *
 * Vì sao tách thành component riêng: mọi màn hình đều phải hiện lỗi, và nếu mỗi màn tự
 * viết một `<div class="error">` thì màu sắc, khoảng cách và thuộc tính trợ năng sẽ mỗi
 * nơi một kiểu. Gom lại đây thì sửa một lần là cả app đổi theo.
 *
 * <b>Không bao giờ chỉ dùng màu để truyền tin.</b> Mỗi tông có một BIỂU TƯỢNG riêng đi
 * kèm — khoảng 8% nam giới bị mù màu đỏ-lục, và không ai trong số họ báo lỗi này cho bạn.
 *
 * `role="alert"` khiến trình đọc màn hình đọc ngay khi nội dung xuất hiện — quan trọng
 * vì người dùng bàn phím không nhìn thấy khối đỏ vừa hiện ra.
 */
@Component({
  selector: 'app-alert',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './alert.scss',
  imports: [TranslatePipe],
  template: `
    <div class="alert" [class]="'alert--' + tone()" role="alert">
      <span class="alert__icon" aria-hidden="true">{{ icon() }}</span>

      <div class="alert__body">
        @if (title()) {
          <p class="alert__title">{{ title() }}</p>
        }

        <p class="alert__message"><ng-content /></p>

        @if (reference()) {
          <p class="alert__reference">
            {{ 'reference' | translate }}: <code>{{ reference() }}</code>
          </p>
        }
      </div>
    </div>
  `,
})
export class Alert {
  readonly tone = input<AlertTone>('error');
  readonly title = input<string | null>(null);

  /**
   * Mã lần vết, hiện nhỏ bên dưới để người dùng đọc cho bộ phận hỗ trợ.
   *
   * Cố ý KHÔNG phải mã lỗi nghiệp vụ: "Auth.InvalidCredentials" chẳng giúp gì cho người
   * đã đọc câu "email hoặc mật khẩu không đúng" ngay bên trên. Thứ đáng hiện là mã lần
   * vết, và chỉ hiện khi ta không giải thích được chuyện gì đã xảy ra.
   */
  readonly reference = input<string | null>(null);

  protected icon(): string {
    switch (this.tone()) {
      case 'error':
        return '✕';
      case 'warning':
        return '!';
      case 'success':
        return '✓';
      default:
        return 'i';
    }
  }
}
