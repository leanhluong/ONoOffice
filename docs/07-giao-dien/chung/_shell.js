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
