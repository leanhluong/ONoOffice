/**
 * `docs/08-huong-dan/*.md`  →  `frontend/public/huong-dan/*.json`
 *
 * ═══════════════════════════════════════════════════════════════════════════
 *  VÌ SAO SINH RA CÂY KHỐI, KHÔNG SINH RA HTML
 * ═══════════════════════════════════════════════════════════════════════════
 *
 * Cách nhanh nhất là đổi Markdown thành chuỗi HTML rồi nhét vào `[innerHTML]`. Ba lý do
 * không làm thế:
 *
 * 1. **XSS ở đúng chỗ không cần nó.** Nội dung hôm nay do ta viết, nhưng `innerHTML` là
 *    một cánh cửa mở sẵn cho ngày có người dán nội dung từ nơi khác vào.
 * 2. **Angular sẽ khử vệ sinh nó** và âm thầm gỡ những thứ nó không thích — đúng cái bẫy
 *    đã gặp với `<svg>` ở màn Vai trò.
 * 3. **Không kiểm được.** Một chuỗi HTML thì test chỉ so được chuỗi; một cây khối thì test
 *    đếm được "bài này có 3 ảnh, 2 khối cảnh báo, 4 mục".
 *
 * Nên đầu ra là JSON có cấu trúc, và `huong-dan.html` dựng lại bằng `@switch`.
 *
 * ═══════════════════════════════════════════════════════════════════════════
 *  BỘ ĐỌC NÀY CỐ Ý HẸP, VÀ CỐ Ý NÉM LỖI
 * ═══════════════════════════════════════════════════════════════════════════
 *
 * Nó hiểu đúng những gì `docs/08-huong-dan/README.md` liệt kê. Gặp cú pháp lạ thì **dừng
 * lại và báo dòng nào**, không bỏ qua im lặng.
 *
 * Bỏ qua im lặng là kiểu hỏng tệ nhất cho tài liệu: bài vẫn dựng ra, vẫn trông bình thường,
 * chỉ thiếu mất một đoạn — và người đọc tin vào cái còn lại. Một bộ đọc kêu to thì người
 * viết sửa ngay; một bộ đọc dễ tính thì lỗi sống mãi.
 */
import { existsSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const GOC = join(dirname(fileURLToPath(import.meta.url)), '..');
const NGUON = join(GOC, 'docs', '08-huong-dan');
const DICH = join(GOC, 'frontend', 'public', 'huong-dan');

/** Hai tông của khối chú ý. Thêm tông mới thì phải thêm cả luật CSS — nên nó là danh sách. */
const TONG = new Set(['luuy', 'canh']);

// ── Đọc phần đầu file ────────────────────────────────────────────────────

/**
 * Frontmatter dạng `khoa: giá trị`, mỗi dòng một cặp — KHÔNG phải YAML đầy đủ.
 *
 * Cố ý không kéo một thư viện YAML về: ta cần đúng năm khoá, tất cả đều là chuỗi một dòng.
 * YAML đầy đủ mang theo cả một lớp bất ngờ (`nhom: no` thành `false`) cho một thứ không ai
 * cần tới.
 */
function docDau(noiDung, ten) {
  const khop = /^---\r?\n([\s\S]*?)\r?\n---\r?\n/.exec(noiDung);

  if (!khop) {
    throw new Error(`${ten}: thiếu khối --- ở đầu file.`);
  }

  const dau = {};

  for (const dong of khop[1].split(/\r?\n/)) {
    const cap = /^([a-z]+):\s*(.+)$/.exec(dong);

    if (!cap) {
      throw new Error(`${ten}: dòng đầu file không đúng khuôn "khoa: giá trị" — ${dong}`);
    }

    dau[cap[1]] = cap[2].trim();
  }

  for (const bat of ['ma', 'tieude', 'nhom', 'thutu', 'tomtat']) {
    if (!dau[bat]) {
      throw new Error(`${ten}: thiếu "${bat}" ở đầu file.`);
    }
  }

  return { dau, than: noiDung.slice(khop[0].length) };
}

// ── Chữ trong một dòng ───────────────────────────────────────────────────

/**
 * Cắt một dòng thành các đoạn chữ: thường, **đậm**, `mã`, [liên kết](đích).
 *
 * Trả về MẢNG chứ không phải chuỗi, vì template dựng từng đoạn bằng thẻ riêng. Đó là chỗ
 * ngăn HTML lọt vào: không có bước nào ghép chuỗi thành thẻ cả.
 */
function catChu(dong, ten) {
  const ra = [];
  let con = dong;

  const MAU = [
    { re: /^\*\*([^*]+)\*\*/, lam: (m) => ({ k: 'dam', v: m[1] }) },
    { re: /^`([^`]+)`/, lam: (m) => ({ k: 'ma', v: m[1] }) },
    { re: /^\[([^\]]+)\]\(([^)]+)\)/, lam: (m) => ({ k: 'lien', v: m[1], den: m[2] }) },
  ];

  while (con.length > 0) {
    const khop = MAU.map((m) => ({ m, kq: m.re.exec(con) })).find((x) => x.kq);

    if (khop) {
      ra.push(khop.m.lam(khop.kq));
      con = con.slice(khop.kq[0].length);
      continue;
    }

    // Không khớp mẫu nào thì nuốt tới ký tự đặc biệt KẾ TIẾP, không nuốt cả dòng —
    // nuốt cả dòng thì `**đậm**` nằm giữa câu sẽ không bao giờ được nhận ra.
    const toi = con.slice(1).search(/[*`[]/);
    const cat = toi === -1 ? con.length : toi + 1;

    ra.push({ k: 'chu', v: con.slice(0, cat) });
    con = con.slice(cat);
  }

  // Dấu `*` lẻ còn sót là dấu hiệu gõ hỏng cú pháp đậm — báo thay vì in ra dấu sao.
  for (const doan of ra) {
    if (doan.k === 'chu' && /\*\*/.test(doan.v)) {
      throw new Error(`${ten}: có "**" không khép — ${dong}`);
    }
  }

  return ra;
}

