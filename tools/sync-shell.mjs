/**
 * Sinh CSS của Angular THẲNG từ bản dựng trong `docs/07-giao-dien`.
 *
 * ═══════════════════════════════════════════════════════════════════════════
 *  VÌ SAO
 * ═══════════════════════════════════════════════════════════════════════════
 *
 * Yêu cầu là "mockup và code phải y hệt nhau, lần sau kiểm không bị lệch". Cách chắc chắn
 * nhất để hai file không lệch nhau là **đừng có hai file**: chỉ một bên được viết tay, bên
 * kia sinh ra từ nó.
 *
 * Bộ này sinh ba thứ, và cả ba đều mang dòng "ĐỪNG SỬA TAY" ở đầu:
 *
 *   docs/07-giao-dien/chung/_shell.css        → frontend/src/styles.scss
 *   docs/07-giao-dien/identity/dang-nhap.html → .../features/auth/login/login.scss
 *   docs/07-giao-dien/identity/dang-ky.html   → .../features/auth/register/register.scss
 *
 * Còn lại phần đánh dấu (HTML ↔ template Angular) vẫn phải chép tay — không sinh được, vì
 * một bên là HTML tĩnh còn bên kia có `@if`, `@for`, ràng buộc dữ liệu. Đó chính là chỗ
 * `npm run parity` canh: nó chụp cả hai rồi so từng điểm ảnh.
 *
 *   node tools/sync-shell.mjs
 */
import { copyFileSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';

const BANNER = (source) => `/**
 * ⚠️ FILE NÀY ĐƯỢC SINH TỰ ĐỘNG — ĐỪNG SỬA TAY.
 *
 * Nguồn:    ${source}
 * Sinh lại: node tools/sync-shell.mjs
 *
 * Sửa ở đây thì lần chạy bộ sinh tiếp theo sẽ xoá mất, và tệ hơn: bản dựng mà người
 * duyệt nhìn sẽ khác thứ đang chạy thật. Muốn đổi giao diện thì sửa bản dựng rồi chạy
 * lại bộ sinh.
 */

`;

/**
 * Cắt bỏ mọi luật CSS có bộ chọn khớp `drop`.
 *
 * Viết tay bộ tách luật thay vì kéo về một thư viện phân tích CSS: đầu vào là CSS do
 * chính mình viết, không phải CSS bất kỳ trên Internet, nên chỉ cần đếm ngoặc cho đúng.
 */
function stripRules(css, drop) {
  let out = '';
  let i = 0;

  while (i < css.length) {
    const open = css.indexOf('{', i);

    if (open === -1) {
      out += css.slice(i);
      break;
    }

    // Đếm ngoặc để nuốt trọn cả khối @media lồng bên trong.
    let depth = 1;
    let j = open + 1;

    while (j < css.length && depth > 0) {
      if (css[j] === '{') depth++;
      else if (css[j] === '}') depth--;
      j++;
    }

    const selector = css.slice(i, open);
    const rule = css.slice(i, j);

    out += drop.test(selector) ? '' : rule;
    i = j;
  }

  return out;
}

/** Lấy nội dung mọi thẻ <style> trong một file HTML. */
function styleBlocks(html) {
  return [...html.matchAll(/<style>([\s\S]*?)<\/style>/g)].map((m) => m[1]).join('\n');
}

/**
 * Mockup dùng `data-skin`; sản phẩm dùng `data-theme`.
 *
 * Hai tên khác nhau là có chủ ý — `skin` nói rõ đây là bản dựng để CHỌN bộ màu, còn
 * `theme` là tính năng thật của sản phẩm. Đổi tên ở đây, một chỗ duy nhất.
 */
const toProduct = (css) =>
  css
    .replaceAll('data-skin', 'data-theme')

    // Bản dựng nằm trong `docs/07-giao-dien/chung/`, nên nó trỏ tới logo bằng `../brand/`.
    // Sản phẩm phục vụ chúng từ `frontend/public/brand/`, tức là gốc site. Không đổi thì
    // trình duyệt đi tìm `/brand` ở sai chỗ và logo im lặng biến mất — `background-image`
    // hỏng không báo lỗi gì, chỉ để lại một khoảng trắng đúng kích thước.
    .replaceAll('url(../brand/', 'url(/brand/');

// ══════════════════════════════════════════════════════════════════════════
// 1. Nền chung + khung + bộ điều khiển → styles.scss
// ══════════════════════════════════════════════════════════════════════════
{
  // Ba file, một đích. Cả ba đều là CSS TOÀN CỤC (token màu, khung ứng dụng, bộ điều
  // khiển dùng chung), nên gộp vào một file sinh ra là đúng — tách ra chỉ thêm mấy dòng
  // nhập file mà không đổi được gì.
  const NGUON = [
    'docs/07-giao-dien/chung/_shell.css',
    // HAI khung, và chúng không bao giờ cùng xuất hiện trên một trang: `_khung.css` là
    // khuôn app (rail + cột ngữ cảnh), `_khung-quantri.css` là khuôn quản trị (thanh
    // ngang + sidebar). Sản phẩm vẫn gộp cả hai vào một `styles.scss` vì Angular chỉ có
    // một file toàn cục — bộ chọn của hai bên không đụng nhau (`.khung` với `.qt`).
    'docs/07-giao-dien/chung/_khung.css',
    'docs/07-giao-dien/chung/_khung-quantri.css',
    'docs/07-giao-dien/chung/_dieukhien.css',
    'docs/07-giao-dien/chung/_brand.css',
  ];

  const SOURCE = NGUON.join('  +  ');
  const TARGET = 'frontend/src/styles.scss';

  const shell = NGUON.map((f) => toProduct(readFileSync(f, 'utf8'))).join('\n\n');

  // Bộ Giấy để làm mặc định cho máy đặt chế độ SÁNG.
  const giay = shell.match(/:root\[data-theme="giay"\]\s*\{([^}]*)\}/)[1];

  const extra = `
/* ══════════════════════════════════════════════════════════════════════════════
   Ba khối dưới đây CHỈ có ở sản phẩm, không có trong bản dựng.

   Bản dựng luôn mở ở bộ Mực và luôn dùng phông tải từ Google Fonts, nên nó không
   cần chúng. Sản phẩm thì cần: nó phải đoán bộ màu theo cài đặt máy người dùng, và
   phải có tên biến cho thang đo mà component gọi tới.
   ══════════════════════════════════════════════════════════════════════════════ */

/* Máy để chế độ SÁNG mà người dùng chưa chọn gì → Giấy.
   \`:not([data-theme])\` nên lựa chọn tay của người dùng luôn thắng. */
@media (prefers-color-scheme: light) {
  :root:not([data-theme]) {${giay}  }
}

:root {
  --radius: 10px;
  --radius-pill: 999px;

  --font-body: 'Be Vietnam Pro', system-ui, -apple-system, 'Segoe UI', sans-serif;
  --font-display: 'Sora', system-ui, sans-serif;
  --font-mono: 'JetBrains Mono', ui-monospace, 'Cascadia Code', monospace;
}

html, body { height: 100%; }

`;

  writeFileSync(TARGET, BANNER(SOURCE) + shell + extra);
  console.log(`${TARGET}  ←  ${SOURCE}`);
}

