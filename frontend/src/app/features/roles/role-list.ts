import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { notBlank } from '../../core/forms/validators';
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

/**
 * Quyền chỉ thuộc về vai Owner, không bao giờ gán cho vai tự đặt.
 *
 * Nó là TOÀN BỘ ranh giới giữa Admin và Owner — xem `SystemRoles.cs`. Backend từ chối bằng
 * `Role.PermissionIsOwnerOnly`; ở đây ta còn không vẽ ra công tắc, vì một công tắc bật được
 * mà backend luôn từ chối thì tệ hơn là không có.
 */
const CHI_CHU_SO_HUU = 'workspace.transfer-ownership';

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
 * <b>Bốn vai HỆ THỐNG chỉ xem; vai TỰ ĐẶT thì sửa được đủ</b> — tạo, đổi tên, bật/tắt từng
 * quyền, xoá. Vai hệ thống dựng lại từ hằng số trong mã nguồn ở mọi workspace, nên sửa
 * chúng ở đây sẽ bị lần nâng cấp sau ghi đè mà không báo gì.
 *
 * Đây cũng là đường DUY NHẤT để một workspace có bộ quyền khác bốn bộ dựng sẵn — và là thứ
 * màn này vẫn luôn hứa ("muốn khác đi thì tạo một vai trò mới") mà mãi tới nay mới làm được.
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
  imports: [ReactiveFormsModule, TranslatePipe],
  templateUrl: './role-list.html',
  styleUrl: './role-list.scss',
})
export class RoleList {
  private readonly users = inject(UserService);
  private readonly popups = inject(PopupService);
  private readonly errorMessages = inject(ErrorMessageService);

  private readonly fb = inject(FormBuilder);
  private readonly translate = inject(TranslateService);

  protected readonly roles = signal<readonly RoleListItem[]>([]);
  protected readonly selectedId = signal<string | null>(null);
  protected readonly saving = signal(false);

  protected readonly selected = computed(
    () => this.roles().find((role) => role.id === this.selectedId()) ?? null,
  );

  /**
   * Bộ quyền ĐANG SỬA của vai đang chọn — chưa gửi đi đâu.
   *
   * Bộ quyền là thứ người ta cân nhắc cả cụm: bật ba cái rồi tắt một cái. Lưu theo từng
   * công tắc thì mỗi lần bấm nhầm là một thay đổi thật, không có đường lùi và cũng không
   * có chỗ nào để huỷ.
   */
  protected readonly draftPermissions = signal<readonly string[]>([]);

  /** Vai HỆ THỐNG chỉ xem. Sửa được chúng thì lần nâng cấp sau ghi đè mà không báo gì. */
  protected readonly canEdit = computed(() => this.selected()?.isSystem === false);

  /**
   * Xoá được không.
   *
   * Vai còn người giữ thì backend từ chối bằng `Role.StillInUse`. Ẩn nút thay vì hiện rồi
   * báo lỗi: số người giữ đã nằm sẵn ngay trên màn hình, nên bắt họ bấm mới biết là thừa.
   */
  protected readonly canDelete = computed(
    () => this.canEdit() && this.selected()?.memberCount === 0,
  );

  protected readonly dirty = computed(() => {
    const goc = this.selected()?.permissions ?? [];
    const nay = this.draftPermissions();

    return goc.length !== nay.length || goc.some((p) => !nay.includes(p));
  });

  protected readonly showCreate = signal(false);
  protected readonly askingDelete = signal(false);

  protected readonly createForm = this.fb.nonNullable.group({
    name: ['', [notBlank, Validators.maxLength(100)]],
  });

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

    // Đọc từ bản NHÁP, không từ dữ liệu gốc — nếu không thì bật công tắc xong nó nhảy về
    // ngay, và người dùng kết luận là màn hình hỏng.
    const granted = new Set(this.draftPermissions());

