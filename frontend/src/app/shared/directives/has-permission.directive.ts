import { Directive, effect, inject, input, TemplateRef, ViewContainerRef } from '@angular/core';
import { AuthStore } from '../../core/auth/auth.store';

/**
 * Ẩn/hiện một khối HTML theo permission.
 *
 *   <button *appHasPermission="'employee.create'">Thêm nhân viên</button>
 *   <a *appHasPermission="['employee.read', 'employee.write']">Nhân sự</a>
 *
 * Đây là bản song sinh của `permissionGuard` ở tầng template: guard chặn cả
 * route, directive này chỉ giấu từng nút. Cùng đọc chung một AuthStore nên
 * không sợ hai nơi hiểu quyền khác nhau.
 *
 * Dùng `effect` để khi phiên đổi (đăng xuất, refresh token đổi permission)
 * thì khối HTML tự xuất hiện/biến mất, không cần ai gọi lại.
 */
@Directive({
  selector: '[appHasPermission]',
})
export class HasPermissionDirective {
  private readonly store = inject(AuthStore);
  private readonly templateRef = inject(TemplateRef<unknown>);
  private readonly viewContainer = inject(ViewContainerRef);

  readonly appHasPermission = input.required<string | readonly string[]>();
  /** 'any' (mặc định): có một quyền là đủ. 'all': phải có đủ mọi quyền. */
  readonly appHasPermissionMode = input<'any' | 'all'>('any');

  private rendered = false;

  constructor() {
    effect(() => {
      const required = [this.appHasPermission()].flat();
      const allowed =
        this.appHasPermissionMode() === 'all'
          ? this.store.hasAllPermissions(required)
          : this.store.hasAnyPermission(required);

      if (allowed && !this.rendered) {
        this.viewContainer.createEmbeddedView(this.templateRef);
        this.rendered = true;
      } else if (!allowed && this.rendered) {
        this.viewContainer.clear();
        this.rendered = false;
      }
    });
  }
}
