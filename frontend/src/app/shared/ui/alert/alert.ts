import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Khối cảnh báo trong biểu mẫu. Chép đúng `.alert` của bản dựng mockup.
 *
 * <b>Dòng `code` bên dưới câu chữ là chủ ý, không phải rác kỹ thuật lọt ra.</b> Nó ghi
 * mã lỗi nghiệp vụ và mã HTTP — thứ mà người dùng đọc nguyên văn cho bộ phận hỗ trợ, và
 * từ đó tìm ra đúng nhánh code đã từ chối họ. Câu chữ thì đổi theo ngôn ngữ và theo lần
 * biên tập; mã thì không.
 *
 * `role="alert"` khiến trình đọc màn hình đọc NGAY khi khối này xuất hiện — cần, vì
 * người dùng bàn phím không nhìn thấy khối đỏ vừa hiện ra phía trên chỗ họ đang gõ.
 */
@Component({
  selector: 'app-alert',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './alert.scss',
  template: `
    <div class="alert" role="alert">
      <svg class="alert__icon" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
        <path
          d="M10 2a8 8 0 100 16 8 8 0 000-16zm0 4a1 1 0 011 1v4a1 1 0 11-2 0V7a1 1 0 011-1zm0 8.5a1.1 1.1 0 110-2.2 1.1 1.1 0 010 2.2z"
        />
      </svg>

      <span>
        <ng-content />
        @if (code()) {
          <code>{{ code() }}{{ status() ? ' · HTTP ' + status() : '' }}</code>
        }
      </span>
    </div>
  `,
})
export class Alert {
  /** Mã lỗi nghiệp vụ, ví dụ `Auth.InvalidCredentials`. */
  readonly code = input<string | null>(null);

  /** Mã HTTP. 0 nghĩa là không gọi tới được máy chủ — lúc đó không hiện gì. */
  readonly status = input<number | null>(null);
}
