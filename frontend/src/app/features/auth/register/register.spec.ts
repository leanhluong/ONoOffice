import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed, type ComponentFixture } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it } from 'vitest';
import { AuthService } from '../../../core/auth/auth.service';
import type { AppError } from '../../../core/models/api-error.model';
import type {
  RegisterWorkspaceRequest,
  RegisterWorkspaceResponse,
} from '../../../core/models/auth.model';
import { Register } from './register';

/**
 * Kiểm màn đăng ký ở mức hành vi: gõ gì thì thấy gì, bấm gửi thì gọi gì.
 *
 * Không đụng tới HTTP thật — <c>AuthService</c> bị thay bằng bản giả. Thứ đang kiểm là
 * <b>luồng của màn hình</b>, không phải đường dây mạng: nó đã có bộ kiểm riêng ở
 * <c>RegisterWorkspaceFlowTests</c> chạy trên Postgres thật.
 */

const RESPONSE: RegisterWorkspaceResponse = {
  accessToken: 'access',
  refreshToken: 'refresh',
  expiresInSeconds: 900,
  user: {
    id: '11111111-1111-1111-1111-111111111111',
    tenantId: '22222222-2222-2222-2222-222222222222',
    email: 'chu@congty.com',
    fullName: 'Lê Anh Lượng',
  },
  workspace: {
    id: '22222222-2222-2222-2222-222222222222',
    code: 'acme',
    name: 'Công ty TNHH ACME',
  },
};

function conflict(code: string): AppError {
  return {
    kind: 'conflict',
    status: 409,
    code,
    message: 'Đã có người dùng.',
    details: [],
    fieldErrors: {},
    correlationId: null,
  };
}

/** Bản giả của AuthService: ghi lại lời gọi và trả về thứ mỗi test cần. */
class FakeAuthService {
  lastRequest: RegisterWorkspaceRequest | null = null;
  result: Observable<RegisterWorkspaceResponse> = of(RESPONSE);

  registerWorkspace(request: RegisterWorkspaceRequest): Observable<RegisterWorkspaceResponse> {
    this.lastRequest = request;
    return this.result;
  }
}

describe('Register', () => {
  let fixture: ComponentFixture<Register>;
  let auth: FakeAuthService;

  /** Điền đủ mọi ô cho biểu mẫu hợp lệ. Từng test chỉ sửa đúng thứ nó quan tâm. */
  function fillValidForm(): void {
    const form = fixture.componentInstance['form'];

    form.setValue({
      companyName: 'Công ty TNHH ACME',
      workspaceCode: 'acme',
      fullName: 'Lê Anh Lượng',
      email: 'chu@congty.com',
      password: 'con meo ngoi tren mai nha',
      terms: true,
    });
  }

  function html(): string {
    return (fixture.nativeElement as HTMLElement).innerHTML;
  }

  beforeEach(async () => {
    auth = new FakeAuthService();

    await TestBed.configureTestingModule({
      imports: [Register],
      providers: [
        provideZonelessChangeDetection(),

        // Không nạp file dịch: test này kiểm LUỒNG, không kiểm câu chữ. Thiếu bản dịch thì
        // ngx-translate trả về chính cái khoá, mà khoá cũng là một chuỗi khác rỗng nên mọi
        // khẳng định "có câu lỗi" vẫn đúng.
        provideTranslateService(),
        provideRouter([]),
        { provide: AuthService, useValue: auth },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Register);
    fixture.detectChanges();
  });

  it('gợi ý mã workspace từ tên công ty', () => {
    fixture.componentInstance['form'].controls.companyName.setValue('Công ty Đường Sắt');

    expect(fixture.componentInstance['form'].controls.workspaceCode.value).toBe(
      'cong-ty-duong-sat',
    );
  });

  it('thôi gợi ý sau khi người dùng tự sửa mã', () => {
    fixture.componentInstance['onCodeTyped']();
    fixture.componentInstance['form'].controls.workspaceCode.setValue('ten-toi-chon');
    fixture.componentInstance['form'].controls.companyName.setValue('Công ty Khác');

    expect(fixture.componentInstance['form'].controls.workspaceCode.value).toBe('ten-toi-chon');
  });

  it('chưa tick điều khoản thì KHÔNG gọi backend', () => {
    fillValidForm();
    fixture.componentInstance['form'].controls.terms.setValue(false);

    fixture.componentInstance['submit']();

    expect(auth.lastRequest).toBeNull();
  });

  it('gửi đúng năm trường backend cần, không kèm ô tick', () => {
    fillValidForm();

    fixture.componentInstance['submit']();

    // Gửi thừa `terms` thì backend từ chối cả request — hợp đồng của nó chỉ có năm trường.
    expect(auth.lastRequest).toEqual({
      companyName: 'Công ty TNHH ACME',
      workspaceCode: 'acme',
      fullName: 'Lê Anh Lượng',
      email: 'chu@congty.com',
      password: 'con meo ngoi tren mai nha',
    });
  });

  it('đăng ký xong thì hiện thẻ xác nhận với mã workspace vừa tạo', async () => {
    fillValidForm();
    fixture.componentInstance['submit']();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance['state']()).toBe('done');
    expect(html()).toContain('acme');
    expect(html()).toContain('chu@congty.com');
  });

  it('mã đã có người dùng thì tô đỏ đúng ô MÃ, không phải ô email', async () => {
    auth.result = throwError(() => conflict('TenantCode.Taken'));

    fillValidForm();
    fixture.componentInstance['submit']();
    await fixture.whenStable();

    expect(fixture.componentInstance['errorOf']('workspaceCode')).not.toBeNull();
    expect(fixture.componentInstance['errorOf']('email')).toBeNull();
  });

  it('email đã có tài khoản thì tô đỏ đúng ô EMAIL', async () => {
    auth.result = throwError(() => conflict('Email.Taken'));

    fillValidForm();
    fixture.componentInstance['submit']();
    await fixture.whenStable();

    expect(fixture.componentInstance['errorOf']('email')).not.toBeNull();
    expect(fixture.componentInstance['errorOf']('workspaceCode')).toBeNull();
  });

  it('sửa lại mã thì bỏ dấu đỏ — giữ nguyên là nói dối về trạng thái hiện tại', async () => {
    auth.result = throwError(() => conflict('TenantCode.Taken'));

    fillValidForm();
    fixture.componentInstance['submit']();
    await fixture.whenStable();

    fixture.componentInstance['onCodeTyped']();

    expect(fixture.componentInstance['errorOf']('workspaceCode')).toBeNull();
  });

  it('backend từ chối thì quay về trạng thái điền được, không kẹt ở "đang gửi"', async () => {
    auth.result = throwError(() => conflict('TenantCode.Taken'));

    fillValidForm();
    fixture.componentInstance['submit']();
    await fixture.whenStable();

    expect(fixture.componentInstance['state']()).toBe('idle');
  });
});
