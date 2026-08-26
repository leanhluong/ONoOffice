import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ChatService } from '../../core/chat/chat.service';
import { ErrorMessageService } from '../../core/i18n/error-message.service';
import { isAppError } from '../../core/models/api-error.model';
import { AuthStore } from '../../core/auth/auth.store';
import { OrgService } from '../../core/org/org.service';
import { ConversationKind } from '../../core/models/chat.model';
import type {
  ConversationSummary,
  MessageItem,
  MessageOnScreen,
} from '../../core/models/chat.model';
import type { MemberListItem } from '../../core/models/org.model';

/** Một khối tin của cùng một ngày — vạch ngày dính lại khi cuộn cần một khối riêng. */
interface NgayKhoi {
  readonly nhan: string;
  readonly tin: readonly MessageOnScreen[];
}

/** Tin nhắn đã tính sẵn hai câu hỏi mà template hỏi ở mọi dòng. */
interface TinTrenMan {
  readonly tin: MessageOnScreen;
  readonly cuaToi: boolean;
  /** Nối vào tin ngay trên: cùng người, cách nhau dưới 5 phút → bỏ ảnh và bỏ tên. */
  readonly noi: boolean;
}

interface NgayKhoiDaTinh {
  readonly nhan: string;
  readonly tin: readonly TinTrenMan[];
}

/** Số phút mà hai tin liền nhau của cùng một người còn được coi là một lượt nói. */
const PHUT_GOP = 5;

/**
 * Màn <b>Trao đổi</b> — lát 1.
 *
 * Nguồn thiết kế: `docs/07-giao-dien/comm/chat-lat-1.html`. Bản dựng ĐÍCH là
 * `chat.html`, nhiều hơn hẳn — xem chú thích đầu file bản dựng lát 1 về vì sao có hai.
 *
 * ═══════════════════════════════════════════════════════════════════════
 *  CHỖ LAI THEN CHỐT: HAI KIỂU LUỒNG KHÁC NHAU
 * ═══════════════════════════════════════════════════════════════════════
 *
 * <b>Nhóm</b> → mọi tin xếp MỘT CỘT bên trái, kiểu Slack. Nhóm 12 người mà đảo tin của
 * mình sang phải thì cột đọc gãy làm đôi: mắt phải nhảy trái–phải liên tục và không quét
 * được ai nói câu nào.
 *
 * <b>Tin nhắn riêng</b> → trái–phải, kiểu Zalo. Chỉ có hai người nên trái–phải là cách
 * đọc nhanh nhất, và đó cũng là thứ người Việt đã quen tay.
 *
 * Cả hai chỉ khác nhau bằng MỘT lớp trên khối luồng (`luong--kenh` / `luong--rieng`) —
 * mọi luật còn lại nằm ở CSS sinh từ bản dựng.
 *
 * ═══════════════════════════════════════════════════════════════════════
 *  CHƯA CÓ REALTIME, VÀ MÀN HÌNH KHÔNG ĐƯỢC GIẢ VỜ CÓ
 * ═══════════════════════════════════════════════════════════════════════
 *
 * Lát 1 không có SignalR. Tin của người khác chỉ hiện khi danh sách được nạp lại — tức
 * là khi đổi hội thoại, hoặc khi chính mình gửi một câu.
 *
 * Nên màn này KHÔNG vẽ "ai đó đang gõ", KHÔNG vẽ "đã xem", và KHÔNG có dấu hiệu nào ngụ ý
 * rằng nó đang lắng nghe. Vẽ chúng ra bằng dữ liệu tĩnh thì người dùng ngồi chờ một câu
 * trả lời đang nằm trên máy chủ mà màn hình thì trông hoàn toàn bình thường — đúng kiểu
 * hỏng tệ nhất của loại giao diện này.
 */
@Component({
  selector: 'app-chat',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgTemplateOutlet, ReactiveFormsModule, TranslatePipe],
  templateUrl: './chat.html',
  styleUrl: './chat.scss',
})
export class Chat {
  private readonly chat = inject(ChatService);
  private readonly org = inject(OrgService);
  private readonly store = inject(AuthStore);
  private readonly i18n = inject(TranslateService);
  private readonly errorMessages = inject(ErrorMessageService);

