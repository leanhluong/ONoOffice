import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators, type AbstractControl } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { isAppError, type AppError } from '../../../core/models/api-error.model';
import { Alert } from '../../../shared/ui/alert/alert';

/**
 * Màn đăng nhập.
 *
 * Vì sao reactive form chứ không template-driven: form này cần nhận thêm lỗi
 * từ server gắn vào từng ô (`setErrors({ server: '...' })`), cần khoá toàn bộ
 * form khi đang gửi, và cần kiểm tra trạng thái trong code. Template-driven
 * làm được nhưng phải luồn `@ViewChild` lòng vòng.
 *
 * !!! CHƯA GỌI ĐƯỢC API THẬT: backend chưa có `POST /api/auth/login`.
 * Luồng ở đây đã nối đầy đủ tới `AuthService.login()`, nhưng chưa ai chạy thử
 * với server thật. Khi backend lên, phải kiểm lại hai điểm:
 *   1. Sai mật khẩu trả mã lỗi gì → đối chiếu với `mapServerError` bên dưới.
 *   2. Access token có đủ claim `sub`/`tenant_id`/`permission` chưa.
 */
@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, Alert],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly submitting = signal(false);
  /** Lỗi chung hiển thị trên đầu form (sai mật khẩu, mất mạng, server lỗi...). */
  protected readonly formError = signal<AppError | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  protected get emailControl(): AbstractControl {
    return this.form.controls.email;
  }

  protected get passwordControl(): AbstractControl {
    return this.form.controls.password;
  }

  /**
   * Sinh câu lỗi cho một ô nhập.
   * Chỉ hiện khi người dùng đã chạm vào ô đó (`touched`) — hiện lỗi ngay lúc
   * form vừa mở, khi họ còn chưa gõ chữ nào, là kiểu trải nghiệm khó chịu.
   */
  protected errorFor(control: AbstractControl, field: 'email' | 'password'): string | null {
    if (!control.touched || !control.errors) {
      return null;
    }
    const errors = control.errors;
    if (errors['server']) {
      return String(errors['server']);
    }
    if (errors['required']) {
      return field === 'email' ? 'Vui lòng nhập email.' : 'Vui lòng nhập mật khẩu.';
    }
    if (errors['email']) {
      return 'Email không đúng định dạng.';
    }
    if (errors['minlength']) {
      return 'Mật khẩu phải có ít nhất 6 ký tự.';
    }
    return 'Giá trị không hợp lệ.';
  }

  protected submit(): void {
    this.formError.set(null);

    if (this.form.invalid) {
      // Đánh dấu touched hết để mọi lỗi hiện ra cùng lúc, thay vì người dùng
      // sửa xong ô này lại lòi ra lỗi ô khác.
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.form.disable({ emitEvent: false });

    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.submitting.set(false);
        void this.router.navigateByUrl(this.returnUrl());
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.form.enable({ emitEvent: false });
        this.handleError(error);
      },
    });
  }

  /** Quay lại trang người dùng định vào trước khi bị guard đá ra. */
  private returnUrl(): string {
    const raw = this.route.snapshot.queryParamMap.get('returnUrl');
    // Chỉ nhận đường dẫn nội bộ. Nếu không chặn, kẻ xấu có thể gửi link
    // `?returnUrl=https://site-gia-mao` để lừa chuyển hướng sau khi đăng nhập.
    if (raw && raw.startsWith('/') && !raw.startsWith('//')) {
      return raw;
    }
    return '/dashboard';
  }

  private handleError(error: unknown): void {
    if (!isAppError(error)) {
      this.formError.set({
        kind: 'unknown',
        status: 0,
        code: 'Client.Unexpected',
        message: 'Đã có lỗi không mong muốn. Vui lòng thử lại.',
        details: [],
        fieldErrors: {},
        correlationId: null,
      });
      return;
    }

    this.applyFieldErrors(error);
    this.formError.set(error);
  }

  /**
   * Gắn lỗi do server trả về vào đúng ô nhập.
   * Ưu tiên `fieldErrors` (dictionary ModelState); nếu không có thì đoán theo
   * mã lỗi nghiệp vụ — backend dùng mã có cấu trúc `Namespace.Reason` nên
   * đoán được khá chắc chắn.
   */
  private applyFieldErrors(error: AppError): void {
    const emailMessages = error.fieldErrors['email'];
    const passwordMessages = error.fieldErrors['password'];

    if (emailMessages?.[0]) {
      this.setServerError(this.emailControl, emailMessages[0]);
    }
    if (passwordMessages?.[0]) {
      this.setServerError(this.passwordControl, passwordMessages[0]);
    }

    if (!emailMessages && !passwordMessages) {
      this.mapServerError(error);
    }
  }

  private mapServerError(error: AppError): void {
    switch (error.code) {
      case 'Auth.EmailNotFound':
        this.setServerError(this.emailControl, error.message);
        break;
      case 'Auth.WrongPassword':
        this.setServerError(this.passwordControl, error.message);
        break;
      default:
        // Cố ý KHÔNG tách "sai email" với "sai mật khẩu" ở nhánh mặc định:
        // để lộ email nào tồn tại là giúp kẻ xấu dò danh sách tài khoản.
        break;
    }
  }

  private setServerError(control: AbstractControl, message: string): void {
    control.setErrors({ ...(control.errors ?? {}), server: message });
    control.markAsTouched();
  }
}
