import { Injectable, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import {
  LANGUAGES,
  LANGUAGE_STORAGE_KEY,
  type ReadyLanguageId,
  readSavedLanguage,
} from './translation.config';

/**
 * Đổi ngôn ngữ lúc chạy, không tải lại trang.
 *
 * Ngôn ngữ là lựa chọn của TỪNG NGƯỜI, không phải cấu hình của workspace — hai người cùng
 * công ty được đọc hai thứ tiếng khác nhau. Nên nó nằm ở `localStorage` của máy đó, giống
 * hệt cách đối xử với bộ màu.
 */
@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly translate = inject(TranslateService);

  private readonly currentState = signal<ReadyLanguageId>(readSavedLanguage());

  readonly current = this.currentState.asReadonly();

  readonly languages = LANGUAGES;

  set(language: ReadyLanguageId): void {
    this.currentState.set(language);
    this.translate.use(language);

    // Cập nhật <html lang> — trình đọc màn hình dựa vào đây để chọn giọng đọc, và trình
    // duyệt dựa vào đây để ngắt dòng đúng quy tắc của từng ngôn ngữ.
    if (typeof document !== 'undefined') {
      document.documentElement.lang = language;
    }

    try {
      localStorage.setItem(LANGUAGE_STORAGE_KEY, language);
    } catch {
      // Chế độ ẩn danh: đổi được cho phiên này, đóng tab thì quên.
    }
  }

  /** Câu báo cho ngôn ngữ chưa có bản dịch. */
  notReadyText(): string {
    return this.translate.instant('language.notReady') as string;
  }
}
