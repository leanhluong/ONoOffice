import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../../core/auth/auth.service';
import { ErrorMessageService } from '../../../core/i18n/error-message.service';
import { isAppError, type AppError } from '../../../core/models/api-error.model';
import type { RegisterWorkspaceResponse } from '../../../core/models/auth.model';
import { PopupService } from '../../../core/ui/popup.service';
import { OrgWeave } from '../../../shared/ui/org-weave/org-weave';
import { PopupHost } from '../../../shared/ui/popup-host/popup-host';
import { Prefs } from '../../../shared/ui/prefs/prefs';
import { Tip } from '../../../shared/ui/tip/tip';
import {
  PASSWORD_MIN_LENGTH,
  WORKSPACE_CODE_MAX_LENGTH,
  WORKSPACE_CODE_MIN_LENGTH,
  WORKSPACE_CODE_PATTERN,
  passwordStrength,
  suggestWorkspaceCode,
} from './register.util';

/** Ba trạng thái của biểu mẫu — khớp `[data-state]` trong bản dựng. */
type FormState = 'idle' | 'sending' | 'done';

/** Tên các ô nhập, dùng cho `errorOf`. */
type FieldName = 'companyName' | 'workspaceCode' | 'fullName' | 'email' | 'password';

/**
 * Màn đăng ký workspace.
 *
 * Một lần bấm tạo ra <b>ba thứ</b>: công ty, bốn vai trò hệ thống, và tài khoản chủ sở
 * hữu. Backend lo cả ba trong một transaction — phía này chỉ cần gửi đúng và hiện đúng.
 *
 * Nguồn thiết kế: `docs/07-giao-dien/identity/dang-ky.html`. `register.scss` sinh tự động
 * từ chính file đó; đánh dấu thì chép tay và được `npm run parity` canh bằng ảnh chụp.
 */
