/**
 * So BẢN DỰNG với BẢN ANGULAR bằng cách chụp cả hai rồi đối chiếu từng điểm ảnh.
 *
 * ═══════════════════════════════════════════════════════════════════════════
 *  VÌ SAO CẦN THỨ NÀY
 * ═══════════════════════════════════════════════════════════════════════════
 *
 * Đã có `palette-parity.spec.ts` canh bảng màu, nhưng nó chỉ canh MÀU. Nó không bắt được
 * lề lệch 4px, chữ sai cỡ, thiếu một icon, hay cả một khối bị đặt nhầm chỗ — mà đó chính
 * là những thứ đã sai ở bản dựng Angular đầu tiên trong khi build, lint và test đều xanh.
 *
 * Chỉ có một cách canh được "trông giống nhau": nhìn cả hai rồi so. Đây là bộ làm việc đó.
 *
 * ═══════════════════════════════════════════════════════════════════════════
 *  BA CHỖ CỐ Ý KHÔNG SO — và vì sao
 * ═══════════════════════════════════════════════════════════════════════════
 *
 * 1. **Cột trái** (nền sơ đồ tổ chức). Các chấm sinh ngẫu nhiên và trôi liên tục, nên hai
 *    lần chụp bất kỳ đã khác nhau — kể cả chụp cùng một trang hai lần. So nó là so nhiễu.
 *    Cột phải chứa toàn bộ chi tiết cần canh: biểu mẫu, ô nhập, nút, thanh tuỳ chọn.
 *
 * 2. **Thanh đổi trạng thái** ở đáy bản dựng. Đó là khung để duyệt, không ship — bản dựng
 *    nhận `?bare=1` để giấu nó đi.
 *
 * 3. **Sai lệch dưới ngưỡng.** Hai trình kết xuất chữ khác nhau một chút ở viền ký tự là
 *    chuyện bình thường. Ngưỡng đặt ở 0,4% số điểm ảnh — đủ rộng cho khử răng cưa, đủ hẹp
 *    để một lề lệch 4px là đỏ ngay.
 *
 * ═══════════════════════════════════════════════════════════════════════════
 *
 *   npm run parity            # so mọi màn đã khai bên dưới
 *   npm run parity -- login   # chỉ một màn
 *
 * Lệch thì nó ghi ba file vào `.shots/parity/`: bản dựng, bản Angular, và ảnh ĐÁNH DẤU
 * chỗ khác (đỏ). Mở ảnh thứ ba ra là thấy ngay phải sửa chỗ nào.
 */
import { execFile, spawn } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { promisify } from 'node:util';
import { PNG } from 'pngjs';
import pixelmatch from 'pixelmatch';

const run = promisify(execFile);

const CHROME = [
  'C:/Program Files/Google/Chrome/Application/chrome.exe',
  'C:/Program Files (x86)/Google/Chrome/Application/chrome.exe',
].find(existsSync);

const WIDTH = 1440;
const HEIGHT = 940;

/** Cột trái kết thúc ở đây: 1.15fr / 1fr của 1440 → đường chia ở ~769px. */
const COMPARE_FROM_X = 780;

/** 0,4% điểm ảnh. Khử răng cưa của chữ nằm dưới mức này; lệch một lề thì không. */
const TOLERANCE = 0.004;

const PORT = 4288;
const OUT = '.shots/parity';

const SCREENS = [
  { id: 'login', mockup: 'identity/dang-nhap.html', route: '/login' },
  { id: 'signup', mockup: 'identity/dang-ky.html', route: '/dang-ky' },
];

// ── Chụp ảnh ──────────────────────────────────────────────────────────────

async function shoot(url, file) {
  await run(CHROME, [
    '--headless',
    '--disable-gpu',
    '--hide-scrollbars',
    '--allow-file-access-from-files',

    // Ép chế độ TỐI ở cả hai bên. Bản dựng mặc định là bộ Mực; app Angular chọn bộ theo
    // cài đặt máy. Không ép thì một bên Mực, một bên Giấy, và mọi điểm ảnh đều khác.
    '--blink-settings=preferredColorScheme=0',

    `--window-size=${WIDTH},${HEIGHT}`,

    // Chờ đủ lâu để Google Fonts về. Thiếu phông thì bên nào tải xong trước sẽ khác bên kia.
    '--virtual-time-budget=8000',

    `--screenshot=${resolve(file)}`,
    url,
  ]);
}

