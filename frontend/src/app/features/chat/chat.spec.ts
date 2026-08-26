import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed, type ComponentFixture } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it } from 'vitest';
import { AuthStore } from '../../core/auth/auth.store';
import { ChatService } from '../../core/chat/chat.service';
import { OrgService } from '../../core/org/org.service';
import { ConversationKind } from '../../core/models/chat.model';
import type {
  ConversationSummary,
  MessageItem,
  MessagePageResponse,
} from '../../core/models/chat.model';
import type { AuthUser } from '../../core/models/auth.model';
import type { MemberListItem } from '../../core/models/org.model';
import { Chat } from './chat';

/**
 * Màn Trao đổi — lát 1.
 *
 * Bốn thứ đáng kiểm nhất, và cả bốn đều là chuyện <b>màn hình nói thật hay nói dối</b>:
 *
 * <list type="bullet">
 * <item>Câu vừa gõ hiện NGAY, và khi gửi hỏng thì nó <b>ở lại</b> kèm nút thử lại — không
 * biến mất.</item>
 * <item>Nhóm và tin nhắn riêng dựng HAI kiểu luồng khác nhau.</item>
 * <item>Bấm nhanh qua hai hội thoại thì tin của cái đầu không đổ vào cái sau.</item>
 * <item>Hộp mời người chỉ hiện người CÓ tài khoản đăng nhập.</item>
 * </list>
 */

const TOI = 'u-toi';
const AN = 'u-an';

function hoiThoai(over: Partial<ConversationSummary> = {}): ConversationSummary {
  return {
    id: crypto.randomUUID(),
    kind: ConversationKind.Rieng,
    displayName: 'Nguyễn An',
    otherUserId: AN,
    participantCount: 2,
    lastMessageBody: 'Chào bạn',
    lastMessageSenderName: 'Nguyễn An',
    lastMessageAtUtc: '2026-08-26T09:00:00+00:00',
    unreadCount: 0,
    ...over,
  };
}

function tin(over: Partial<MessageItem> = {}): MessageItem {
  return {
    id: crypto.randomUUID(),
    senderUserId: AN,
    senderName: 'Nguyễn An',
    body: 'Chào bạn',
    sentAtUtc: '2026-08-26T09:00:00+00:00',
    ...over,
  };
}

function member(over: Partial<MemberListItem> = {}): MemberListItem {
  return {
    employeeId: crypto.randomUUID(),
    userId: crypto.randomUUID(),
    fullName: 'Trần Bình',
    code: 'NV002',
    jobTitle: null,
    email: 'binh@congty.vn',
    phone: null,
    departmentId: null,
    departmentName: null,
    roleName: 'Member',
    isActive: true,
    mustChangePassword: false,
    ...over,
  };
}

class FakeChatService {
  danhSach: Observable<ConversationSummary[]> = of([]);
  trang: Observable<MessagePageResponse> = of({ items: [], hasMore: false });
  guiTra: Observable<MessageItem> = of(tin({ senderUserId: TOI, senderName: 'Tôi' }));

  daDanhDau: string[] = [];
  daGui: string[] = [];
  xinTrang: { id: string; before?: string }[] = [];

  conversations(): Observable<ConversationSummary[]> {
    return this.danhSach;
  }

  messages(conversationId: string, before?: string): Observable<MessagePageResponse> {
    this.xinTrang.push({ id: conversationId, before });

    return this.trang;
  }

  send(_conversationId: string, body: string): Observable<MessageItem> {
    this.daGui.push(body);

    return this.guiTra;
  }

  markRead(conversationId: string): Observable<void> {
    this.daDanhDau.push(conversationId);

    return of(undefined);
  }

  createGroup(): Observable<ConversationSummary> {
    return of(hoiThoai({ kind: ConversationKind.Nhom, displayName: 'Nhóm mới' }));
  }
}

class FakeOrgService {
  ds: MemberListItem[] = [];

  members(): Observable<MemberListItem[]> {
    return of(this.ds);
  }
}

