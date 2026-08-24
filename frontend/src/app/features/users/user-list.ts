import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthStore } from '../../core/auth/auth.store';
import { ErrorMessageService } from '../../core/i18n/error-message.service';
import { isAppError, type AppError } from '../../core/models/api-error.model';
import {
  UserStatusFilter,
  type CreateUserResponse,
  type PagedList,
  type RoleListItem,
  type UserListItem,
} from '../../core/models/user.model';
import { PopupService } from '../../core/ui/popup.service';
import { UserService } from '../../core/users/user.service';
import { Tip } from '../../shared/ui/tip/tip';

/** Trạng thái của vùng bảng — khớp `[data-state]` trong bản dựng. */
type ViewState = 'idle' | 'loc' | 'khongthay' | 'rong';

/** Hộp thoại thêm người có hai bước; bước hai hiện mật khẩu tạm. */
type CreateStep = 'nhap' | 'xong';

/**
 * Màn Nhân sự — danh sách, bộ lọc, thêm người, xem và sửa chi tiết.
 *
 * Nguồn thiết kế: `docs/07-giao-dien/org/nhan-su.html`. `user-list.scss` sinh tự động từ
 * chính file đó (`node tools/sync-shell.mjs`); chỉ đánh dấu là chép tay.
 *
 * <b>Lọc và phân trang đều ở SERVER.</b> Lọc trong bộ nhớ thì với 38 người vẫn chạy, và
 * với 3.800 người thì sập — mà không có gì trong mã báo trước điều đó.
 */
@Component({
  selector: 'app-user-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, TranslatePipe, DatePipe, Tip],
  templateUrl: './user-list.html',
  styleUrl: './user-list.scss',
})
export class UserList {
  private readonly users = inject(UserService);
  private readonly fb = inject(FormBuilder);
  private readonly popups = inject(PopupService);
  private readonly translate = inject(TranslateService);
  private readonly errorMessages = inject(ErrorMessageService);
  private readonly auth = inject(AuthStore);

  protected readonly StatusFilter = UserStatusFilter;

  protected readonly page = signal<PagedList<UserListItem> | null>(null);
  protected readonly roles = signal<readonly RoleListItem[]>([]);
  protected readonly loading = signal(true);

  /** Bộ lọc đang bật. Chúng cũng quyết định trạng thái rỗng nào được hiện. */
  protected readonly search = signal('');
  protected readonly status = signal(UserStatusFilter.Any);
  protected readonly roleId = signal('');
  protected readonly currentPage = signal(1);

  protected readonly selected = signal<ReadonlySet<string>>(new Set());

  protected readonly showCreate = signal(false);
  protected readonly createStep = signal<CreateStep>('nhap');
  protected readonly created = signal<CreateUserResponse | null>(null);
  protected readonly saving = signal(false);

  protected readonly detail = signal<UserListItem | null>(null);
  protected readonly detailTab = signal<'tt' | 'qu'>('tt');

  private readonly searchInput = new Subject<string>();

  protected readonly hasFilter = computed(
    () => this.search() !== '' || this.status() !== UserStatusFilter.Any || this.roleId() !== '',
  );

  /**
   * Bốn trạng thái, và chỉ một được hiện.
   *
   * Phân biệt "chưa có ai" với "lọc không ra" là quan trọng: hai câu cần nói khác hẳn
   * nhau. Gộp làm một thì người dùng bật nhầm bộ lọc từ lần trước sẽ tưởng dữ liệu trống.
   */
  protected readonly state = computed<ViewState>(() => {
    const list = this.page();

    if (!list || list.totalCount > 0) {
      return this.hasFilter() ? 'loc' : 'idle';
    }

    return this.hasFilter() ? 'khongthay' : 'rong';
  });