  protected readonly ConversationKind = ConversationKind;

  protected readonly conversations = signal<ConversationSummary[]>([]);
  protected readonly dangNap = signal(true);

  protected readonly selectedId = signal<string | null>(null);
  protected readonly messages = signal<MessageOnScreen[]>([]);
  protected readonly conNua = signal(false);
  protected readonly dangNapTin = signal(false);

  protected readonly search = new FormControl('', { nonNullable: true });
  protected readonly draft = new FormControl('', { nonNullable: true });

  /*
    ═══════════════════════════════════════════════════════════════════
     VÌ SAO PHẢI SOI BA Ô NÀY QUA `toSignal`
    ═══════════════════════════════════════════════════════════════════

    `FormControl.value` là một thuộc tính thường, KHÔNG phải signal. Một `computed` đọc nó
    sẽ tính đúng đúng MỘT lần rồi đứng yên vĩnh viễn — người dùng gõ vào ô tìm kiếm và
    danh sách không nhúc nhích, gõ vào ô soạn và bộ đếm ký tự không đổi.

    Và nó hỏng theo kiểu tệ nhất: không lỗi, không cảnh báo, màn hình vẫn dựng đủ. Bản đầu
    của màn này đúng như vậy, và chỉ một test soi thẳng `riengList()` sau khi gõ mới lộ ra.
  */
  private readonly tuTim = toSignal(this.search.valueChanges, { initialValue: '' });
  private readonly chuDangGo = toSignal(this.draft.valueChanges, { initialValue: '' });

  /** Hai nhóm ở cột trái mở hay gập. Không lưu xuống đâu cả — nó là trạng thái của phiên nhìn. */
  protected readonly moNhom = signal(true);
  protected readonly moRieng = signal(true);

  // ── Hộp tạo nhóm ──────────────────────────────────────────────────
  protected readonly hopNhom = signal(false);
  protected readonly tenNhom = new FormControl('', { nonNullable: true });
  protected readonly locNguoi = new FormControl('', { nonNullable: true });
  private readonly tuLocNguoi = toSignal(this.locNguoi.valueChanges, { initialValue: '' });
  protected readonly moiAi = signal<readonly string[]>([]);
  protected readonly nguoiTrongCongTy = signal<MemberListItem[]>([]);
  /**
   * CÂU CHỮ đã dịch, không phải mã lỗi.
   *
   * Bản đầu giữ mã rồi để template dịch bằng `'errors.' + ma | translate` — sai hai lần:
   * mã lỗi backend là khoá dịch ở CẤP GỐC (`Conversation.NameEmpty`), không nằm dưới
   * nhánh `errors.`; và ghép khoá trong template thì không bộ canh nào đọc ra được.
   * `translation-usage.spec` bắt được đúng chỗ đó.
   */
  protected readonly loiNhom = signal<string | null>(null);
  protected readonly dangTao = signal(false);

  private readonly vungCuon = viewChild<ElementRef<HTMLElement>>('cuon');

  constructor() {
    this.napDanhSach();

    /*
      Cuộn xuống đáy MỖI KHI danh sách tin đổi.

      Một `effect` chứ không phải một lệnh gọi sau mỗi chỗ sửa `messages`: có bốn chỗ sửa
      (mở hội thoại, gửi tin, gửi hỏng, nạp thêm tin cũ) và ba trong bốn chỗ đó cần cuộn.
      Rải bốn lệnh gọi thì chỗ thứ năm sẽ quên — và triệu chứng là "thỉnh thoảng tin mới
      không hiện", thứ rất khó dựng lại.

      Nạp tin CŨ thì không cuộn: người dùng đang đọc ngược lên, kéo họ xuống đáy là xoá
      đúng thứ họ vừa xin.
    */
    effect(() => {
      const soTin = this.messages().length;

      if (soTin > 0 && !this.dangNapTin()) {
        queueMicrotask(() => this.xuongDay());
      }
    });
  }

  // ══════════════════════════════════════════════════════════════════
  // Cột trái
  // ══════════════════════════════════════════════════════════════════

