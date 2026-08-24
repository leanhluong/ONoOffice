import type { Provider } from '@angular/core';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateMultiHttpLoader } from '@ngx-translate/http-loader';

/** Hai ngôn ngữ hiện có. Tiếng Việt là ngôn ngữ GỐC — viết `vi` trước, `en` dịch theo. */
export const LANGUAGES = [
  { id: 'vi', name: 'Tiếng Việt' },
  { id: 'en', name: 'English' },
] as const;

export type LanguageId = (typeof LANGUAGES)[number]['id'];

export const DEFAULT_LANGUAGE: LanguageId = 'vi';

export const LANGUAGE_STORAGE_KEY = 'onooffice.language';

/**
 * File dịch chia THEO MODULE, giống hệt cách chia của backend và của thư mục tài liệu.
 *
 * Vì sao không gộp một file: một file duy nhất cho 40 màn là chỗ mà mọi nhánh git đều
 * sửa, nên mọi lần merge đều xung đột. Chia theo module thì hai người làm hai module
 * khác nhau không bao giờ giẫm chân.
 *
 * `errors` là file đặc biệt: khoá của nó chính là MÃ LỖI backend trả về.
 */
const RESOURCES = ['common', 'errors', 'identity'] as const;

/**
 * Người dùng đã chọn thì theo họ; chưa chọn thì <b>tiếng Việt</b>.
 *
 * <b>Cố ý KHÔNG suy đoán từ <c>navigator.language</c></b>, dù nghe rất hợp lý. Đây là
 * sản phẩm nội bộ cho công ty Việt Nam — chân trang ghi thẳng "VIỆT NAM". Nhưng rất
 * nhiều máy ở Việt Nam để mặc định <c>en-US</c>: máy mua sẵn, máy công ty cấp, Windows
 * cài bản tiếng Anh. Suy đoán theo cài đặt máy thì phần lớn người dùng thật sẽ mở app
 * lên và thấy tiếng Anh, rồi phải đi tìm chỗ đổi.
 *
 * Đoán sai ở đây không phải lỗi kỹ thuật, nó chỉ là phiền — nhưng phiền với đúng nhóm
 * người mà sản phẩm phục vụ.
 */
export function readSavedLanguage(): LanguageId {
  try {
    const saved = localStorage.getItem(LANGUAGE_STORAGE_KEY);

    if (LANGUAGES.some((language) => language.id === saved)) {
      return saved as LanguageId;
    }
  } catch {
    // Chế độ ẩn danh — rơi xuống mặc định.
  }

  return DEFAULT_LANGUAGE;
}

/**
 * Nạp bản dịch qua HTTP, một bản build duy nhất cho mọi ngôn ngữ.
 *
 * Chọn `@ngx-translate` thay vì `@angular/localize` vì hai lý do bám đúng hoàn cảnh
 * (xem `docs/07-giao-dien/da-ngon-ngu.md`): đổi tiếng KHÔNG phải tải lại trang, và chỉ
 * có MỘT bản build — chưa chốt được tên miền lẫn cách deploy thì thêm chuyện "định
 * tuyến theo ngôn ngữ ở tầng máy chủ" lúc này là tự trói tay.
 */
export function provideAppTranslation(): Provider[] {
  return [
    provideTranslateService({
      lang: readSavedLanguage(),

      /**
       * Thiếu một khoá ở `en` thì rơi về `vi`, không hiện ra chính cái khoá.
       *
       * Hiện khoá trần (`login.submit`) lên màn hình khách hàng là kiểu lỗi mà NextX đã
       * dính: 18 mã lỗi không có bản dịch nào, và người dùng nhìn thấy
       * `dms.checklist.template_not_found` giữa giao diện.
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
         * Mặc định của thư viện là nuốt lỗi 404 và trả về object rỗng — nghĩa là một
         * file lạc mất lúc đóng gói sẽ biểu hiện thành "vài chỗ hiện tên khoá", rất khó
         * lần. Bật cờ này thì lỗi đóng gói lộ ra ngay ở lần chạy đầu tiên.
         */
        failOnError: true,
      }),
    }),
  ];
}
