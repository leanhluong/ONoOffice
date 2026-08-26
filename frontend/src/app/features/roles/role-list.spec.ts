import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed, type ComponentFixture } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it } from 'vitest';
import type { AppError } from '../../core/models/api-error.model';
import type { RoleListItem } from '../../core/models/user.model';
import { UserService } from '../../core/users/user.service';
import { RoleList } from './role-list';

/**
 * Màn Vai trò &amp; quyền — nay SỬA được, không chỉ xem.
 *
 * Thứ đáng kiểm nhất ở đây không phải bảng công tắc mà là <b>ranh giới giữa vai hệ thống
 * và vai tự đặt</b>: bốn vai hệ thống dựng lại từ hằng số trong mã nguồn ở mọi workspace,
 * nên sửa được chúng nghĩa là lần nâng cấp sau ghi đè mà không báo gì.
 *
 * Và một luật nữa, quan trọng hơn cả: <c>workspace.transfer-ownership</c> không bao giờ
 * xuất hiện trong bảng của vai tự đặt.
 */

function role(over: Partial<RoleListItem> = {}): RoleListItem {
  return {
    id: crypto.randomUUID(),
    name: 'Kế toán',
    isSystem: false,
    permissions: ['employee.read'],
    memberCount: 0,
    ...over,
  };
}

const OWNER = role({
  id: 'r-owner',
  name: 'Owner',
  isSystem: true,
  permissions: ['employee.read', 'workspace.transfer-ownership'],
  memberCount: 1,
});

class FakeUserService {
  result: Observable<RoleListItem[]> = of([OWNER, role({ id: 'r-ketoan' })]);

  created: { name: string; permissions: string[] }[] = [];
  updated: { id: string; name: string; permissions: string[] }[] = [];
  deleted: string[] = [];

  saveResult: Observable<void> = of(undefined);

  roles(): Observable<RoleListItem[]> {
    return this.result;
  }

  createRole(name: string, permissions: string[]): Observable<{ id: string }> {
    this.created.push({ name, permissions });

    return of({ id: 'r-moi' });
  }

  updateRole(id: string, name: string, permissions: string[]): Observable<void> {
    this.updated.push({ id, name, permissions });

    return this.saveResult;
  }

  deleteRole(id: string): Observable<void> {
    this.deleted.push(id);

    return of(undefined);
  }
}

