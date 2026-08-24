import { readFileSync, readdirSync } from 'node:fs';
import { join } from 'node:path';

/**
 * Đối chiếu ba nguồn phải luôn khớp nhau:
 *
 * <pre>
 *   Messages.vi.resx (backend)  ↔  vi/errors.json  ↔  en/errors.json
 * </pre>
 *
 * <b>Vì sao đáng viết, và vì sao nó là bản sao của một test đã có ở backend:</b> quyết
 * định "backend gửi MÃ, frontend tự tra câu chữ" mở ra một khe hở mới — backend thêm một
 * mã lỗi, frontend không biết, và người dùng nhận được câu tiếng Việt viết cứng trong
 * code backend thay vì bản dịch. Không lỗi nào báo.
 *
 * Backend đã có `LocalizationParityTests` canh chiều của nó. Đây là chiều còn lại, và
 * nó phải nằm ở frontend vì frontend mới là bên có thể thiếu.
 *
 * Sự cố có thật ở NextX: 129 mã khai trong code, 18 mã không có bản dịch nào — người
 * dùng nhìn thấy chuỗi `dms.checklist.template_not_found` trên màn hình.
 */

const I18N_DIR = join(process.cwd(), 'src', 'assets', 'i18n');
const RESX_DIR = join(
  process.cwd(),
  '..',
  'backend',
  'src',
  'ONoOffice.Api',
  'Resources',
);

function readJson(language: string, file: string): Record<string, unknown> {
  return JSON.parse(readFileSync(join(I18N_DIR, language, file), 'utf8')) as Record<
    string,
    unknown
  >;
}

/** Đọc THẲNG file .resx của backend — nguồn sự thật về danh sách mã lỗi. */
function backendCodes(): string[] {
  const xml = readFileSync(join(RESX_DIR, 'Messages.vi.resx'), 'utf8');
  const matches = xml.matchAll(/<data name="([^"]+)"/g);

  return [...matches].map((match) => match[1]);
}

function languages(): string[] {
  return readdirSync(I18N_DIR, { withFileTypes: true })
    .filter((entry) => entry.isDirectory())
    .map((entry) => entry.name);
}

describe('Bản dịch', () => {
  it('có đúng hai ngôn ngữ: vi (gốc) và en', () => {
    expect(languages().sort()).toEqual(['en', 'vi']);
  });

  it('mọi mã lỗi backend khai ĐỀU có bản dịch ở frontend', () => {
    const codes = backendCodes();

    // Bẫy tự thân: đường dẫn hỏng thì test xanh vì không có gì để đối chiếu.
    expect(codes.length).toBeGreaterThanOrEqual(30);

    for (const language of languages()) {
      const translations = readJson(language, 'errors.json');
      const missing = codes.filter((code) => !(code in translations));

      expect(missing, `${language} thiếu bản dịch cho: ${missing.join(', ')}`).toEqual([]);
    }
  });

  /**
   * Chiều ngược lại cũng phải canh: bản dịch còn sót của một mã đã xoá khỏi backend là
   * rác — không ai dám xoá vì không biết còn ai dùng, và nó nằm đó mãi.
   */
  it('mọi bản dịch đều ứng với một mã lỗi có thật', () => {
    const codes = new Set(backendCodes());

    for (const language of languages()) {
      const extra = Object.keys(readJson(language, 'errors.json')).filter(
        (key) => !codes.has(key),
      );

      expect(extra, `${language} có bản dịch thừa: ${extra.join(', ')}`).toEqual([]);
    }
  });

  /**
   * Mọi file dịch phải có ĐÚNG cùng bộ khoá ở cả hai ngôn ngữ.
   *
   * So khoá phẳng (`login.submit`) chứ không so cấu trúc lồng nhau: thiếu cả một nhánh
   * con thì so cấu trúc vẫn có thể lọt, còn so khoá phẳng thì không.
   */
  it.each(['common.json', 'errors.json', 'identity.json'])(
    '%s có cùng bộ khoá ở mọi ngôn ngữ',
    (file) => {
      const [first, ...rest] = languages().map((language) => ({
        language,
        keys: flatKeys(readJson(language, file)).sort(),
      }));

      for (const other of rest) {
        expect(other.keys, `${other.language}/${file} lệch với ${first.language}/${file}`).toEqual(
          first.keys,
        );
      }
    },
  );

  it('không có bản dịch nào bỏ trống', () => {
    for (const language of languages()) {
      for (const file of ['common.json', 'errors.json', 'identity.json']) {
        const empty = flatEntries(readJson(language, file))
          .filter(([, value]) => value.trim().length === 0)
          .map(([key]) => key);

        expect(empty, `${language}/${file} bỏ trống: ${empty.join(', ')}`).toEqual([]);
      }
    }
  });
});

/** `{a:{b:1}}` → `['a.b']`. */
function flatKeys(object: Record<string, unknown>, prefix = ''): string[] {
  return flatEntries(object, prefix).map(([key]) => key);
}

function flatEntries(object: Record<string, unknown>, prefix = ''): [string, string][] {
  return Object.entries(object).flatMap(([key, value]) => {
    const path = prefix ? `${prefix}.${key}` : key;

    return typeof value === 'object' && value !== null
      ? flatEntries(value as Record<string, unknown>, path)
      : ([[path, String(value)]] as [string, string][]);
  });
}
