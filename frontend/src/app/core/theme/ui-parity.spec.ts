import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

/**
 * Bản dựng và template Angular phải mô tả <b>cùng một màn hình</b>.
 *
 * ═══════════════════════════════════════════════════════════════════════
 *  VÌ SAO CẦN BỘ NÀY, KHI ĐÃ CÓ BA BỘ CHỐNG LỆCH KHÁC
 * ═══════════════════════════════════════════════════════════════════════
 *
 * Ba bộ đang có đều bỏ sót đúng chỗ này:
 *
 * <list type="bullet">
 * <item><c>sync-shell.mjs</c> <b>sinh</b> CSS từ bản dựng — nên màu và luật CSS không thể
 * lệch. Nhưng nó không đụng tới ĐÁNH DẤU: bản dựng thêm một thẻ mới mà Angular không
 * thêm thì CSS vẫn khớp hoàn hảo.</item>
 * <item><c>palette-parity</c> canh bảng màu. Chỉ màu.</item>
 * <item><c>npm run parity</c> so từng điểm ảnh — thứ DUY NHẤT bắt được lệch bố cục — nhưng
 * nó chỉ với tới <b>hai màn công khai</b>. Mọi màn sau đăng nhập thì <c>authGuard</c> đá
 * về <c>/login</c> trước khi chụp được.</item>
 * </list>
 *
 * Nên toàn bộ phần đã đăng nhập — Thành viên, Vai trò, Hồ sơ, Tổng quan quản trị — chưa
 * có gì canh cả. Và nó đã lệch thật: màn Tổng quan có một thẻ "Cần bạn xử lý" trong bản
 * dựng mà Angular không hề dựng.
 *
 * ═══════════════════════════════════════════════════════════════════════
 *  SO CÁI GÌ, VÀ VÌ SAO KHÔNG SO NHIỀU HƠN
 * ═══════════════════════════════════════════════════════════════════════
 *
 * So <b>tập lớp CSS</b> mà mỗi bên gắn lên phần tử. Không so từng dòng: một bên là HTML
 * tĩnh, bên kia có <c>{{ }}</c>, <c>&#64;if</c>, <c>&#64;for</c> — so dòng thì đỏ vĩnh viễn.
 *
 * Tập lớp là thứ vừa đủ: một khối mới, một trạng thái mới, một cột mới đều kéo theo lớp
 * mới. Nó KHÔNG bắt được lệch về thứ tự hay câu chữ — chỗ đó vẫn phải nhìn bằng mắt.
 * Một bộ canh biết rõ mình không canh gì thì tốt hơn một bộ canh giả vờ canh tất.
 */

const GOC = join(process.cwd(), '..');
const BAN_DUNG = join(GOC, 'docs', '07-giao-dien');
const ANGULAR = join(process.cwd(), 'src', 'app');

interface Cap {
  readonly ten: string;
  readonly banDung: string;
  readonly angular: string;
}

const CAP: readonly Cap[] = [
  { ten: 'Đăng nhập', banDung: 'identity/dang-nhap.html', angular: 'features/auth/login/login.html' },
  { ten: 'Đăng ký', banDung: 'identity/dang-ky.html', angular: 'features/auth/register/register.html' },
  { ten: 'Hồ sơ', banDung: 'identity/tai-khoan.html', angular: 'features/account/account.html' },
  { ten: 'Thành viên', banDung: 'org/nhan-su.html', angular: 'features/users/user-list.html' },
  { ten: 'Vai trò', banDung: 'identity/vai-tro.html', angular: 'features/roles/role-list.html' },
  {
    ten: 'Tổng quan quản trị',
    banDung: 'khung/quan-tri.html',
    angular: 'features/admin/overview/overview.html',
  },
];

/**
 * Lớp KHÔNG so, kèm lý do. Danh sách này phải ngắn — mỗi dòng thêm vào là một mảng giao
 * diện thôi được canh, nên phải trả lời được câu "vì sao lớp này không thể khớp".
 */
