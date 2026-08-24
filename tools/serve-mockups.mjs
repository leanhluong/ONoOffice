/**
 * Mở `docs/07-giao-dien` bằng một máy chủ tĩnh để xem bản dựng trên trình duyệt.
 *
 * Vì sao cần, khi mở thẳng file bằng `file://` cũng chạy: `_shell.js` là ES module, và
 * trình duyệt CHẶN import module qua `file://` vì lý do bảo mật (CORS). Mở bằng file thì
 * bốn chấm chọn bộ màu, danh sách ngôn ngữ và popup đều không hoạt động — trang vẫn hiện
 * ra nên rất dễ tưởng là nó hỏng.
 *
 *   node tools/serve-mockups.mjs
 *   → http://localhost:4300/identity/dang-nhap.html
 */
import { createServer } from 'node:http';
import { existsSync, readFileSync, statSync } from 'node:fs';
import { extname, join, normalize } from 'node:path';

const ROOT = 'docs/07-giao-dien';
const PORT = 4300;

const TYPES = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.svg': 'image/svg+xml',
  '.png': 'image/png',
  '.md': 'text/plain; charset=utf-8',
};

/** Trang chỉ mục: liệt kê mọi bản dựng để khỏi phải nhớ đường dẫn. */
const INDEX = `<!doctype html><meta charset="utf-8">
<title>Bản dựng ONoOffice</title>
<link rel="stylesheet" href="/chung/_shell.css">
<style>
  body { display: grid; place-items: center; min-height: 100vh; }
  main { width: min(520px, calc(100vw - 40px)); }
  h1 { font-family: "Sora", system-ui, sans-serif; font-size: 22px; margin: 0 0 4px; }
  p  { color: var(--ink-soft); margin: 0 0 24px; font-size: 14px; }
  a  { display: flex; justify-content: space-between; align-items: center; gap: 16px;
       padding: 14px 16px; margin-bottom: 10px;
       border: 1px solid var(--line); border-radius: 10px;
       background: var(--surface); color: var(--ink);
       text-decoration: none; font-size: 14.5px; }
  a:hover { border-color: var(--accent); }
  small { color: var(--ink-faint); font-family: "JetBrains Mono", monospace; font-size: 11px; }
</style>
<main>
  <h1>Bản dựng giao diện</h1>
  <p>Mỗi màn có thanh đổi trạng thái ở đáy và bốn chấm đổi bộ màu ở góc.</p>
  <a href="/identity/dang-nhap.html">Đăng nhập <small>identity/dang-nhap.html</small></a>
  <a href="/identity/dang-ky.html">Đăng ký workspace <small>identity/dang-ky.html</small></a>
  <a href="/comm/chat.html">Trao đổi nội bộ <small>comm/chat.html</small></a>
  <a href="/wireframes.html">Wireframe 6 màn <small>wireframes.html</small></a>
</main>
`;

createServer((req, res) => {
  const path = decodeURIComponent(req.url.split('?')[0]);

  if (path === '/') {
    res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
    res.end(INDEX);
    return;
  }

  // `normalize` chặn `../` leo ra ngoài thư mục gốc.
  const file = join(ROOT, normalize(path).replace(/^(\.\.[/\\])+/, ''));

  if (!existsSync(file) || !statSync(file).isFile()) {
    res.writeHead(404, { 'Content-Type': 'text/plain; charset=utf-8' });
    res.end(`Không có: ${path}`);
    return;
  }

  res.writeHead(200, { 'Content-Type': TYPES[extname(file)] ?? 'application/octet-stream' });
  res.end(readFileSync(file));
}).listen(PORT, () => {
  console.log(`Bản dựng đang chạy ở  http://localhost:${PORT}`);
  console.log(`  đăng nhập  http://localhost:${PORT}/identity/dang-nhap.html`);
  console.log(`  đăng ký    http://localhost:${PORT}/identity/dang-ky.html`);
  console.log(`  trao đổi   http://localhost:${PORT}/comm/chat.html`);
});
