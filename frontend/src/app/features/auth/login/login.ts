import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators, type AbstractControl } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../../core/auth/auth.service';
import { ErrorMessageService } from '../../../core/i18n/error-message.service';
import { isAppError, type AppError } from '../../../core/models/api-error.model';
import { PopupService } from '../../../core/ui/popup.service';
import { OrgWeave } from '../../../shared/ui/org-weave/org-weave';
import { PopupHost } from '../../../shared/ui/popup-host/popup-host';
import { Prefs } from '../../../shared/ui/prefs/prefs';

/**
 * Màn đăng nhập.
 *
 * Đây là màn DUY NHẤT ai cũng vào được mà chưa cần token, nên cũng là màn bị dò nhiều
 * nhất. Nguồn thiết kế: `docs/07-giao-dien/identity/dang-nhap.html` — mọi con số về giao
 * diện lấy từ đó, và `login.scss` được sinh tự động từ nó.
 */
@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe, OrgWeave, PopupHost, Prefs],
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
  private readonly popups = inject(PopupService);

  protected readonly submitting = signal(false);
  protected readonly showPassword = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],

    /**
     * CHỈ kiểm "không được rỗng".
     *
     * Cố ý không kiểm độ dài ở màn ĐĂNG NHẬP: mật khẩu đã tồn tại rồi, luật độ mạnh thuộc
     * về màn đăng ký. Kiểm ở đây thì người có mật khẩu cũ ngắn hơn luật mới không vào
     * được chính tài khoản của họ — và thông báo lỗi nói cho kẻ đang dò biết luật mật khẩu.
     */
    password: ['', [Validators.required]],

    /** CHƯA NỐI GÌ. Sẽ quyết định hạn refresh token dài/ngắn — xem dang-nhap.md. */
    remember: [true],
  });

  constructor() {
    // Bị đá về đây vì phiên hết hạn, chứ không phải tự bấm vào. Người đang làm dở mà bị
    // văng ra cần biết VÌ SAO, nếu không họ tưởng mình bấm nhầm hoặc app hỏng.
    if (this.route.snapshot.queryParamMap.get('lyDo') === 'het-phien') {
      this.popups.show(this.translate.instant('login.sessionExpired') as string);
    }
  }

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

    if (control.errors['required']) {
      return this.translate.instant(
        field === 'email' ? 'login.validation.emailRequired' : 'login.validation.passwordRequired',
      ) as string;
    }

    if (control.errors['email']) {
      return this.translate.instant('login.validation.emailInvalid') as string;
    }

    return null;
  }

  protected submit(): void {
    if (this.form.invalid) {
      // Đánh dấu touched hết để mọi lỗi hiện cùng lúc, thay vì người dùng sửa xong ô này
      // lại lòi ra lỗi ô khác.
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);

    const { email, password } = this.form.getRawValue();

    /**
     * CỐ Ý không khoá hai ô nhập khi đang gửi — chỉ khoá nút.
     *
     * Khoá cả form thì người vừa nhận ra mình gõ nhầm email phải ngồi chờ hết một vòng
     * mạng mới sửa được. Với hạ tầng miễn phí đang ngủ, vòng đó có thể là 30–60 giây.
     */
    this.auth.login({ email, password }).subscribe({
      next: () => {
        this.submitting.set(false);
        void this.router.navigateByUrl(this.returnUrl());
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.showError(error);
      },
    });
  }

  /** Nút chưa làm: một thông báo thoáng qua, không phải khối lỗi trong biểu mẫu. */
  protected notBuiltYet(event: Event, labelKey: string): void {
    event.preventDefault();

    const label = this.translate.instant(labelKey) as string;
    const suffix = this.translate.instant('login.comingSoon') as string;

    this.popups.show(`${label} — ${suffix}`);
  }

  /**
   * Quay lại trang người dùng định vào trước khi bị guard đá ra.
   *
   * CHỈ nhận đường dẫn nội bộ. Không chặn thì một link `?returnUrl=https://trang-gia-mao`
   * sẽ đưa người vừa đăng nhập sang trang của kẻ xấu, và họ tin nó vì vừa đăng nhập xong
   * ở trang thật.
   */
  private returnUrl(): string {
    const raw = this.route.snapshot.queryParamMap.get('returnUrl');

    if (raw && raw.startsWith('/') && !raw.startsWith('//') && !raw.startsWith('/login')) {
      return raw;
    }

    return '/dashboard';
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
