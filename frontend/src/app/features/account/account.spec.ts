import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it } from 'vitest';
import type { AppError } from '../../core/models/api-error.model';
import type { ChangePasswordRequest, MyProfile } from '../../core/models/user.model';
import { UserService } from '../../core/users/user.service';
import { Account } from './account';

/**
 * Màn Hồ sơ & cài đặt ở mức hành vi.
 *
 * Thứ đáng kiểm là những chỗ màn hình tự quyết định: hai lần nhập mật khẩu có khớp không,
 * và người dùng đang cầm mật khẩu tạm thì có được đưa thẳng tới chỗ đổi hay không.
 */

const PROFILE: MyProfile = {
  id: 'u-1',
  tenantId: 't-1',
  email: 'chu@congty.vn',
  fullName: 'Lê Anh Lượng',
  roleName: 'Owner',
  isOwner: true,
  mustChangePassword: false,
};

class FakeUserService {
  profile: MyProfile = PROFILE;
  savedName: string | null = null;
  changedWith: ChangePasswordRequest | null = null;
  changeResult: Observable<void> = of(undefined);

  myProfile(): Observable<MyProfile> {
    return of(this.profile);
  }

  updateMyProfile(fullName: string): Observable<void> {
    this.savedName = fullName;

    return of(undefined);
  }

  changeMyPassword(request: ChangePasswordRequest): Observable<void> {
    this.changedWith = request;

    return this.changeResult;
  }
}

describe('Account', () => {
  let service: FakeUserService;

  function make(): Account {
    const fixture = TestBed.createComponent(Account);

    fixture.detectChanges();

    return fixture.componentInstance;
  }

  beforeEach(() => {
    service = new FakeUserService();

    TestBed.configureTestingModule({
      imports: [Account],
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService(),
        { provide: UserService, useValue: service },
      ],
    });
  });

  // ── Hồ sơ ─────────────────────────────────────────────────────────

  it('mở màn là điền sẵn tên hiện tại', () => {
    expect(make()['profileForm'].getRawValue()).toEqual({ fullName: 'Lê Anh Lượng' });
  });

  it('lưu tên rỗng thì KHÔNG gọi backend', () => {
    const component = make();

    component['profileForm'].setValue({ fullName: '   ' });
    component['saveProfile']();

    // Khoảng trắng vẫn qua được `required` của Angular, nên nếu handler không chặn thì
    // người dùng lưu được một cái tên rỗng và biến mất khỏi mọi danh sách.
    expect(service.savedName).toBeNull();
  });

  it('lưu xong thì tên mới hiện ngay, không chờ nạp lại', () => {
    const component = make();

    component['profileForm'].setValue({ fullName: 'Lê Anh Lượng B' });
    component['saveProfile']();

    expect(service.savedName).toBe('Lê Anh Lượng B');
    expect(component['profile']()!.fullName).toBe('Lê Anh Lượng B');
  });

  // ── Mật khẩu ──────────────────────────────────────────────────────

  it('hai lần nhập KHÔNG khớp thì không gọi backend', () => {
    const component = make();

    component['passwordForm'].setValue({
      currentPassword: 'mat-khau-hien-tai',
      newPassword: 'mot-cau-de-nho',
      repeatPassword: 'mot-cau-khac-han',
    });

    component['changePassword']();

    expect(service.changedWith).toBeNull();
  });

  it('sửa ô THỨ NHẤT thì lỗi "chưa khớp" cũng biến mất', () => {
    // Đây là lý do luật này gắn ở cấp NHÓM chứ không ở một ô. Gắn vào ô thứ hai thì sửa ô
    // thứ nhất không kích hoạt kiểm lại, và câu lỗi nằm lại dù người dùng đã sửa xong.
    const component = make();
    const form = component['passwordForm'];

    form.setValue({
      currentPassword: 'x'.repeat(12),
      newPassword: 'mot-cau-de-nho',
      repeatPassword: 'mot-cau-khac-han',
    });

    expect(form.errors?.['mismatch']).toBe(true);

    form.controls.newPassword.setValue('mot-cau-khac-han');

    expect(form.errors).toBeNull();
  });

  it('mật khẩu mới ngắn hơn 10 ký tự thì không gọi backend', () => {
    const component = make();

    component['passwordForm'].setValue({
      currentPassword: 'mat-khau-hien-tai',
      newPassword: 'ngan',
      repeatPassword: 'ngan',
    });

    component['changePassword']();

    expect(service.changedWith).toBeNull();
  });

  it('đổi xong thì XOÁ SẠCH biểu mẫu', () => {
    // Để nguyên thì mật khẩu vừa đặt nằm hiển nhiên trên màn hình của một máy có thể đang
    // mở giữa văn phòng.
    const component = make();

    component['passwordForm'].setValue({
      currentPassword: 'mat-khau-hien-tai',
      newPassword: 'mot-cau-rat-de-nho',
      repeatPassword: 'mot-cau-rat-de-nho',
    });

    component['changePassword']();

    expect(service.changedWith).toEqual({
      currentPassword: 'mat-khau-hien-tai',
      newPassword: 'mot-cau-rat-de-nho',
    });

    expect(component['passwordForm'].getRawValue().newPassword).toBe('');
  });

  it('đổi HỎNG thì GIỮ NGUYÊN biểu mẫu', () => {
    // Xoá sạch khi lỗi thì người gõ sai mật khẩu hiện tại phải gõ lại cả ba ô.
    const wrong: AppError = {
      kind: 'validation',
      status: 400,
      code: 'User.WrongCurrentPassword',
      message: 'Mật khẩu hiện tại không đúng.',
      details: [],
      fieldErrors: {},
      correlationId: null,
    };

    service.changeResult = throwError(() => wrong);

    const component = make();

    component['passwordForm'].setValue({
      currentPassword: 'doan-bua-cho-vui',
      newPassword: 'mot-cau-rat-de-nho',
      repeatPassword: 'mot-cau-rat-de-nho',
    });

    component['changePassword']();

    expect(component['passwordForm'].getRawValue().newPassword).toBe('mot-cau-rat-de-nho');
  });

  // ── Mật khẩu tạm ──────────────────────────────────────────────────

  it('người đang dùng mật khẩu tạm được đưa THẲNG tới thẻ Bảo mật', () => {
    // Đây là lý do `mustChangePassword` có mặt trong phản hồi đăng nhập. Bắt họ tự đi tìm
    // chỗ đổi thì phần lớn sẽ không đổi.
    service.profile = { ...PROFILE, mustChangePassword: true };

    expect(make()['tab']()).toBe('baomat');
  });

  it('người bình thường vào thẳng thẻ Hồ sơ', () => {
    expect(make()['tab']()).toBe('hoso');
  });
});
