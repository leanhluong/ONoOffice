/**
 * Chụp ảnh minh hoạ cho màn Hướng dẫn — từ chính sản phẩm, không vẽ tay.
 *
 * ═══════════════════════════════════════════════════════════════════════════
 *  VÌ SAO SINH TỰ ĐỘNG
 * ═══════════════════════════════════════════════════════════════════════════
 *
 * Ảnh chụp tay lạc hậu ngay lần đổi giao diện kế tiếp, và **không có gì báo**: bài hướng
 * dẫn vẫn dựng ra, vẫn trông chuyên nghiệp, chỉ mô tả một màn hình không còn tồn tại.
 * Người đọc làm theo, không thấy cái nút trong ảnh, rồi kết luận là mình sai.
 *
 * Đây là cùng một lý do `styles.scss` được SINH từ bản dựng chứ không chép tay. Chạy lại
 * lệnh này sau mỗi lần đổi giao diện là ảnh khớp lại.
 *
 * ═══════════════════════════════════════════════════════════════════════════
 *  HAI NGUỒN, VÀ VÌ SAO KHÔNG PHẢI MỘT
 * ═══════════════════════════════════════════════════════════════════════════
 *
 * <b>Màn tĩnh</b> chụp từ **app thật** đang chạy chế độ demo (`?demo=1&auto=1` vào thẳng,
 * không qua đăng nhập). Đó là thứ người dùng sẽ thấy.
 *
 * <b>Trạng thái cần bấm mới ra</b> — hộp thoại, menu xổ, thanh chọn nhiều dòng — chụp từ
 * **bản dựng**. Chrome headless điều hướng tới một URL rồi chụp; nó không bấm được nút.
 * Bản dựng thì mở sẵn được mọi trạng thái qua `?mo=...`, và nó KHỚP với app vì cả hai
 * đang bị `ui-parity.spec` và `npm run parity` canh cho khỏi lệch. Chụp từ bản dựng ở đây
 * không phải đi tắt — đó là dùng đúng công cụ đã dựng sẵn cho việc duyệt từng trạng thái.
 */
