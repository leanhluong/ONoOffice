/*
  Phần chạy được dùng chung cho mọi bản dựng trong 07-giao-dien.

  Ba thứ: thanh chọn bộ màu + ngôn ngữ, popup thông báo, và nền sơ đồ tổ chức.
  Gom vào một chỗ để bốn bộ màu và cách cư xử của popup giống hệt nhau ở mọi màn —
  chép đi chép lại thì hai màn sẽ lệch, và mockup lệch nhau thì không duyệt được gì.

  ⚠️ Đây là BẢN DỰNG, không phải sản phẩm. Sản phẩm là Angular. File này chỉ cần chạy
  được khi mở bằng trình duyệt.
*/

/* ════════════════════════════════════════════════════════════════════════
   Bộ màu và ngôn ngữ
   ════════════════════════════════════════════════════════════════════════ */

export const SKINS = [
  { id: 'muc', name: 'Mực', dot: '#D9A441' },
  { id: 'haidang', name: 'Hải đăng', dot: '#FF7A5C' },
  { id: 'giay', name: 'Giấy', dot: '#8C2F39' },
  { id: 'reu', name: 'Rêu', dot: '#A3B18A' },
];

/*
  Cờ vẽ bằng SVG, KHÔNG dùng emoji.

  Emoji cờ (🇻🇳) trông gọn hơn nhiều, nhưng Windows không ship phông cho chúng — trên
  Windows nó hiện ra thành hai chữ cái "VN". Nghĩa là trên đúng hệ điều hành mà phần lớn
  người dùng đang chạy, giải pháp đó hỏng, và hỏng theo kiểu người viết code trên máy Mac
  không bao giờ nhìn thấy.

  Danh sách này cố ý để sẵn hai nước chưa làm — để thấy trước danh sách dài ra thì trông
  thế nào, và để chốt luôn cách hiển thị ngôn ngữ chưa có bản dịch.
*/
export const LANGUAGES = [
  { id: 'vi', name: 'Tiếng Việt', short: 'VI', ready: true },
  { id: 'en', name: 'English', short: 'EN', ready: true },
  { id: 'ja', name: '日本語', short: 'JA', ready: false },
  { id: 'ko', name: '한국어', short: 'KO', ready: false },
];

const FLAGS = {
  vi: `<rect width="24" height="16" fill="#DA251D"/><polygon fill="#FF0" points="12,3 13.18,6.38 16.76,6.46 13.9,8.62 14.94,12.05 12,10 9.06,12.05 10.1,8.62 7.24,6.46 10.82,6.38"/>`,
  en: `<rect width="24" height="16" fill="#012169"/>
       <path d="M0,0 L24,16 M24,0 L0,16" stroke="#fff" stroke-width="3.2"/>
       <path d="M0,0 L24,16 M24,0 L0,16" stroke="#C8102E" stroke-width="1.6"/>
       <path d="M12,0 V16 M0,8 H24" stroke="#fff" stroke-width="5.3"/>
       <path d="M12,0 V16 M0,8 H24" stroke="#C8102E" stroke-width="3.2"/>`,
  ja: `<rect width="24" height="16" fill="#fff"/><circle cx="12" cy="8" r="4.6" fill="#BC002D"/>`,
  ko: `<rect width="24" height="16" fill="#fff"/>
       <path d="M12 8a2.4 2.4 0 014.8 0 2.4 2.4 0 01-4.8 0z" fill="#CD2E3A"/>
       <path d="M7.2 8a2.4 2.4 0 014.8 0 2.4 2.4 0 01-4.8 0z" fill="#0047A0"/>
       <circle cx="12" cy="8" r="4.8" fill="none" stroke="#0047A0" stroke-width="0"/>`,
};

const flag = (id) =>
  `<svg class="flag" viewBox="0 0 24 16" aria-hidden="true">${FLAGS[id] ?? ''}</svg>`;

const STORAGE_SKIN = 'onooffice.mockup.skin';
const STORAGE_LANG = 'onooffice.mockup.lang';

const read = (key, fallback) => {
  try {
    return localStorage.getItem(key) ?? fallback;
  } catch {
    return fallback;
  }
};

const save = (key, value) => {
  try {
    localStorage.setItem(key, value);
  } catch {
    /* chế độ ẩn danh — bỏ qua */
  }
};

