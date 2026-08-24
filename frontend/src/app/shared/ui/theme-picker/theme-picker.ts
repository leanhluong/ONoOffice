import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageService } from '../../../core/i18n/language.service';
import type { LanguageId } from '../../../core/i18n/translation.config';
import { ThemeService, type ThemeId } from '../../../core/theme/theme.service';

/**
 * Chọn bộ màu và ngôn ngữ.
 *
 * Kiểu dáng chép từ `.swatch` của bản dựng mockup: viên thuốc bo tròn, một chấm màu bên
 * trái, tên bộ bên phải. Bản đang chọn đổi màu VIỀN sang accent — không tô đậm nền, vì
 * nền đậm sẽ chọi với chính chấm màu bên trong.
 *
 * Hai thứ này đi cùng nhau vì chúng cùng một loại: <b>lựa chọn của từng người</b>, lưu
 * trên máy họ, không phải cấu hình của workspace. Hai người cùng công ty được dùng hai
 * giao diện và hai thứ tiếng khác nhau.
 *
 * Đặt cả ở màn đăng nhập, không chỉ trong menu sau khi vào: người chưa đăng nhập được
 * cũng cần đọc thông báo lỗi bằng tiếng của họ, và cũng có mắt nhạy sáng.
 */
@Component({
  selector: 'app-theme-picker',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslatePipe],
  styleUrl: './theme-picker.scss',
  template: `
    <div class="prefs">
      <div class="prefs__group" role="group" [attr.aria-label]="'theme.label' | translate">
        @for (theme of themes; track theme.id) {
          <button
            type="button"
            class="swatch"
            [attr.aria-pressed]="themeService.current() === theme.id"
            (click)="setTheme(theme.id)"
          >
            <span class="swatch__dot" [style.background]="theme.dot"></span>
            {{ 'theme.' + theme.id | translate }}
          </button>
        }
      </div>

      <div class="prefs__group" role="group" [attr.aria-label]="'language.label' | translate">
        @for (language of languages; track language.id) {
          <button
            type="button"
            class="lang"
            [attr.aria-pressed]="languageService.current() === language.id"
            (click)="setLanguage(language.id)"
          >
            {{ language.id.toUpperCase() }}
          </button>
        }
      </div>
    </div>
  `,
})
export class ThemePicker {
  protected readonly themeService = inject(ThemeService);
  protected readonly languageService = inject(LanguageService);

  protected readonly themes = this.themeService.themes;
  protected readonly languages = this.languageService.languages;

  protected setTheme(theme: ThemeId): void {
    this.themeService.set(theme);
  }

  protected setLanguage(language: LanguageId): void {
    this.languageService.set(language);
  }
}
