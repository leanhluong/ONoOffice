import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { PopupService } from '../../../core/ui/popup.service';

/**
 * Chỗ vẽ các popup. Đặt một lần ở mỗi màn công khai; nội dung do
 * <see cref="PopupService"/> đẩy vào.
 *
 * Không có style riêng — mọi lớp (`popups`, `popup`, `popup__dot`…) nằm ở `styles.scss`
 * toàn cục, và file đó được SINH từ bản dựng. Nhờ vậy hình dáng popup ở sản phẩm không
 * thể lệch khỏi bản dựng.
 */
@Component({
  selector: 'app-popup-host',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="popups" aria-live="polite">
      @for (popup of popups(); track popup.id) {
        <div
          class="popup"
          [class.popup--error]="popup.tone === 'error'"
          [attr.role]="popup.tone === 'error' ? 'alert' : 'status'"
          (mouseenter)="service.hold(popup.id)"
          (mouseleave)="service.release(popup.id)"
        >
          <span class="popup__dot" aria-hidden="true"></span>

          <span class="popup__body">
            {{ popup.text }}
            @if (popup.reference) {
              <span class="popup__ref">#{{ popup.reference }}</span>
            }
          </span>

          <button
            type="button"
            class="popup__close"
            [attr.aria-label]="'action.close' | translate"
            (click)="service.dismiss(popup.id)"
          >
            ×
          </button>

          <span class="popup__timer" [style.animation-duration.ms]="popup.durationMs"></span>
        </div>
      }
    </div>
  `,
  imports: [TranslatePipe],
})
export class PopupHost {
  protected readonly service = inject(PopupService);
  protected readonly popups = this.service.popups;
}
