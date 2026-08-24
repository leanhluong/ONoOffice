import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { DomSanitizer, type SafeHtml } from '@angular/platform-browser';
import { inject } from '@angular/core';
import type { LanguageId } from '../../../core/i18n/translation.config';

/**
 * Lá cờ, vẽ bằng SVG.
 *
 * <b>Vì sao không dùng emoji cờ (🇻🇳):</b> Windows không ship phông cho chúng — nó hiện
 * ra thành hai chữ cái "VN". Nghĩa là hỏng trên đúng hệ điều hành mà phần lớn người dùng
 * đang chạy, và hỏng theo kiểu người viết code trên máy Mac không bao giờ nhìn thấy.
 *
 * Hình chép từ `docs/07-giao-dien/chung/_shell.js`.
 */
@Component({
  selector: 'app-flag',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<svg class="flag" viewBox="0 0 24 16" aria-hidden="true" [innerHTML]="shape()"></svg>`,
  styles: `
    :host {
      display: contents;
    }
  `,
})
export class Flag {
  private readonly sanitizer = inject(DomSanitizer);

  readonly language = input.required<LanguageId>();

  protected readonly shape = computed<SafeHtml>(() =>
    // Nội dung là hằng số trong chính file này, không phải dữ liệu từ người dùng — nên
    // bỏ qua bộ khử độc là an toàn. Cần bỏ qua vì Angular vốn cắt sạch thẻ SVG.
    this.sanitizer.bypassSecurityTrustHtml(SHAPES[this.language()] ?? ''),
  );
}

const SHAPES: Record<string, string> = {
  vi: `<rect width="24" height="16" fill="#DA251D"/><polygon fill="#FF0" points="12,3 13.18,6.38 16.76,6.46 13.9,8.62 14.94,12.05 12,10 9.06,12.05 10.1,8.62 7.24,6.46 10.82,6.38"/>`,

  en: `<rect width="24" height="16" fill="#012169"/>
       <path d="M0,0 L24,16 M24,0 L0,16" stroke="#fff" stroke-width="3.2"/>
       <path d="M0,0 L24,16 M24,0 L0,16" stroke="#C8102E" stroke-width="1.6"/>
       <path d="M12,0 V16 M0,8 H24" stroke="#fff" stroke-width="5.3"/>
       <path d="M12,0 V16 M0,8 H24" stroke="#C8102E" stroke-width="3.2"/>`,

  ja: `<rect width="24" height="16" fill="#fff"/><circle cx="12" cy="8" r="4.6" fill="#BC002D"/>`,

  ko: `<rect width="24" height="16" fill="#fff"/>
       <path d="M12 8a2.4 2.4 0 014.8 0 2.4 2.4 0 01-4.8 0z" fill="#CD2E3A"/>
       <path d="M7.2 8a2.4 2.4 0 014.8 0 2.4 2.4 0 01-4.8 0z" fill="#0047A0"/>`,
};
