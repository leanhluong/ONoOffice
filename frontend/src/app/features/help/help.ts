import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  computed,
  effect,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { HelpService } from '../../core/help/help.service';
import type { BaiHuongDan, NhomHuongDan, TomTatBai } from '../../core/models/help.model';

/** Một dòng của mục lục bên phải — dựng từ các tiêu đề `##` và `###` của bài. */
interface MucLuc {
  readonly neo: string;
  readonly chu: string;
  readonly muc: number;
}

/** Bài trước / bài sau, đã trải phẳng qua tất cả các nhóm. */
interface KeBen {
  readonly truoc: TomTatBai | null;
  readonly sau: TomTatBai | null;
}

/**
 * Màn HƯỚNG DẪN — trung tâm trợ giúp nằm ngay trong app.
 *
 * Nguồn thiết kế: `docs/07-giao-dien/huongdan/tai-lieu.html`. Nội dung đến từ
 * `docs/08-huong-dan/*.md` qua `tools/sync-huongdan.mjs`.
 *
 * <b>Không có `innerHTML` ở đâu trong màn này.</b> Bài viết là một CÂY KHỐI, và template
 * dựng lại bằng `@switch`. Đổi lấy một template dài hơn, ta được ba thứ: không mở cửa XSS,
 * không bị Angular âm thầm khử vệ sinh mất thẻ (đúng cái bẫy đã gặp với `<svg>` ở màn Vai
 * trò), và test đếm được nội dung thay vì so chuỗi.
 *
 * <b>Chạy được cả khi backend chết.</b> Nội dung là tệp tĩnh cùng máy chủ với app — xem
 * `HelpService`. Đó là chủ ý: người gặp lỗi lạ cần tra hướng dẫn đúng lúc hệ thống đang hỏng.
 */
@Component({
  selector: 'app-help',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgTemplateOutlet, RouterLink, TranslatePipe],
  templateUrl: './help.html',
  styleUrl: './help.scss',
})
export class Help {
  private readonly help = inject(HelpService);

  /**
   * Mã bài lấy từ đường dẫn (`withComponentInputBinding`). Rỗng = trang chủ hướng dẫn.
   *
   * Bài nằm trên URL chứ không giữ trong signal nội bộ: người ta gửi link hướng dẫn cho
   * nhau, và một trung tâm trợ giúp không chia sẻ được từng bài thì mất một nửa công dụng.
   */
  readonly ma = input('');

  protected readonly nhom = signal<readonly NhomHuongDan[]>([]);
  protected readonly bai = signal<BaiHuongDan | null>(null);
  protected readonly dangTai = signal(true);
  protected readonly hong = signal(false);
  protected readonly tim = signal('');

  /** Đã bấm "có ích / chưa" chưa — chỉ giữ trong phiên, chưa gửi đi đâu. */
  protected readonly daDanhGia = signal(false);

  constructor() {
    this.help.chiMuc().subscribe({
      next: (ds) => this.nhom.set(ds),
      error: () => this.hong.set(true),
    });

    // Nạp lại mỗi khi mã trên URL đổi — kể cả khi người dùng bấm một bài khác ở cột trái
    // mà component không bị dựng lại. `input()` là signal nên `effect` thấy được thay đổi
    // đó; đọc nó trong `ngOnInit` thì chỉ đúng ở lần đầu.
    effect(() => this.nap(this.ma()));
  }

  private nap(ma: string): void {
    this.daDanhGia.set(false);

    // `!ma`, KHÔNG phải `ma === ''`. Route `/huong-dan` không có tham số `:ma`, và
    // `withComponentInputBinding` để nguyên `undefined` thay vì đặt giá trị mặc định của
    // `input()`. So bằng chuỗi rỗng thì trang chủ đi hỏi `huong-dan/undefined.json`, nhận
    // 404, và hiện "không có bài này" — đúng chỗ lẽ ra phải là danh sách nhóm.
    if (!ma) {
      this.bai.set(null);
      this.hong.set(false);
      this.dangTai.set(false);

      return;
    }

    this.dangTai.set(true);
    this.hong.set(false);

    this.help.bai(ma).subscribe({
      next: (b) => {
        this.bai.set(b);
        this.dangTai.set(false);
      },

      // Mã lạ trên URL (gõ tay, hoặc link cũ sau khi đổi tên bài) thì nói thẳng là không
      // có bài đó, đừng để màn trắng. Cây bên trái vẫn còn nguyên để họ đi tiếp.
      error: () => {
        this.bai.set(null);
        this.dangTai.set(false);
        this.hong.set(true);
      },
    });
  }