import { execFile, spawn } from 'node:child_process';
import { copyFileSync, existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { promisify } from 'node:util';
import { PNG } from 'pngjs';

const run = promisify(execFile);

const GOC = join(dirname(fileURLToPath(import.meta.url)), '..', '..');
const ANH = join(GOC, 'docs', '08-huong-dan', 'anh');
const BAN_DUNG = join(GOC, 'docs', '07-giao-dien');
const FE = join(GOC, 'frontend');

const CHROME = [
  'C:/Program Files/Google/Chrome/Application/chrome.exe',
  'C:/Program Files (x86)/Google/Chrome/Application/chrome.exe',
].find(existsSync);

const RONG = 1280;
const CAO = 800;
const PORT = 4299;

/**
 * Chụp ở bộ màu TỐI, vì đó là mặc định của app trên máy đặt chế độ tối, và ba trong bốn bộ
 * màu là bộ tối. Một bộ ảnh trộn sáng/tối đọc như tài liệu của hai sản phẩm khác nhau.
 */
const MAN = [
  { tep: 'dang-nhap.png', app: '/login' },
  { tep: 'dang-ky.png', app: '/dang-ky' },
  { tep: 'thanh-vien.png', app: '/admin/users' },
  { tep: 'phong-ban.png', app: '/admin/departments' },
  { tep: 'danh-ba.png', app: '/contacts' },
  { tep: 'huong-dan.png', app: '/huong-dan/man-thanh-vien' },

  // Bốn trạng thái dưới đây chỉ hiện ra sau một cú bấm, nên lấy từ bản dựng.
  { tep: 'noi-tai-khoan.png', dung: 'org/nhan-su.html?bare=1&mo=manNoi' },
  { tep: 'hang-loat.png', dung: 'org/nhan-su.html?bare=1&mo=manHangLoat' },
  { tep: 'dat-lai-mat-khau.png', dung: 'org/nhan-su.html?bare=1&mo=manDatLai' },
  { tep: 'chuyen-quyen.png', dung: 'org/nhan-su.html?bare=1&mo=manChuyen' },
  { tep: 'menu-toi.png', dung: 'org/danh-ba.html?bare=1&menu=toi' },
];

async function chup(url, tep) {
  await run(CHROME, [
    '--headless',
    '--disable-gpu',
    '--hide-scrollbars',
    '--allow-file-access-from-files',

    // Ép bộ màu TỐI ở cả hai nguồn. Không ép thì ảnh chụp app theo cài đặt của máy đang
    // chạy lệnh, và bộ ảnh đổi tông tuỳ người chạy.
    '--blink-settings=preferredColorScheme=0',

    `--window-size=${RONG},${CAO}`,

    // Đủ lâu cho phông Google Fonts và lời gọi API giả (chúng có trễ giả 180ms).
    '--virtual-time-budget=16000',

    `--screenshot=${resolve(tep)}`,
    url,
  ]);
}

/** Máy chủ tĩnh cho bản build — dùng bản BUILD vì đó là thứ sẽ đi lên máy chủ thật. */
function phucVu(root) {
  return spawn(
    process.execPath,
    [
      '-e',
      `
      const { createServer } = require('node:http');
      const { readFileSync, existsSync } = require('node:fs');
      const { join, extname } = require('node:path');

      const TYPES = { '.html':'text/html', '.js':'text/javascript', '.css':'text/css',
                      '.json':'application/json', '.ico':'image/x-icon', '.svg':'image/svg+xml',
                      '.png':'image/png', '.woff2':'font/woff2' };

      createServer((req, res) => {
        const path = decodeURIComponent(req.url.split('?')[0]);
        let file = join(${JSON.stringify(root)}, path === '/' ? 'index.html' : path);

        if (!existsSync(file) || !extname(file)) file = join(${JSON.stringify(root)}, 'index.html');

        res.writeHead(200, { 'Content-Type': TYPES[extname(file)] ?? 'application/octet-stream' });
        res.end(readFileSync(file));
      }).listen(${PORT});
      `,
    ],
    { stdio: 'ignore' },
  );
}

/**
 * Chiều cao dải "CHẾ ĐỘ DEMO" — phải khớp `height: 26px` trong `demo-banner.ts`.
 *
 * Cắt nó đi vì tài liệu nói về SẢN PHẨM, không nói về chế độ demo: để nguyên thì người đọc
 * đi tìm cái dải vàng đó trên app của mình và không thấy.
 *
 * Cắt ở đây chứ KHÔNG thêm một tham số tắt dải trong app: dải đó cố ý không có nút tắt, và
 * mở một đường tắt nó — dù chỉ để chụp ảnh — là làm yếu đúng cái nó sinh ra để bảo đảm.
 */
const CAO_DAI_DEMO = 26;

/**
 * Cắt ảnh: bỏ `tren` hàng đầu, rồi giữ lại tối đa `cao` hàng.
 *
 * Ảnh toàn màn hình nhét vào cột bài viết rộng ~720px thì chữ trong ảnh nhỏ hơn chữ xung
 * quanh và không đọc được, nên phần thừa phía dưới bị bỏ.
 */
function cat(tep, cao, tren = 0) {
  const png = PNG.sync.read(readFileSync(tep));
  const caoRa = Math.min(cao, png.height - tren);

  if (tren === 0 && png.height <= cao) {
    return;
  }

  const ra = new PNG({ width: png.width, height: caoRa });

  png.data.copy(ra.data, 0, png.width * tren * 4, png.width * (tren + caoRa) * 4);
  writeFileSync(tep, PNG.sync.write(ra));
}

// ── Chạy ─────────────────────────────────────────────────────────────────

if (!CHROME) {
  console.error('Không tìm thấy Chrome. Bộ chụp ảnh cần Chrome.');
  process.exit(1);
}

mkdirSync(ANH, { recursive: true });

/**
 * Build cấu hình DEVELOPMENT, không phải production — và đây là chỗ dễ sai nhất tệp này.
 *
 * `environment.ts` (production) có `demo: false`, nên interceptor demo **không được đăng
 * ký** và `?demo=1&auto=1` chẳng làm gì cả. Bản đầu dùng `npm run build` và mọi ảnh chụp
 * màn sau đăng nhập đều ra... màn đăng nhập. Nó không báo lỗi: Chrome chụp được một trang
 * hợp lệ, chỉ là trang khác.
 *
 * `npm run parity` dùng bản production được, vì nó chỉ soi hai màn công khai.
 */
console.log('Build bản Angular (cấu hình development — cần chế độ demo)…');
await run(
  process.platform === 'win32' ? 'npm.cmd' : 'npm',
  ['run', 'build', '--', '--configuration', 'development'],
  { cwd: FE, shell: true },
);

const server = phucVu(resolve(FE, 'dist/onooffice-web/browser'));

await new Promise((r) => setTimeout(r, 500));

try {
  for (const man of MAN) {
    const tep = join(ANH, man.tep);

    const url = man.app
      ? `http://localhost:${PORT}${man.app}?demo=1&auto=1`
      : `file:///${join(BAN_DUNG, man.dung).replace(/\\/g, '/')}`;

    await chup(url, tep);

    // Chỉ ảnh chụp từ APP mới có dải demo; bản dựng không có.
    cat(tep, CAO, man.app ? CAO_DAI_DEMO : 0);

    console.log(`✓ ${man.tep}  ←  ${man.app ?? man.dung}`);
  }
} finally {
  server.kill();
}

// Chép sang `public/` để app phục vụ được. Cùng cách `sync-shell.mjs` chép logo: hai bản
// của một file ảnh thì lệch mà không ai thấy.
const DICH = join(FE, 'public', 'huong-dan', 'anh');

mkdirSync(DICH, { recursive: true });

for (const man of MAN) {
  copyFileSync(join(ANH, man.tep), join(DICH, man.tep));
}

console.log(`\nfrontend/public/huong-dan/anh/  ←  docs/08-huong-dan/anh/  (${MAN.length} ảnh)`);
