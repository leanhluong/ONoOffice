import type { Provider } from '@angular/core';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateMultiHttpLoader } from '@ngx-translate/http-loader';

/**
 * Ngôn ngữ, kèm cờ vẽ bằng SVG.
 *
 * <b>Không dùng emoji cờ (🇻🇳)</b> — Windows không ship phông cho chúng, nó hiện ra thành
 * hai chữ cái "VN". Nghĩa là hỏng trên đúng hệ điều hành mà phần lớn người dùng đang
 * chạy, và hỏng theo kiểu người viết code trên máy Mac không bao giờ nhìn thấy.
 *
 * Danh sách cố ý để sẵn hai ngôn ngữ <c>ready: false</c> — chúng hiện xám kèm chữ "sắp
 * có". Thà nói thẳng còn hơn giấu đi rồi người dùng tưởng sản phẩm không hỗ trợ tiếng
 * của họ. Đồng bộ với `docs/07-giao-dien/chung/_shell.js`.
 */
export const LANGUAGES = [
  { id: 'vi', name: 'Tiếng Việt', short: 'VI', ready: true },
  { id: 'en', name: 'English', short: 'EN', ready: true },
  { id: 'ja', name: '日本語', short: 'JA', ready: false },
  { id: 'ko', name: '한국어', short: 'KO', ready: false },
] as const;

export type LanguageId = (typeof LANGUAGES)[number]['id'];

/** Chỉ hai ngôn ngữ này có file dịch thật. */
export type ReadyLanguageId = 'vi' | 'en';

export const DEFAULT_LANGUAGE: ReadyLanguageId = 'vi';

export const LANGUAGE_STORAGE_KEY = 'onooffice.language';

/**
 * File dịch chia THEO MODULE, giống hệt cách chia của backend và của thư mục tài liệu.
 *
 * Vì sao không gộp một file: một file duy nhất cho 40 màn là chỗ mà mọi nhánh git đều
 * sửa, nên mọi lần merge đều xung đột.
 *
 * `errors` là file đặc biệt: khoá của nó chính là MÃ LỖI backend trả về, và nó được
 * SINH TỰ ĐỘNG từ `.resx` bằng `tools/sync-error-messages.mjs`.
 */
const RESOURCES = ['common', 'errors', 'identity'] as const;

/**
 * Người dùng đã chọn thì theo họ; chưa chọn thì <b>tiếng Việt</b>.
 *
 * <b>Cố ý KHÔNG suy đoán từ <c>navigator.language</c></b>, dù nghe rất hợp lý. Đây là sản
 * phẩm nội bộ cho công ty Việt Nam. Nhưng rất nhiều máy ở Việt Nam để mặc định
 * <c>en-US</c>: máy mua sẵn, máy công ty cấp, Windows bản tiếng Anh. Suy đoán theo cài
 * đặt máy thì phần lớn người dùng thật mở app lên và thấy tiếng Anh, rồi phải đi tìm chỗ đổi.
 */
export function readSavedLanguage(): ReadyLanguageId {
  try {
    const saved = localStorage.getItem(LANGUAGE_STORAGE_KEY);

    if (saved === 'vi' || saved === 'en') {
      return saved;
    }
  } catch {
    // Chế độ ẩn danh — rơi xuống mặc định.
  }

  return DEFAULT_LANGUAGE;
}

/**
 * Nạp bản dịch qua HTTP, một bản build duy nhất cho mọi ngôn ngữ.
 *
 * Chọn `@ngx-translate` thay vì `@angular/localize` vì hai lý do bám đúng hoàn cảnh (xem
 * `docs/07-giao-dien/da-ngon-ngu.md`): đổi tiếng KHÔNG phải tải lại trang, và chỉ có MỘT
 * bản build.
 */
export function provideAppTranslation(): Provider[] {
  return [
    provideTranslateService({
      lang: readSavedLanguage(),

      /**
       * Thiếu một khoá ở `en` thì rơi về `vi`, không hiện ra chính cái khoá.
       *
       * Hiện khoá trần (`login.submit`) lên màn hình khách hàng là kiểu lỗi mà NextX đã
       * dính: 18 mã lỗi không có bản dịch, và người dùng nhìn thấy
       * `dms.checklist.template_not_found`.
       */
      fallbackLang: DEFAULT_LANGUAGE,

      loader: provideTranslateMultiHttpLoader({
        resources: RESOURCES.map((name) => ({
          prefix: './assets/i18n/',
          suffix: `/${name}.json`,
        })),

        /**
         * Thiếu file dịch thì HỎNG TO, đừng phục vụ một nửa.
         *
         * Mặc định của thư viện là nuốt lỗi 404 và trả object rỗng — một file lạc mất lúc
         * đóng gói sẽ biểu hiện thành "vài chỗ hiện tên khoá", rất khó lần.
         */
        failOnError: true,
      }),
    }),
  ];
}