@Component({
  selector: 'app-register',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe, OrgWeave, PopupHost, Prefs, Tip],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class Register {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);
  private readonly errorMessages = inject(ErrorMessageService);
  private readonly popups = inject(PopupService);

  protected readonly state = signal<FormState>('idle');
  protected readonly showPassword = signal(false);

  /** Phản hồi của lần đăng ký thành công — nguồn duy nhất cho thẻ xác nhận. */
  protected readonly created = signal<RegisterWorkspaceResponse | null>(null);

  /**
   * Lỗi do BACKEND từ chối, gắn vào đúng ô gây ra nó.
   *
   * Hai lỗi duy nhất rơi vào đây là "mã đã có người dùng" và "email đã có tài khoản" —
   * cả hai chỉ biết được sau khi hỏi database, nên kiểm ở phía này là bất khả. Ngoài
   * thông báo nổi, ta còn tô đỏ đúng ô: người dùng cần biết phải sửa Ô NÀO, chứ không chỉ
   * biết là có gì đó sai.
   */
  private readonly rejected = signal<{ field: FieldName; message: string } | null>(null);

  /** Người dùng đã tự sửa mã thì thôi gợi ý đè lên. */
  private codeTouchedByUser = false;

  protected readonly form = this.fb.nonNullable.group({
    companyName: ['', [Validators.required, Validators.maxLength(200)]],

    workspaceCode: [
      '',
      [
        Validators.required,
        Validators.minLength(WORKSPACE_CODE_MIN_LENGTH),
        Validators.maxLength(WORKSPACE_CODE_MAX_LENGTH),

        // Cùng một luật với `TenantCode.Create` ở backend. Chép luật là một chỗ dễ lệch,
        // nên nó nằm trong bộ kiểm hợp đồng — hỏng thì test đỏ chứ không âm thầm.
        Validators.pattern(WORKSPACE_CODE_PATTERN),
      ],
    ],

    fullName: ['', [Validators.required, Validators.maxLength(200)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(PASSWORD_MIN_LENGTH)]],

    // `requiredTrue` chứ không phải `required`: `false` vẫn là một giá trị có mặt.
    terms: [false, [Validators.requiredTrue]],
  });

  /** Điểm độ mạnh mật khẩu, đọc từ giá trị đang gõ. */
  protected readonly strength = computed(() => passwordStrength(this.passwordValue()));

  protected readonly strengthLabel = computed(() => {
    const keys = [
      '',
      'register.strength.weak',
      'register.strength.fair',
      'register.strength.good',
      'register.strength.strong',
    ];
    const key = keys[this.strength()];

    return key ? (this.translate.instant(key) as string) : '';
  });

  /**
   * Giá trị mật khẩu dưới dạng signal.
   *
   * Reactive forms chưa phát signal, nên phải bắc cầu bằng một signal tự cập nhật từ
   * `valueChanges`. Không có nó thì thanh đo độ mạnh đứng im dưới `OnPush`.
   */
  private readonly passwordValue = signal('');

  constructor() {
    this.form.controls.password.valueChanges.subscribe((value) => this.passwordValue.set(value));

    // Gợi ý mã từ tên công ty — cho tới khi người dùng tự gõ mã.
    this.form.controls.companyName.valueChanges.subscribe((name) => {
      if (!this.codeTouchedByUser) {
        this.form.controls.workspaceCode.setValue(suggestWorkspaceCode(name), {
          emitEvent: false,
        });
      }
    });
  }

  protected onCodeTyped(): void {
    // Ghi đè lên thứ người dùng vừa gõ là kiểu khó chịu ai cũng từng gặp ở form đăng ký.
    this.codeTouchedByUser = true;
    this.clearRejection('workspaceCode');
  }

  protected togglePassword(): void {
    this.showPassword.update((shown) => !shown);
  }

  /**
   * Câu lỗi của một ô. Chỉ hiện sau khi người dùng đã RỜI ô đó — kiểm khi đang gõ là gào
   * lên "email không hợp lệ" ngay từ chữ cái đầu tiên.
   *
   * Lỗi do backend từ chối thì hiện ngay, không cần chạm: nó đến SAU khi người dùng đã
   * bấm gửi, tức là họ đã rời hết mọi ô rồi.
   */
  protected errorOf(field: FieldName): string | null {
    const rejection = this.rejected();

    if (rejection?.field === field) {
      return rejection.message;
    }

    const control = this.form.controls[field];

    if (!control.touched || !control.errors) {
      return null;
    }

    return this.translate.instant(this.messageKeyFor(field, control.errors)) as string;
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();

      // Ô tick điều khoản không có chỗ nào hiện câu lỗi — nó nằm ngoài lưới `.field`.
      // Không nói gì thì người dùng bấm nút mà không có chuyện gì xảy ra, và họ tưởng hỏng.
      if (this.form.controls.terms.invalid) {
        this.popups.error(this.translate.instant('register.validation.termsRequired') as string);
      }

      return;
    }

    this.state.set('sending');
    this.rejected.set(null);

    const { companyName, workspaceCode, fullName, email, password } = this.form.getRawValue();

    this.auth
      .registerWorkspace({ companyName, workspaceCode, fullName, email, password })
      .subscribe({
        next: (response) => {
          this.created.set(response);
          this.state.set('done');
        },
        error: (error: unknown) => {
          this.state.set('idle');
          this.showError(error);
        },
      });
  }

  /**
   * Vào workspace vừa tạo.
   *
   * Phiên đã mở ngay lúc đăng ký xong (backend trả kèm cặp token), nên đây chỉ là chuyển
   * trang — không phải một lần đăng nhập nữa.
   */
  protected enterWorkspace(): void {
    void this.router.navigateByUrl('/dashboard');
  }

  protected notBuiltYet(event: Event, labelKey: string): void {
    event.preventDefault();

    const label = this.translate.instant(labelKey) as string;
    const suffix = this.translate.instant('register.comingSoon') as string;

    this.popups.show(`${label} — ${suffix}`);
  }

  private messageKeyFor(field: FieldName, errors: Record<string, unknown>): string {
    if (errors['required']) {
      return `register.validation.${field}Required`;
    }

    if (field === 'email') {
      return 'register.validation.emailInvalid';
    }

    if (field === 'password') {
      return 'register.validation.passwordTooShort';
    }

    if (field === 'workspaceCode') {
      // Sai độ dài và sai ký tự là hai chuyện khác nhau, và cách sửa cũng khác nhau.
      return errors['pattern']
        ? 'register.validation.codeInvalid'
        : 'register.validation.codeLength';
    }

    return `register.validation.${field}TooLong`;
  }

  /** Sửa lại ô đã bị từ chối thì bỏ dấu đỏ — giữ nguyên là nói dối về trạng thái hiện tại. */
  private clearRejection(field: FieldName): void {
    if (this.rejected()?.field === field) {
      this.rejected.set(null);
    }
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

    const text = this.errorMessages.resolve(appError);

    // Hai mã này backend chỉ trả về khi đã hỏi database xong — gắn được vào đúng ô.
    const field: FieldName | null =
      appError.code === 'TenantCode.Taken'
        ? 'workspaceCode'
        : appError.code === 'Email.Taken'
          ? 'email'
          : null;

    if (field) {
      this.rejected.set({ field, message: text });
      this.form.controls[field].markAsTouched();
    }

    this.popups.error(text, this.errorMessages.reference(appError) ?? undefined);
  }
}