// ══════════════════════════════════════════════════════════════════════════
// 2 & 3. CSS của từng màn
// ══════════════════════════════════════════════════════════════════════════
const SCREENS = [
  {
    source: 'docs/07-giao-dien/identity/dang-nhap.html',
    target: 'frontend/src/app/features/auth/login/login.scss',
  },
  {
    source: 'docs/07-giao-dien/identity/dang-ky.html',
    target: 'frontend/src/app/features/auth/register/register.scss',
  },
  {
    source: 'docs/07-giao-dien/org/nhan-su.html',
    target: 'frontend/src/app/features/users/user-list.scss',
  },
  {
    source: 'docs/07-giao-dien/identity/tai-khoan.html',
    target: 'frontend/src/app/features/account/account.scss',
  },
  {
    source: 'docs/07-giao-dien/identity/vai-tro.html',
    target: 'frontend/src/app/features/roles/role-list.scss',
  },
  {
    source: 'docs/07-giao-dien/khung/quan-tri.html',
    target: 'frontend/src/app/features/admin/overview/overview.scss',
  },
];

for (const { source, target } of SCREENS) {
  const html = readFileSync(source, 'utf8');

  // `.states` là thanh đổi trạng thái để duyệt — nó không ship, nên không sinh sang.
  const css = stripRules(toProduct(styleBlocks(html)), /\.states/);

  mkdirSync(dirname(target), { recursive: true });
  writeFileSync(target, BANNER(source) + css.trim() + '\n');

  console.log(`${target}  ←  ${source}`);
}

// ══════════════════════════════════════════════════════════════════════════
// 4. Tệp nhận diện thương hiệu → frontend/public/brand/
// ══════════════════════════════════════════════════════════════════════════
/*
  Chép chứ không phải trỏ tới, vì Angular chỉ đóng gói những gì nằm trong `public/`.

  Và chép bằng BỘ SINH chứ không phải bằng tay, vì lý do y hệt phần CSS ở trên: hai bản
  của cùng một file logo thì sớm muộn sẽ lệch — người thiết kế sửa bản trong `docs/`, sản
  phẩm vẫn chạy bản cũ, và không ai thấy vì logo nào cũng trông "đúng" khi không có cái
  kia đặt cạnh. Bản trong `docs/07-giao-dien/brand/` là bản GỐC duy nhất.
*/
{
  const NGUON = 'docs/07-giao-dien/brand';
  const DICH = 'frontend/public/brand';

  mkdirSync(DICH, { recursive: true });

  // Chỉ .svg — README.md và tokens.css là tài liệu cho người, không phải thứ đem ship.
  const files = readdirSync(NGUON).filter((f) => f.endsWith('.svg'));

  for (const file of files) {
    copyFileSync(join(NGUON, file), join(DICH, file));
  }

  console.log(`${DICH}/  ←  ${NGUON}/  (${files.length} tệp svg)`);
}
