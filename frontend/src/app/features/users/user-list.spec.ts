import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed, type ComponentFixture } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { AppError } from '../../core/models/api-error.model';
import {
  UserStatusFilter,
  type CreateUserRequest,
  type CreateUserResponse,
  type PagedList,
  type RoleListItem,
  type UpdateUserRequest,
  type UserListItem,
  type UserQuery,
} from '../../core/models/user.model';
import type { MemberListItem } from '../../core/models/org.model';
import { OrgService } from '../../core/org/org.service';
import { UserService } from '../../core/users/user.service';
import { UserList } from './user-list';

/**
 * Màn Nhân sự ở mức hành vi.
 *
 * Không đụng HTTP thật — <c>UserService</c> bị thay bằng bản giả. Truy vấn thật đã có bộ
 * kiểm riêng chạy trên Postgres (<c>UserManagementFlowTests</c>); thứ đáng kiểm ở đây là
 * <b>những gì màn hình quyết định</b>: gửi bộ lọc nào xuống, hiện trạng thái rỗng nào, và
 * hai chỗ dễ hỏng nhất — ô "chọn tất cả" và hộp thoại hai bước.
 */

/**
 * Một dòng của danh sách GỘP.
 *
 * Mặc định là ca "có cả hai" — người bình thường. Hai ca còn lại dựng bằng cách truyền
 * `userId: null` (chỉ hồ sơ) hoặc `employeeId: null, code: null` (chỉ tài khoản).
 */
function user(over: Partial<MemberListItem> = {}): MemberListItem {
  const id = crypto.randomUUID();

  return {
    employeeId: `e-${id}`,
    userId: id,
    fullName: 'Nguyễn An',
    code: 'NV001',
    jobTitle: null,
    email: 'an@congty.vn',
    phone: null,
    departmentId: null,
    departmentName: null,
    roleName: 'Member',
    isActive: true,
    mustChangePassword: false,
    ...over,
  };
}

/**
 * Giữ tên `paged` cho các test cũ đọc quen, nhưng nay nó chỉ trả về MẢNG.
 *
 * `/api/members` không phân trang — nó buộc phải trả toàn bộ để gộp được hai nguồn. Việc
 * cắt trang chuyển sang component, và đó chính là thứ vài test dưới đây kiểm.
 */
function paged(items: MemberListItem[]): MemberListItem[] {
  return items;
}

const ROLES: RoleListItem[] = [
  { id: 'r-member', name: 'Member', isSystem: true, permissions: ['user.read'], memberCount: 3 },
  {
    id: 'r-admin',
    name: 'Admin',
    isSystem: true,
    permissions: ['user.read', 'user.manage'],
    memberCount: 1,
  },
];

/**
 * MỘT bộ giả phục vụ CẢ HAI cổng mà màn hình dùng.
 *
 * Màn Thành viên nay đọc danh sách từ `OrgService.members()` (nguồn GỘP) nhưng vẫn ghi qua
 * `UserService` (tạo tài khoản, đổi vai, vô hiệu hoá). Hai bộ giả riêng thì mỗi test phải
 * dựng và nối cả hai; một bộ dùng chung giữ test đọc được, và vẫn tách bạch được đường
 * đọc với đường ghi vì chúng là hai phương thức khác nhau.
 */
class FakeUserService {
  queries: UserQuery[] = [];

  /** Số lần màn hình đi HỎI SERVER. Lọc tại chỗ thì con số này KHÔNG được tăng. */
  loads = 0;

  result: MemberListItem[] = paged([user()]);

  members(): Observable<MemberListItem[]> {
    this.loads++;

    return of(this.result);
  }

  createdWith: CreateUserRequest | null = null;
  createResult: Observable<CreateUserResponse> = of({
    id: 'u-1',
    email: 'an@congty.vn',
    fullName: 'Nguyễn An',
    roleName: 'Member',
    temporaryPassword: 'k7np-2wqx-hs4m',
  });

  updatedWith: { id: string; body: UpdateUserRequest } | null = null;
  activeCalls: { id: string; isActive: boolean }[] = [];