  protected readonly createForm = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.maxLength(200)]],
    email: ['', [Validators.required, Validators.email]],
    roleId: ['', [Validators.required]],
    mustChangePassword: [true],
  });

  protected readonly detailForm = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.maxLength(200)]],
    roleId: ['', [Validators.required]],
  });

  /** Lỗi do backend từ chối, gắn vào đúng ô — chỉ có "email đã có tài khoản". */
  private readonly rejectedEmail = signal<string | null>(null);

  constructor() {
    // Gõ tới đâu gọi tới đó thì một cái tên mười ký tự là mười lượt đi về. Chờ 300ms sau
    // khi người dùng NGỪNG gõ, và bỏ qua khi giá trị không thật sự đổi.
    this.searchInput
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((term) => {
        this.search.set(term);
        this.currentPage.set(1);
        this.load();
      });

    this.load();
    this.loadRoles();
  }

  // ── Nạp dữ liệu ─────────────────────────────────────────────────────

  protected load(): void {
    this.loading.set(true);

    this.users
      .list({
        search: this.search(),
        status: this.status(),
        roleId: this.roleId() || undefined,
        page: this.currentPage(),
      })
      .subscribe({
        next: (result) => {
          this.page.set(result);
          this.loading.set(false);

          // Bỏ chọn những dòng không còn trên trang này. Giữ lại thì thanh "đã chọn 3
          // người" nói về những người đang không nhìn thấy, và thao tác hàng loạt sẽ
          // chạm vào người mà quản trị viên không hề định chạm.
          const visible = new Set(result.items.map((item) => item.id));

          this.selected.update((current) => new Set([...current].filter((id) => visible.has(id))));
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.showError(error);
        },
      });
  }

  private loadRoles(): void {
    this.users.roles().subscribe({
      next: (roles) => {
        this.roles.set(roles);

        // Chọn sẵn vai hẹp nhất. Mặc định là vai rộng nhất thì một cú bấm vội tạo ra một
        // quản trị viên — sai theo hướng nguy hiểm.
        const member = roles.find((role) => role.name === 'Member') ?? roles[0];

        if (member) {
          this.createForm.controls.roleId.setValue(member.id);
        }
      },

      // Không có danh sách vai trò thì chỉ mất ô chọn ở hộp thoại thêm người; bảng vẫn
      // dùng được. Không đáng để phá cả màn hình bằng một thông báo lỗi.
      error: () => this.roles.set([]),
    });
  }

  // ── Bộ lọc ──────────────────────────────────────────────────────────

  protected onSearchInput(event: Event): void {
    this.searchInput.next((event.target as HTMLInputElement).value);
  }

  protected onStatusChange(event: Event): void {
    this.status.set(Number((event.target as HTMLSelectElement).value) as UserStatusFilter);
    this.currentPage.set(1);
    this.load();
  }

  protected onRoleChange(event: Event): void {
    this.roleId.set((event.target as HTMLSelectElement).value);
    this.currentPage.set(1);
    this.load();
  }

  protected clearFilters(): void {
    this.search.set('');
    this.status.set(UserStatusFilter.Any);
    this.roleId.set('');
    this.currentPage.set(1);
    this.load();
  }

  protected goToPage(delta: number): void {
    this.currentPage.update((value) => Math.max(1, value + delta));
    this.load();
  }

  // ── Chọn nhiều dòng ─────────────────────────────────────────────────

  protected isSelected(id: string): boolean {
    return this.selected().has(id);
  }

  protected toggleRow(id: string): void {
    this.selected.update((current) => {
      const next = new Set(current);

      if (!next.delete(id)) {
        next.add(id);
      }

      return next;
    });
  }

  protected toggleAll(): void {
    const items = this.page()?.items ?? [];

    this.selected.update((current) =>
      current.size === items.length ? new Set() : new Set(items.map((item) => item.id)),
    );
  }

  protected readonly allSelected = computed(() => {
    const items = this.page()?.items ?? [];

    return items.length > 0 && this.selected().size === items.length;
  });

  /**
   * Ô "chọn tất cả" cần trạng thái THỨ BA: chọn một phần.
   *
   * Chỉ có tick/không tick thì nó nói dối — nhìn vào tưởng chưa chọn ai trong khi đang
   * chọn dở ba dòng.
   */
  protected readonly someSelected = computed(() => {
    const count = this.selected().size;

    return count > 0 && count < (this.page()?.items.length ?? 0);
  });

  protected clearSelection(): void {
    this.selected.set(new Set());
  }

  // ── Thêm người ──────────────────────────────────────────────────────

  protected openCreate(): void {
    // Mở lại thì luôn về bước NHẬP. Giữ nguyên bước "xong" thì lần sau người dùng mở ra
    // thấy mật khẩu của người trước — vừa khó hiểu vừa là rò rỉ.
    this.createStep.set('nhap');
    this.created.set(null);
    this.rejectedEmail.set(null);
    this.createForm.patchValue({ fullName: '', email: '' });
    this.createForm.markAsUntouched();
    this.showCreate.set(true);
  }

  protected submitCreate(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.rejectedEmail.set(null);

    this.users.create(this.createForm.getRawValue()).subscribe({
      next: (response) => {
        this.saving.set(false);
        this.created.set(response);
        this.createStep.set('xong');
        this.load();
      },
      error: (error: unknown) => {
        this.saving.set(false);

        if (isAppError(error) && error.code === 'Email.Taken') {
          this.rejectedEmail.set(this.errorMessages.resolve(error));
          this.createForm.controls.email.markAsTouched();
        }

        this.showError(error);
      },
    });
  }

  protected createAnother(): void {
    this.createStep.set('nhap');
    this.created.set(null);
    this.createForm.patchValue({ fullName: '', email: '' });
    this.createForm.markAsUntouched();
  }

  protected async copyPassword(): Promise<void> {
    const password = this.created()?.temporaryPassword;

    if (!password) {
      return;
    }

    try {
      await navigator.clipboard.writeText(password);
      this.popups.show(this.translate.instant('users.copied') as string);
    } catch {
      // Trình duyệt từ chối quyền clipboard (hay gặp khi trang không chạy HTTPS). Nói
      // thẳng thay vì im lặng — người dùng vẫn bôi đen chép tay được.
      this.popups.error(this.translate.instant('users.copyFailed') as string);
    }
  }

  protected emailError(): string | null {
    if (this.rejectedEmail()) {
      return this.rejectedEmail();
    }

    const control = this.createForm.controls.email;

    if (!control.touched || !control.errors) {
      return null;
    }

    return this.translate.instant(
      control.errors['required']
        ? 'users.validation.emailRequired'
        : 'users.validation.emailInvalid',
    ) as string;
  }

  protected nameError(): string | null {
    const control = this.createForm.controls.fullName;

    if (!control.touched || !control.errors) {
      return null;
    }

    return this.translate.instant('users.validation.nameRequired') as string;
  }

  // ── Chi tiết ────────────────────────────────────────────────────────

  protected openDetail(user: UserListItem): void {
    this.detail.set(user);
    this.detailTab.set('tt');

    const role = this.roles().find((item) => item.name === user.roleName);

    this.detailForm.setValue({ fullName: user.fullName, roleId: role?.id ?? '' });
  }

  protected saveDetail(): void {
    const user = this.detail();

    if (!user || this.detailForm.invalid) {
      this.detailForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);

    this.users.update(user.id, this.detailForm.getRawValue()).subscribe({
      next: () => {
        this.saving.set(false);
        this.detail.set(null);
        this.popups.show(this.translate.instant('users.saved') as string);
        this.load();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.showError(error);
      },
    });
  }

  protected setActive(user: UserListItem, isActive: boolean): void {
    this.saving.set(true);

    this.users.setActive(user.id, isActive).subscribe({
      next: () => {
        this.saving.set(false);
        this.detail.set(null);
        this.popups.show(
          this.translate.instant(isActive ? 'users.enabled' : 'users.disabled') as string,
        );
        this.load();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.showError(error);
      },
    });
  }

  /**
   * Escape đóng hộp thoại đang mở.
   *
   * Đây là đường thoát của người dùng bàn phím, và nó phải có ngay cả khi nền tối đã là
   * một nút bấm được: Tab tới nút nền rồi Enter là ba thao tác cho một việc mà mọi ứng
   * dụng khác chỉ cần một phím.
   */
  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    this.showCreate.set(false);
    this.detail.set(null);
  }

  protected closeDetail(): void {
    this.detail.set(null);
  }

  protected closeCreate(): void {
    this.showCreate.set(false);
  }

  // ── Dùng chung ──────────────────────────────────────────────────────

  /** Chữ cái đầu của từ đầu và từ cuối — xem `Shell.initials` cho lý do. */
  protected initials(name: string): string {
    const words = name.trim().split(/\s+/).filter(Boolean);

    return words.length === 0 ? '?' : (words[0][0] + words[words.length - 1][0]).toUpperCase();
  }

  /**
   * Người này có vô hiệu hoá được không.
   *
   * Hai ca backend luôn từ chối: chính mình, và chủ sở hữu. Hiện nút rồi báo lỗi khi bấm
   * là cách chắc chắn nhất làm người dùng bực — họ không biết mình đã làm gì sai. Ẩn nút
   * và nói một câu ngắn thì họ hiểu ngay.
   *
   * Nhận diện chủ sở hữu qua TÊN VAI TRÒ: Owner là vai hệ thống chỉ gán cho đúng người
   * đó lúc dựng workspace. Đây là suy đoán chứ không phải sự thật từ server, nên nó chỉ
   * dùng để ẩn nút — luật thật vẫn nằm ở backend, và nó vẫn từ chối nếu suy đoán này sai.
   */
  protected canDisable(user: UserListItem): boolean {
    return user.id !== this.auth.user()?.userId && user.roleName !== 'Owner';
  }

  protected statusKey(user: UserListItem): string {
    if (!user.isActive) {
      return 'users.status.disabled';
    }

    return user.mustChangePassword ? 'users.status.pending' : 'users.status.active';
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