  // ── Cột trái ────────────────────────────────────────────────────────

  protected onTim(event: Event): void {
    this.tim.set((event.target as HTMLInputElement).value);
  }

  private readonly oTim = viewChild<ElementRef<HTMLInputElement>>('oTim');

  /**
   * `/` nhảy vào ô tìm — phím tắt của mọi trang tài liệu.
   *
   * Phải bỏ qua khi con trỏ ĐANG ở trong một ô nhập, nếu không thì gõ một dấu gạch chéo
   * trong chính ô tìm sẽ bị nuốt. Cùng lý do bỏ qua khi người dùng đang giữ Ctrl/Alt/Cmd:
   * đó là phím tắt của trình duyệt, không phải của ta.
   */
  @HostListener('document:keydown', ['$event'])
  protected onPhim(event: KeyboardEvent): void {
    if (event.key !== '/' || event.ctrlKey || event.metaKey || event.altKey) {
      return;
    }

    const dang = document.activeElement;

    if (dang instanceof HTMLInputElement || dang instanceof HTMLTextAreaElement) {
      return;
    }

    event.preventDefault();
    this.oTim()?.nativeElement.focus();
  }

  /**
   * Lọc cây bên trái theo ô tìm — khớp cả tiêu đề lẫn câu tóm tắt.
   *
   * Tóm tắt nằm trong phép khớp vì người đang bí gõ VẤN ĐỀ chứ không gõ tên bài: họ gõ
   * "quên mật khẩu", còn bài thì tên là "Đặt lại mật khẩu hộ đồng nghiệp".
   *
   * Nhóm rỗng bị bỏ hẳn, không để lại cái nhãn trơ trọi.
   */
  protected readonly nhomLoc = computed<readonly NhomHuongDan[]>(() => {
    const t = this.tim().trim().toLowerCase();

    if (t === '') {
      return this.nhom();
    }

    return this.nhom()
      .map((n) => ({
        ...n,
        bai: n.bai.filter((b) => `${b.tieude} ${b.tomtat}`.toLowerCase().includes(t)),
      }))
      .filter((n) => n.bai.length > 0);
  });

  protected readonly soBai = computed(() =>
    this.nhom().reduce((s, n) => s + n.bai.length, 0),
  );

  protected readonly khongThay = computed(
    () => this.tim().trim() !== '' && this.nhomLoc().length === 0,
  );

  // ── Trong một bài ───────────────────────────────────────────────────

  protected readonly tenNhom = computed(
    () => this.nhom().find((n) => n.ma === this.bai()?.nhom)?.ten ?? '',
  );

  /**
   * Mục lục bên phải, dựng từ tiêu đề của bài.
   *
   * Neo sinh từ **thứ tự** (`muc-0`, `muc-1`) chứ không từ chữ. Sinh từ chữ thì hai mục
   * trùng tên trong một bài sẽ có cùng neo, và bấm mục thứ hai nhảy về mục thứ nhất — một
   * lỗi chỉ lộ ra ở đúng bài xui xẻo đó.
   */
  protected readonly mucLuc = computed<readonly MucLuc[]>(() => {
    const khoi = this.bai()?.khoi ?? [];

    return khoi
      .map((k, i) => ({ k, i }))
      .filter((x) => x.k.k === 'de')
      .map((x) => ({
        neo: this.neo(x.i),
        chu: (x.k as { chu: string }).chu,
        muc: (x.k as { muc: number }).muc,
      }));
  });

  protected neo(i: number): string {
    return `muc-${i}`;
  }

  /** Bài trước và bài sau, đi xuyên qua ranh giới nhóm — đọc hết nhóm này thì sang nhóm kia. */
  protected readonly keBen = computed<KeBen>(() => {
    const phang = this.nhom().flatMap((n) => n.bai);
    const i = phang.findIndex((b) => b.ma === this.ma());

    if (i === -1) {
      return { truoc: null, sau: null };
    }

    return { truoc: phang[i - 1] ?? null, sau: phang[i + 1] ?? null };
  });

  protected danhGia(): void {
    // Chưa gửi đi đâu cả — chưa có chỗ nhận. Nhưng vẫn ghi nhận trên màn để người bấm biết
    // là mình đã bấm; một cái nút không phản hồi gì thì người ta bấm lại vài lần.
    this.daDanhGia.set(true);
  }
}
