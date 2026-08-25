import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

/**
 * Mọi lớp CSS <b>của bộ thiết kế dùng chung</b> mà template gọi tới đều phải có luật thật
 * trong <c>styles.scss</c>.
 *
 * <b>Vì sao:</b> CSS của khung ứng dụng và của bộ điều khiển nằm ở file TOÀN CỤC, sinh ra
 * từ bản dựng. Template thì viết tay. Gõ sai một chữ, hoặc bịa ra một tên nghe hợp lý
 * (<c>nav__muc--dang</c>) mà bản dựng không có, thì <b>không có gì báo</b>: HTML nhận mọi
 * tên lớp, CSS bỏ qua lớp không ai khai. Phần tử chỉ đơn giản là không được tô.
 *
 * Chuyện đã xảy ra ngay lần đầu dựng khung v3: template gắn <c>nav__muc--dang</c> qua
 * <c>routerLinkActive</c>, trong khi bản dựng viết luật là
 * <c>.nav__muc[aria-current="page"]</c>. Mục đang mở mất hẳn màu nhấn, mà build và lint
 * đều xanh.
 *
 * Chỉ soi những TIỀN TỐ thuộc bộ dùng chung. Lớp riêng của một màn (ví dụ <c>.field</c> ở
 * màn đăng nhập) nằm trong CSS bó theo component, không thuộc phạm vi của test này.
 */

const SRC = join(process.cwd(), 'src');
const STYLES = join(SRC, 'styles.scss');

/** Tiền tố thuộc bộ dùng chung — sinh từ `_khung.css` và `_dieukhien.css`. */
const CUA_CHUNG = [
  'khung',
  'nav',

  // Khung v4: `rail` là cột app của khung APP, `qt` là toàn bộ khung QUẢN TRỊ. Cả hai
  // sinh từ bản dựng nên template gõ sai một chữ là mất trắng phần tô mà không ai báo —
  // đúng loại lỗi bộ canh này sinh ra để bắt.
  'rail',
  'qt',
  'noidung',
  'trangdau',
  'trang',
  'popover',
  'mat',
  'online',
  'nut',
  'nuti',
  'bang',
  'bangbao',
  'bangcuon',
  'dachon',
  'the',
  'vai',
  'o',
  'luoi',
  'congtac',
  'tab',
  'man',
  'hop',
  'keo',
  'rong',
  'doi',

  // Sinh từ `_brand.css`. Đáng canh hơn phần còn lại một bậc: gõ sai `.logo--lockup` thì
  // `background-image` không được áp, và chỗ đó để lại một khoảng TRẮNG đúng kích thước —
  // trông y hệt như logo đang tải dở, nên rất dễ bị bỏ qua khi soi ảnh chụp.
  'logo',
];

function laCuaChung(ten: string): boolean {
  // So theo KHỐI trước dấu `__` hoặc `--`, để `nav__muc` tính là thuộc `nav` còn
  // `navbar-custom` thì không.
  const khoi = ten.split(/__|--/)[0];

  return CUA_CHUNG.includes(khoi);
}

function moiTemplate(thuMuc: string): string[] {
  return readdirSync(thuMuc).flatMap((ten) => {
    const duongDan = join(thuMuc, ten);

    if (statSync(duongDan).isDirectory()) {
      return moiTemplate(duongDan);
    }

    return ten.endsWith('.html') ? [duongDan] : [];
  });
}

/** Tên lớp có luật thật trong styles.scss. */
function coLuat(): Set<string> {
  const css = readFileSync(STYLES, 'utf8');

  return new Set([...css.matchAll(/\.([a-z][\w-]*)/gi)].map(([, ten]) => ten));
}

/**
 * Tên lớp mà template gọi tới.
 *
 * Phải quét cả `routerLinkActive="…"`, không chỉ `class="…"`. Lần chạy đầu của test này
 * bỏ sót đúng chỗ đó — mà `routerLinkActive` CHÍNH LÀ nơi lớp bịa `nav__muc--dang` được
 * gắn vào. Một bộ canh không soi đúng cơ chế đã gây ra lỗi thì nó chỉ tạo cảm giác an toàn.
 */
function dangDung(): Map<string, string[]> {
  const ketQua = new Map<string, string[]>();

  const ghi = (ten: string, file: string) => {
    if (!laCuaChung(ten)) {
      return;
    }

    const noi = ketQua.get(ten) ?? [];

    noi.push(file);
    ketQua.set(ten, noi);
  };

  for (const duongDan of moiTemplate(SRC)) {
    const html = readFileSync(duongDan, 'utf8');
    const ten = duongDan.slice(SRC.length + 1).replaceAll('\\', '/');

    for (const [, giaTri] of html.matchAll(/\s(?:class|routerLinkActive)="([^"]*)"/g)) {
      giaTri
        .split(/\s+/)
        .filter(Boolean)
        .forEach((c) => ghi(c, ten));
    }

    for (const [, c] of html.matchAll(/\[class\.([\w-]+)\]/g)) {
      ghi(c, ten);
    }
  }

  return ketQua;
}

describe('lớp CSS dùng chung', () => {
  it('không có template nào gọi một lớp không hề có luật', () => {
    const luat = coLuat();

    const bia = [...dangDung().entries()]
      .filter(([ten]) => !luat.has(ten))
      .map(([ten, noi]) => `.${ten} — dùng ở ${[...new Set(noi)].join(', ')}`);

    expect(bia).toEqual([]);
  });
});
