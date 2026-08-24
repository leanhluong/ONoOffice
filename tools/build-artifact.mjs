/**
 * Gộp một bản dựng trong `docs/07-giao-dien` thành MỘT file tự chứa, để đăng lên
 * claude.ai làm bản xem trên web.
 *
 * Vì sao cần: bản dựng trong repo tách phần dùng chung ra `chung/_shell.css` và
 * `_shell.js` — đúng cho repo, vì bốn bộ màu chỉ khai một lần. Nhưng artifact chạy trong
 * một khung có CSP chặn mọi máy chủ ngoài (trừ Google Fonts) và không có đường dẫn tương
 * đối nào để với sang file khác. Nên bản đăng phải là một file duy nhất.
 *
 * Vì sao gộp bằng máy chứ không chép tay: chép tay thì bản trên web và bản trong repo sẽ
 * lệch nhau — mà đó đúng là chuyện vừa xảy ra: artifact còn là bản cũ trong khi repo đã
 * sửa, và người duyệt xem nhầm bản.
 *
 *   node tools/build-artifact.mjs docs/07-giao-dien/identity/dang-nhap.html
 *
 * In ra đường dẫn file đã gộp. Đăng file đó lên, đừng đăng file gốc.
 */
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';

const source = process.argv[2];

if (!source) {
  console.error('Thiếu đường dẫn. Ví dụ: node tools/build-artifact.mjs docs/07-giao-dien/identity/dang-nhap.html');
  process.exit(1);
}

const dir = dirname(source);
let html = readFileSync(source, 'utf8');

// ── 1. Nhúng CSS dùng chung ───────────────────────────────────────────────
html = html.replace(
  /<link rel="stylesheet" href="(\.\.?\/[^"]+\.css)">/g,
  (_, href) => `<style>\n${readFileSync(resolve(dir, href), 'utf8')}\n</style>`,
);

// ── 2. Nhúng JS dùng chung ────────────────────────────────────────────────
//
// Bỏ từ khoá `export` và bỏ dòng `import` của trang: sau khi gộp thì mọi thứ nằm chung
// một phạm vi module, không còn gì để nhập từ đâu.
html = html.replace(
  /<script type="module">([\s\S]*?)<\/script>/g,
  (whole, body) => {
    const imports = [...body.matchAll(/import\s*\{[^}]*\}\s*from\s*'(\.\.?\/[^']+\.js)';?/g)];

    if (imports.length === 0) {
      return whole;
    }

    const shells = imports
      .map(([, href]) => readFileSync(resolve(dir, href), 'utf8').replace(/^export /gm, ''))
      .join('\n');

    const page = body.replace(/import\s*\{[^}]*\}\s*from\s*'(\.\.?\/[^']+\.js)';?\n?/g, '');

    return `<script type="module">\n${shells}\n${page}</script>`;
  },
);

// ── 3. Gỡ khung để duyệt ──────────────────────────────────────────────────
//
// Thanh đổi trạng thái ở đáy màn hình là công cụ duyệt, không phải sản phẩm. Nhưng bản
// đăng lên web CHÍNH LÀ để duyệt — nên giữ lại. Chỉ gỡ tham số ?state=, vì trong khung
// artifact thì URL không phải của trang.
html = html.replace(/const preset = [\s\S]*?\?\.click\(\);\n?/g, '');

// ── 4. Ghi ra ─────────────────────────────────────────────────────────────
const out = join('.artifacts', source.split('/').pop());

mkdirSync('.artifacts', { recursive: true });
writeFileSync(out, html);

console.log(out);