  /**
   * Lọc ở CLIENT, không hỏi lại máy chủ.
   *
   * Danh sách hội thoại của một người là hàng chục, đã nằm sẵn trong bộ nhớ. Gõ một chữ
   * mà đi một vòng mạng thì ô tìm kiếm giật từng nhịp, và nó vẫn giật y như thế khi mạng
   * kém — đúng lúc người ta cần tìm nhanh nhất.
   */
  private readonly locDuoc = computed(() => {
    const tu = this.tuTim().trim().toLowerCase();
    const tatCa = this.conversations();

    return tu ? tatCa.filter((c) => c.displayName.toLowerCase().includes(tu)) : tatCa;
  });

  protected readonly nhomList = computed(() =>
    this.locDuoc().filter((c) => c.kind === ConversationKind.Nhom),
  );

  protected readonly riengList = computed(() =>
    this.locDuoc().filter((c) => c.kind === ConversationKind.Rieng),
  );

  protected readonly trong = computed(() => this.conversations().length === 0);

  protected readonly dangMo = computed(() => {
    const id = this.selectedId();

    return id === null ? null : (this.conversations().find((c) => c.id === id) ?? null);
  });

  protected chon(c: ConversationSummary): void {
    if (this.selectedId() === c.id) {
      return;
    }

    this.selectedId.set(c.id);
    this.messages.set([]);
    this.draft.setValue('');
    this.napTin(c.id);

    /*
      Xoá huy hiệu đỏ NGAY, không chờ máy chủ trả lời.

      Người dùng vừa mở hội thoại ra — với họ nó đã đọc rồi. Chờ một vòng mạng để con số
      biến mất là để lại một dấu hiệu nói sai về thứ đang hiện ngay trước mắt họ.

      Máy chủ hỏng thì con số sai theo hướng "báo ít hơn thực tế" trong đúng một phiên, và
      lần nạp danh sách sau sẽ dựng lại đúng. Sai theo hướng đó rẻ hơn nhiều so với một
      huy hiệu đỏ đứng lì trên một hội thoại đang mở.
    */
    this.conversations.update((ds) =>
      ds.map((x) => (x.id === c.id ? { ...x, unreadCount: 0 } : x)),
    );

    this.chat.markRead(c.id).subscribe({ error: () => undefined });
  }

  protected doiNhom(nhom: boolean): void {
    (nhom ? this.moNhom : this.moRieng).update((v) => !v);
  }

  // ══════════════════════════════════════════════════════════════════
  // Luồng tin
  // ══════════════════════════════════════════════════════════════════

  /**
   * Chia tin theo ngày, rồi tính sẵn "của tôi" và "nối vào tin trên".
   *
   * Tính ở đây chứ không trong template: template gọi hàm thì Angular gọi lại ở MỌI lần
   * dò thay đổi, cho mọi dòng — một luồng 100 tin thành hàng trăm lượt tính cho mỗi lần
   * gõ một chữ vào ô soạn. `computed` chỉ tính lại khi danh sách tin thật sự đổi.
   */
  protected readonly khoiNgay = computed<readonly NgayKhoiDaTinh[]>(() => {
    const toi = this.store.user()?.userId;
    const khoi: NgayKhoi[] = [];

    for (const tin of this.messages()) {
      const nhan = this.nhanNgay(tin.sentAtUtc);
      const cuoi = khoi.at(-1);

      if (cuoi?.nhan === nhan) {
        (cuoi.tin as MessageOnScreen[]).push(tin);
      } else {
        khoi.push({ nhan, tin: [tin] });
      }
    }

    return khoi.map((k) => ({
      nhan: k.nhan,
      tin: k.tin.map((tin, i) => ({
        tin,
        cuaToi: tin.senderUserId === toi,
        noi: this.noiVaoTinTren(k.tin[i - 1], tin),
      })),
    }));
  });