describe('RoleList', () => {
  let fixture: ComponentFixture<RoleList>;
  let service: FakeUserService;

  function make(): RoleList {
    fixture = TestBed.createComponent(RoleList);
    fixture.detectChanges();

    return fixture.componentInstance;
  }

  beforeEach(() => {
    service = new FakeUserService();

    TestBed.configureTestingModule({
      imports: [RoleList],
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService(),
        { provide: UserService, useValue: service },
      ],
    });
  });

  // ── Ranh giới hệ thống ↔ tự đặt ───────────────────────────────────

  /**
   * Vai HỆ THỐNG không sửa được, và giao diện phải nói điều đó TRƯỚC khi người ta bấm.
   *
   * Backend từ chối bằng `Role.SystemRoleIsImmutable`, nhưng để người dùng tick mười công
   * tắc rồi mới báo là cách chắc chắn nhất làm họ bực.
   */
  it('vai hệ thống thì không sửa được', () => {
    const component = make();

    component['select']('r-owner');

    expect(component['canEdit']()).toBe(false);
  });

  it('vai tự đặt thì sửa được', () => {
    const component = make();

    component['select']('r-ketoan');

    expect(component['canEdit']()).toBe(true);
  });

  /**
   * Quyền chuyển nhượng workspace KHÔNG có trong bảng của vai tự đặt.
   *
   * Đây là phép kiểm quan trọng nhất tệp này. Nó là toàn bộ ranh giới Admin ↔ Owner; hiện
   * nó ra dưới dạng một công tắc bật được nghĩa là người quản trị tin rằng họ trao đi được
   * thứ mà backend sẽ từ chối bằng `Role.PermissionIsOwnerOnly`.
   *
   * Với vai HỆ THỐNG thì nó vẫn hiện — ở đó nó là sự thật cần đọc: Owner có, Admin không.
   */
  it('vai tự đặt KHÔNG hiện quyền chuyển nhượng workspace', () => {
    const component = make();

    component['select']('r-ketoan');

    const codes = component['blocks']().flatMap((b) => b.rows.map((r) => r.code));

    expect(codes).not.toContain('workspace.transfer-ownership');
  });

  it('vai hệ thống VẪN hiện quyền chuyển nhượng — ở đó nó là sự thật', () => {
    const component = make();

    component['select']('r-owner');

    const codes = component['blocks']().flatMap((b) => b.rows.map((r) => r.code));

    expect(codes).toContain('workspace.transfer-ownership');
  });

  // ── Sửa quyền ─────────────────────────────────────────────────────

  /**
   * Bật/tắt công tắc KHÔNG gọi backend ngay.
   *
   * Bộ quyền là thứ người ta cân nhắc cả cụm — bật ba cái rồi tắt một cái. Lưu theo từng
   * công tắc thì mỗi lần bấm nhầm là một thay đổi thật, không có đường lùi và cũng không
   * có chỗ nào để huỷ.
   */
  it('bật công tắc thì chưa lưu, phải bấm Lưu', () => {
    const component = make();

    component['select']('r-ketoan');
    component['toggle']('department.read');

    expect(service.updated).toHaveLength(0);
    expect(component['dirty']()).toBe(true);

    component['save']();

    expect(service.updated).toEqual([
      { id: 'r-ketoan', name: 'Kế toán', permissions: ['employee.read', 'department.read'] },
    ]);
  });

  it('hoàn tác thì trả về đúng bộ quyền ban đầu', () => {
    const component = make();

    component['select']('r-ketoan');
    component['toggle']('department.read');
    component['toggle']('employee.read');
    component['revert']();

    expect(component['dirty']()).toBe(false);
    expect(component['draftPermissions']()).toEqual(['employee.read']);
  });

  /**
   * Đổi sang vai khác thì BỎ luôn thay đổi chưa lưu.
   *
   * Giữ lại thì người dùng quay về vai cũ và thấy những công tắc họ đã quên — rồi bấm Lưu
   * cho một thay đổi họ không còn nhớ mình đã làm.
   */
  it('đổi vai thì bỏ thay đổi chưa lưu', () => {
    const component = make();

    component['select']('r-ketoan');
    component['toggle']('department.read');

    component['select']('r-owner');
    component['select']('r-ketoan');

    expect(component['dirty']()).toBe(false);
  });

  // ── Tạo và xoá ────────────────────────────────────────────────────

  it('tạo vai mới thì gửi tên và KHÔNG kèm quyền nào', () => {
    const component = make();

    component['openCreate']();
    component['createForm'].setValue({ name: 'Nhân sự' });
    component['submitCreate']();

    expect(service.created).toEqual([{ name: 'Nhân sự', permissions: [] }]);
  });

  it('tên rỗng thì không gọi backend', () => {
    const component = make();

    component['openCreate']();
    component['submitCreate']();

    expect(service.created).toHaveLength(0);
  });

  it('xoá thì hỏi trước, và chỉ gọi khi xác nhận', () => {
    const component = make();

    component['select']('r-ketoan');
    component['askDelete']();

    expect(service.deleted).toHaveLength(0);

    component['confirmDelete']();

    expect(service.deleted).toEqual(['r-ketoan']);
  });

  /**
   * Vai còn người giữ thì nút xoá KHÔNG hiện.
   *
   * Backend từ chối bằng `Role.StillInUse`. Hiện nút rồi báo lỗi khi bấm là bắt người dùng
   * bấm mới biết, trong khi số người giữ đã nằm sẵn ngay trên màn hình.
   */
  it('vai còn người giữ thì không cho xoá', () => {
    service.result = of([OWNER, role({ id: 'r-ketoan', memberCount: 2 })]);

    const component = make();

    component['select']('r-ketoan');

    expect(component['canDelete']()).toBe(false);
  });

  it('lưu hỏng thì giữ nguyên thay đổi để người dùng thử lại', () => {
    const loi: AppError = {
      kind: 'conflict',
      status: 409,
      code: 'Role.NameTaken',
      message: 'Trùng tên.',
      details: [],
      fieldErrors: {},
      correlationId: null,
    };

    service.saveResult = throwError(() => loi);

    const component = make();

    component['select']('r-ketoan');
    component['toggle']('department.read');
    component['save']();

    // Mất sạch thay đổi khi lưu hỏng là bắt người dùng tick lại từ đầu cho một lỗi
    // thường chỉ cần sửa cái tên.
    expect(component['dirty']()).toBe(true);
  });
});
