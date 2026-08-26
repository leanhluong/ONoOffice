import { readFileSync, readdirSync } from 'node:fs';
import { join } from 'node:path';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed, type ComponentFixture } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it } from 'vitest';
import { HelpService } from '../../core/help/help.service';
import type { BaiHuongDan, NhomHuongDan } from '../../core/models/help.model';
import { Help } from './help';

/**
 * Màn Hướng dẫn — và cả NỘI DUNG mà nó dựng ra.
 *
 * Tệp này canh hai thứ rất khác nhau, cố ý gộp một chỗ:
 *
 * 1. <b>Hành vi màn hình</b> — cây bên trái, ô tìm, bài trước/sau, ca bài không tồn tại.
 * 2. <b>Nội dung đã sinh ra</b> — đọc thẳng `public/huong-dan/*.json` như một người dùng
 *    thật sẽ tải về. `sync-huongdan.mjs` đã kiểm lúc SINH, nhưng không có gì bảo đảm thứ
 *    nằm trong `public/` là thứ vừa sinh: quên chạy lại bộ sinh sau khi sửa Markdown là
 *    chuyện xảy ra hằng ngày, và hậu quả là app phục vụ một bản cũ mà không ai biết.
 */

const CONG_KHAI = join(process.cwd(), 'public', 'huong-dan');

function docJson<T>(ten: string): T {
  return JSON.parse(readFileSync(join(CONG_KHAI, ten), 'utf8')) as T;
}

// ══════════════════════════════════════════════════════════════════════
// Phần 1 — nội dung đã sinh ra
// ══════════════════════════════════════════════════════════════════════

describe('nội dung hướng dẫn đã sinh', () => {
  const chiMuc = docJson<NhomHuongDan[]>('chi-muc.json');

  it('có bài, và mỗi nhóm đều có bài', () => {
    // Bẫy tự thân: bộ sinh hỏng trả về mảng rỗng thì mọi phép kiểm dưới đây thành vòng lặp
    // rỗng và xanh vĩnh viễn.
    expect(chiMuc.length).toBeGreaterThan(0);

    for (const n of chiMuc) {
      expect(n.bai.length, `nhóm "${n.ten}" không có bài nào`).toBeGreaterThan(0);
    }
  });

  it('mọi bài trong chỉ mục đều có file nội dung', () => {
    const co = new Set(readdirSync(CONG_KHAI).filter((f) => f.endsWith('.json')));

    const thieu = chiMuc
      .flatMap((n) => n.bai)
      .filter((b) => !co.has(`${b.ma}.json`))
      .map((b) => b.ma);

    expect(thieu).toEqual([]);
  });

  /**
   * Mọi ảnh bài viết trỏ tới phải TỒN TẠI trong `public/`.
   *
   * `sync-huongdan.mjs` kiểm ảnh ở `docs/08-huong-dan/anh/`, nhưng app phục vụ từ
   * `public/huong-dan/anh/` — hai thư mục khác nhau, và bộ chép nằm ở `chup-huong-dan.mjs`.
   * Chạy bộ sinh mà quên chạy bộ chụp thì bài vẫn dựng ra, chỉ có một ô ảnh vỡ ở giữa.
   */
  it('mọi ảnh được nhắc tới đều có mặt ở public/', () => {
    const anhCo = new Set(readdirSync(join(CONG_KHAI, 'anh')));
    const thieu: string[] = [];

    for (const n of chiMuc) {
      for (const b of n.bai) {
        for (const k of docJson<BaiHuongDan>(`${b.ma}.json`).khoi) {
          if (k.k === 'anh' && !anhCo.has(k.tep.replace('anh/', ''))) {
            thieu.push(`${b.ma}: ${k.tep}`);
          }
        }
      }
    }

    expect(thieu).toEqual([]);
  });

  it('mọi ảnh đều có mô tả cho trình đọc màn hình', () => {
    const thieu: string[] = [];

    for (const n of chiMuc) {
      for (const b of n.bai) {
        for (const k of docJson<BaiHuongDan>(`${b.ma}.json`).khoi) {
          if (k.k === 'anh' && k.mota.trim() === '') {
            thieu.push(`${b.ma}: ${k.tep}`);
          }
        }
      }
    }

    expect(thieu).toEqual([]);
  });
});

// ══════════════════════════════════════════════════════════════════════
// Phần 2 — hành vi màn hình
// ══════════════════════════════════════════════════════════════════════

const NHOM: NhomHuongDan[] = [
  {
    ma: 'bat-dau',
    ten: 'Bắt đầu',
    mota: 'Dựng workspace.',
    bai: [
      { ma: 'a', tieude: 'Bài A', tomtat: 'Nói về đăng ký.' },
      { ma: 'b', tieude: 'Bài B', tomtat: 'Nói về quên mật khẩu.' },
    ],
  },
  {
    ma: 'hang-ngay',
    ten: 'Hằng ngày',
    mota: 'Việc thường ngày.',
    bai: [{ ma: 'c', tieude: 'Bài C', tomtat: 'Nói về danh bạ.' }],
  },
];