  /**
   * Hai tin liền nhau của cùng một người, cách nhau dưới {@link PHUT_GOP} phút, thì gộp.
   *
   * Không gộp thì một người gửi năm câu liên tiếp sẽ thấy tên họ năm lần — luồng biến
   * thành danh bạ. Nhưng gộp bất kể thời gian cũng sai: hai câu cách nhau ba tiếng là hai
   * lượt nói khác nhau, và bỏ mất cái giờ ở giữa là bỏ mất thứ giải thích cả đoạn.
   */
  private noiVaoTinTren(tren: MessageOnScreen | undefined, nay: MessageOnScreen): boolean {
    if (!tren || tren.senderUserId !== nay.senderUserId) {
      return false;
    }

    const cach = Date.parse(nay.sentAtUtc) - Date.parse(tren.sentAtUtc);

    return Number.isFinite(cach) && cach < PHUT_GOP * 60_000;
  }

  /**
   * Mã ngôn ngữ cho `toLocaleTimeString`.
   *
   * `currentLang` của ngx-translate là một signal, không phải chuỗi — truyền thẳng vào
   * `Intl` thì TypeScript bắt được, nhưng nếu nó là `any` ở đâu đó thì kết quả là mọi
   * giờ trên màn hình lặng lẽ rơi về múi giờ mặc định của trình duyệt.
   */
  private tieng(): string {
    return this.i18n.currentLang() || 'vi';
  }

  protected gio(iso: string): string {
    const luc = new Date(iso);

    return Number.isNaN(luc.getTime())
      ? ''
      : luc.toLocaleTimeString(this.tieng(), {
          hour: '2-digit',
          minute: '2-digit',
        });
  }

  /**
   * Giờ ở cột trái nói NGÀY khi tin không phải hôm nay.
   *
   * "14:32" cho một tin từ tuần trước là một câu nói dối nhỏ mà mắt tin ngay — nhìn danh
   * sách thấy toàn giờ thì tưởng mọi hội thoại đều mới.
   */
  protected gioNgan(iso: string | null): string {
    if (!iso) {
      return '';
    }

    const luc = new Date(iso);

    if (Number.isNaN(luc.getTime())) {
      return '';
    }

    return this.cungNgay(luc, new Date())
      ? this.gio(iso)
      : luc.toLocaleDateString(this.tieng(), { day: '2-digit', month: '2-digit' });
  }

  private nhanNgay(iso: string): string {
    const luc = new Date(iso);

    if (Number.isNaN(luc.getTime())) {
      return '';
    }

    const homNay = new Date();
    const homQua = new Date(homNay);
    homQua.setDate(homQua.getDate() - 1);

    if (this.cungNgay(luc, homNay)) {
      return this.i18n.instant('chat.today');
    }

    if (this.cungNgay(luc, homQua)) {
      return this.i18n.instant('chat.yesterday');
    }

    return luc.toLocaleDateString(this.tieng(), {
      day: 'numeric',
      month: 'long',
      year: 'numeric',
    });
  }

  private cungNgay(a: Date, b: Date): boolean {
    return (
      a.getFullYear() === b.getFullYear() &&
      a.getMonth() === b.getMonth() &&
      a.getDate() === b.getDate()
    );
  }

  protected chuCai(ten: string): string {
    const tu = ten.trim().split(/\s+/).filter(Boolean);

    return tu.length === 0 ? '?' : (tu.at(-1)![0] + (tu.length > 1 ? tu[0][0] : '')).toUpperCase();
  }

  // ══════════════════════════════════════════════════════════════════
  // Gửi tin
  // ══════════════════════════════════════════════════════════════════

  protected readonly conLai = computed(() => 4000 - this.chuDangGo().length);

  protected guiDuoc(): boolean {
    return this.selectedId() !== null && this.draft.value.trim().length > 0 && this.conLai() >= 0;
  }

  /**
   * Enter gửi, Shift+Enter xuống dòng.
   *
   * Ngược lại (Enter xuống dòng, Ctrl+Enter gửi) thì đúng hơn với một ô soạn thảo, nhưng
   * sai với thứ này: chat là nơi người ta gõ một câu rồi gửi, hàng trăm lần một ngày.
   * Zalo, Slack, Lark đều chọn Enter — và tay người dùng đã học nó từ lâu trước khi mở
   * ứng dụng này.
   */
  protected phim(e: KeyboardEvent): void {
    if (e.key === 'Enter' && !e.shiftKey && !e.isComposing) {
      e.preventDefault();
      this.gui();
    }
  }

