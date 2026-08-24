import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
  type AbstractControl,
  type ValidationErrors,
} from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { notBlank } from '../../core/forms/validators';
import { ErrorMessageService } from '../../core/i18n/error-message.service';
import { isAppError, type AppError } from '../../core/models/api-error.model';
import type { MyProfile } from '../../core/models/user.model';
import { PopupService } from '../../core/ui/popup.service';
import { UserService } from '../../core/users/user.service';
import { Prefs } from '../../shared/ui/prefs/prefs';

/** Ba thẻ của màn — khớp bản dựng. */
type Tab = 'hoso' | 'baomat' | 'giaodien';

/** Độ dài tối thiểu, khớp `RegisterWorkspaceCommandValidator` ở backend. */
const MIN_PASSWORD_LENGTH = 10;

/**
 * Hai lần nhập mật khẩu mới phải khớp nhau.
 *
 * Kiểm ở cấp NHÓM chứ không ở từng ô: điều kiện này nói về quan hệ giữa hai ô, và một ô
 * đơn lẻ không biết gì về ô kia. Gắn vào ô thứ hai thì sửa ô thứ nhất sẽ không kích hoạt
 * kiểm lại — câu lỗi nằm lại trên màn hình dù người dùng đã sửa xong.
 */
function passwordsMatch(group: AbstractControl): ValidationErrors | null {
  const password = group.get('newPassword')?.value as string;
  const repeat = group.get('repeatPassword')?.value as string;

  return password === repeat ? null : { mismatch: true };
}

/**
 * Màn Hồ sơ & cài đặt — của CHÍNH người đang đăng nhập.
 *
 * Khác hẳn màn Nhân sự: ở đó quản trị viên sửa hồ sơ người khác, ở đây mỗi người sửa của
 * chính mình. Hai màn không gộp được vì thứ sửa được cũng khác nhau — không ai tự đổi vai
 * trò của mình, và không quản trị viên nào đổi mật khẩu hộ người khác.
 *
 * Nguồn thiết kế: `docs/07-giao-dien/identity/tai-khoan.html`.
 */
@Component({
  selector: 'app-account',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, TranslatePipe, Prefs],
  templateUrl: './account.html',
  styleUrl: './account.scss',
})
export class Account {
  private readonly users = inject(UserService);
  private readonly fb = inject(FormBuilder);
  private readonly popups = inject(PopupService);
  private readonly translate = inject(TranslateService);
  private readonly errorMessages = inject(ErrorMessageService);

  protected readonly tab = signal<Tab>('hoso');
  protected readonly profile = signal<MyProfile | null>(null);
  protected readonly saving = signal(false);

  /** Cột điều hướng thu gọn — lưu trên máy người dùng, không phải trên server. */
  protected readonly navCollapsed = signal(readCollapsed());

  protected readonly profileForm = this.fb.nonNullable.group({
    fullName: ['', [notBlank, Validators.maxLength(200)]],
  });

  protected readonly passwordForm = this.fb.nonNullable.group(
    {
      currentPassword: ['', [Validators.required]],
      newPassword: ['', [Validators.required, Validators.minLength(MIN_PASSWORD_LENGTH)]],
      repeatPassword: ['', [Validators.required]],
    },
    { validators: passwordsMatch },
  );