describe('Chat', () => {
  let fixture: ComponentFixture<Chat>;
  let chat: FakeChatService;
  let org: FakeOrgService;

  const user = signal<AuthUser | null>({
    userId: TOI,
    tenantId: 't-acme',
    email: 'toi@congty.vn',
    displayName: 'Lê Anh Lượng',
  });

  function make(): Chat {
    fixture = TestBed.createComponent(Chat);
    fixture.detectChanges();

    return fixture.componentInstance;
  }

  beforeEach(() => {
    chat = new FakeChatService();
    org = new FakeOrgService();

    TestBed.configureTestingModule({
      imports: [Chat],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideTranslateService(),
        { provide: ChatService, useValue: chat },
        { provide: OrgService, useValue: org },
        {
          provide: AuthStore,
          useValue: { user, hasPermission: () => true },
        },
      ],
    });
  });

  // ══════════════════════════════════════════════════════════════════
  // Mở màn
  // ══════════════════════════════════════════════════════════════════

  /**
   * Mở sẵn hội thoại đầu tiên.
   *
   * Màn chat mà cột phải trống trơn thì người dùng phải bấm một lần chỉ để thấy đúng thứ
   * họ vừa vào đây để xem.
   */
  it('mở màn thì tự chọn hội thoại đầu tiên', () => {
    const dau = hoiThoai({ displayName: 'Nguyễn An' });
    chat.danhSach = of([dau, hoiThoai({ displayName: 'Trần Bình' })]);

    const c = make();

    expect(c['selectedId']()).toBe(dau.id);
    expect(chat.xinTrang[0].id).toBe(dau.id);
  });

  it('chưa có hội thoại nào thì không hỏi tin của ai', () => {
    const c = make();

    expect(c['trong']()).toBe(true);
    expect(chat.xinTrang).toHaveLength(0);
  });

  /**
   * Huy hiệu đỏ biến mất NGAY, không chờ máy chủ.
   *
   * Người dùng vừa mở hội thoại ra — với họ nó đã đọc rồi. Chờ một vòng mạng để con số
   * biến mất là để lại một dấu hiệu nói sai về thứ đang hiện ngay trước mắt họ.
   */
  it('mở hội thoại thì huy hiệu chưa đọc về 0 ngay', () => {
    chat.danhSach = of([hoiThoai({ unreadCount: 7 })]);

    const c = make();

    expect(c['conversations']()[0].unreadCount).toBe(0);
    expect(chat.daDanhDau).toHaveLength(1);
  });

  /**
   * Bấm nhanh qua hai hội thoại thì tin của cái đầu KHÔNG đổ vào cái sau.
   *
   * Mạng chậm thì hai lượt hỏi chồng nhau, và lượt trả về sau chưa chắc là lượt xin sau.
   * Không kiểm thì người dùng đọc tin của người này dưới cái tên của người kia — một lỗi
   * quyền riêng tư, không phải một lỗi hiển thị.
   */
  it('đổi hội thoại giữa chừng thì bỏ kết quả cũ', () => {
    const a = hoiThoai({ displayName: 'A' });
    const b = hoiThoai({ displayName: 'B' });
    chat.danhSach = of([a, b]);
    chat.trang = of({ items: [tin({ body: 'của A' })], hasMore: false });

    const c = make();

    // Trả kết quả của A SAU khi đã chuyển sang B.
    let traVe!: (t: MessagePageResponse) => void;
    chat.trang = new Observable<MessagePageResponse>((sub) => {
      traVe = (t) => {
        sub.next(t);
        sub.complete();
      };
    });

    c['chon'](b);
    c['selectedId'].set(a.id);
    traVe({ items: [tin({ body: 'của B' })], hasMore: false });

    expect(c['messages']()).toHaveLength(0);
  });

  // ══════════════════════════════════════════════════════════════════
  // Gửi tin
  // ══════════════════════════════════════════════════════════════════

  it('gửi thì câu vừa gõ hiện ra ngay, trước khi máy chủ trả lời', () => {
    chat.danhSach = of([hoiThoai()]);
    chat.guiTra = new Observable<MessageItem>(() => undefined); // không bao giờ trả lời

    const c = make();
    c['draft'].setValue('Chào cả nhà');
    c['gui']();

    expect(c['messages']()).toHaveLength(1);
    expect(c['messages']()[0].body).toBe('Chào cả nhà');
    expect(c['messages']()[0].trangThai).toBe('dang');

    // Ô soạn phải trống ngay: người dùng gõ tiếp câu sau, không phải xoá tay câu vừa gửi.
    expect(c['draft'].value).toBe('');
  });

  /**
   * Gửi hỏng thì câu đó <b>Ở LẠI</b>, kèm nút thử lại.
   *
   * Xoá đi là thứ người dùng không bao giờ tha thứ: họ vừa gõ ba dòng, mạng chớp một cái,
   * và ba dòng đó không còn ở đâu cả. Giữ lại kèm nút thử lại thì tệ nhất họ mất một lần
   * bấm.
   */
  it('gửi hỏng thì tin ở lại và đánh dấu hỏng', () => {
    chat.danhSach = of([hoiThoai()]);
    chat.guiTra = throwError(() => new Error('mạng'));

    const c = make();
    c['draft'].setValue('Câu quan trọng');
    c['gui']();

    expect(c['messages']()).toHaveLength(1);
    expect(c['messages']()[0].body).toBe('Câu quan trọng');
    expect(c['messages']()[0].trangThai).toBe('hong');
  });

  it('thử lại thì gửi đúng câu cũ và bỏ tin hỏng đi', () => {
    chat.danhSach = of([hoiThoai()]);
    chat.guiTra = throwError(() => new Error('mạng'));

    const c = make();
    c['draft'].setValue('Câu quan trọng');
    c['gui']();

    chat.guiTra = of(tin({ senderUserId: TOI, body: 'Câu quan trọng' }));
    c['thuLai'](c['messages']()[0]);

    expect(chat.daGui).toEqual(['Câu quan trọng', 'Câu quan trọng']);
    expect(c['messages']()).toHaveLength(1);
    expect(c['messages']()[0].trangThai).toBeUndefined();
  });

  it('câu chỉ toàn khoảng trắng thì không gửi', () => {
    chat.danhSach = of([hoiThoai()]);

    const c = make();
    c['draft'].setValue('   \n  ');

    expect(c['guiDuoc']()).toBe(false);

    c['gui']();

    expect(chat.daGui).toHaveLength(0);
  });

  // ══════════════════════════════════════════════════════════════════
  // Gộp tin và chia ngày
  // ══════════════════════════════════════════════════════════════════

  /**
   * Hai tin liền nhau của cùng một người, cách nhau dưới 5 phút, thì gộp.
   *
   * Không gộp thì một người gửi năm câu liên tiếp sẽ thấy tên họ năm lần — luồng biến
   * thành danh bạ. Nhưng gộp bất kể thời gian cũng sai: hai câu cách nhau ba tiếng là hai
   * lượt nói khác nhau.
   */
  it('gộp tin liền nhau của cùng người, nhưng chỉ trong 5 phút', () => {
    chat.danhSach = of([hoiThoai()]);
    chat.trang = of({
      items: [
        tin({ body: 'một', sentAtUtc: '2026-08-26T09:00:00+00:00' }),
        tin({ body: 'hai', sentAtUtc: '2026-08-26T09:02:00+00:00' }),
        tin({ body: 'ba', sentAtUtc: '2026-08-26T12:00:00+00:00' }),
        tin({ body: 'bốn', senderUserId: TOI, sentAtUtc: '2026-08-26T12:01:00+00:00' }),
      ],
      hasMore: false,
    });

    const c = make();
    const dong = c['khoiNgay']().flatMap((k) => k.tin);

    expect(dong.map((d) => d.noi)).toEqual([false, true, false, false]);
    expect(dong.map((d) => d.cuaToi)).toEqual([false, false, false, true]);
  });

  it('tin khác ngày nằm ở hai khối khác nhau', () => {
    chat.danhSach = of([hoiThoai()]);
    chat.trang = of({
      items: [
        tin({ sentAtUtc: '2026-08-20T09:00:00+00:00' }),
        tin({ sentAtUtc: '2026-08-21T09:00:00+00:00' }),
      ],
      hasMore: false,
    });

    const c = make();

    expect(c['khoiNgay']()).toHaveLength(2);
  });

  // ══════════════════════════════════════════════════════════════════
  // Hai kiểu luồng
  // ══════════════════════════════════════════════════════════════════

  /**
   * Nhóm và tin nhắn riêng dựng HAI kiểu luồng khác nhau.
   *
   * Nhóm 12 người mà đảo tin của mình sang phải thì cột đọc gãy làm đôi. Cả hai chỉ khác
   * nhau bằng MỘT lớp, nên đây là chỗ rẻ nhất để nó lặng lẽ sai.
   */
  it('nhóm dùng luong--kenh, tin nhắn riêng dùng luong--rieng', () => {
    chat.danhSach = of([
      hoiThoai({ kind: ConversationKind.Nhom, displayName: 'Khối Kỹ thuật' }),
      hoiThoai({ kind: ConversationKind.Rieng }),
    ]);

    const c = make();
    const luong = (): HTMLElement => fixture.nativeElement.querySelector('.luong');

    expect(luong().classList.contains('luong--kenh')).toBe(true);
    expect(luong().classList.contains('luong--rieng')).toBe(false);

    c['chon'](c['conversations']()[1]);
    fixture.detectChanges();

    expect(luong().classList.contains('luong--rieng')).toBe(true);
    expect(luong().classList.contains('luong--kenh')).toBe(false);
  });

  it('chia đúng hai nhóm ở cột trái', () => {
    chat.danhSach = of([
      hoiThoai({ kind: ConversationKind.Nhom }),
      hoiThoai({ kind: ConversationKind.Rieng }),
      hoiThoai({ kind: ConversationKind.Rieng }),
    ]);

    const c = make();

    expect(c['nhomList']()).toHaveLength(1);
    expect(c['riengList']()).toHaveLength(2);
  });

  it('ô tìm lọc tại chỗ, không hỏi lại máy chủ', () => {
    chat.danhSach = of([
      hoiThoai({ displayName: 'Nguyễn An' }),
      hoiThoai({ displayName: 'Trần Bình' }),
    ]);

    const c = make();
    const truoc = chat.xinTrang.length;

    c['search'].setValue('bình');

    expect(c['riengList']()).toHaveLength(1);
    expect(chat.xinTrang).toHaveLength(truoc);
  });

  // ══════════════════════════════════════════════════════════════════
  // Tạo nhóm
  // ══════════════════════════════════════════════════════════════════

  /**
   * Chỉ mời được người CÓ tài khoản đăng nhập.
   *
   * Màn Thành viên có ba loại dòng, trong đó một loại là hồ sơ nhân sự chưa có tài khoản.
   * Mời họ thì máy chủ trả `User.NotFound` và cả nhóm hỏng — mà lỗi đó chẳng nói gì về
   * việc người vừa chọn đơn giản là chưa đăng nhập được.
   */
  it('danh sách mời bỏ người chưa có tài khoản, người bị khoá, và chính mình', () => {
    org.ds = [
      member({ fullName: 'Có tài khoản' }),
      member({ fullName: 'Chưa có tài khoản', userId: null }),
      member({ fullName: 'Đã bị khoá', isActive: false }),
      member({ fullName: 'Chính tôi', userId: TOI }),
    ];

    const c = make();
    c['moHopNhom']();

    expect(c['nguoiMoiDuoc']().map((m) => m.fullName)).toEqual(['Có tài khoản']);
  });

  it('nhóm không tên hoặc không mời ai thì không gọi máy chủ', () => {
    org.ds = [member()];

    const c = make();
    c['moHopNhom']();

    c['tenNhom'].setValue('   ');
    c['taoNhom']();
    expect(c['loiNhom']()).not.toBeNull();

    c['tenNhom'].setValue('Nhóm A');
    c['loiNhom'].set(null);
    c['taoNhom']();
    expect(c['loiNhom']()).not.toBeNull();
  });

  it('tạo nhóm xong thì nó lên đầu danh sách và được mở ra', () => {
    org.ds = [member()];

    const c = make();
    c['moHopNhom']();
    c['tenNhom'].setValue('Nhóm mới');
    c['doiChon'](org.ds[0].userId!);
    c['taoNhom']();

    expect(c['hopNhom']()).toBe(false);
    expect(c['conversations']()[0].displayName).toBe('Nhóm mới');
    expect(c['selectedId']()).toBe(c['conversations']()[0].id);
  });

  // ══════════════════════════════════════════════════════════════════
  // Cuộn ngược
  // ══════════════════════════════════════════════════════════════════

  it('xem tin cũ hơn thì lấy con trỏ từ tin CŨ NHẤT đang hiện', () => {
    const cuNhat = tin({ body: 'cũ nhất' });
    chat.danhSach = of([hoiThoai()]);
    chat.trang = of({ items: [cuNhat, tin({ body: 'mới hơn' })], hasMore: true });

    const c = make();
    c['napThemCu']();

    expect(chat.xinTrang.at(-1)?.before).toBe(cuNhat.id);
  });
});
