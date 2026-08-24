import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Trang giữ chỗ cho module Nhân sự.
 *
 * Lý do tồn tại lúc này: nó là ví dụ chạy thật của `permissionGuard`.
 * Route trỏ tới đây yêu cầu quyền `employee.read`; ai không có sẽ bị đưa
 * sang `/forbidden`. Không có nó thì guard chỉ nằm đó chứ chẳng ai kiểm
 * chứng được là nó hoạt động.
 */
@Component({
  selector: 'app-employee-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="placeholder">
      <h1>Nhân sự</h1>
      <p>
        Route này được bảo vệ bằng <code>permissionGuard('employee.read')</code>. Danh sách nhân
        viên sẽ hiển thị ở đây khi backend có endpoint tương ứng.
      </p>
    </section>
  `,
  styles: `
    .placeholder {
      max-width: 40rem;

      h1 {
        margin: 0 0 0.5rem;
        font-size: 1.35rem;
      }

      p {
        margin: 0;
        color: var(--ink-soft);
        font-size: 0.9rem;
        line-height: 1.6;
      }
    }
  `,
})
export class EmployeeList {}
