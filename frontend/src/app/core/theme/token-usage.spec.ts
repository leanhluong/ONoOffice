import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

/**
 * Mọi biến CSS được GỌI phải có chỗ nào đó KHAI BÁO nó.
 *
 * <b>Vì sao cần một test riêng cho chuyện tưởng như hiển nhiên này:</b> CSS gọi một biến
 * không tồn tại thì <b>lặng lẽ bỏ luôn cả dòng khai báo</b> — không lỗi, không cảnh báo,
 * không dòng nào trong console. Thuộc tính đó đơn giản là không được áp.
 *
 * Chuyện đã xảy ra thật ở dự án này: khi chốt bốn bộ màu, bộ tên biến cũ
 * (<c>--color-surface-strong</c>, <c>--color-text</c>…) bị xoá, nhưng khung ứng dụng và
 * bốn màn bên trong vẫn gọi chúng — <b>18 chỗ</b>. Hậu quả: thanh trên mất nền, mục đang
 * mở mất màu nhấn, thẻ mất viền. Mà <c>ng build</c>, <c>ng lint</c> và 72 test đều xanh,
 * và không ai nhìn ra cho tới khi mở màn đó ra xem bằng mắt.
 *
 * Bộ so ảnh <c>npm run parity</c> bắt được kiểu lỗi này, nhưng chỉ ở hai màn nó chụp. Test
 * này canh MỌI file, kể cả màn chưa có bản dựng để so.
 */

const SRC = join(process.cwd(), 'src');
const STYLES = join(SRC, 'styles.scss');

/** Đuôi file có thể chứa CSS: style rời, style nội tuyến trong component, style trong template. */
const DUOI = ['.scss', '.css', '.ts', '.html'];

function moiFile(thuMuc: string): string[] {
  return readdirSync(thuMuc).flatMap((ten) => {
    const duongDan = join(thuMuc, ten);

    if (statSync(duongDan).isDirectory()) {
      return moiFile(duongDan);
    }

    return DUOI.some((d) => ten.endsWith(d)) ? [duongDan] : [];
  });
}

/** Tên biến được KHAI BÁO: `--ten: giá trị`. */
function daKhai(): Set<string> {
  const css = readFileSync(STYLES, 'utf8');

  return new Set([...css.matchAll(/(--[\w-]+)\s*:/g)].map(([, ten]) => ten));
}

/** Tên biến được GỌI: `var(--ten)` hoặc `var(--ten, dự phòng)`. */
function daGoi(): Map<string, string[]> {
  const ketQua = new Map<string, string[]>();

  for (const duongDan of moiFile(SRC)) {
    // Bỏ qua chính bộ test — nó viết tên biến giả để tự kiểm tra.
    if (duongDan.endsWith('.spec.ts')) {
      continue;
    }

    const noiDung = readFileSync(duongDan, 'utf8');

    for (const [, ten] of noiDung.matchAll(/var\(\s*(--[\w-]+)/g)) {
      const noiGoi = ketQua.get(ten) ?? [];

      noiGoi.push(duongDan.slice(SRC.length + 1).replaceAll('\\', '/'));
      ketQua.set(ten, noiGoi);
    }
  }

  return ketQua;
}

describe('biến CSS', () => {
  it('không có chỗ nào gọi một biến chưa được khai báo', () => {
    const khai = daKhai();

    const thieu = [...daGoi().entries()]
      .filter(([ten]) => !khai.has(ten))

      // Nêu đích danh file, không chỉ nêu tên biến. Biết thiếu `--color-text` mà không biết
      // nó nằm ở đâu thì vẫn phải đi tìm bằng tay khắp dự án.
      .map(([ten, noi]) => `${ten} — gọi ở ${[...new Set(noi)].join(', ')}`);

    expect(thieu).toEqual([]);
  });

  it('bốn bộ màu khai đủ mọi biến mà giao diện dùng tới', () => {
    // Khai ở `:root` trần thì mọi bộ đều có. Khai riêng trong một bộ mà quên ba bộ kia thì
    // ba bộ đó mất thuộc tính — đúng kiểu lỗi chỉ lộ ra khi người dùng đổi bộ màu.
    const css = readFileSync(STYLES, 'utf8');
    const bo = ['muc', 'haidang', 'giay', 'reu'];

    const cuaBo = bo.map((id) => {
      const khoi = new RegExp(`:root\\[data-theme="${id}"\\]\\s*\\{([^}]*)\\}`).exec(css);

      expect(khoi, `thiếu hẳn bộ ${id}`).not.toBeNull();

      return new Set([...khoi![1].matchAll(/(--[\w-]+)\s*:/g)].map(([, t]) => t));
    });

    const day = cuaBo[0];

    cuaBo.slice(1).forEach((set, i) => {
      const thieu = [...day].filter((t) => !set.has(t));

      expect(thieu, `bộ ${bo[i + 1]} thiếu`).toEqual([]);
    });
  });
});
