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

function user(over: Partial<UserListItem> = {}): UserListItem {
  return {
    id: crypto.randomUUID(),
    email: 'an@congty.vn',
    fullName: 'Nguyễn An',
    isActive: true,
    mustChangePassword: false,
    roleName: 'Member',
    createdAtUtc: '2026-08-24T07:00:00+00:00',
    ...over,
  };
}

function paged(items: UserListItem[], total = items.length): PagedList<UserListItem> {
  return {
    items,
    page: 1,
    pageSize: 20,
    totalCount: total,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false,
  };
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

class FakeUserService {
  queries: UserQuery[] = [];
  result: PagedList<UserListItem> = paged([user()]);

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

  list(query: UserQuery): Observable<PagedList<UserListItem>> {
    this.queries.push(query);

    return of(this.result);
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
      ],
    });
  });

  // ── Nạp và lọc ────────────────────────────────────────────────────

  it('mở màn là nạp trang đầu, không kèm bộ lọc nào', () => {
    make();

    expect(service.queries[0]).toMatchObject({ page: 1, status: UserStatusFilter.Any, search: '' });
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
    component['onStatusChange']({ target: { value: '3' } } as unknown as Event);

    expect(service.queries.at(-1)).toMatchObject({ page: 1, status: UserStatusFilter.Disabled });
  });

  it('gõ tìm kiếm KHÔNG gọi ngay mỗi phím', async () => {
    // Một cái tên mười ký tự là mười lượt đi về nếu gọi theo từng phím.
    vi.useFakeTimers();

    const component = make();
    const before = service.queries.length;

    for (const term of ['n', 'ng', 'ngu']) {
      component['onSearchInput']({ target: { value: term } } as unknown as Event);
    }

    expect(service.queries.length).toBe(before);

    vi.advanceTimersByTime(400);

    expect(service.queries.length).toBe(before + 1);
    expect(service.queries.at(-1)!.search).toBe('ngu');

    vi.useRealTimers();
  });

  // ── Trạng thái rỗng ───────────────────────────────────────────────

  it('không có ai và KHÔNG lọc gì thì hiện "chưa có ai"', () => {
    service.result = paged([], 0);

    expect(make()['state']()).toBe('rong');
  });

  it('không có ai NHƯNG đang lọc thì hiện "lọc không ra"', () => {
    // Hai câu cần nói khác hẳn nhau. Gộp làm một thì người bật nhầm bộ lọc từ lần trước
    // sẽ kết luận là công ty không có ai.
    service.result = paged([], 0);

    const component = make();

    component['status'].set(UserStatusFilter.Disabled);

    expect(component['state']()).toBe('khongthay');
  });

  // ── Chọn nhiều dòng ───────────────────────────────────────────────

  it('chọn một phần thì ô "chọn tất cả" ở trạng thái THỨ BA', () => {
    // Chỉ có tick/không tick thì nó nói dối: nhìn vào tưởng chưa chọn ai.
    service.result = paged([user(), user(), user()]);

    const component = make();

    component['toggleRow'](service.result.items[0].id);

    expect(component['someSelected']()).toBe(true);
    expect(component['allSelected']()).toBe(false);
  });

  it('nạp lại danh sách thì BỎ CHỌN những người không còn trên trang', () => {
    // Giữ lại thì thanh "đã chọn 3 người" nói về những người đang không nhìn thấy, và
    // thao tác hàng loạt sẽ chạm vào người mà quản trị viên không hề định chạm.
    const cu = user();

    service.result = paged([cu]);

    const component = make();

    component['toggleRow'](cu.id);
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

    expect(service.activeCalls).toEqual([{ id: target.id, isActive: false }]);
    expect(component['detail']()).toBeNull();
  });
});