  /**
   * KHÔNG còn được màn hình gọi — danh sách nay đến từ `members()`.
   *
   * Giữ lại và NÉM LỖI thay vì xoá: nếu một thay đổi sau này lỡ gọi lại đường cũ thì test
   * đỏ ngay và nói rõ vì sao, thay vì lặng lẽ đọc từ một nguồn chưa gộp — lúc đó bảng sẽ
   * thiếu mọi người chưa có tài khoản, và không có gì báo.
   */
  list(query: UserQuery): Observable<PagedList<UserListItem>> {
    this.queries.push(query);

    throw new Error('Màn Thành viên phải đọc từ /api/members, không phải /api/users.');
  }

  roles(): Observable<RoleListItem[]> {
    return of(ROLES);
  }

  create(request: CreateUserRequest): Observable<CreateUserResponse> {
    this.createdWith = request;

    return this.createResult;
  }

  update(id: string, body: UpdateUserRequest): Observable<void> {
    this.updatedWith = { id, body };

    return of(undefined);
  }

  setActive(id: string, isActive: boolean): Observable<void> {
    this.activeCalls.push({ id, isActive });

    return of(undefined);
  }
}

describe('UserList', () => {
  let fixture: ComponentFixture<UserList>;
  let service: FakeUserService;

  function make(): UserList {
    fixture = TestBed.createComponent(UserList);
    fixture.detectChanges();

    return fixture.componentInstance;
  }

  beforeEach(() => {
    service = new FakeUserService();

    TestBed.configureTestingModule({
      imports: [UserList],
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService(),
        { provide: UserService, useValue: service },
        // Cùng một bộ giả cho cả hai cổng — xem chú thích ở `FakeUserService`.
        { provide: OrgService, useValue: service },
      ],
    });
  });

  // ── Nạp và lọc ────────────────────────────────────────────────────

  it('mở màn là nạp danh sách GỘP, không phải danh sách tài khoản', () => {
    make();

    expect(service.loads).toBe(1);

    // `list()` của UserService ném lỗi nếu bị gọi, nên tới được đây đã là một nửa bằng
    // chứng. Nửa còn lại: nó chưa từng được gọi.
    expect(service.queries).toHaveLength(0);
  });

  /**
   * Đổi bộ lọc thì lọc TẠI CHỖ, không đi hỏi server thêm lần nào.
   *
   * `/api/members` trả về toàn bộ danh sách, nên dữ liệu đã nằm sẵn trong tay. Gọi lại
   * server cho mỗi lần gõ là một vòng mạng không mua được gì, và nó làm ô tìm giật.
   */
  it('đổi bộ lọc thì KHÔNG đi hỏi server lần nữa', () => {
    const component = make();

    expect(service.loads).toBe(1);

    component['onStatusChange']({
      target: { value: String(UserStatusFilter.Disabled) },
    } as unknown as Event);

    expect(service.loads).toBe(1);
  });

  /**
   * Ba loại dòng, và cả ba đều phải hiện ra.
   *
   * Đây là lý do màn này tồn tại ở dạng gộp. Lấy nhầm nguồn (quay về `/api/users`) thì
   * dòng "chỉ hồ sơ" biến mất — người mới chưa được cấp tài khoản sẽ không ai nhìn thấy.
   */
  it('hiện đủ cả ba loại dòng: cả hai · chỉ hồ sơ · chỉ tài khoản', () => {
    service.result = [
      user({ fullName: 'Cả hai' }),
      user({ fullName: 'Chỉ hồ sơ', userId: null, roleName: null }),
      user({ fullName: 'Chỉ tài khoản', employeeId: null, code: null }),
    ];

    const component = make();
    const items = component['page']()!.items;

    expect(items).toHaveLength(3);
    expect(items.filter((m) => m.userId === null)).toHaveLength(1);
    expect(items.filter((m) => m.employeeId === null)).toHaveLength(1);

    // Và cả ba phải DỰNG RA được. Dừng ở signal thì một lỗi trong template — thiếu khoá
    // dịch, một `@if` sai nhánh — vẫn để test xanh trong khi bảng trống trơn.
    const rows = fixture.nativeElement.querySelectorAll('tbody tr');

    expect(rows).toHaveLength(3);
    expect(fixture.nativeElement.querySelectorAll('.thieu').length).toBeGreaterThanOrEqual(2);
  });

  it('lọc "chưa có tài khoản" chỉ giữ dòng khuyết tài khoản', () => {
    service.result = [
      user({ fullName: 'Có tài khoản' }),
      user({ fullName: 'Chưa có', userId: null, roleName: null }),
    ];

    const component = make();

    component['onKindChange']({ target: { value: 'khongTaiKhoan' } } as unknown as Event);

    const items = component['page']()!.items;

    expect(items).toHaveLength(1);
    expect(items[0].fullName).toBe('Chưa có');
  });

  /**
   * Ngăn kéo chi tiết KHÔNG mở cho dòng chưa có tài khoản.
   *
   * Nó sửa vai trò và tên của một TÀI KHOẢN. Mở ra một biểu mẫu không lưu được là cách
   * chắc chắn nhất làm người dùng bực — họ điền xong rồi mới biết.
   */
  it('không mở ngăn kéo cho dòng chưa có tài khoản', () => {
    const chiHoSo = user({ userId: null, roleName: null });

    service.result = [chiHoSo];

    const component = make();

    component['openDetail'](chiHoSo);

    expect(component['detail']()).toBeNull();
  });

  it('vai trò mặc định của hộp thoại là vai HẸP nhất', () => {
    // Mặc định là Admin thì một cú bấm vội tạo ra một quản trị viên — sai theo hướng
    // nguy hiểm. Người tạo phải chủ động nâng quyền, không phải chủ động hạ.
    const component = make();

    expect(component['createForm'].controls.roleId.value).toBe('r-member');
  });

  it('đổi bộ lọc thì QUAY VỀ trang một', () => {
    // Đang ở trang 3 mà lọc lại thì kết quả mới thường không có tới trang 3 — người dùng
    // nhận về một trang trống và tưởng không tìm thấy ai.
    const component = make();

    component['currentPage'].set(3);
    component['onStatusChange']({
      target: { value: String(UserStatusFilter.Disabled) },
    } as unknown as Event);

    expect(component['page']()!.page).toBe(1);
    expect(component['status']()).toBe(UserStatusFilter.Disabled);
  });

  /**
   * Ô tìm vẫn chờ người dùng ngừng gõ, dù nay lọc tại chỗ.
   *
   * Không còn để tiết kiệm lượt mạng — mà để bảng không nhấp nháy qua ba kết quả trung
   * gian trong lúc gõ một cái tên.
   */
  it('gõ tìm kiếm KHÔNG lọc lại ngay mỗi phím', () => {
    vi.useFakeTimers();

    service.result = [user({ fullName: 'Nguyễn An' }), user({ fullName: 'Trần Bình' })];

    const component = make();

    expect(component['page']()!.totalCount).toBe(2);

    for (const term of ['n', 'ng', 'ngu']) {
      component['onSearchInput']({ target: { value: term } } as unknown as Event);
    }

    expect(component['page']()!.totalCount).toBe(2);

    vi.advanceTimersByTime(400);

    expect(component['search']()).toBe('ngu');
    expect(component['page']()!.totalCount).toBe(1);

    vi.useRealTimers();
  });

  // ── Trạng thái rỗng ───────────────────────────────────────────────

  it('không có ai và KHÔNG lọc gì thì hiện "chưa có ai"', () => {
    service.result = paged([]);

    expect(make()['state']()).toBe('rong');
  });

  it('không có ai NHƯNG đang lọc thì hiện "lọc không ra"', () => {
    // Hai câu cần nói khác hẳn nhau. Gộp làm một thì người bật nhầm bộ lọc từ lần trước
    // sẽ kết luận là công ty không có ai.
    service.result = paged([]);

    const component = make();

    component['status'].set(UserStatusFilter.Disabled);

    expect(component['state']()).toBe('khongthay');
  });

  // ── Chọn nhiều dòng ───────────────────────────────────────────────

  it('chọn một phần thì ô "chọn tất cả" ở trạng thái THỨ BA', () => {
    // Chỉ có tick/không tick thì nó nói dối: nhìn vào tưởng chưa chọn ai.
    service.result = paged([user(), user(), user()]);

    const component = make();

    component['toggleRow'](component['rowKey'](service.result[0]));

    expect(component['someSelected']()).toBe(true);
    expect(component['allSelected']()).toBe(false);
  });

  it('nạp lại danh sách thì BỎ CHỌN những người không còn trên trang', () => {
    // Giữ lại thì thanh "đã chọn 3 người" nói về những người đang không nhìn thấy, và
    // thao tác hàng loạt sẽ chạm vào người mà quản trị viên không hề định chạm.
    const cu = user();

    service.result = paged([cu]);

    const component = make();

    component['toggleRow'](component['rowKey'](cu));
    expect(component['selected']().size).toBe(1);

    service.result = paged([user()]);
    component['load']();

    expect(component['selected']().size).toBe(0);
  });

  // ── Hộp thoại hai bước ────────────────────────────────────────────

  it('tạo xong thì sang bước hiện mật khẩu tạm', () => {
    const component = make();

    component['openCreate']();
    component['createForm'].patchValue({ fullName: 'Nguyễn An', email: 'an@congty.vn' });
    component['submitCreate']();

    expect(component['createStep']()).toBe('xong');
    expect(component['created']()!.temporaryPassword).toBe('k7np-2wqx-hs4m');
  });

  it('MỞ LẠI hộp thoại thì không còn thấy mật khẩu của người trước', () => {
    // Vừa khó hiểu vừa là rò rỉ: người tạo tiếp theo nhìn thấy mật khẩu của đồng nghiệp.
    const component = make();

    component['openCreate']();
    component['createForm'].patchValue({ fullName: 'Nguyễn An', email: 'an@congty.vn' });
    component['submitCreate']();

    component['openCreate']();

    expect(component['createStep']()).toBe('nhap');
    expect(component['created']()).toBeNull();
  });

  it('biểu mẫu thiếu dữ liệu thì KHÔNG gọi backend', () => {
    const component = make();

    component['openCreate']();
    component['submitCreate']();

    expect(service.createdWith).toBeNull();
  });

  it('email đã có tài khoản thì báo ngay tại ô email', () => {
    const conflict: AppError = {
      kind: 'conflict',
      status: 409,
      code: 'Email.Taken',
      message: 'Email này đã có tài khoản.',
      details: [],
      fieldErrors: {},
      correlationId: null,
    };

    service.createResult = throwError(() => conflict);

    const component = make();

    component['openCreate']();
    component['createForm'].patchValue({ fullName: 'Nguyễn An', email: 'an@congty.vn' });
    component['submitCreate']();

    expect(component['emailError']()).not.toBeNull();
    expect(component['createStep']()).toBe('nhap');
  });

  // ── Chi tiết ──────────────────────────────────────────────────────

  it('mở chi tiết thì điền sẵn tên và ĐÚNG vai trò hiện tại', () => {
    const admin = user({ roleName: 'Admin', fullName: 'Phạm Hà' });

    service.result = paged([admin]);

    const component = make();

    component['openDetail'](admin);

    expect(component['detailForm'].getRawValue()).toEqual({
      fullName: 'Phạm Hà',
      roleId: 'r-admin',
    });
  });

  it('vô hiệu hoá gọi đúng endpoint và đóng ngăn kéo', () => {
    const target = user();

    service.result = paged([target]);

    const component = make();

    component['openDetail'](target);
    component['setActive'](target, false);

    expect(service.activeCalls).toEqual([{ id: target.userId, isActive: false }]);
    expect(component['detail']()).toBeNull();
  });
});