  /**
   * Vẽ câu vừa gõ ra NGAY, rồi mới đi hỏi máy chủ.
   *
   * Chờ một vòng mạng mới thấy chữ của mình là cảm giác ứng dụng bị đơ — và trên mạng kém
   * thì nó kéo dài đủ lâu để người ta gõ lại câu đó lần nữa.
   *
   * Đổi lại phải nói thật về khoảng giữa: tin lạc quan mờ đi (`dang`), và nếu hỏng thì
   * viền đỏ kèm nút thử lại (`hong`). KHÔNG âm thầm xoá nó đi — câu đã gõ mà biến mất là
   * thứ người dùng không bao giờ tha thứ.
   */
  protected gui(): void {
    const id = this.selectedId();
    const noi = this.draft.value.trim();

    if (id === null || noi.length === 0 || this.conLai() < 0) {
      return;
    }

    this.draft.setValue('');
    this.guiNoiDung(id, noi);
  }

  private guiNoiDung(id: string, noi: string): void {
    const toi = this.store.user();
    const maTam = `tam-${id}-${this.messages().length}-${noi.length}`;

    const lacQuan: MessageOnScreen = {
      id: maTam,
      senderUserId: toi?.userId ?? '',
      senderName: toi?.displayName || (toi?.email ?? ''),
      body: noi,
      sentAtUtc: new Date().toISOString(),
      trangThai: 'dang',
    };

    this.messages.update((ds) => [...ds, lacQuan]);

    this.chat.send(id, noi).subscribe({
      next: (that) => this.thayTinTam(maTam, that),
      error: () =>
        this.messages.update((ds) =>
          ds.map((m) => (m.id === maTam ? { ...m, trangThai: 'hong' as const } : m)),
        ),
    });
  }

  private thayTinTam(maTam: string, that: MessageItem): void {
    this.messages.update((ds) => ds.map((m) => (m.id === maTam ? { ...that } : m)));

    // Câu cuối ở cột trái phải đổi theo, nếu không thì dòng đang mở nói về một tin cũ hơn
    // thứ đang hiện ngay bên phải nó.
    this.conversations.update((ds) =>
      ds.map((c) =>
        c.id === this.selectedId()
          ? {
              ...c,
              lastMessageBody: that.body,
              lastMessageSenderName: that.senderName,
              lastMessageAtUtc: that.sentAtUtc,
            }
          : c,
      ),
    );
  }

  protected thuLai(tin: MessageOnScreen): void {
    const id = this.selectedId();

    if (id === null) {
      return;
    }

    this.messages.update((ds) => ds.filter((m) => m.id !== tin.id));
    this.guiNoiDung(id, tin.body);
  }

  // ══════════════════════════════════════════════════════════════════
  // Tạo nhóm
  // ══════════════════════════════════════════════════════════════════

  protected moHopNhom(): void {
    this.hopNhom.set(true);
    this.tenNhom.setValue('');
    this.locNguoi.setValue('');
    this.moiAi.set([]);
    this.loiNhom.set(null);

    // Nạp mỗi lần mở, không nạp sẵn lúc vào màn: phần lớn phiên làm việc không tạo nhóm
    // nào, và danh sách người là một lượt hỏi không rẻ.
    this.org.members().subscribe({
      next: (ds) => this.nguoiTrongCongTy.set(ds),
      error: () => this.nguoiTrongCongTy.set([]),
    });
  }

  protected dongHopNhom(): void {
    this.hopNhom.set(false);
  }

