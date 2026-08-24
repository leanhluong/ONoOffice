import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { THEMES } from './theme.service';

/**
 * Đối chiếu bảng màu đang chạy với <b>bản dựng đã duyệt</b>.
 *
 * <pre>
 *   docs/07-giao-dien/chung/_shell.css   ↔   frontend/src/styles.scss
 * </pre>
 *
 * <b>Vì sao cần, khi styles.scss vốn được SINH TỰ ĐỘNG từ chính file đó:</b> sinh tự
 * động chỉ bảo đảm cho lần sinh. Nó không ngăn ai đó mở `styles.scss` ra chỉnh
 * <code>--ink</code> "cho dễ đọc hơn một chút", và cũng không ngăn bản dựng được sửa mà
 * quên chạy lại bộ sinh. Cả hai chiều đều làm sản phẩm lệch khỏi thứ đã duyệt, và cả hai
 * đều <b>không ai nhìn ra bằng mắt</b> — chênh một hai bậc sáng thì trông vẫn "đúng đúng".
 *
 * Đây chính là kiểu sai mà bài học hôm nay dạy: bản đầu tiên của màn đăng nhập được dựng
 * theo file `.md` mô tả, không mở `.html` ra đối chiếu, và 17 giá trị màu sai mà build,
 * lint, test đều xanh.
 */

const MOCKUP = join(process.cwd(), '..', 'docs', '07-giao-dien', 'chung', '_shell.css');

const STYLES = join(process.cwd(), 'src', 'styles.scss');

/** Mã bộ trong mockup dùng `data-skin`; trong sản phẩm là `data-theme`. Tên bộ thì trùng. */
function palettesFromMockup(): Map<string, Map<string, string>> {
  const css = readFileSync(MOCKUP, 'utf8');

  const blocks = [...css.matchAll(/:root(?:\[data-skin="(\w+)"\])?\s*\{([^}]*--glow[^}]*)\}/g)];

  return new Map(
    blocks.map(([, skin, body]) => [
      skin ?? 'muc',
      new Map([...body.matchAll(/(--[\w-]+):\s*([^;]+);/g)].map(([, n, v]) => [n, v.trim()])),
    ]),
  );
}

function palettesFromStyles(): Map<string, Map<string, string>> {
  const scss = readFileSync(STYLES, 'utf8');

  const blocks = [...scss.matchAll(/:root\[data-theme=["'](\w+)["']\]\s*\{([^}]*)\}/g)];

  return new Map(
    blocks.map(([, theme, body]) => [
      theme,
      new Map([...body.matchAll(/(--[\w-]+):\s*([^;]+);/g)].map(([, n, v]) => [n, v.trim()])),
    ]),
  );
}

describe('Bảng màu', () => {
  const mockup = palettesFromMockup();
  const styles = palettesFromStyles();

  // Bẫy tự thân: biểu thức tìm kiếm hỏng thì mọi test dưới đều xanh vì không có gì để so.
  it('đọc được đủ bốn bộ ở CẢ HAI nguồn', () => {
    expect([...mockup.keys()].sort()).toEqual(['giay', 'haidang', 'muc', 'reu']);
    expect([...styles.keys()].sort()).toEqual(['giay', 'haidang', 'muc', 'reu']);
  });

  it.each(['muc', 'haidang', 'giay', 'reu'])('bộ %s khớp từng token với bản dựng', (skin) => {
    const expected = mockup.get(skin);
    const actual = styles.get(skin);

    expect(expected, `mockup không có bộ ${skin}`).toBeDefined();
    expect(actual, `styles.scss không có bộ ${skin}`).toBeDefined();

    expect(Object.fromEntries(actual!)).toEqual(Object.fromEntries(expected!));
  });

  /**
   * Mã bộ trong `theme.service.ts` phải trùng mã trong CSS.
   *
   * Lệch thì `data-theme` mang một giá trị không selector nào bắt được, và giao diện rơi
   * về bộ mặc định — im lặng, không lỗi nào báo. Người dùng bấm "Rêu" và không thấy gì đổi.
   */
  it('mã bộ trong TypeScript trùng mã trong CSS', () => {
    expect(THEMES.map((theme) => theme.id).sort()).toEqual([...styles.keys()].sort());
  });

  /**
   * Chấm màu xem trước phải đúng bằng `--accent` của bộ nó đại diện.
   *
   * Đây là ngoại lệ duy nhất được phép viết mã màu trực tiếp trong TypeScript, nên nó
   * cũng là chỗ duy nhất có thể lệch mà không ai biết.
   */
  it('chấm xem trước bằng đúng --accent của bộ đó', () => {
    for (const theme of THEMES) {
      const accent = mockup.get(theme.id)?.get('--accent');

      expect(theme.dot.toUpperCase(), `chấm của bộ ${theme.id}`).toBe(accent?.toUpperCase());
    }
  });

  /**
   * `--danger` KHÔNG BAO GIỜ được trùng `--accent`.
   *
   * Luật ghi ở he-thong-thiet-ke.md, và bộ Giấy là chỗ nó dễ vi phạm nhất: điểm nhấn vốn
   * đã là một sắc đỏ. Trùng thì nút chính và thông báo lỗi trông y như nhau.
   */
  it.each(['muc', 'haidang', 'giay', 'reu'])('bộ %s có --danger khác hẳn --accent', (skin) => {
    const palette = styles.get(skin)!;

    expect(palette.get('--danger')).not.toBe(palette.get('--accent'));
  });

  /**
   * `--glow` là `--accent` viết dưới dạng `R, G, B`, để dùng được trong `rgb(… / alpha)`.
   *
   * Lệch thì vòng sáng lúc focus mang một màu khác hẳn nút chính — trông như lỗi hiển thị.
   */
  it.each(['muc', 'haidang', 'giay', 'reu'])('bộ %s có --glow đúng bằng --accent', (skin) => {
    const palette = styles.get(skin)!;

    const [r, g, b] = palette
      .get('--glow')!
      .split(',')
      .map((part) => Number(part.trim()));

    const hex = `#${[r, g, b].map((n) => n.toString(16).padStart(2, '0')).join('')}`;

    expect(hex.toUpperCase()).toBe(palette.get('--accent')!.toUpperCase());
  });
});
