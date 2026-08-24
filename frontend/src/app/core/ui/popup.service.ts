import { DestroyRef, Injectable, inject, signal } from '@angular/core';

export type PopupTone = 'error' | 'info';

export interface Popup {
  readonly id: number;
  readonly tone: PopupTone;
  readonly text: string;

  /**
   * Mã tham chiếu — CHỈ có khi ta không giải thích được chuyện gì đã xảy ra.
   *
   * Với lỗi nghiệp vụ đã có câu chữ rõ ràng thì để trống: một mã kỹ thuật không giúp
   * người dùng làm được gì, chỉ khiến câu thông báo trông đáng sợ hơn.
   */
  readonly reference?: string;

  readonly durationMs: number;
}

/**
 * Thông báo nổi ở ĐẦU màn hình, tự biến mất.
 *
 * Thay cho khối lỗi nằm cố định trong biểu mẫu. Khối đó đẩy mọi thứ bên dưới xuống mỗi
 * lần xuất hiện, và nằm lại đó cho tới khi người dùng làm gì khác.
 *
 * <b>Đánh đổi phải nói thẳng:</b> thông báo tự tắt nghĩa là người đang nhìn xuống bàn
 * phím lúc nó hiện sẽ không biết vì sao thao tác hỏng. Ba thứ bù lại: vạch đếm ngược để
 * họ thấy nó sắp đi, dừng đồng hồ khi rê chuột vào, và nút đóng. Ghi ở
 * `docs/07-giao-dien/identity/dang-nhap.md`.
 */
@Injectable({ providedIn: 'root' })
export class PopupService {
  /** Lỗi để lâu hơn tin thường — người ta cần đọc kỹ hơn. Khớp với bản dựng. */
  private static readonly ERROR_MS = 6000;
  private static readonly INFO_MS = 3200;

  private readonly destroyRef = inject(DestroyRef);

  private readonly items = signal<readonly Popup[]>([]);

  private readonly timers = new Map<number, ReturnType<typeof setTimeout>>();

  private nextId = 1;

  readonly popups = this.items.asReadonly();

  constructor() {
    // Hẹn giờ còn chạy khi ứng dụng đóng thì callback vẫn nổ và ghi vào một signal của
    // đối tượng đã chết. Không sập ngay, nhưng là rò rỉ.
    this.destroyRef.onDestroy(() => {
      this.timers.forEach(clearTimeout);
      this.timers.clear();
    });
  }

  show(text: string, options: { tone?: PopupTone; reference?: string } = {}): void {
    const tone = options.tone ?? 'info';

    const popup: Popup = {
      id: this.nextId++,
      tone,
      text,
      reference: options.reference,
      durationMs: tone === 'error' ? PopupService.ERROR_MS : PopupService.INFO_MS,
    };

    this.items.update((list) => [...list, popup]);
    this.arm(popup.id, popup.durationMs);
  }

  error(text: string, reference?: string): void {
    this.show(text, { tone: 'error', reference });
  }

  dismiss(id: number): void {
    this.clear(id);
    this.items.update((list) => list.filter((popup) => popup.id !== id));
  }

  /** Rê chuột vào thì dừng đồng hồ — người đang đọc dở không bị cướp mất câu chữ. */
  hold(id: number): void {
    this.clear(id);
  }

  /** Rời chuột ra thì cho thêm một khoảng ngắn rồi mới đi. */
  release(id: number): void {
    this.arm(id, 1200);
  }

  private arm(id: number, ms: number): void {
    this.clear(id);
    this.timers.set(
      id,
      setTimeout(() => this.dismiss(id), ms),
    );
  }

  private clear(id: number): void {
    const timer = this.timers.get(id);

    if (timer !== undefined) {
      clearTimeout(timer);
      this.timers.delete(id);
    }
  }
}
