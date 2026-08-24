import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';

/**
 * Thông báo thoáng qua ở đáy màn hình.
 *
 * Dùng cho những thứ <b>chưa làm</b> (Google, Facebook, quên mật khẩu). Vì sao không
 * dùng khối cảnh báo trong form: khối đó dành cho lý do người dùng KHÔNG vào được — sai
 * mật khẩu, tài khoản bị khoá. Nhét "tính năng đang phát triển" vào cùng chỗ đó khiến
 * một thông tin vô hại trông như một lỗi nghiêm trọng.
 *
 * `role="status"` chứ không phải `role="alert"`: trình đọc màn hình đọc nó lúc rảnh, chứ
 * không cắt ngang thứ đang đọc dở. Đây là tin phụ, không phải tin khẩn.
 */
@Component({
  selector: 'app-toast',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './toast.scss',
  template: `
    <div class="toast" [attr.data-show]="message() ? '1' : '0'" role="status">
      {{ message() }}
    </div>
  `,
})
export class Toast {
  private readonly destroyRef = inject(DestroyRef);

  protected readonly message = signal<string | null>(null);

  private timer: ReturnType<typeof setTimeout> | undefined;

  constructor() {
    // Component bị huỷ khi hẹn giờ còn chạy thì callback vẫn nổ và ghi vào một signal
    // của đối tượng đã chết. Không sập ngay, nhưng là rò rỉ — và loại rò rỉ này chỉ lộ
    // ra sau vài giờ dùng liên tục.
    this.destroyRef.onDestroy(() => clearTimeout(this.timer));
  }

  show(message: string): void {
    this.message.set(message);

    clearTimeout(this.timer);
    this.timer = setTimeout(() => this.message.set(null), 2400);
  }
}
