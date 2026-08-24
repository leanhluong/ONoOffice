import type { AbstractControl, ValidationErrors } from '@angular/forms';

/**
 * Bắt buộc có nội dung THẬT, không phải chỉ có khoảng trắng.
 *
 * <b>Vì sao không dùng <c>Validators.required</c>:</b> nó chỉ xem chuỗi có rỗng hay không,
 * và <c>'   '</c> thì không rỗng. Người dùng gõ ba dấu cách vào ô Họ tên là qua được, rồi
 * họ biến mất khỏi mọi danh sách vì tên hiển thị là khoảng trắng.
 *
 * Backend cũng chặn (<c>User.FullNameEmpty</c>), nhưng để nó chặn nghĩa là người dùng phải
 * chờ hết một vòng mạng mới biết mình gõ sai — và nhận về một câu lỗi chung thay vì một ô
 * đỏ ngay chỗ cần sửa.
 *
 * Trả về <c>{ required: true }</c> chứ không đặt tên khoá mới: mọi chỗ hiển thị lỗi đã
 * hiểu khoá đó rồi, và với người dùng thì "chưa nhập" và "chỉ nhập khoảng trắng" là cùng
 * một chuyện.
 */
export function notBlank(control: AbstractControl): ValidationErrors | null {
  const value = control.value as unknown;

  if (typeof value !== 'string') {
    return value === null || value === undefined ? { required: true } : null;
  }

  return value.trim().length > 0 ? null : { required: true };
}
