/**
 * Sinh phần token màu của `styles.scss` THẲNG từ bản dựng mockup.
 *
 * Chép tay 4 bộ × 11 token = 44 giá trị hex là 44 cơ hội gõ sai một ký tự — và sai một
 * ký tự thì không ai nhìn ra bằng mắt. Sinh máy thì không thể lệch.
 */
import { readFileSync, writeFileSync } from 'node:fs';

const MOCKUP = 'docs/07-giao-dien/chung/_shell.css';
const TARGET = 'frontend/src/styles.scss';

const html = readFileSync(MOCKUP, 'utf8');

const skins = [...html.matchAll(/:root(?:\[data-skin="(\w+)"\])?\s*\{([^}]*--glow[^}]*)\}/g)].map(
  ([, skin, body]) => ({
    skin: skin ?? 'muc',
    tokens: [...body.matchAll(/(--[\w-]+):\s*([^;]+);/g)].map(([, n, v]) => [n, v.trim()]),
  }),
);

if (skins.length !== 4) {
  throw new Error(`Đọc được ${skins.length} bộ màu, phải là 4 — biểu thức tìm kiếm hỏng.`);
}

const NAMES = {
  muc: 'Mực — nền đen ám xanh, điểm nhấn hổ phách',
  haidang: 'Hải đăng — nền xanh mực sâu, điểm nhấn san hô',
  giay: 'Giấy — nền trắng ngà, điểm nhấn đỏ rượu',
  reu: 'Rêu — nền xanh rêu tối, điểm nhấn xanh xô thơm',
};

const render = (tokens, indent = '  ') =>
  tokens.map(([n, v]) => `${indent}${n}: ${v};`).join('\n');

const giay = skins.find((s) => s.skin === 'giay');

const blocks = skins
  .map(({ skin, tokens }) => {
    const selector = skin === 'muc' ? `:root,\n:root[data-theme='muc']` : `:root[data-theme='${skin}']`;
    return `/* ${NAMES[skin]} */\n${selector} {\n${render(tokens)}\n}`;
  })
  .join('\n\n');

const css = `/**
 * Style toàn cục.
 *
 * Chỉ chứa ba thứ: nạp chữ, khai token của bốn bộ màu, và một reset tối thiểu.
 * Mọi thứ khác thuộc về SCSS của từng component (Angular tự bọc scope).
 *
 * LUẬT BẤT DI BẤT DỊCH: component chỉ được dùng \`var(--token)\`, không bao giờ viết mã
 * màu trực tiếp. Đó là toàn bộ lý do ship được bốn bộ màu mà không tốn gì thêm — bốn bộ
 * chỉ là bốn lần khai lại đúng mười một biến bên dưới.
 */

/* Chữ chọn theo TIẾNG VIỆT trước, không phải theo thẩm mỹ trước — xem he-thong-thiet-ke.md.
   Danh sách và trọng lượng chép đúng từ bản dựng mockup. */
@import url('https://fonts.googleapis.com/css2?family=Sora:wght@400;600;700&family=Be+Vietnam+Pro:wght@400;500;600&family=JetBrains+Mono:wght@400;500&display=swap');

/* ══════════════════════════════════════════════════════════════════════════════
   BỐN BỘ MÀU

   ⚠️ KHỐI DƯỚI ĐÂY ĐƯỢC SINH TỰ ĐỘNG — đừng sửa tay.

   Nguồn: docs/07-giao-dien/chung/_shell.css (nền chung của mọi bản dựng).
   Sinh lại:  node tools/sync-palette.mjs

   Vì sao sinh máy: 4 bộ × 11 token = 44 giá trị hex. Chép tay là 44 cơ hội gõ sai một
   ký tự, và sai một ký tự thì không ai nhìn ra bằng mắt — nó chỉ làm giao diện "hơi
   khác mockup" theo cách không ai chỉ ra được.

   --ground      nền ngoài cùng          --ink-faint   chữ mờ, gợi ý
   --surface     nền thẻ, ô nhập         --accent      điểm nhấn — CHỈ cho hành động chính
   --surface-2   nền chìm hơn một bậc    --accent-ink  chữ nằm trên nền accent
   --line        đường kẻ, viền          --danger      lỗi
   --ink         chữ chính               --glow        accent dạng R,G,B — cho vòng sáng focus
   --ink-soft    chữ phụ, nhãn
   ══════════════════════════════════════════════════════════════════════════════ */

${blocks}

/* Máy để chế độ SÁNG mà người dùng chưa chọn gì → Giấy.
   \`:not([data-theme])\` nên lựa chọn tay của người dùng luôn thắng. */
@media (prefers-color-scheme: light) {
  :root:not([data-theme]) {
${render(giay.tokens, '    ')}
  }
}

/* ══════════════════════════════════════════════════════════════════════════════
   Thang đo — bội số của 4px, không có ngoại lệ
   ══════════════════════════════════════════════════════════════════════════════ */

:root {
  --radius: 10px;
  --radius-pill: 999px;

  --font-body: 'Be Vietnam Pro', system-ui, -apple-system, 'Segoe UI', sans-serif;
  --font-display: 'Sora', system-ui, sans-serif;
  --font-mono: 'JetBrains Mono', ui-monospace, 'Cascadia Code', monospace;
}

*,
*::before,
*::after {
  box-sizing: border-box;
}

html,
body {
  height: 100%;
}

body {
  margin: 0;
  background: var(--ground);
  color: var(--ink);
  font-family: var(--font-body);
  font-size: 15px;
  line-height: 1.6;
  -webkit-font-smoothing: antialiased;

  /* Đổi bộ màu thì chuyển mượt, không giật một nhát. */
  transition:
    background 320ms ease,
    color 320ms ease;
}

code,
kbd,
.mono {
  font-family: var(--font-mono);
}

/* Viền focus nhìn thấy được ở CẢ BỐN bộ màu. Bỏ outline mà không thay bằng gì là cắt
   đường đi của người dùng bàn phím — họ không còn biết mình đang đứng ở đâu. */
:focus-visible {
  outline: 2px solid var(--accent);
  outline-offset: 2px;
  border-radius: 4px;
}

/* Máy đã xin giảm chuyển động thì tắt hẳn, không "giảm bớt". Với người bị rối loạn tiền
   đình, một chuyển động nhỏ vẫn đủ gây chóng mặt. */
@media (prefers-reduced-motion: reduce) {
  *,
  *::before,
  *::after {
    animation-duration: 0.001ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.001ms !important;
  }
}

/* Chỉ trình đọc màn hình thấy. Dùng cho nhãn của nút chỉ có biểu tượng — \`display:none\`
   thì trình đọc màn hình cũng không đọc, tức là nút đó câm với người khiếm thị. */
.visually-hidden {
  position: absolute;
  width: 1px;
  height: 1px;
  margin: -1px;
  padding: 0;
  overflow: hidden;
  clip-path: inset(50%);
  white-space: nowrap;
}
`;

writeFileSync(TARGET, css);
console.log(`Đã sinh ${TARGET} từ ${skins.length} bộ màu trong mockup.`);