  /**
   * Chỉ những người CÓ TÀI KHOẢN mới mời được.
   *
   * Màn Thành viên có ba loại dòng, trong đó một loại là hồ sơ nhân sự chưa có tài khoản
   * đăng nhập. Mời họ vào nhóm thì máy chủ trả `User.NotFound` và cả nhóm hỏng — mà lỗi
   * đó chẳng nói gì về việc người vừa chọn đơn giản là chưa đăng nhập được.
   *
   * Bỏ cả chính mình: người tạo tự động ở trong nhóm, hiện tên mình trong danh sách mời
   * là mời một người đã ở đó.
   */
  protected readonly nguoiMoiDuoc = computed(() => {
    const tu = this.tuLocNguoi().trim().toLowerCase();
    const toi = this.store.user()?.userId;

    return this.nguoiTrongCongTy()
      .filter((m) => m.userId !== null && m.userId !== toi && m.isActive)
      .filter((m) => !tu || m.fullName.toLowerCase().includes(tu));
  });

  protected daChon(userId: string): boolean {
    return this.moiAi().includes(userId);
  }

  protected doiChon(userId: string): void {
    this.moiAi.update((ds) =>
      ds.includes(userId) ? ds.filter((x) => x !== userId) : [...ds, userId],
    );
  }

  protected taoNhom(): void {
    const ten = this.tenNhom.value.trim();

    if (ten.length === 0) {
      this.loiNhom.set(this.i18n.instant('Conversation.NameEmpty'));

      return;
    }

    if (this.moiAi().length === 0) {
      this.loiNhom.set(this.i18n.instant('Conversation.GroupNeedsSomeone'));

      return;
    }

    this.dangTao.set(true);
    this.loiNhom.set(null);

    this.chat.createGroup({ name: ten, memberUserIds: [...this.moiAi()] }).subscribe({
      next: (moi) => {
        this.dangTao.set(false);
        this.hopNhom.set(false);
        this.conversations.update((ds) => [moi, ...ds]);
        this.chon(moi);
      },
      /*
        Hiện câu của MÁY CHỦ, không đoán lại.

        Máy chủ từ chối vì nhiều lý do khác nhau — tên quá dài, một người trong danh sách
        vừa bị vô hiệu hoá, tên trùng. Đoán bừa một mã ở đây thì người dùng đọc một câu
        không liên quan gì tới thứ vừa xảy ra, và họ sửa nhầm chỗ.
      */
      error: (loi: unknown) => {
        this.dangTao.set(false);
        this.loiNhom.set(
          isAppError(loi)
            ? this.errorMessages.resolve(loi)
            : this.i18n.instant('errorKind.unknown'),
        );
      },
    });
  }

  // ══════════════════════════════════════════════════════════════════
  // Nạp dữ liệu
  // ══════════════════════════════════════════════════════════════════

  protected napThemCu(): void {
    const id = this.selectedId();
    const cuNhat = this.messages()[0];

    if (id === null || !cuNhat) {
      return;
    }

    this.dangNapTin.set(true);

    this.chat.messages(id, cuNhat.id).subscribe({
      next: (trang) => {
        this.dangNapTin.set(false);
        this.messages.update((ds) => [...trang.items, ...ds]);
        this.conNua.set(trang.hasMore);
      },
      error: () => this.dangNapTin.set(false),
    });
  }

  private napDanhSach(): void {
    this.chat.conversations().subscribe({
      next: (ds) => {
        this.dangNap.set(false);
        this.conversations.set(ds);

        // Mở sẵn hội thoại đầu tiên: màn chat mà trống trơn ở cột phải thì người dùng
        // phải bấm một lần chỉ để thấy thứ họ vừa vào đây để xem.
        if (ds.length > 0) {
          this.chon(ds[0]);
        }
      },
      error: () => this.dangNap.set(false),
    });
  }

  private napTin(id: string): void {
    this.chat.messages(id).subscribe({
      next: (trang) => {
        // Hội thoại đã đổi trong lúc chờ mạng thì bỏ kết quả này đi. Không kiểm thì bấm
        // nhanh qua ba hội thoại sẽ đổ tin của cái thứ nhất vào cái thứ ba.
        if (this.selectedId() === id) {
          this.messages.set(trang.items);
          this.conNua.set(trang.hasMore);
        }
      },
      error: () => undefined,
    });
  }

  private xuongDay(): void {
    const el = this.vungCuon()?.nativeElement;

    if (el) {
      el.scrollTop = el.scrollHeight;
    }
  }
}