// ── Thân bài ─────────────────────────────────────────────────────────────

function docThan(than, ten) {
  const dong = than.split(/\r?\n/);
  const khoi = [];
  let i = 0;

  const gomDoan = (dung) => {
    const cac = [];

    while (i < dong.length && dong[i].trim() !== '' && !dung(dong[i])) {
      cac.push(dong[i].trim());
      i++;
    }

    return cac.join(' ');
  };

  while (i < dong.length) {
    const hien = dong[i];

    if (hien.trim() === '') {
      i++;
      continue;
    }

    // ── Tiêu đề mục ──
    const de = /^(#{2,3})\s+(.+)$/.exec(hien);

    if (de) {
      khoi.push({ k: 'de', muc: de[1].length, chu: de[2].trim() });
      i++;
      continue;
    }

    if (/^#\s/.test(hien)) {
      throw new Error(`${ten}: đừng dùng "#" — tiêu đề bài đã nằm ở "tieude". Dùng "##".`);
    }

    // ── Ảnh ──
    const anh = /^!\[([^\]]*)\]\(([^)\s]+)(?:\s+"([^"]*)")?\)$/.exec(hien.trim());

    if (anh) {
      if (anh[1].trim() === '') {
        throw new Error(`${ten}: ảnh ${anh[2]} thiếu mô tả cho trình đọc màn hình.`);
      }

      khoi.push({ k: 'anh', mota: anh[1], tep: anh[2], chuthich: anh[3] ?? null });
      i++;
      continue;
    }

    // ── Khối chú ý ──
    const chuy = /^>\s*\[!(\w+)\]\s*(.*)$/.exec(hien);

    if (chuy) {
      if (!TONG.has(chuy[1])) {
        throw new Error(`${ten}: tông "${chuy[1]}" không có. Chỉ có: ${[...TONG].join(', ')}`);
      }

      const cac = [chuy[2].trim()];

      i++;

      while (i < dong.length && /^>\s?/.test(dong[i])) {
        cac.push(dong[i].replace(/^>\s?/, '').trim());
        i++;
      }

      khoi.push({ k: 'chuy', tong: chuy[1], chu: catChu(cac.join(' ').trim(), ten) });
      continue;
    }

    if (hien.startsWith('>')) {
      throw new Error(`${ten}: khối trích dẫn phải mở đầu bằng [!luuy] hoặc [!canh] — ${hien}`);
    }

    // ── Bảng ──
    if (hien.trim().startsWith('|')) {
      const hang = [];

      while (i < dong.length && dong[i].trim().startsWith('|')) {
        hang.push(dong[i].trim());
        i++;
      }

      // Hàng thứ hai là đường kẻ `|---|---|`; nó chỉ để Markdown nhìn ra bảng.
      if (hang.length < 3 || !/^\|[\s:|-]+\|$/.test(hang[1])) {
        throw new Error(`${ten}: bảng phải có hàng tiêu đề, đường kẻ, rồi ít nhất một hàng.`);
      }

      const o = (d) =>
        d
          .replace(/^\|/, '')
          .replace(/\|$/, '')
          .split('|')
          .map((x) => catChu(x.trim(), ten));

      khoi.push({ k: 'bang', dau: o(hang[0]), than: hang.slice(2).map(o) });
      continue;
    }

    // ── Danh sách ──
    const dsCo = /^(\d+)\.\s+(.+)$/.exec(hien);
    const dsKhong = /^-\s+(.+)$/.exec(hien);

    if (dsCo || dsKhong) {
      const coThuTu = Boolean(dsCo);
      const muc = [];

      while (i < dong.length) {
        const d = dong[i];
        const m = coThuTu ? /^(\d+)\.\s+(.+)$/.exec(d) : /^-\s+(.+)$/.exec(d);

        if (!m) {
          // Dòng thụt lề là phần tiếp theo của mục vừa rồi, không phải mục mới.
          if (/^\s{2,}\S/.test(d) && muc.length > 0) {
            muc[muc.length - 1] += ` ${d.trim()}`;
            i++;
            continue;
          }

          break;
        }

        muc.push((coThuTu ? m[2] : m[1]).trim());
        i++;
      }

      khoi.push({ k: 'ds', thutu: coThuTu, muc: muc.map((x) => catChu(x, ten)) });
      continue;
    }

    // ── Đoạn văn ──
    const doan = gomDoan(
      (d) => /^(#{1,3}\s|>|!\[|-\s|\d+\.\s)/.test(d) || d.trim().startsWith('|'),
    );

    if (doan !== '') {
      khoi.push({ k: 'doan', chu: catChu(doan, ten) });
      continue;
    }

    i++;
  }

  return khoi;
}