/** Dựng thanh tuỳ chọn vào một phần tử có sẵn. */
export function mountPrefs(host) {
  // `?skin=giay` để mở thẳng một bộ màu. Có nó thì gửi được link duyệt đúng bộ đang bàn,
  // và bộ chụp ảnh xem được cả bốn bộ mà không phải bấm tay. Chỉ có ở bản dựng.
  const skinTrenUrl = new URLSearchParams(location.search).get('skin');
  let skin = SKINS.some((s) => s.id === skinTrenUrl) ? skinTrenUrl : read(STORAGE_SKIN, 'muc');
  let lang = read(STORAGE_LANG, 'vi');

  host.classList.add('prefs');
  host.innerHTML = `
    <div class="skins" role="group" aria-label="Bộ màu">
      ${SKINS.map(
        (s) => `<button type="button" class="skin" data-skin="${s.id}"
                        style="background:${s.dot}"
                        title="${s.name}" aria-pressed="false">
                  <span class="visually-hidden">${s.name}</span>
                </button>`,
      ).join('')}
    </div>

    <div class="lang">
      <button type="button" class="lang__button" aria-haspopup="listbox" aria-expanded="false">
        <span class="lang__flag"></span>
        <span class="lang__short"></span>
        <svg class="lang__caret" viewBox="0 0 10 10" fill="none" stroke="currentColor" stroke-width="1.4" aria-hidden="true">
          <path d="m2 4 3 3 3-3"/>
        </svg>
      </button>
      <ul class="lang__menu" role="listbox" aria-label="Ngôn ngữ" hidden>
        ${LANGUAGES.map(
          (l) => `<li role="none">
            <button type="button" class="lang__option" role="option" data-lang="${l.id}"
                    aria-selected="false" ${l.ready ? '' : 'data-soon="1"'}>
              ${flag(l.id)}<span>${l.name}</span>
              ${l.ready ? '<span class="tick">✓</span>' : '<span class="soon">sắp có</span>'}
            </button>
          </li>`,
        ).join('')}
      </ul>
    </div>
  `;

  const button = host.querySelector('.lang__button');
  const menu = host.querySelector('.lang__menu');

  const applySkin = (id) => {
    skin = id;
    document.documentElement.setAttribute('data-skin', id);
    host.querySelectorAll('.skin').forEach((b) =>
      b.setAttribute('aria-pressed', String(b.dataset.skin === id)),
    );
    save(STORAGE_SKIN, id);
    document.dispatchEvent(new CustomEvent('skinchange'));
  };

  const applyLang = (id) => {
    lang = id;
    const chosen = LANGUAGES.find((l) => l.id === id);
    host.querySelector('.lang__flag').innerHTML = flag(id);
    host.querySelector('.lang__short').textContent = chosen.short;
    host.querySelectorAll('.lang__option').forEach((b) =>
      b.setAttribute('aria-selected', String(b.dataset.lang === id)),
    );
    save(STORAGE_LANG, id);
  };

  const closeMenu = () => {
    menu.hidden = true;
    button.setAttribute('aria-expanded', 'false');
  };

  host.querySelectorAll('.skin').forEach((b) =>
    b.addEventListener('click', () => applySkin(b.dataset.skin)),
  );

  button.addEventListener('click', () => {
    const open = menu.hidden;
    menu.hidden = !open;
    button.setAttribute('aria-expanded', String(open));
  });

  host.querySelectorAll('.lang__option').forEach((b) =>
    b.addEventListener('click', () => {
      closeMenu();

      if (b.dataset.soon) {
        const name = LANGUAGES.find((l) => l.id === b.dataset.lang).name;
        popup({ text: `${name} — bản dịch chưa có, sẽ thêm sau.` });
        return;
      }

      applyLang(b.dataset.lang);
    }),
  );

  // Bấm ra ngoài hoặc bấm Esc thì đóng. Thiếu hai chỗ này là kiểu lỗi ai cũng gặp:
  // danh sách xổ ra rồi nằm lì trên màn hình.
  document.addEventListener('click', (e) => {
    if (!host.contains(e.target)) closeMenu();
  });
  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && !menu.hidden) {
      closeMenu();
      button.focus();
    }
  });

  applySkin(skin);
  applyLang(lang);
}

/* ════════════════════════════════════════════════════════════════════════
   Popup thông báo — nổi ở đầu màn hình, tự biến mất
   ════════════════════════════════════════════════════════════════════════ */

/**
 * @param {{ tone?: 'error'|'info', text: string, ref?: string, ms?: number }} options
 *
 * `ref` là mã tham chiếu, CHỈ truyền khi không giải thích được chuyện gì đã xảy ra
 * (lỗi 500, mất mạng). Với lỗi nghiệp vụ đã có câu chữ rõ ràng thì đừng truyền — mã kỹ
 * thuật không giúp người dùng làm được gì, chỉ khiến câu thông báo trông đáng sợ hơn.
 */