    return PERMISSION_GROUPS.map((group) => {
      const rows = permissionsByGroup(group)
        // `workspace.transfer-ownership` KHÔNG hiện với vai TỰ ĐẶT. Nó là toàn bộ ranh
        // giới Admin ↔ Owner; hiện nó thành một công tắc bật được nghĩa là người quản trị
        // tin rằng họ trao đi được thứ mà backend sẽ từ chối bằng
        // `Role.PermissionIsOwnerOnly`.
        //
        // Với vai HỆ THỐNG thì vẫn hiện: ở đó nó là sự thật cần đọc — Owner có, Admin không.
        .filter((code) => role.isSystem || code !== CHI_CHU_SO_HUU)
        .map((code) => ({
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

        // `select` chứ không `selectedId.set`: nó còn nạp bản nháp quyền. Đặt thẳng
        // `selectedId` thì bảng công tắc đọc một bản nháp rỗng và mọi quyền trông như tắt.
        this.select(roles[0]?.id ?? '');
      },
      error: (error: unknown) => this.showError(error),
    });
  }

  /**
   * Đổi vai thì BỎ luôn thay đổi chưa lưu.
   *
   * Giữ lại thì người dùng quay về vai cũ và thấy những công tắc họ đã quên — rồi bấm Lưu
   * cho một thay đổi họ không còn nhớ mình đã làm.
   */
  protected select(id: string): void {
    this.selectedId.set(id);
    this.draftPermissions.set([...(this.roles().find((r) => r.id === id)?.permissions ?? [])]);
  }

  protected toggle(code: string): void {
    if (!this.canEdit()) {
      return;
    }

    this.draftPermissions.update((hien) =>
      hien.includes(code) ? hien.filter((p) => p !== code) : [...hien, code],
    );
  }

  protected revert(): void {
    this.draftPermissions.set([...(this.selected()?.permissions ?? [])]);
  }

  protected save(): void {
    const role = this.selected();

    if (role === null || !this.canEdit()) {
      return;
    }

    this.saving.set(true);

    this.users.updateRole(role.id, role.name, [...this.draftPermissions()]).subscribe({
      next: () => {
        this.saving.set(false);
        this.popups.show(this.translate.instant('roles.saved') as string);
        this.reload(role.id);
      },

      // KHÔNG hoàn tác khi lưu hỏng: mất sạch thay đổi là bắt người dùng tick lại từ đầu
      // cho một lỗi thường chỉ cần sửa cái tên.
      error: (error: unknown) => {
        this.saving.set(false);
        this.showError(error);
      },
    });
  }

  // ── Tạo và xoá ──────────────────────────────────────────────────────

  protected openCreate(): void {
    this.createForm.reset({ name: '' });
    this.createForm.markAsUntouched();
    this.showCreate.set(true);
  }

  protected closeCreate(): void {
    this.showCreate.set(false);
  }

  protected submitCreate(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();

      return;
    }

    this.saving.set(true);

    // Vai mới ra đời KHÔNG có quyền nào — xem chú thích ở `UserService.createRole`.
    this.users.createRole(this.createForm.getRawValue().name.trim(), []).subscribe({
      next: (moi) => {
        this.saving.set(false);
        this.showCreate.set(false);
        this.popups.show(this.translate.instant('roles.created') as string);
        this.reload(moi.id);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.showError(error);
      },
    });
  }

  protected askDelete(): void {
    if (this.canDelete()) {
      this.askingDelete.set(true);
    }
  }

  protected closeDelete(): void {
    this.askingDelete.set(false);
  }

  protected confirmDelete(): void {
    const role = this.selected();

    if (role === null || !this.canDelete()) {
      return;
    }

    this.saving.set(true);

    this.users.deleteRole(role.id).subscribe({
      next: () => {
        this.saving.set(false);
        this.askingDelete.set(false);
        this.popups.show(this.translate.instant('roles.deleted') as string);
        this.reload(null);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.showError(error);
      },
    });
  }

  /** Nạp lại danh sách, giữ nguyên vai đang chọn nếu nó còn tồn tại. */
  private reload(giuLai: string | null): void {
    this.users.roles().subscribe({
      next: (roles) => {
        this.roles.set(roles);
        this.select(roles.find((r) => r.id === giuLai)?.id ?? roles[0]?.id ?? '');
      },
      error: (error: unknown) => this.showError(error),
    });
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
