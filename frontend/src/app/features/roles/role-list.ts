import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ErrorMessageService } from '../../core/i18n/error-message.service';
import { isAppError, type AppError } from '../../core/models/api-error.model';
import type { RoleListItem } from '../../core/models/user.model';
import { PopupService } from '../../core/ui/popup.service';
import { UserService } from '../../core/users/user.service';
import {
  PERMISSION_CATALOG,
  PERMISSION_GROUPS,
  permissionsByGroup,
  type PermissionGroup,
  type PermissionInfo,
} from './permission-catalog';

/** Một dòng quyền trong bảng, đã gắn sẵn nhãn và trạng thái bật/tắt. */
interface PermissionRow {
  readonly code: string;
  readonly info: PermissionInfo;
  readonly granted: boolean;
}

/** Một khối quyền trên màn hình. */
interface PermissionBlock {
  readonly group: PermissionGroup;
  readonly rows: readonly PermissionRow[];
  readonly grantedCount: number;
}

/**
 * Màn Vai trò & quyền.
 *
 * <b>CHỈ XEM.</b> Backend mới có <c>GET /api/roles</c>; chưa có endpoint tạo, đổi tên hay
 * sửa quyền. Vẽ sẵn nút "Tạo vai trò" rồi báo "đang phát triển" thì người dùng bấm vào mới
 * biết — thà đừng vẽ.
 *
 * Luật nền của cả hệ thống: <b>quyền đi theo VAI TRÒ, không gán lẻ cho từng người</b>
 * (ADR-0002). Gán lẻ thì sau một năm không ai trả lời nổi câu "vì sao người này xem được
 * bảng lương" — phải mở từng tài khoản ra dò.
 *
 * Nguồn thiết kế: `docs/07-giao-dien/identity/vai-tro.html`.
 */
@Component({
  selector: 'app-role-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslatePipe],
  templateUrl: './role-list.html',
  styleUrl: './role-list.scss',
})
export class RoleList {
  private readonly users = inject(UserService);
  private readonly popups = inject(PopupService);
  private readonly errorMessages = inject(ErrorMessageService);

  protected readonly roles = signal<readonly RoleListItem[]>([]);
  protected readonly selectedId = signal<string | null>(null);

  protected readonly selected = computed(
    () => this.roles().find((role) => role.id === this.selectedId()) ?? null,
  );

  /**
   * Bảng quyền của vai đang chọn: MỌI quyền của hệ thống, đánh dấu cái nào vai này có.
   *
   * Chỉ liệt kê quyền vai đó CÓ thì màn hình trả lời được câu "vai này làm được gì", nhưng
   * không trả lời được câu quan trọng hơn: "vai này KHÔNG làm được gì" — mà đó mới là thứ
   * người ta cần khi quyết định trao vai cho ai.
   */
  protected readonly blocks = computed<readonly PermissionBlock[]>(() => {
    const role = this.selected();

    if (!role) {
      return [];
    }

    const granted = new Set(role.permissions);

    return PERMISSION_GROUPS.map((group) => {
      const rows = permissionsByGroup(group).map((code) => ({
        code,
        info: PERMISSION_CATALOG[code],
        granted: granted.has(code),
      }));

      return { group, rows, grantedCount: rows.filter((row) => row.granted).length };
    });
  });

  constructor() {
    this.users.roles().subscribe({
      next: (roles) => {
        this.roles.set(roles);
        this.selectedId.set(roles[0]?.id ?? null);
      },
      error: (error: unknown) => this.showError(error),
    });
  }

  protected select(id: string): void {
    this.selectedId.set(id);
  }

  protected readonly totalPermissions = Object.keys(PERMISSION_CATALOG).length;

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
