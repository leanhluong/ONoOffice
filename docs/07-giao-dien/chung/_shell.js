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

/**
 * Dựng cột điều hướng của khung ứng dụng.
 *
 * Vì sao là hàm chứ không phải chép HTML vào từng trang: bản dựng có nhiều màn, và cột này
 * giống hệt nhau ở mọi màn. Chép tay thì thêm một mục ở màn A mà quên màn B — người duyệt
 * thấy hai cột khác nhau và không biết cái nào mới đúng.
 *
 * @param host  phần tử sẽ chứa cột
 * @param dang  mã trang đang mở, để tô mục tương ứng
 */
export function mountNav(host, dang) {
  const MUC = [
    {
      nhan: 'Làm việc',
      muc: [
        { ma: 'trao-doi', ten: 'Trao đổi', href: '../comm/chat.html', dem: 4,
          icon: '<path d="M17 11.3a3 3 0 01-3 3H8.4L4.5 17.2v-2.9H6a3 3 0 01-3-3V5.7a3 3 0 013-3h8a3 3 0 013 3z"/>' },
        { ma: 'duyet', ten: 'Chờ duyệt', dem: 9, soon: 'Chờ duyệt',
          icon: '<path d="M4 3.5h12v13l-3-2-3 2-3-2-3 2z"/><path d="M7 7.5h6M7 10.5h4"/>' },
      ],
    },
    {
      nhan: 'Tổ chức',
      muc: [
        { ma: 'nhan-su', ten: 'Nhân sự', href: '../org/nhan-su.html', dem: 38, nhat: true,
          icon: '<circle cx="7.5" cy="6.8" r="2.9"/><path d="M2.4 16.6a5.1 5.1 0 0110.2 0"/><path d="M13.2 4.4a2.9 2.9 0 010 4.8M14.8 16.6a5.1 5.1 0 00-1.7-3.8"/>' },
        { ma: 'phong-ban', ten: 'Phòng ban', dem: 6, nhat: true, soon: 'Phòng ban',
          icon: '<rect x="7" y="2.5" width="6" height="4.5" rx="1.3"/><rect x="2" y="13" width="5.5" height="4.5" rx="1.3"/><rect x="12.5" y="13" width="5.5" height="4.5" rx="1.3"/><path d="M10 7v3.5M4.75 13v-2.5h10.5V13"/>' },
        { ma: 'vai-tro', ten: 'Vai trò & quyền', href: '../identity/vai-tro.html',
          icon: '<path d="M10 2.5 4 5v4.5c0 3.4 2.5 6.5 6 8 3.5-1.5 6-4.6 6-8V5z"/><path d="m7.6 10 1.7 1.7 3.3-3.4"/>' },
      ],
    },
  ];

  const bieu = (d) =>
    `<svg class="nav__icon" viewBox="0 0 20 20" fill="none" stroke="currentColor"
       stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${d}</svg>`;

  const dong = (m) => {
    const chon = m.ma === dang;
    const the = m.href && !chon ? 'a' : 'button';
    const thuoc = m.href && !chon ? `href="${m.href}"` : 'type="button"';

    return `<${the} class="nav__muc" ${thuoc} ${chon ? 'aria-current="page"' : ''}
       ${m.soon ? `data-soon="${m.soon}"` : ''}>
      ${bieu(m.icon)}
      <span class="nav__chu">${m.ten}</span>
      ${m.dem ? `<span class="nav__dem ${m.nhat ? 'nav__dem--nhat' : ''}">${m.dem}</span>` : ''}
      <span class="nav__meo">${m.ten}</span>
    </${the}>`;
  };

  host.className = 'nav';
  host.setAttribute('aria-label', 'Điều hướng chính');
  host.innerHTML = `
    <button class="nav__toi" id="navToi" aria-expanded="false">
      <span class="mat mat--nho">LL<span class="online"></span></span>
      <span class="nav__ten"><b>Lê Anh Lượng</b><span>Công ty TNHH ACME</span></span>
    </button>

    <div class="nav__tim">
      <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true">
        <circle cx="7" cy="7" r="4.5"/><path d="m10.5 10.5 3 3"/>
      </svg>
      <input type="search" placeholder="Tìm kiếm" aria-label="Tìm kiếm toàn hệ thống">
      <span class="nav__phim">Ctrl K</span>
    </div>

    <div class="nav__ds">
      ${MUC.map((n) => `<div class="nav__nhan">${n.nhan}</div>${n.muc.map(dong).join('')}`).join('')}
    </div>

    <div class="nav__duoi">
      <button class="nuti" id="navCaidat" aria-expanded="false" aria-label="Giao diện và ngôn ngữ">
        <svg viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true">
          <circle cx="10" cy="10" r="2.6"/>
          <path d="M10 2.5v2M10 15.5v2M17.5 10h-2M4.5 10h-2M15.3 4.7l-1.4 1.4M6.1 13.9l-1.4 1.4M15.3 15.3l-1.4-1.4M6.1 6.1 4.7 4.7"/>
        </svg>
      </button>
      <button class="nuti" id="navGon" aria-label="Thu gọn thanh điều hướng">
        <svg viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true">
          <rect x="2.5" y="3.5" width="15" height="13" rx="2.5"/><path d="M7.5 3.5v13"/>
        </svg>
      </button>
    </div>

    <div class="popover" id="navMenuCaidat" hidden style="left: 8px; bottom: 52px;">
      <div class="popover__hang">Bộ màu &amp; ngôn ngữ</div>
      <div style="padding: 2px 10px 8px;"><div id="prefs"></div></div>
    </div>

    <div class="popover" id="navMenuToi" hidden style="left: 8px; top: 56px;">
      <div class="popover__ai">
        <div class="popover__ten">Lê Anh Lượng</div>
        <div class="popover__mail">chu@congty.vn · Owner</div>
      </div>
      <a class="popover__nut" href="../identity/tai-khoan.html">
        <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true"><circle cx="8" cy="5.5" r="2.4"/><path d="M3.5 13.2a4.5 4.5 0 019 0"/></svg>
        Hồ sơ &amp; cài đặt
      </a>
      <button class="popover__nut" data-soon="Đổi trạng thái">
        <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true"><circle cx="8" cy="8" r="5.5"/></svg>
        Đang trực tuyến
      </button>
      <div class="popover__vach"></div>
      <button class="popover__nut" data-soon="Đăng xuất">
        <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true"><path d="M6 14H3.5A1.5 1.5 0 012 12.5v-9A1.5 1.5 0 013.5 2H6M10.5 11 14 8l-3.5-3M14 8H6"/></svg>
        Đăng xuất
      </button>
    </div>
  `;

  mountPrefs(host.querySelector('#prefs'));

  const khung = host.closest('.khung');

  host.querySelector('#navGon').addEventListener('click', () => khung.classList.toggle('khung--gon'));

  const gan = (nutId, menuId) => {
    const nut = host.querySelector(nutId);
    const menu = host.querySelector(menuId);

    nut.addEventListener('click', (e) => {
      e.stopPropagation();
      const mo = menu.hidden;

      // Đóng mọi menu khác trước: hai cái cùng mở thì chồng lên nhau.
      host.querySelectorAll('.popover').forEach((m) => (m.hidden = true));
      host.querySelectorAll('[aria-expanded]').forEach((b) => b.setAttribute('aria-expanded', 'false'));

      menu.hidden = !mo;
      nut.setAttribute('aria-expanded', String(mo));
    });

    menu.addEventListener('click', (e) => e.stopPropagation());
  };

  gan('#navToi', '#navMenuToi');
  gan('#navCaidat', '#navMenuCaidat');

  // Bấm ra ngoài thì đóng. Thiếu chỗ này là kiểu lỗi ai cũng gặp: menu xổ ra rồi nằm lì
  // trên màn hình cho tới khi đổi trang.
  const dong2 = () => {
    host.querySelectorAll('.popover').forEach((m) => (m.hidden = true));
    host.querySelectorAll('[aria-expanded]').forEach((b) => b.setAttribute('aria-expanded', 'false'));
  };

  document.addEventListener('click', dong2);
  document.addEventListener('keydown', (e) => e.key === 'Escape' && dong2());
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