// ── Chạy ─────────────────────────────────────────────────────────────────

const nhom = JSON.parse(readFileSync(join(NGUON, 'nhom.json'), 'utf8'));
const maNhom = new Set(nhom.map((n) => n.ma));

const tepMd = readdirSync(NGUON)
  .filter((f) => f.endsWith('.md') && f !== 'README.md')
  .sort();

if (tepMd.length === 0) {
  throw new Error('Không có bài hướng dẫn nào. Bộ sinh này không được phép trả về rỗng.');
}

const bai = tepMd.map((ten) => {
  const { dau, than } = docDau(readFileSync(join(NGUON, ten), 'utf8'), ten);

  if (!maNhom.has(dau.nhom)) {
    throw new Error(`${ten}: nhóm "${dau.nhom}" không có trong nhom.json.`);
  }

  return {
    ma: dau.ma,
    tieude: dau.tieude,
    nhom: dau.nhom,
    thutu: Number(dau.thutu),
    tomtat: dau.tomtat,
    khoi: docThan(than, ten),
    tep: ten,
  };
});

// ── Ba phép kiểm toàn cục ────────────────────────────────────────────────

const daThay = new Set();

for (const b of bai) {
  if (daThay.has(b.ma)) {
    throw new Error(`Mã bài "${b.ma}" bị trùng — mã là đường dẫn, nên nó phải là duy nhất.`);
  }

  daThay.add(b.ma);
}

/**
 * Gom MỌI đoạn chữ của một khối, bất kể nó nằm ở `chu`, `muc`, hay trong ô bảng.
 *
 * Viết một hàm đi sâu thay vì liệt kê từng chỗ: thêm một loại khối mới mà quên cập nhật
 * danh sách thì phép kiểm liên kết bên dưới lặng lẽ bỏ sót đúng khối đó.
 */
function moiDoanChu(nut) {
  if (Array.isArray(nut)) {
    return nut.flatMap(moiDoanChu);
  }

  if (nut !== null && typeof nut === 'object') {
    return typeof nut.k === 'string' && 'v' in nut
      ? [nut]
      : Object.values(nut).flatMap(moiDoanChu);
  }

  return [];
}

for (const b of bai) {
  for (const k of b.khoi) {
    // Liên kết NỘI BỘ trỏ tới một bài không tồn tại thì đưa người đọc vào ngõ cụt, và
    // ngõ cụt trong tài liệu thì tệ hơn ở chỗ khác: họ đang bí mới vào đây.
    const dich = moiDoanChu(k)
      .filter((c) => c.k === 'lien' && c.den.startsWith('#'))
      .map((c) => c.den.slice(1));

    for (const d of dich) {
      if (!daThay.has(d)) {
        throw new Error(`${b.tep}: liên kết tới "#${d}" nhưng không có bài nào mang mã đó.`);
      }
    }

    if (k.k === 'anh' && !existsSync(join(NGUON, k.tep))) {
      throw new Error(
        `${b.tep}: ảnh "${k.tep}" chưa có. Chạy "node tools/chup-huong-dan.mjs" hoặc bỏ dòng ảnh.`,
      );
    }
  }
}

mkdirSync(DICH, { recursive: true });

const chiMuc = nhom.map((n) => ({
  ...n,
  bai: bai
    .filter((b) => b.nhom === n.ma)
    .sort((a, b) => a.thutu - b.thutu)
    .map((b) => ({ ma: b.ma, tieude: b.tieude, tomtat: b.tomtat })),
}));

writeFileSync(join(DICH, 'chi-muc.json'), `${JSON.stringify(chiMuc, null, 2)}\n`, 'utf8');

for (const b of bai) {
  writeFileSync(
    join(DICH, `${b.ma}.json`),
    `${JSON.stringify({ ma: b.ma, tieude: b.tieude, nhom: b.nhom, tomtat: b.tomtat, khoi: b.khoi }, null, 2)}\n`,
    'utf8',
  );
}

console.log(
  `frontend/public/huong-dan/  ←  docs/08-huong-dan/  ` +
    `(${bai.length} bài · ${nhom.length} nhóm · ` +
    `${bai.reduce((s, b) => s + b.khoi.filter((k) => k.k === 'anh').length, 0)} ảnh)`,
);