const BO_QUA = new Map<string, string>([
  // Bản dựng là một trang trọn vẹn nên nó tự vẽ khung. Template Angular chỉ là phần ruột
  // nằm trong <router-outlet>; khung do Shell / AdminShell dựng.
  ['khung', 'khung do Shell dựng'],
  ['noidung', 'khung do Shell dựng'],
  ['rail', 'khung do Shell dựng'],
  ['qt', 'khung do AdminShell dựng'],
  ['qt__than', 'khung do AdminShell dựng'],
  ['qt__noi', 'khung do AdminShell dựng'],

  // Angular dựng bằng component con, nên lớp nằm trong template của component đó.
  ['popups', '<app-popup-host />'],
  ['tip', '<app-tip />'],
  ['tip__bubble', '<app-tip />'],

  // Thanh đổi trạng thái ở đáy bản dựng — khung để duyệt, không ship.
  ['states', 'thanh duyệt, không ship'],
  ['states__vach', 'thanh duyệt, không ship'],

  // Trạng thái của KHUNG, do Shell / AdminShell giữ trong signal chứ không nằm ở màn.
  ['khung--gon', 'Shell giữ'],
  ['qt--gon', 'AdminShell giữ'],

  /*
    Bộ đổi trạng thái của bản dựng.

    Bản dựng hiện MỌI trạng thái cùng lúc trong một file rồi ẩn bớt bằng
    `[data-state="…"] .khi--x { display: … }`, để người duyệt bấm qua lại được. Angular
    không cần cơ chế đó — nó có `@switch` / `@if`, và chỉ dựng đúng nhánh đang hiện.

    Nói cách khác: đây là khung để DUYỆT, giống `.states`, không phải đánh dấu của sản
    phẩm. Ép Angular mang chúng thì chỉ tổ thêm mấy thẻ rỗng.
  */
  ['khi', 'bộ đổi trạng thái của bản dựng'],
  ['khi--bang', 'bộ đổi trạng thái của bản dựng'],
  ['khi--rong', 'bộ đổi trạng thái của bản dựng'],
  ['khi--khongthay', 'bộ đổi trạng thái của bản dựng'],
  ['loc__xoa', 'bộ đổi trạng thái của bản dựng — Angular dùng @if (hasFilter())'],
]);

function thanTrang(html: string): string {
  return html
    .replace(/<style>[\s\S]*?<\/style>/g, '')
    .replace(/<script[\s\S]*?<\/script>/g, '')
    .replace(/<!--[\s\S]*?-->/g, '');
}

/**
 * Lớp mà một file gắn lên phần tử.
 *
 * Phải quét CẢ `[class.x]` của Angular lẫn `classList.add('x')` của bản dựng, không chỉ
 * `class="…"`. Mọi lớp TRẠNG THÁI đều được gắn bằng hai cách đó — quét thiếu thì bộ canh
 * báo nhầm hàng loạt rồi bị người ta tắt đi. `class-usage.spec` đã dẫm phải đúng bẫy này.
 */
function lop(html: string, laAngular: boolean): Set<string> {
  const than = thanTrang(html);
  const out = new Set<string>();

  for (const [, giaTri] of than.matchAll(/\sclass="([^"]*)"/g)) {
    for (const c of giaTri.split(/\s+/).filter(Boolean)) {
      if (!c.includes('{') && !c.includes('(')) {
        out.add(c);
      }
    }
  }

  if (laAngular) {
    for (const [, c] of than.matchAll(/\[class\.([\w-]+)\]/g)) {
      out.add(c);
    }
  }

  return out;
}

/** Bản dựng bật lớp trạng thái bằng JS và bằng bộ chọn `[data-state]` trong CSS. */
function lopBanDung(html: string): Set<string> {
  const out = lop(html, false);

  for (const [, c] of html.matchAll(/classList\.(?:add|remove|toggle)\('([\w-]+)'/g)) {
    out.add(c);
  }

  // `.khi--rong { … }` bật bằng `[data-state="rong"] .khi--rong` — chỉ có trong CSS.
  for (const [, c] of html.matchAll(/\[data-state="[^"]+"\]\s+\.([\w-]+)/g)) {
    out.add(c);
  }

  return out;
}

/** Lớp chỉ khai trong CSS của chính bản dựng thì Angular vẫn dùng được qua binding. */
function coTrongCss(html: string, ten: string): boolean {
  return new RegExp(`\\.${ten.replace(/[-]/g, '\\-')}\\b`).test(html);
}

describe('bản dựng ↔ template Angular', () => {
  for (const cap of CAP) {
    it(`${cap.ten}: hai bên dựng cùng một tập khối`, () => {
      const banDung = readFileSync(join(BAN_DUNG, cap.banDung), 'utf8');
      const angular = readFileSync(join(ANGULAR, cap.angular), 'utf8');

      const cuaBanDung = lopBanDung(banDung);
      const cuaAngular = lop(angular, true);

      const bo = (ten: string) => BO_QUA.has(ten) || ten.startsWith('states');

      const thieuOAngular = [...cuaBanDung]
        .filter((ten) => !bo(ten) && !cuaAngular.has(ten))
        .sort();

      // Chiều ngược lại chỉ tính khi bản dựng KHÔNG hề khai lớp đó trong CSS. Angular gắn
      // một lớp mà bản dựng có luật sẵn thì hai bên vẫn đang nói về cùng một thứ.
      const thieuOBanDung = [...cuaAngular]
        .filter((ten) => !bo(ten) && !cuaBanDung.has(ten) && !coTrongCss(banDung, ten))
        .sort();

      expect({ thieuOAngular, thieuOBanDung }).toEqual({
        thieuOAngular: [],
        thieuOBanDung: [],
      });
    });
  }
});