const BAI_B: BaiHuongDan = {
  ma: 'b',
  tieude: 'Bài B',
  nhom: 'bat-dau',
  tomtat: 'Nói về quên mật khẩu.',
  khoi: [
    { k: 'de', muc: 2, chu: 'Mục một' },
    {
      k: 'doan',
      chu: [
        { k: 'chu', v: 'Bảng có ' },
        { k: 'dam', v: 'ba loại dòng' },
        { k: 'chu', v: ', và cả ba đều bình thường.' },
      ],
    },
    { k: 'de', muc: 3, chu: 'Mục con' },
  ],
};

class FakeHelpService {
  chiMucResult: Observable<NhomHuongDan[]> = of(NHOM);
  baiResult: Observable<BaiHuongDan> = of(BAI_B);
  daHoi: string[] = [];

  chiMuc(): Observable<NhomHuongDan[]> {
    return this.chiMucResult;
  }

  bai(ma: string): Observable<BaiHuongDan> {
    this.daHoi.push(ma);

    return this.baiResult;
  }
}

describe('Help', () => {
  let fixture: ComponentFixture<Help>;
  let service: FakeHelpService;

  function make(ma = ''): Help {
    fixture = TestBed.createComponent(Help);
    fixture.componentRef.setInput('ma', ma);
    fixture.detectChanges();

    return fixture.componentInstance;
  }

  beforeEach(() => {
    service = new FakeHelpService();

    TestBed.configureTestingModule({
      imports: [Help],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideTranslateService(),
        { provide: HelpService, useValue: service },
      ],
    });
  });

  /**
   * Trang chủ KHÔNG đi hỏi một bài nào.
   *
   * Đã hỏng đúng kiểu này một lần: phép kiểm là `ma === ''`, nhưng route `/huong-dan`
   * không có tham số nên `withComponentInputBinding` để nguyên `undefined`. Kết quả là
   * trang chủ gọi `huong-dan/undefined.json`, nhận 404, và hiện "không có bài này" ngay
   * chỗ lẽ ra là danh sách nhóm.
   */
  it('trang chủ không đi hỏi bài nào', () => {
    make();

    expect(service.daHoi).toEqual([]);
    expect(fixture.nativeElement.querySelectorAll('.tdthe')).toHaveLength(2);
  });

  it('mở một bài thì dựng đủ các khối', () => {
    const c = make('b');

    expect(service.daHoi).toEqual(['b']);
    expect(fixture.nativeElement.querySelector('.td__ten').textContent).toContain('Bài B');
    expect(c['mucLuc']()).toHaveLength(2);
  });

  /**
   * KHÔNG có khoảng trắng thừa quanh chữ đậm.
   *
   * Template dựng từng mẩu chữ bằng một thẻ riêng, nên mỗi lần xuống dòng trong `@case` là
   * một text node trắng lọt vào giữa câu — và câu hiện ra thành "ba loại dòng , và cả ba".
   * Đã xảy ra thật; sửa bằng cách viết `@case` trên một dòng.
   *
   * Đặt bộ canh ở đây vì lỗi này QUAY LẠI được: chỉ cần ai đó chạy `npm run format` là
   * prettier xuống dòng lại, và không có gì khác báo.
   */
  it('không chèn khoảng trắng thừa quanh chữ đậm', () => {
    make('b');

    const doan = fixture.nativeElement.querySelector('.td__than p').textContent as string;

    expect(doan).toContain('ba loại dòng, và cả ba');
    expect(doan).not.toContain('dòng ,');
  });

  it('bài không tồn tại thì nói thẳng, không để màn trắng', () => {
    service.baiResult = throwError(() => new Error('404'));

    make('khong-co');

    expect(fixture.nativeElement.querySelector('.rong')).not.toBeNull();

    // Cây bên trái vẫn còn nguyên để người đọc đi tiếp — đó mới là chỗ họ cần.
    expect(fixture.nativeElement.querySelectorAll('.nav__muc').length).toBeGreaterThan(0);
  });

  /**
   * Ô tìm khớp cả TÓM TẮT, không chỉ tiêu đề.
   *
   * Người đang bí gõ VẤN ĐỀ chứ không gõ tên bài: họ gõ "quên mật khẩu", còn bài thì tên
   * là "Đặt lại mật khẩu hộ đồng nghiệp".
   */
  it('tìm khớp cả câu tóm tắt', () => {
    const c = make('b');

    c['onTim']({ target: { value: 'quên mật khẩu' } } as unknown as Event);

    expect(c['nhomLoc']().flatMap((n) => n.bai).map((b) => b.ma)).toEqual(['b']);
  });

  it('tìm không ra thì bỏ luôn nhãn nhóm, không để nhãn trơ trọi', () => {
    const c = make('b');

    c['onTim']({ target: { value: 'khong-co-gi-khop' } } as unknown as Event);

    expect(c['nhomLoc']()).toEqual([]);
    expect(c['khongThay']()).toBe(true);
  });

  /**
   * Bài trước / bài sau đi XUYÊN qua ranh giới nhóm.
   *
   * Đọc hết nhóm này thì sang nhóm kia; dừng ở cuối nhóm là bảo người đọc rằng hết rồi,
   * trong khi còn hai nhóm nữa ở cột bên trái.
   */
  it('bài sau đi xuyên sang nhóm kế tiếp', () => {
    const c = make('b');

    expect(c['keBen']().truoc?.ma).toBe('a');
    expect(c['keBen']().sau?.ma).toBe('c');
  });

  it('bài cuối cùng thì không có bài sau', () => {
    const c = make('c');

    expect(c['keBen']().sau).toBeNull();
  });
});
