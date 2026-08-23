import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AuthStore } from '../../core/auth/auth.store';

/**
 * Màn hình tạm sau khi đăng nhập.
 *
 * Hiện tại nó đóng vai trò trang chẩn đoán: đọc thẳng những gì AuthStore đang
 * giữ (user, tenant, danh sách permission). Nhìn vào đây là biết ngay token
 * backend cấp đã đủ claim chưa — nhanh hơn mở DevTools giải mã JWT bằng tay.
 * Khi có nội dung nghiệp vụ thật thì thay phần thân, giữ lại khối permission
 * cũng không hại gì (chỉ người đang đăng nhập thấy dữ liệu của chính họ).
 */
@Component({
  selector: 'app-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  protected readonly store = inject(AuthStore);
}
