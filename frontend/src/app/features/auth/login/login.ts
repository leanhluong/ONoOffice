import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators, type AbstractControl } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../../core/auth/auth.service';
import { ErrorMessageService } from '../../../core/i18n/error-message.service';
import { isAppError, type AppError } from '../../../core/models/api-error.model';
import { Alert } from '../../../shared/ui/alert/alert';
import { ThemePicker } from '../../../shared/ui/theme-picker/theme-picker';

/**
 * Màn đăng nhập.
 *
 * Đây là màn DUY NHẤT ai cũng vào được mà chưa cần token, nên cũng là màn bị dò nhiều
 * nhất. Mọi lựa chọn dưới đây xoay quanh chuyện đó — xem
 * `docs/07-giao-dien/identity/dang-nhap.md`.
 *
 * Đã chạy thật với backend ngày 2026-08-24.
 */
@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, TranslatePipe, Alert, ThemePicker],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly errorMessages = inject(ErrorMessageService);
  private readonly translate = inject(TranslateService);

  protected readonly submitting = signal(false);
  protected readonly showPassword = signal(false);

  /** Lỗi chung hiện trên đầu form: sai mật khẩu, tài khoản bị khoá, mất mạng. */
  protected readonly formError = signal<AppError | null>(null);

  protected readonly errorText = computed(() => {
    const error = this.formError();
    return error === null ? null : this.errorMessages.resolve(error);
  });

  protected readonly errorReference = computed(() => {
    const error = this.formError();
    return error === null ? null : this.errorMessages.reference(error);
  });

  /**
   * Bị đá về đây vì phiên hết hạn, chứ không phải tự bấm vào.
   *
   * Phân biệt hai ca này là chuyện nhỏ nhưng thật: người đang làm dở mà bị văng ra cần
   * biết VÌ SAO, nếu không họ tưởng mình bấm nhầm hoặc app hỏng.
   */
  protected readonly sessionExpired = this.route.snapshot.queryParamMap.get('lyDo') === 'het-phien';

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],

    /**
     * CHỈ kiểm "không được rỗng".
     *
     * Cố ý không kiểm độ dài tối thiểu ở màn ĐĂNG NHẬP: mật khẩu đã tồn tại rồi, luật độ
     * mạnh thuộc về màn đăng ký và màn đổi mật khẩu. Kiểm ở đây thì người có mật khẩu cũ
     * ngắn hơn luật mới sẽ không đăng nhập được vào chính tài khoản của họ — và thông
     * báo lỗi sẽ nói cho kẻ đang dò biết luật mật khẩu của hệ thống.
     */
    password: ['', [Validators.required]],
  });

  protected get emailControl(): AbstractControl {
    return this.form.controls.email;
  }

  protected get passwordControl(): AbstractControl {
    return this.form.controls.password;
  }

  protected togglePassword(): void {
    this.showPassword.update((shown) => !shown);
  }

  /**
   * Câu lỗi cho một ô nhập. Chỉ hiện khi người dùng đã RỜI ô đó.
   *
   * Kiểm khi đang gõ là gào lên "email không hợp lệ" ngay từ chữ cái đầu tiên — đúng về
   * kỹ thuật, khó chịu về cảm giác.
   */
  protected errorFor(control: AbstractControl, field: 'email' | 'password'): string | null {
    if (!control.touched || !control.errors) {
      return null;
    }

    const errors = control.errors;

    if (errors['required']) {
      return this.translate.instant(
        field === 'email'
          ? 'login.validation.emailRequired'
          : 'login.validation.passwordRequired',
      ) as string;
    }

    if (errors['email']) {
      return this.translate.instant('login.validation.emailInvalid') as string;
    }

    return null;
  }

  protected submit(): void {
    this.formError.set(null);

    if (this.form.invalid) {
      // Đánh dấu touched hết để mọi lỗi hiện cùng lúc, thay vì người dùng sửa xong ô này
      // lại lòi ra lỗi ô khác.
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);

    /**
     * CỐ Ý không khoá hai ô nhập khi đang gửi — chỉ khoá nút.
     *
     * Khoá cả form thì người vừa nhận ra mình gõ nhầm email phải ngồi chờ hết một vòng
     * mạng mới sửa được. Với hạ tầng miễn phí đang ngủ, vòng đó có thể là 30–60 giây.
     */
    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.submitting.set(false);
        void this.router.navigateByUrl(this.returnUrl());
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.formError.set(this.toAppError(error));
      },
    });
  }

  /** Nút Google/Facebook: đã có chỗ, chưa có gì phía sau. Nói thẳng thay vì im lặng. */
  protected notBuiltYet(): void {
    this.formError.set({
      kind: 'unknown',
      status: 0,
      code: 'Client.NotBuiltYet',
      message: this.translate.instant('login.comingSoon') as string,
      details: [],
      fieldErrors: {},
      correlationId: null,
    });
  }

  /**
   * Quay lại trang người dùng định vào trước khi bị guard đá ra.
   *
   * CHỈ nhận đường dẫn nội bộ. Không chặn thì một link
   * `?returnUrl=https://trang-gia-mao` sẽ đưa người vừa đăng nhập sang trang của kẻ xấu,
   * và họ tin nó vì vừa đăng nhập xong ở trang thật.
   */
  private returnUrl(): string {
    const raw = this.route.snapshot.queryParamMap.get('returnUrl');

    if (raw && raw.startsWith('/') && !raw.startsWith('//') && !raw.startsWith('/login')) {
      return raw;
    }

    return '/dashboard';
  }

  private toAppError(error: unknown): AppError {
    if (isAppError(error)) {
      return error;
    }

    return {
      kind: 'unknown',
      status: 0,
      code: 'Client.Unexpected',
      message: '',
      details: [],
      fieldErrors: {},
      correlationId: null,
    };
  }
}
