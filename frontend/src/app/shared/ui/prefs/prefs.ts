import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  inject,
  signal,
} from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageService } from '../../../core/i18n/language.service';
import {
  LANGUAGES,
  type LanguageId,
  type ReadyLanguageId,
} from '../../../core/i18n/translation.config';
import { PopupService } from '../../../core/ui/popup.service';
import { ThemeService, type ThemeId } from '../../../core/theme/theme.service';
import { Flag } from '../flag/flag';

/**
 * Chọn bộ màu và ngôn ngữ — góc trên phải của mọi màn công khai.
 *
 * Đánh dấu chép đúng `mountPrefs` trong `docs/07-giao-dien/chung/_shell.js`; mọi lớp CSS
 * (`prefs`, `skins`, `skin`, `lang`…) nằm ở `styles.scss` toàn cục, và file đó được SINH
 * từ bản dựng. Hai bên vì thế không thể lệch về hình dáng.
 *
 * <b>Bộ màu chỉ là CHẤM, không kèm tên.</b> Tên bộ chẳng nói được gì mà một ô màu không
 * nói rõ hơn, lại chiếm gấp bốn lần chỗ và phải dịch. Tên vẫn còn trong `title` và trong
 * nhãn cho trình đọc màn hình.
 *
 * <b>Ngôn ngữ là danh sách xổ tự viết, không phải `<select>`.</b> `<select>` không cho
 * đặt hình vào `<option>` nên không có cách nào hiện cờ; mà emoji cờ thì Windows không có
 * phông, nó ra thành hai chữ cái. Đổi lại phải tự lo bàn phím và ARIA — đã làm.
 */
@Component({
  selector: 'app-prefs',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslatePipe, Flag],
  template: `
    <div class="prefs">
      <div class="skins" role="group" [attr.aria-label]="'theme.label' | translate">
        @for (theme of themes; track theme.id) {
          <button
            type="button"
            class="skin"
            [style.background]="theme.dot"
            [title]="'theme.' + theme.id | translate"
            [attr.aria-pressed]="themeService.current() === theme.id"
            (click)="setTheme(theme.id)"
          >
            <span class="visually-hidden">{{ 'theme.' + theme.id | translate }}</span>
          </button>
        }
      </div>

      <div class="lang">
        <button
          type="button"
          class="lang__button"
          aria-haspopup="listbox"
          [attr.aria-expanded]="open()"
          (click)="toggle()"
        >
          <app-flag [language]="languageService.current()" />
          <span>{{ shortOf(languageService.current()) }}</span>
          <svg
            class="lang__caret"
            viewBox="0 0 10 10"
            fill="none"
            stroke="currentColor"
            stroke-width="1.4"
            aria-hidden="true"
          >
            <path d="m2 4 3 3 3-3" />
          </svg>
        </button>

        <ul
          class="lang__menu"
          role="listbox"
          [hidden]="!open()"
          [attr.aria-label]="'language.label' | translate"
        >
          @for (language of languages; track language.id) {
            <li role="none">
              <button
                type="button"
                class="lang__option"
                role="option"
                [attr.data-soon]="language.ready ? null : '1'"
                [attr.aria-selected]="languageService.current() === language.id"
                (click)="choose(language.id)"
              >
                <app-flag [language]="language.id" />
                <span>{{ language.name }}</span>

                @if (language.ready) {
                  <span class="tick">✓</span>
                } @else {
                  <span class="soon">{{ 'language.soon' | translate }}</span>
                }
              </button>
            </li>
          }
        </ul>
      </div>
    </div>
  `,
})
export class Prefs {
  protected readonly themeService = inject(ThemeService);
  protected readonly languageService = inject(LanguageService);

  private readonly popups = inject(PopupService);
  private readonly host = inject(ElementRef<HTMLElement>);

  protected readonly themes = this.themeService.themes;
  protected readonly languages = LANGUAGES;

  protected readonly open = signal(false);

  protected shortOf(id: LanguageId): string {
    return LANGUAGES.find((language) => language.id === id)!.short;
  }

  protected setTheme(theme: ThemeId): void {
    this.themeService.set(theme);
  }

  protected toggle(): void {
    this.open.update((value) => !value);
  }

  protected choose(id: LanguageId): void {
    this.open.set(false);

    const language = LANGUAGES.find((item) => item.id === id)!;

    if (!language.ready) {
      // Nói thẳng thay vì im lặng không làm gì — im lặng thì người dùng tưởng nút hỏng.
      this.popups.show(`${language.name} — ${this.languageService.notReadyText()}`);
      return;
    }

    this.languageService.set(id as ReadyLanguageId);
  }

  /**
   * Bấm ra ngoài thì đóng. Thiếu chỗ này là kiểu lỗi ai cũng gặp: danh sách xổ ra rồi
   * nằm lì trên màn hình cho tới khi đổi trang.
   */
  @HostListener('document:click', ['$event'])
  protected onDocumentClick(event: MouseEvent): void {
    if (this.open() && !this.host.nativeElement.contains(event.target as Node)) {
      this.open.set(false);
    }
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    this.open.set(false);
  }
}