/** Cắt lấy cột phải. Đọc bằng PNG thô, không cần thư viện xử lý ảnh nào. */
function rightPanel(file) {
  const png = PNG.sync.read(readFileSync(file));
  const width = png.width - COMPARE_FROM_X;
  const out = new PNG({ width, height: png.height });

  for (let y = 0; y < png.height; y++) {
    for (let x = 0; x < width; x++) {
      const from = ((png.width * y) + x + COMPARE_FROM_X) << 2;
      const to = ((width * y) + x) << 2;
      png.data.copy(out.data, to, from, from + 4);
    }
  }

  return out;
}

// ── Máy chủ tĩnh cho bản build ────────────────────────────────────────────

/**
 * Dùng bản BUILD chứ không dùng máy chủ phát triển: bản build là thứ đi lên máy chủ thật.
 * Máy chủ phát triển chèn thêm mã theo dõi thay đổi, và một ngày nào đó sự khác biệt đó
 * sẽ làm bộ so này nói dối.
 */
function serve(root) {
  const server = spawn(
    process.execPath,
    [
      '-e',
      `
      const { createServer } = require('node:http');
      const { readFileSync, existsSync } = require('node:fs');
      const { join, extname } = require('node:path');

      const TYPES = { '.html':'text/html', '.js':'text/javascript', '.css':'text/css',
                      '.json':'application/json', '.ico':'image/x-icon', '.svg':'image/svg+xml' };

      createServer((req, res) => {
        const path = req.url.split('?')[0];
        let file = join(${JSON.stringify(root)}, path === '/' ? 'index.html' : path);

        // Angular định tuyến phía client: đường dẫn nào không phải file thì trả index.html.
        if (!existsSync(file) || !extname(file)) file = join(${JSON.stringify(root)}, 'index.html');

        res.writeHead(200, { 'Content-Type': TYPES[extname(file)] ?? 'application/octet-stream' });
        res.end(readFileSync(file));
      }).listen(${PORT});
      `,
    ],
    { stdio: 'ignore' },
  );

  return server;
}

// ── Chạy ──────────────────────────────────────────────────────────────────

const only = process.argv[2];
const screens = only ? SCREENS.filter((s) => s.id === only) : SCREENS;

if (screens.length === 0) {
  console.error(`Không có màn nào tên "${only}". Có: ${SCREENS.map((s) => s.id).join(', ')}`);
  process.exit(1);
}

if (!CHROME) {
  console.error('Không tìm thấy Chrome. Bộ so ảnh cần Chrome để chụp.');
  process.exit(1);
}

mkdirSync(OUT, { recursive: true });

console.log('Build bản Angular…');
await run(process.platform === 'win32' ? 'npm.cmd' : 'npm', ['run', 'build'], { shell: true });

const server = serve(resolve('dist/onooffice-web/browser'));

// Cho máy chủ kịp mở cổng.
await new Promise((r) => setTimeout(r, 400));

let failed = 0;

try {
  for (const screen of screens) {
    const mockupFile = `${OUT}/${screen.id}-mockup.png`;
    const appFile = `${OUT}/${screen.id}-app.png`;

    const mockupUrl = `file:///${resolve('../docs/07-giao-dien', screen.mockup).replace(/\\/g, '/')}?bare=1`;

    await shoot(mockupUrl, mockupFile);
    await shoot(`http://localhost:${PORT}${screen.route}`, appFile);

    const a = rightPanel(mockupFile);
    const b = rightPanel(appFile);

    if (a.width !== b.width || a.height !== b.height) {
      console.error(`✗ ${screen.id}: kích thước ảnh khác nhau — không so được.`);
      failed++;
      continue;
    }

    const diff = new PNG({ width: a.width, height: a.height });
    const differing = pixelmatch(a.data, b.data, diff.data, a.width, a.height, { threshold: 0.12 });

    const ratio = differing / (a.width * a.height);
    const percent = (ratio * 100).toFixed(2);

    if (ratio > TOLERANCE) {
      writeFileSync(`${OUT}/${screen.id}-diff.png`, PNG.sync.write(diff));
      console.error(`✗ ${screen.id}: lệch ${percent}% (ngưỡng ${(TOLERANCE * 100).toFixed(2)}%)`);
      console.error(`  Xem: ${OUT}/${screen.id}-diff.png — chỗ đỏ là chỗ khác nhau.`);
      failed++;
    } else {
      console.log(`✓ ${screen.id}: lệch ${percent}%`);
    }
  }
} finally {
  server.kill();
}

process.exit(failed === 0 ? 0 : 1);
