import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed, type ComponentFixture } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it } from 'vitest';
import { AuthStore } from '../../core/auth/auth.store';
import type { AuthUser } from '../../core/models/auth.model';
import type { MemberListItem } from '../../core/models/org.model';
import { OrgService } from '../../core/org/org.service';
import { Dashboard } from './dashboard';

/**
 * Bảng điều khiển — màn đầu tiên sau khi đăng nhập.
 *
 * Thứ đáng kiểm nhất ở đây là <b>những gì màn hình KHÔNG hiện</b>. Bảng điều khiển là chỗ
 * cám dỗ nhất để bịa số, và ba luật dưới đây giữ nó khỏi nói dối:
 *
 * <list type="bullet">
 * <item>Không có quyền thì khối "cần xử lý" KHÔNG TỒN TẠI — không phải hiện số 0.</item>
 * <item>Không đếm được thì không hiện, chứ không hiện 0.</item>
 * <item>Mỗi con số là một liên kết đi tới danh sách đã lọc sẵn.</item>
 * </list>
 */

function member(over: Partial<MemberListItem> = {}): MemberListItem {
  return {
    employeeId: crypto.randomUUID(),
    userId: crypto.randomUUID(),
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

class FakeOrgService {
  loads = 0;
  result: Observable<MemberListItem[]> = of([member()]);

  members(): Observable<MemberListItem[]> {
    this.loads++;

    return this.result;
  }
}

describe('Dashboard', () => {
  let fixture: ComponentFixture<Dashboard>;
  let org: FakeOrgService;

  const user = signal<AuthUser | null>({
    userId: 'u-toi',
    tenantId: 't-acme',
    email: 'toi@congty.vn',
    displayName: 'Lê Anh Lượng',
  });

  const quyen = signal<readonly string[]>(['user.read', 'employee.read']);

  function make(): Dashboard {
    fixture = TestBed.createComponent(Dashboard);
    fixture.detectChanges();

    return fixture.componentInstance;
  }

  beforeEach(() => {
    org = new FakeOrgService();
    quyen.set(['user.read', 'employee.read']);

    TestBed.configureTestingModule({
      imports: [Dashboard],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideTranslateService(),
        { provide: OrgService, useValue: org },
        {
          provide: AuthStore,
          useValue: {
            user,
            hasPermission: (p: string) => quyen().includes(p),
          },
        },
      ],
    });
  });

  /**
   * Người chỉ có `employee.read` KHÔNG gọi `/api/members`.
   *
   * Endpoint đó đòi CẢ HAI quyền (`user.read` và `employee.read`), nên gọi nó với vai
   * Member là một request chắc chắn nhận 403 — và một popup lỗi đỏ ngay khi vừa đăng nhập
   * là ấn tượng đầu tiên tệ nhất có thể tạo ra.
   */
  it('không đủ quyền thì KHÔNG gọi danh sách người', () => {
    quyen.set(['employee.read']);

    const component = make();

    expect(org.loads).toBe(0);
    expect(component['showTasks']()).toBe(false);
  });

  it('đủ quyền thì đếm đúng ba loại việc', () => {
    org.result = of([
      member({ mustChangePassword: true }),
      member({ mustChangePassword: true }),
      member({ userId: null, roleName: null }),
      member({ employeeId: null, code: null }),
      member(),
    ]);

    const component = make();

    expect(component['showTasks']()).toBe(true);
    expect(component['pendingPassword']()).toBe(2);
    expect(component['noAccount']()).toBe(1);
    expect(component['noProfile']()).toBe(1);
  });

  /**
   * Người đã nghỉ / tài khoản đã tắt KHÔNG tính vào việc cần xử lý.
   *
   * "Chưa có tài khoản" chỉ là việc phải làm với người còn đang làm. Đếm cả người đã nghỉ
   * thì con số phình lên vì những dòng không ai định xử lý, và thẻ đó mất hết ý nghĩa.
   */
  it('bỏ qua người đã nghỉ khi đếm việc', () => {
    org.result = of([
      member({ userId: null, roleName: null, isActive: false }),
      member({ userId: null, roleName: null }),
    ]);

    expect(make()['noAccount']()).toBe(1);
  });

  /**
   * Gọi hỏng thì khối "cần xử lý" biến mất, KHÔNG hiện ba số 0.
   *
   * Ba số 0 là một câu trả lời SAI — "workspace của bạn không có việc gì" — chứ không phải
   * một câu trả lời thiếu.
   */
  it('gọi hỏng thì giấu hẳn khối việc, không hiện số 0', () => {
    org.result = throwError(() => new Error('500'));

    const component = make();

    expect(component['showTasks']()).toBe(false);
  });

  /**
   * Workspace chỉ có một người thì hiện lời dẫn "bắt đầu".
   *
   * Công ty vừa đăng ký thì mọi con số đều bằng 0, và một màn hình rỗng không nói cho họ
   * biết việc tiếp theo là gì.
   */
  it('workspace một người thì hiện lời dẫn bắt đầu', () => {
    org.result = of([member({ userId: 'u-toi' })]);

    expect(make()['isNewWorkspace']()).toBe(true);
  });

  it('workspace có nhiều người thì KHÔNG hiện lời dẫn bắt đầu', () => {
    org.result = of([member(), member()]);

    expect(make()['isNewWorkspace']()).toBe(false);
  });

  /**
   * Lối tắt chỉ hiện những màn người đó VÀO ĐƯỢC.
   *
   * Một thẻ dẫn tới màn sẽ trả về "bạn không có quyền" là mời người dùng đi vào ngõ cụt.
   */
  it('lối tắt lọc theo quyền', () => {
    quyen.set(['employee.read']);

    const codes = make()['shortcuts']().map((s) => s.ma);

    expect(codes).toContain('contacts');
    expect(codes).not.toContain('members');
  });
});