export function popup({ tone = 'info', text, ref, ms = tone === 'error' ? 6000 : 3200 }) {
  const host = document.getElementById('popups');
  if (!host) return;

  const el = document.createElement('div');
  el.className = `popup popup--${tone}`;
  el.setAttribute('role', tone === 'error' ? 'alert' : 'status');
  el.innerHTML = `
    <span class="popup__dot" aria-hidden="true"></span>
    <span class="popup__body">${text}${ref ? `<span class="popup__ref">#${ref}</span>` : ''}</span>
    <button type="button" class="popup__close" aria-label="Đóng">×</button>
    <span class="popup__timer" style="animation-duration:${ms}ms"></span>
  `;

  const remove = () => {
    el.classList.add('popup--leaving');
    el.addEventListener('animationend', () => el.remove(), { once: true });
  };

  let timer = setTimeout(remove, ms);

  // Rê chuột vào thì dừng đồng hồ — người đang đọc dở không bị cướp mất câu chữ.
  el.addEventListener('mouseenter', () => clearTimeout(timer));
  el.addEventListener('mouseleave', () => {
    timer = setTimeout(remove, 1200);
  });

  el.querySelector('.popup__close').addEventListener('click', () => {
    clearTimeout(timer);
    remove();
  });

  host.append(el);
}

/* ════════════════════════════════════════════════════════════════════════
   Nền: sơ đồ tổ chức trôi chậm
   ════════════════════════════════════════════════════════════════════════ */

/**
 * Chọn hình này vì nó CHÍNH LÀ thứ sản phẩm nói về — cây phòng ban và người trong công
 * ty — chứ không phải hoa văn trang trí bất kỳ.
 */
export function weave(canvas) {
  if (!canvas) return;

  const ctx = canvas.getContext('2d');
  const still = matchMedia('(prefers-reduced-motion: reduce)').matches;

  let nodes = [];
  let w = 0;
  let h = 0;
  let raf = 0;

  function seed() {
    const box = canvas.getBoundingClientRect();
    const dpr = Math.min(devicePixelRatio || 1, 2);

    w = box.width;
    h = box.height;
    canvas.width = w * dpr;
    canvas.height = h * dpr;
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);

    // Mật độ theo DIỆN TÍCH: một con số cố định thì màn 13 inch rối, màn 32 inch thưa.
    const count = Math.max(14, Math.round((w * h) / 26000));

    nodes = Array.from({ length: count }, () => ({
      x: Math.random() * w,
      y: Math.random() * h,
      vx: (Math.random() - 0.5) * 0.14,
      vy: (Math.random() - 0.5) * 0.14,
      r: Math.random() * 1.6 + 1.4,
    }));
  }

  function paint() {
    const ink = getComputedStyle(document.documentElement).getPropertyValue('--ink-faint').trim();

    ctx.clearRect(0, 0, w, h);
    ctx.strokeStyle = ink;

    for (let i = 0; i < nodes.length; i++) {
      for (let j = i + 1; j < nodes.length; j++) {
        const d = Math.hypot(nodes[i].x - nodes[j].x, nodes[i].y - nodes[j].y);
        if (d > 168) continue;

        ctx.globalAlpha = (1 - d / 168) * 0.24;
        ctx.beginPath();
        ctx.moveTo(nodes[i].x, nodes[i].y);
        ctx.lineTo(nodes[j].x, nodes[j].y);
        ctx.stroke();
      }
    }

    ctx.fillStyle = ink;
    for (const n of nodes) {
      ctx.globalAlpha = 0.45;
      ctx.beginPath();
      ctx.arc(n.x, n.y, n.r, 0, Math.PI * 2);
      ctx.fill();
    }
    ctx.globalAlpha = 1;
  }

  function step() {
    for (const n of nodes) {
      n.x += n.vx;
      n.y += n.vy;
      if (n.x < 0 || n.x > w) n.vx *= -1;
      if (n.y < 0 || n.y > h) n.vy *= -1;
    }
    paint();
    raf = requestAnimationFrame(step);
  }

  function start() {
    cancelAnimationFrame(raf);
    seed();
    still ? paint() : step();
  }

  addEventListener('resize', start);
  document.addEventListener('skinchange', paint);
  start();
}

/*
  ════════════════════════════════════════════════════════════════════════
   HAI KHUNG — và vì sao chúng ngược nhau

   A · KHUNG APP      rail 56px  +  cột ngữ cảnh  +  nội dung
                      Lark Messenger, Zalo PC, Slack. Mở app là vào thẳng CHAT.
                      Dựng bằng: mountRail() + (tuỳ app) mountNav()

   B · KHUNG QUẢN TRỊ thanh ngang  +  sidebar 240px  +  nội dung dạng thẻ
                      Lark Admin. KHÔNG có rail — nó là một sản phẩm khác.
                      Dựng bằng: mountQuanTri()

   Lý do đầy đủ nằm ở `_khung-quantri.css`. Tóm tắt: điều hướng app thì RỘNG (6–8 app
   ngang hàng, biểu tượng là đủ), điều hướng quản trị thì SÂU (12 nhóm × 3–6 trang con,
   bắt buộc phải có chữ và phải gập được).
  ════════════════════════════════════════════════════════════════════════
*/

const BIEU = {
  'trao-doi':
    '<path d="M17 11.3a3 3 0 01-3 3H8.4L4.5 17.2v-2.9H6a3 3 0 01-3-3V5.7a3 3 0 013-3h8a3 3 0 013 3z"/>',
  hop: '<rect x="2.5" y="5" width="10" height="10" rx="2"/><path d="m12.5 9 5-2.6v7.2L12.5 11z"/>',
  lich:
    '<rect x="3" y="4.2" width="14" height="13" rx="2.2"/><path d="M3 8.2h14M7 2.6v3.2M13 2.6v3.2"/>',
  'tai-lieu':
    '<path d="M5 2.8h6.6L15 6.2v11H5z"/><path d="M11.2 2.8v3.6H15"/><path d="M7.6 10.4h4.8M7.6 13.2h3.2"/>',
  'danh-ba':
    '<circle cx="7.5" cy="6.8" r="2.9"/><path d="M2.4 16.6a5.1 5.1 0 0110.2 0"/><path d="M13.2 4.4a2.9 2.9 0 010 4.8M14.8 16.6a5.1 5.1 0 00-1.7-3.8"/>',
  duyet: '<path d="M4 3.5h12v13l-3-2-3 2-3-2-3 2z"/><path d="M7 7.5h6M7 10.5h4"/>',
  'quan-tri':
    '<path d="M10 2.5 4 5v4.5c0 3.4 2.5 6.5 6 8 3.5-1.5 6-4.6 6-8V5z"/><path d="M10 7.2v3.4M10 13.1v.1"/>',
};

/**
 * Các app trên rail.
 *
 * `soon` = chưa làm, bấm vào nói thẳng. Cố ý để chúng trong danh sách chứ không giấu:
 * người duyệt cần thấy trước rail ĐẦY ĐỦ trông thế nào, vì số app quyết định cả chiều
 * cao của rail. Giấu rồi thêm dần thì mỗi lần thêm là một lần bố cục đổi.
 *
 * Thứ tự KHÔNG tuỳ tiện: Trao đổi đứng đầu vì đó là app mặc định — mở ONoOffice là vào
 * thẳng đó, giống Lark và Zalo. App dùng nhiều nhất phải ở chỗ ngón tay chạm tới trước.
 */
const APPS = [
  { ma: 'trao-doi', ten: 'Trao đổi', href: '../comm/chat.html', dem: 4 },
  { ma: 'lich', ten: 'Lịch', soon: 'Lịch' },
  { ma: 'tai-lieu', ten: 'Tài liệu', soon: 'Tài liệu' },
  { ma: 'duyet', ten: 'Chờ duyệt', dem: 9, soon: 'Chờ duyệt' },
  { ma: 'danh-ba', ten: 'Danh bạ', cham: true, soon: 'Danh bạ' },
];

const bieu = (d, lop = 'nav__icon') =>
  `<svg class="${lop}" viewBox="0 0 20 20" fill="none" stroke="currentColor"
     stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${d}</svg>`;

/*
  Menu tài khoản — giống hệt Zalo PC, và giống vì đúng chứ không vì bắt chước.

  Bốn dòng, theo thứ tự Zalo đặt: Nâng cấp · Hồ sơ · Cài đặt · Đăng xuất. "Nâng cấp" đứng
  đầu và có mũi tên chéo (mở ra một chỗ khác) — đó là chỗ người dùng đi tìm khi hết dung
  lượng, và cũng là chỗ DUY NHẤT trong khung app dẫn sang vùng quản trị.

  Vì sao lối vào quản trị nằm ở đây chứ không phải một biểu tượng trên rail: rail là các
  APP người ta dùng hằng ngày. Quản trị thì mỗi tháng vào một lần, và nó không phải app —
  nó là chỗ sửa thứ của người khác. Menu tài khoản là đúng chỗ: "những việc về TÔI và về
  CÔNG TY TÔI", tách khỏi "những việc tôi làm hằng ngày".
*/
const MENU_TOI = `
  <div class="popover__ai">
    <div class="popover__ten">Lê Anh Lượng</div>
    <div class="popover__mail">chu@congty.vn · Owner</div>
  </div>

  <a class="popover__nut" href="../khung/quan-tri.html">
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true">
      <path d="M8 2 3 4v3.6c0 2.7 2 5.2 5 6.4 3-1.2 5-3.7 5-6.4V4z"/>
    </svg>
    Quản trị &amp; gói cước
    <svg class="popover__ngoai" viewBox="0 0 16 16" fill="none" stroke="currentColor"
         stroke-width="1.6" stroke-linecap="round" aria-hidden="true">
      <path d="M6.5 3.5H12.5V9.5M12.5 3.5 6 10"/><path d="M11 12.5H3.5V5"/>
    </svg>
  </a>

  <a class="popover__nut" href="../identity/tai-khoan.html">
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true">
      <circle cx="8" cy="5.5" r="2.4"/><path d="M3.5 13.2a4.5 4.5 0 019 0"/>
    </svg>
    Hồ sơ của bạn
  </a>

  <a class="popover__nut" href="../identity/tai-khoan.html?tab=giaodien">
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true">
      <circle cx="8" cy="8" r="2.1"/>
      <path d="M8 2v1.6M8 12.4V14M14 8h-1.6M3.6 8H2M12.2 3.8l-1.1 1.1M4.9 11.1l-1.1 1.1M12.2 12.2l-1.1-1.1M4.9 4.9 3.8 3.8"/>
    </svg>
    Cài đặt
  </a>

  <div class="popover__vach"></div>

  <button class="popover__nut" data-soon="Đăng xuất">
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true">
      <path d="M6 14H3.5A1.5 1.5 0 012 12.5v-9A1.5 1.5 0 013.5 2H6M10.5 11 14 8l-3.5-3M14 8H6"/>
    </svg>
    Đăng xuất
  </button>
`;

/** Gắn hành vi mở/đóng cho các menu bật ra trong một vùng chrome. */
function ganPopover(host, cap) {
  for (const [nutId, menuId] of cap) {
    const nut = host.querySelector(nutId);
    const menu = host.querySelector(menuId);

    if (!nut || !menu) continue;

    nut.addEventListener('click', (e) => {
      e.stopPropagation();
      const mo = menu.hidden;

      // Đóng mọi menu khác trước: hai cái cùng mở thì chồng lên nhau.
      host.querySelectorAll('.popover').forEach((m) => (m.hidden = true));
      host
        .querySelectorAll('[aria-haspopup]')
        .forEach((b) => b.setAttribute('aria-expanded', 'false'));

      menu.hidden = !mo;
      nut.setAttribute('aria-expanded', String(mo));
    });

    menu.addEventListener('click', (e) => e.stopPropagation());
  }

  // Bấm ra ngoài hoặc Esc thì đóng. Thiếu chỗ này là kiểu lỗi ai cũng gặp: menu xổ ra rồi
  // nằm lì trên màn hình cho tới khi đổi trang.
  const dong = () => {
    host.querySelectorAll('.popover').forEach((m) => (m.hidden = true));
    host
      .querySelectorAll('[aria-haspopup]')
      .forEach((b) => b.setAttribute('aria-expanded', 'false'));
  };

  document.addEventListener('click', dong);
  document.addEventListener('keydown', (e) => e.key === 'Escape' && dong());
}

/**
 * KHUNG A — rail của khung app.
 *
 * @param host  phần tử sẽ thành rail
 * @param dang  mã app đang mở
 */
export function mountRail(host, dang) {
  const dong = (a) => {
    const chon = a.ma === dang;
    const the = a.href && !chon ? 'a' : 'button';
    const thuoc = a.href && !chon ? `href="${a.href}"` : 'type="button"';

    // Số đếm ở rail chỉ trả lời "CÓ hay KHÔNG có việc"; con số chính xác để dành cho cột
    // ngữ cảnh. Quá 9 thì "9+" — ba chữ số trên viên 17px thì không đọc được, mà "47 hay
    // 52 tin chưa đọc" không đổi hành vi của ai.
    const dem = a.dem
      ? `<span class="rail__dem">${a.dem > 9 ? '9+' : a.dem}</span>`
      : a.cham
        ? '<span class="rail__dem rail__dem--cham"></span>'
        : '';

    return `<${the} class="rail__muc" ${thuoc} ${chon ? 'aria-current="page"' : ''}
       ${a.soon ? `data-soon="${a.soon}"` : ''} aria-label="${a.ten}">
      ${bieu(BIEU[a.ma], 'rail__icon')}
      ${dem}
      <span class="rail__meo">${a.ten}</span>
    </${the}>`;
  };

  host.className = 'rail';
  host.setAttribute('aria-label', 'Ứng dụng');
  host.innerHTML = `
    <a class="rail__logo logo logo--mark" href="../comm/chat.html"
       role="img" aria-label="ONoOffice"></a>

    <div class="rail__ds">
      ${APPS.map(dong).join('')}
    </div>

    <div class="rail__duoi">
      <button class="nuti" id="railCaidat" aria-haspopup="true" aria-expanded="false"
              aria-label="Giao diện và ngôn ngữ">
        <svg viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true">
          <circle cx="10" cy="10" r="2.6"/>
          <path d="M10 2.5v2M10 15.5v2M17.5 10h-2M4.5 10h-2M15.3 4.7l-1.4 1.4M6.1 13.9l-1.4 1.4M15.3 15.3l-1.4-1.4M6.1 6.1 4.7 4.7"/>
        </svg>
      </button>

      <button class="rail__toi" id="railToi" aria-haspopup="true" aria-expanded="false"
              aria-label="Tài khoản của tôi">
        <span class="mat mat--nho">LL<span class="online"></span></span>
      </button>
    </div>

    <div class="popover" id="railMenuCaidat" hidden style="left: 60px; bottom: 52px;">
      <div class="popover__hang">Bộ màu &amp; ngôn ngữ</div>
      <div style="padding: 2px 10px 8px;"><div id="prefs"></div></div>
    </div>

    <div class="popover" id="railMenuToi" hidden style="left: 60px; bottom: 8px;">
      ${MENU_TOI}
    </div>
  `;

  mountPrefs(host.querySelector('#prefs'));
  ganPopover(host, [
    ['#railCaidat', '#railMenuCaidat'],
    ['#railToi', '#railMenuToi'],
  ]);
}

/**
 * KHUNG A — cột ngữ cảnh mặc định.
 *
 * App nào có cột giàu hơn thì tự dựng lấy: màn Trao đổi dùng `.ds` với ảnh đại diện, câu
 * cuối và giờ. App không cần cột thì đơn giản là không gọi hàm này.
 *
 * @param host   phần tử sẽ thành cột
 * @param tieuDe tên app, hiện ở đầu cột
 * @param nhom   [{ nhan, muc: [{ ma, ten, href, dem, nhat, soon }] }]
 * @param dang   mã trang đang mở
 */
export function mountNav(host, tieuDe, nhom, dang) {
  const dongMuc = (m) => {
    const chon = m.ma === dang;
    const the = m.href && !chon ? 'a' : 'button';
    const thuoc = m.href && !chon ? `href="${m.href}"` : 'type="button"';

    return `<${the} class="nav__muc" ${thuoc} ${chon ? 'aria-current="page"' : ''}
       ${m.soon ? `data-soon="${m.soon}"` : ''}>
      ${bieu(BIEU[m.ma] ?? m.icon ?? '')}
      <span class="nav__chu">${m.ten}</span>
      ${m.dem ? `<span class="nav__dem ${m.nhat ? 'nav__dem--nhat' : ''}">${m.dem}</span>` : ''}
    </${the}>`;
  };

  host.className = 'nav';
  host.setAttribute('aria-label', tieuDe);
  host.innerHTML = `
    <div class="nav__dau"><h2>${tieuDe}</h2></div>

    <div class="nav__tim">
      <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true">
        <circle cx="7" cy="7" r="4.5"/><path d="m10.5 10.5 3 3"/>
      </svg>
      <input type="search" placeholder="Tìm trong ${tieuDe.toLowerCase()}"
             aria-label="Tìm trong ${tieuDe}">
      <span class="nav__phim">Ctrl K</span>
    </div>

    <div class="nav__ds">
      ${nhom
        .map(
          (n) =>
            `${n.nhan ? `<div class="nav__nhan">${n.nhan}</div>` : ''}${n.muc.map(dongMuc).join('')}`,
        )
        .join('')}
    </div>
  `;
}

/*
  ════════════════════════════════════════════════════════════════════════
   KHUNG B — QUẢN TRỊ
  ════════════════════════════════════════════════════════════════════════

  Sidebar hai cấp, gập được. Đây là chỗ khuôn app không gánh nổi: 12 nhóm, mỗi nhóm vài
  trang con. Rail 56px không có chỗ cho chữ, mà không có chữ thì "Tuân thủ" và "Bảo mật"
  là hai biểu tượng khiên gần giống nhau.

  `soon` vẫn để trong danh sách, cùng lý do với rail: người duyệt phải thấy trước cây menu
  ĐẦY ĐỦ, vì độ dài của nó quyết định có cần gập hay không.
*/
const QT_MUC = [
  {
    ma: 'tong-quan',
    ten: 'Tổng quan tổ chức',
    href: '../khung/quan-tri.html',
    icon: '<path d="M10 2.6 3 6l7 3.4L17 6z"/><path d="m3 10 7 3.4L17 10M3 14l7 3.4L17 14"/>',
  },
  {
    ma: 'to-chuc',
    ten: 'Tổ chức',
    icon: '<rect x="7" y="2.5" width="6" height="4.5" rx="1.3"/><rect x="2" y="13" width="5.5" height="4.5" rx="1.3"/><rect x="12.5" y="13" width="5.5" height="4.5" rx="1.3"/><path d="M10 7v3.5M4.75 13v-2.5h10.5V13"/>',
    con: [
      { ma: 'tai-khoan-ds', ten: 'Thành viên', href: '../org/nhan-su.html', dem: 38 },
      { ma: 'phong-ban', ten: 'Phòng ban', dem: 6, soon: 'Phòng ban' },
      { ma: 'vai-tro', ten: 'Vai trò & quyền', href: '../identity/vai-tro.html', dem: 4 },
    ],
  },
  {
    ma: 'goi-cuoc',
    ten: 'Gói cước & hoá đơn',
    icon: '<rect x="2.5" y="4.5" width="15" height="11" rx="2"/><path d="M2.5 8.5h15M5.5 12.5h3"/>',
    con: [
      { ma: 'goi', ten: 'Gói đang dùng', href: '../khung/quan-tri.html?tab=goi' },
      { ma: 'hoa-don', ten: 'Hoá đơn', soon: 'Hoá đơn' },
    ],
  },
  {
    ma: 'dung-luong',
    ten: 'Dung lượng lưu trữ',
    icon: '<ellipse cx="10" cy="5.4" rx="6.5" ry="2.6"/><path d="M3.5 5.4v9.2c0 1.4 2.9 2.6 6.5 2.6s6.5-1.2 6.5-2.6V5.4"/><path d="M3.5 10c0 1.4 2.9 2.6 6.5 2.6s6.5-1.2 6.5-2.6"/>',
    con: [{ ma: 'quota', ten: 'Hạn ngạch', href: '../khung/quan-tri.html?tab=quota' }],
  },
  {
    ma: 'bao-mat',
    ten: 'Bảo mật',
    icon: '<path d="M10 2.5 4 5v4.5c0 3.4 2.5 6.5 6 8 3.5-1.5 6-4.6 6-8V5z"/><path d="m7.6 10 1.7 1.7 3.3-3.4"/>',
    con: [
      { ma: 'phien', ten: 'Phiên đăng nhập', soon: 'Phiên đăng nhập' },
      { ma: 'mat-khau', ten: 'Chính sách mật khẩu', soon: 'Chính sách mật khẩu' },
    ],
  },
  {
    ma: 'nhat-ky',
    ten: 'Nhật ký hoạt động',
    soon: 'Nhật ký hoạt động',
    icon: '<path d="M5 2.8h10v14.4H5z"/><path d="M7.6 6.4h4.8M7.6 9.6h4.8M7.6 12.8h2.8"/>',
  },
  {
    ma: 'cai-dat',
    ten: 'Cài đặt workspace',
    soon: 'Cài đặt workspace',
    icon: '<circle cx="10" cy="10" r="2.6"/><path d="M10 2.5v2M10 15.5v2M17.5 10h-2M4.5 10h-2M15.3 4.7l-1.4 1.4M6.1 13.9l-1.4 1.4M15.3 15.3l-1.4-1.4M6.1 6.1 4.7 4.7"/>',
  },
];

/**
 * KHUNG B — thanh ngang + sidebar của màn quản trị.
 *
 * @param tren  phần tử thanh ngang
 * @param ben   phần tử sidebar
 * @param dang  mã trang đang mở
 */
export function mountQuanTri(tren, ben, dang) {
  const mui = `<svg class="qt__mui" viewBox="0 0 16 16" fill="none" stroke="currentColor"
      stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m6 3.5 4.5 4.5L6 12.5"/></svg>`;

  // Nhóm nào chứa trang đang mở thì mở sẵn. Gập hết lại rồi bắt người dùng tự tìm xem
  // mình đang ở đâu là cách chắc chắn nhất làm họ lạc trong một cây 12 nhánh.
  const dongNhom = (m) => {
    const coCon = Array.isArray(m.con) && m.con.length > 0;
    const moSan = coCon && m.con.some((c) => c.ma === dang);

    if (!coCon) {
      const chon = m.ma === dang;
      const the = m.href && !chon ? 'a' : 'button';
      const thuoc = m.href && !chon ? `href="${m.href}"` : 'type="button"';

      return `<${the} class="qt__muc" ${thuoc} ${chon ? 'aria-current="page"' : ''}
         ${m.soon ? `data-soon="${m.soon}"` : ''}>
        ${bieu(m.icon, 'qt__icon')}<span>${m.ten}</span>
      </${the}>`;
    }

    return `
      <button type="button" class="qt__muc" aria-expanded="${moSan}" data-nhom="${m.ma}">
        ${bieu(m.icon, 'qt__icon')}<span>${m.ten}</span>${mui}
      </button>
      <div class="qt__con" data-con="${m.ma}" ${moSan ? '' : 'hidden'}>
        ${m.con
          .map((c) => {
            const chon = c.ma === dang;
            const the = c.href && !chon ? 'a' : 'button';
            const thuoc = c.href && !chon ? `href="${c.href}"` : 'type="button"';

            return `<${the} class="qt__conmuc" ${thuoc} ${chon ? 'aria-current="page"' : ''}
               ${c.soon ? `data-soon="${c.soon}"` : ''}>
              <span>${c.ten}</span>
              ${c.dem ? `<span class="qt__dem">${c.dem}</span>` : ''}
            </${the}>`;
          })
          .join('')}
      </div>`;
  };

  tren.className = 'qt__tren';
  tren.innerHTML = `
    <div class="qt__logo">
      <span class="logo logo--mark" style="height:22px" role="img" aria-label="ONoOffice"></span>
      <b>Quản trị</b>
    </div>

    <a class="qt__the" href="../khung/quan-tri.html" aria-current="page">
      <svg viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.6"
           stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
        <rect x="2.5" y="3.5" width="15" height="13" rx="2"/><path d="M8 3.5v13"/>
      </svg>
      Quản lý tổ chức
    </a>

    <button type="button" class="qt__the" data-soon="Cài đặt sản phẩm">
      <svg viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.6"
           stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
        <rect x="2.5" y="2.5" width="6" height="6" rx="1.6"/><rect x="11.5" y="2.5" width="6" height="6" rx="1.6"/>
        <rect x="2.5" y="11.5" width="6" height="6" rx="1.6"/><rect x="11.5" y="11.5" width="6" height="6" rx="1.6"/>
      </svg>
      Cài đặt sản phẩm
    </button>

    <div class="qt__tim">
      <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true">
        <circle cx="7" cy="7" r="4.5"/><path d="m10.5 10.5 3 3"/>
      </svg>
      <input type="search" placeholder="Tìm tính năng, thành viên, cài đặt"
             aria-label="Tìm trong màn quản trị">
    </div>

    <div class="qt__phai">
      <button class="nuti" id="qtCaidat" aria-haspopup="true" aria-expanded="false"
              aria-label="Giao diện và ngôn ngữ">
        <svg viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true">
          <circle cx="10" cy="10" r="2.6"/>
          <path d="M10 2.5v2M10 15.5v2M17.5 10h-2M4.5 10h-2M15.3 4.7l-1.4 1.4M6.1 13.9l-1.4 1.4M15.3 15.3l-1.4-1.4M6.1 6.1 4.7 4.7"/>
        </svg>
      </button>

      <button class="qt__toi" id="qtToi" aria-haspopup="true" aria-expanded="false">
        <span class="mat mat--nho">LL</span>
        <span class="qt__ten"><b>Lê Anh Lượng</b><span>Chủ sở hữu</span></span>
      </button>
    </div>

    <div class="popover" id="qtMenuCaidat" hidden style="right: 16px; top: 52px;">
      <div class="popover__hang">Bộ màu &amp; ngôn ngữ</div>
      <div style="padding: 2px 10px 8px;"><div id="qtPrefs"></div></div>
    </div>

    <div class="popover" id="qtMenuToi" hidden style="right: 16px; top: 52px;">
      ${MENU_TOI}
    </div>
  `;

  ben.className = 'qt__ben';
  ben.setAttribute('aria-label', 'Điều hướng quản trị');
  ben.innerHTML = `
    <div class="qt__ds">
      <a class="qt__ve" href="../comm/chat.html">
        <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6"
             stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
          <path d="M9.5 3.5 5 8l4.5 4.5"/>
        </svg>
        Về không gian làm việc
      </a>
      ${QT_MUC.map(dongNhom).join('')}
    </div>

    <div class="qt__day">
      <button type="button" class="qt__ve" id="qtGon" style="margin:0">
        <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6"
             stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
          <path d="M2.5 4h11M2.5 8h7M2.5 12h11"/>
        </svg>
        Ẩn điều hướng
      </button>
    </div>
  `;

  mountPrefs(tren.querySelector('#qtPrefs'));
  ganPopover(tren, [
    ['#qtCaidat', '#qtMenuCaidat'],
    ['#qtToi', '#qtMenuToi'],
  ]);

  for (const nut of ben.querySelectorAll('[data-nhom]')) {
    nut.addEventListener('click', () => {
      const con = ben.querySelector(`[data-con="${nut.dataset.nhom}"]`);
      const mo = con.hidden;

      con.hidden = !mo;
      nut.setAttribute('aria-expanded', String(mo));
    });
  }

  ben.querySelector('#qtGon').addEventListener('click', () =>
    document.querySelector('.qt').classList.toggle('qt--gon'),
  );
}

/**
 * Nút nào chưa làm thì nói thẳng, đừng im lặng.
 *
 * Im lặng không làm gì khiến người duyệt tưởng bản dựng hỏng, rồi báo lại một lỗi không
 * tồn tại. Gắn một lần cho cả trang.
 */
export function mountChuaLam() {
  document.body.addEventListener('click', (e) => {
    const el = e.target.closest('[data-soon]');
    if (!el) return;

    e.preventDefault();
    popup({ text: `${el.dataset.soon} — tính năng đang phát triển.` });
  });
}