  constructor() {
    this.users.myProfile().subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.profileForm.setValue({ fullName: profile.fullName });

        // Tài khoản do quản trị viên tạo hộ: đưa thẳng tới thẻ Bảo mật thay vì bắt họ đi
        // tìm. Đây là lý do `mustChangePassword` có mặt trong phản hồi đăng nhập.
        if (profile.mustChangePassword) {
          this.tab.set('baomat');
          this.popups.show(this.translate.instant('account.mustChange') as string);
        }
      },
      error: (error: unknown) => this.showError(error),
    });
  }

  protected select(tab: Tab): void {
    this.tab.set(tab);
  }

  protected initials(name: string): string {
    const words = name.trim().split(/\s+/).filter(Boolean);

    return words.length === 0 ? '?' : (words[0][0] + words[words.length - 1][0]).toUpperCase();
  }

  // ── Hồ sơ ───────────────────────────────────────────────────────────

  protected saveProfile(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);

    // Cắt khoảng trắng hai đầu trước khi gửi: người dán tên từ chỗ khác gần như luôn
    // kèm một dấu cách, và "Lê Anh Lượng " với "Lê Anh Lượng" là hai chuỗi khác nhau khi
    // đem đi sắp xếp hay tìm kiếm.
    const fullName = this.profileForm.getRawValue().fullName.trim();

    this.users.updateMyProfile(fullName).subscribe({
      next: () => {
        this.saving.set(false);
        this.profile.update((current) => (current ? { ...current, fullName } : current));
        this.popups.show(this.translate.instant('account.saved') as string);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.showError(error);
      },
    });
  }

  protected nameError(): string | null {
    const control = this.profileForm.controls.fullName;

    if (!control.touched || !control.errors) {
      return null;
    }

    return this.translate.instant('account.validation.nameRequired') as string;
  }

  // ── Mật khẩu ────────────────────────────────────────────────────────

  protected changePassword(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);

    const { currentPassword, newPassword } = this.passwordForm.getRawValue();

    this.users.changeMyPassword({ currentPassword, newPassword }).subscribe({
      next: () => {
        this.saving.set(false);
        this.passwordForm.reset();
        this.profile.update((current) =>
          current ? { ...current, mustChangePassword: false } : current,
        );
        this.popups.show(this.translate.instant('account.passwordChanged') as string);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.showError(error);
      },
    });
  }

  /**
   * Câu lỗi cho hai ô mật khẩu mới.
   *
   * "Chưa khớp" gắn vào ô NHẬP LẠI dù nó là lỗi của cả nhóm: đó là ô người dùng vừa rời,
   * và là ô họ sẽ sửa. Báo ở ô thứ nhất thì mắt họ phải quay ngược lên.
   */
  protected passwordError(
    field: 'currentPassword' | 'newPassword' | 'repeatPassword',
  ): string | null {
    const control = this.passwordForm.controls[field];

    if (!control.touched) {
      return null;
    }

    if (control.errors?.['required']) {
      return this.translate.instant('account.validation.passwordRequired') as string;
    }

    if (control.errors?.['minlength']) {
      return this.translate.instant('account.validation.passwordTooShort') as string;
    }

    if (field === 'repeatPassword' && this.passwordForm.errors?.['mismatch']) {
      return this.translate.instant('account.validation.mismatch') as string;
    }

    return null;
  }

  // ── Giao diện ───────────────────────────────────────────────────────

  protected toggleNav(event: Event): void {
    const collapsed = (event.target as HTMLInputElement).checked;

    this.navCollapsed.set(collapsed);
    writeCollapsed(collapsed);

    // Khung ứng dụng nằm ở component cha, ngoài tầm với của signal này. Đổi lớp thẳng
    // trên phần tử là cách rẻ nhất; nhét một service dùng chung cho đúng một công tắc thì
    // đắt hơn thứ nó giải quyết.
    document.querySelector('.khung')?.classList.toggle('khung--gon', collapsed);
  }

  private showError(error: unknown): void {
    const appError: AppError = isAppError(error)
      ? error
      : {
          kind: 'unknown',
          status: 0,
          code: 'Client.Unexpected',
          message: '',
          details: [],
          fieldErrors: {},
          correlationId: null,
        };

    this.popups.error(
      this.errorMessages.resolve(appError),
      this.errorMessages.reference(appError) ?? undefined,
    );
  }
}

const COLLAPSED_KEY = 'onooffice.nav-collapsed';

function readCollapsed(): boolean {
  try {
    return localStorage.getItem(COLLAPSED_KEY) === '1';
  } catch {
    // Trình duyệt chặn lưu trữ (cửa sổ ẩn danh). Mặc định là không thu gọn.
    return false;
  }
}

function writeCollapsed(value: boolean): void {
  try {
    localStorage.setItem(COLLAPSED_KEY, value ? '1' : '0');
  } catch {
    // Không lưu được thì lần sau mở lại nó về mặc định — không ảnh hưởng chức năng.
  }
}
