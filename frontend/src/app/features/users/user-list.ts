import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthStore } from '../../core/auth/auth.store';
import { notBlank } from '../../core/forms/validators';
import { OrgService } from '../../core/org/org.service';
import type { DepartmentTreeItem, MemberListItem } from '../../core/models/org.model';
import { ErrorMessageService } from '../../core/i18n/error-message.service';
import { isAppError, type AppError } from '../../core/models/api-error.model';
import {
  UserStatusFilter,
  type CreateUserResponse,
  type PagedList,
  type ResetPasswordResponse,
  type RoleListItem,
} from '../../core/models/user.model';
import { PopupService } from '../../core/ui/popup.service';
import { UserService } from '../../core/users/user.service';
import { Tip } from '../../shared/ui/tip/tip';

/** Trạng thái của vùng bảng — khớp `[data-state]` trong bản dựng. */
type ViewState = 'idle' | 'loc' | 'khongthay' | 'rong';

/** Hộp thoại thêm người có hai bước; bước hai hiện mật khẩu tạm. */
type CreateStep = 'nhap' | 'xong';

/**
 * Ba việc làm được cho nhiều người một lúc.
 *
 * Cả ba đều cần một điều kiện KHÁC nhau, và đó là chỗ dễ sai nhất: danh sách gộp có ba
 * loại dòng, nên một lựa chọn bất kỳ gần như luôn lẫn cả những dòng không áp được.
 */
type BulkAction = 'phongban' | 'vaitro' | 'vohieu';

/** Một dòng của ô chọn phòng ban — cây đã trải phẳng, `depth` chỉ để thụt lề. */
interface DepartmentOption {
  readonly id: string;
  readonly name: string;
  readonly depth: number;
}

/**
 * Màn Nhân sự — danh sách, bộ lọc, thêm người, xem và sửa chi tiết.
 *
 * Nguồn thiết kế: `docs/07-giao-dien/org/nhan-su.html`. `user-list.scss` sinh tự động từ
 * chính file đó (`node tools/sync-shell.mjs`); chỉ đánh dấu là chép tay.
 *
 * <b>Danh sách GỘP hai module:</b> tài khoản đăng nhập (Identity) và hồ sơ nhân sự (Org).
 * Ba loại dòng, và cả ba đều có thật — có cả hai · chỉ hồ sơ (nhân viên mới chưa được cấp
 * tài khoản) · chỉ tài khoản (bot chạy sao lưu). Phép gộp nằm ở handler của Org; xem
 * `GetMembersQueryHandler` để biết vì sao nó không thể nằm chỗ nào khác.
 *
 * <b>⚠️ Lọc và phân trang nay ở CLIENT</b>, ngược hẳn bản trước dùng `/api/users`.
 * Không phải vì client tốt hơn, mà vì `/api/members` buộc phải trả về toàn bộ: gộp hai
 * nguồn thì không thể phân trang từng nguồn rồi ghép — người ở trang sau của nguồn này sẽ
 * bị coi là "chưa có tài khoản", tức một câu trả lời SAI chứ không phải thiếu.
 *
 * Đánh đổi chấp nhận được ở quy mô vài trăm người. Đến hàng chục nghìn thì phải đổi CÁCH
 * GỘP (hỏi Identity theo lô id thay vì lấy tất cả), không phải đổi chỗ phân trang.
 *
 * <b>Chỉ ĐỌC từ `/api/members`.</b> Mọi thao tác sửa vẫn đi về đúng module sở hữu dữ
 * liệu: `/api/users` cho tài khoản, `/api/employees` cho hồ sơ.
 */
@Component({
  selector: 'app-user-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe, Tip],
  templateUrl: './user-list.html',
  styleUrl: './user-list.scss',
})
export class UserList {
  private readonly users = inject(UserService);
  private readonly org = inject(OrgService);
  private readonly fb = inject(FormBuilder);
  private readonly popups = inject(PopupService);
  private readonly translate = inject(TranslateService);
  private readonly errorMessages = inject(ErrorMessageService);
  private readonly auth = inject(AuthStore);

  protected readonly StatusFilter = UserStatusFilter;

  /**
   * Trang đang hiện — dựng ở CLIENT từ danh sách gộp.
   *
   * Khác `/api/users` cũ (lọc và phân trang ở server): `/api/members` trả về toàn bộ, vì
   * nó gộp hai nguồn và không thể phân trang từng nguồn rồi ghép — người ở trang sau của
   * nguồn này sẽ bị coi là "chưa có tài khoản". Đánh đổi chấp nhận được ở quy mô vài trăm
   * người; đến hàng chục nghìn thì phải đổi cách gộp, không phải đổi chỗ phân trang.
   */
  protected readonly page = signal<PagedList<MemberListItem> | null>(null);

  /** Toàn bộ danh sách chưa lọc — nguồn để lọc và phân trang tại chỗ. */
  private readonly allMembers = signal<readonly MemberListItem[]>([]);

  /** Lọc theo LOẠI DÒNG, chỉ có ở màn gộp. `''` = tất cả. */
  protected readonly kind = signal<'' | 'khongTaiKhoan' | 'khongHoSo'>('');
  protected readonly roles = signal<readonly RoleListItem[]>([]);
  protected readonly loading = signal(true);

  /** Bộ lọc đang bật. Chúng cũng quyết định trạng thái rỗng nào được hiện. */
  protected readonly search = signal('');
  protected readonly status = signal(UserStatusFilter.Any);
  protected readonly roleId = signal('');
  protected readonly currentPage = signal(1);

  protected readonly selected = signal<ReadonlySet<string>>(new Set());

  /** Việc hàng loạt đang chờ xác nhận. `null` = hộp xác nhận đang đóng. */
  protected readonly bulkAction = signal<BulkAction | null>(null);

  /** Đích của việc hàng loạt: mã phòng ban hoặc mã vai trò. Vô hiệu hoá thì không cần. */
  protected readonly bulkTarget = signal('');

  /** Đang chạy tới người thứ mấy — để thanh tiến trình nói thật thay vì quay vòng. */
  protected readonly bulkDone = signal(0);

  protected readonly departments = signal<readonly DepartmentOption[]>([]);

  protected readonly showCreate = signal(false);
  protected readonly createStep = signal<CreateStep>('nhap');
  protected readonly created = signal<CreateUserResponse | null>(null);
  protected readonly saving = signal(false);

  protected readonly detail = signal<MemberListItem | null>(null);
  protected readonly detailTab = signal<'tt' | 'qu'>('tt');

  /** Người đang chờ xác nhận đặt lại mật khẩu. `null` = hộp đang đóng. */
  protected readonly resetFor = signal<MemberListItem | null>(null);

  /** Mật khẩu tạm vừa sinh ra. Tồn tại ĐÚNG một lần — đóng hộp là mất. */
  protected readonly resetResult = signal<ResetPasswordResponse | null>(null);

  /**
   * Dòng đang mở hộp thoại NỐI. `null` = hộp đang đóng.
   *
   * Một hộp cho CẢ HAI chiều: mở từ dòng "chỉ hồ sơ" thì nó hỏi chọn tài khoản, mở từ
   * dòng "chỉ tài khoản" thì hỏi chọn hồ sơ. Cùng một phép nối, chỉ khác phía nào đã
   * biết — tách làm hai hộp là chép cùng một khung hai lần rồi để chúng lệch nhau.
   */
  protected readonly linkFor = signal<MemberListItem | null>(null);

  /** Khoá của ứng viên đang chọn trong danh sách xổ. `''` = chưa chọn ai. */
  protected readonly linkTarget = signal('');

  /**
   * Hai đường đi cho một người còn thiếu một nửa: nối vào thứ ĐÃ CÓ, hoặc tạo thứ MỚI.
   *
   * Chúng là hai thẻ trong MỘT hộp thoại chứ không phải hai mục trong một menu xổ, vì
   * người dùng đang hỏi đúng một câu — "cho người này một tài khoản kiểu gì?". Ngoài ra
   * menu xổ đặt trong bảng thì bị `.bangcuon { overflow-x: auto }` cắt cụt: đặt overflow
   * một chiều thì chiều kia thành `auto` theo, nên nội dung absolute bên trong bị cắt.
   */
  protected readonly linkMode = signal<'noi' | 'tao'>('noi');

  private readonly searchInput = new Subject<string>();

  protected readonly hasFilter = computed(
    () =>
      this.search() !== '' ||
      this.status() !== UserStatusFilter.Any ||
      this.roleId() !== '' ||
      this.kind() !== '',
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
    fullName: ['', [notBlank, Validators.maxLength(200)]],
    email: ['', [Validators.required, Validators.email]],
    roleId: ['', [Validators.required]],
    mustChangePassword: [true],
  });

  protected readonly detailForm = this.fb.nonNullable.group({
    fullName: ['', [notBlank, Validators.maxLength(200)]],
    roleId: ['', [Validators.required]],
  });

  /**
   * Biểu mẫu tạo HỒ SƠ cho một dòng chỉ có tài khoản.
   *
   * Chỉ có mã nhân viên là bắt buộc — tên lấy thẳng từ tài khoản, còn chức danh và phòng
   * ban điền sau được. `Employee.Create` từ chối mã rỗng, nên gửi đi mà chưa điền là một
   * vòng mạng chắc chắn thất bại.
   */
  protected readonly employeeForm = this.fb.nonNullable.group({
    code: ['', [notBlank, Validators.maxLength(30)]],
    jobTitle: [''],
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
        this.applyFilters();
      });

    this.load();
    this.loadRoles();
    this.loadDepartments();
  }

  // ── Nạp dữ liệu ─────────────────────────────────────────────────────

  protected load(): void {
    this.loading.set(true);

    this.org.members().subscribe({
      next: (all) => {
        this.allMembers.set(all);
        this.loading.set(false);
        this.applyFilters();
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.showError(error);
      },
    });
  }

  /**
   * Lọc và cắt trang tại chỗ.
   *
   * Gọi lại sau mỗi lần đổi bộ lọc — KHÔNG gọi lại `load()`. Đi hỏi server cho một phép
   * lọc mà dữ liệu đã nằm sẵn trong tay là một vòng mạng không mua được gì, và nó làm ô
   * tìm giật mỗi lần gõ.
   */
  private applyFilters(): void {
    const tim = this.search().trim().toLowerCase();
    const vai = this.roleId();
    const tenVai = vai ? (this.roles().find((r) => r.id === vai)?.name ?? null) : null;

    const loc = this.allMembers().filter((m) => {
      if (tim && !`${m.fullName} ${m.email ?? ''} ${m.code ?? ''}`.toLowerCase().includes(tim)) {
        return false;
      }

      if (tenVai !== null && m.roleName !== tenVai) {
        return false;
      }

      switch (this.kind()) {
        case 'khongTaiKhoan':
          if (m.userId !== null) return false;
          break;
        case 'khongHoSo':
          if (m.employeeId !== null) return false;
          break;
      }

      switch (this.status()) {
        case UserStatusFilter.Active:
          return m.isActive && !m.mustChangePassword;
        case UserStatusFilter.PendingFirstLogin:
          return m.mustChangePassword;
        case UserStatusFilter.Disabled:
          return !m.isActive;
        default:
          return true;
      }
    });

    const pageSize = 20;
    const totalPages = Math.max(1, Math.ceil(loc.length / pageSize));
    const page = Math.min(this.currentPage(), totalPages);

    this.currentPage.set(page);

    this.page.set({
      items: loc.slice((page - 1) * pageSize, page * pageSize),
      page,
      pageSize,
      totalCount: loc.length,
      totalPages,
      hasPreviousPage: page > 1,
      hasNextPage: page < totalPages,
    });

    // Bỏ chọn những dòng không còn trên trang này. Giữ lại thì thanh "đã chọn 3 người"
    // nói về những người đang không nhìn thấy, và thao tác hàng loạt sẽ chạm vào người mà
    // quản trị viên không hề định chạm.
    const visible = new Set(this.page()!.items.map((m) => this.rowKey(m)));

    this.selected.update((current) => new Set([...current].filter((id) => visible.has(id))));
  }

  /**
   * Khoá của một dòng.
   *
   * Ưu tiên `userId` vì phần lớn thao tác hàng loạt là thao tác lên TÀI KHOẢN. Dòng chỉ
   * có hồ sơ thì lấy `employeeId` — vẫn phân biệt được, và vẫn ổn định qua các lần lọc.
   */
  protected rowKey(member: MemberListItem): string {
    return member.userId ?? member.employeeId!;
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

  /*
    Mọi bộ lọc gọi `applyFilters()`, KHÔNG gọi `load()`.

    Dữ liệu đã nằm sẵn trong `allMembers` — đi hỏi server thêm một vòng cho một phép lọc
    làm được tại chỗ là không mua được gì, và nó khiến ô tìm giật mỗi lần gõ. `load()`
    chỉ dùng khi dữ liệu THẬT SỰ đổi: sau khi tạo, sửa hay vô hiệu hoá.
  */

  protected onStatusChange(event: Event): void {
    this.status.set(Number((event.target as HTMLSelectElement).value) as UserStatusFilter);
    this.currentPage.set(1);
    this.applyFilters();
  }

  protected onRoleChange(event: Event): void {
    this.roleId.set((event.target as HTMLSelectElement).value);
    this.currentPage.set(1);
    this.applyFilters();
  }

  protected onKindChange(event: Event): void {
    this.kind.set((event.target as HTMLSelectElement).value as '' | 'khongTaiKhoan' | 'khongHoSo');
    this.currentPage.set(1);
    this.applyFilters();
  }

  protected clearFilters(): void {
    this.search.set('');
    this.status.set(UserStatusFilter.Any);
    this.roleId.set('');
    this.kind.set('');
    this.currentPage.set(1);
    this.applyFilters();
  }

  protected goToPage(delta: number): void {
    this.currentPage.update((value) => Math.max(1, value + delta));
    this.applyFilters();
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
      current.size === items.length ? new Set() : new Set(items.map((item) => this.rowKey(item))),
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

  // ── Thao tác hàng loạt ──────────────────────────────────────────────

  /** Những dòng đang được chọn VÀ đang nhìn thấy. */
  private readonly selectedMembers = computed<readonly MemberListItem[]>(() => {
    const chon = this.selected();

    return (this.page()?.items ?? []).filter((m) => chon.has(this.rowKey(m)));
  });

  /**
   * Trong số đã chọn, ai THẬT SỰ áp được việc này.
   *
   * Ba việc, ba điều kiện khác nhau — và không việc nào áp được cho mọi loại dòng:
   * <list type="bullet">
   * <item>Đổi phòng ban ghi vào HỒ SƠ, nên dòng chưa có hồ sơ thì không có gì để ghi.</item>
   * <item>Đổi vai trò ghi vào TÀI KHOẢN, và backend từ chối hạ vai chủ sở hữu.</item>
   * <item>Vô hiệu hoá cũng là việc của tài khoản, cộng thêm luật không tự khoá mình.</item>
   * </list>
   */
  protected readonly bulkTargets = computed<readonly MemberListItem[]>(() => {
    const viec = this.bulkAction();

    switch (viec) {
      case 'phongban':
        return this.selectedMembers().filter((m) => m.employeeId !== null);
      case 'vaitro':
        return this.selectedMembers().filter((m) => m.userId !== null && m.roleName !== 'Owner');
      case 'vohieu':
        return this.selectedMembers().filter((m) => this.canDisable(m) && m.isActive);
      default:
        return [];
    }
  });

  /**
   * Bao nhiêu người bị BỎ QUA — con số này phải hiện ra TRƯỚC khi bấm xác nhận.
   *
   * Im lặng bỏ qua thì quản trị viên tin là đã đổi cho cả 10 người, trong khi thật ra chỉ
   * 6. Họ không kiểm lại, vì không có gì gợi ý là cần kiểm.
   */
  protected readonly bulkSkipped = computed(
    () => this.selectedMembers().length - this.bulkTargets().length,
  );

  protected openBulk(action: BulkAction): void {
    this.bulkAction.set(action);
    this.bulkDone.set(0);

    // Không ai áp được thì đừng mở một hộp ghi "sẽ áp cho 0 người" rồi vẫn có nút Xác
    // nhận — đó là mời người dùng bấm một nút không làm gì cả.
    if (this.bulkTargets().length === 0) {
      this.bulkAction.set(null);
      this.popups.show(this.translate.instant('users.bulk.noneEligible') as string);

      return;
    }

    // Vai hẹp nhất làm mặc định, cùng lý do với hộp thoại thêm người: một cú bấm vội
    // không được phép nâng ai lên quản trị viên.
    const member = this.roles().find((role) => role.name === 'Member') ?? this.roles()[0];

    this.bulkTarget.set(action === 'vaitro' ? (member?.id ?? '') : '');
  }

  protected closeBulk(): void {
    this.bulkAction.set(null);
  }

  protected onBulkTargetChange(event: Event): void {
    this.bulkTarget.set((event.target as HTMLSelectElement).value);
  }

  /**
   * Chạy việc hàng loạt — TUẦN TỰ, từng người một.
   *
   * Bắn song song thì nhanh hơn và sai hơn: mỗi lời gọi là một transaction riêng, nên
   * hỏng giữa chừng để lại một trạng thái không đoán được ai xong ai chưa. Tuần tự thì
   * `bulkDone` đếm được người thứ mấy, và câu tóm tắt cuối cùng nói đúng sự thật.
   *
   * Trần là 20 — đúng một trang. Chọn chỉ giữ những dòng đang nhìn thấy, nên không có
   * cách nào chọn nhiều hơn thế.
   */
  protected runBulk(): void {
    const viec = this.bulkAction();
    const danhSach = [...this.bulkTargets()];

    if (viec === null || danhSach.length === 0) {
      return;
    }

    this.saving.set(true);

    let hong = 0;

    const chay = (i: number): void => {
      if (i >= danhSach.length) {
        this.saving.set(false);
        this.bulkAction.set(null);
        this.selected.set(new Set());

        // Bỏ chọn xong mới báo: giữ nguyên thì thanh vẫn ghi "đã chọn 2 người" sau khi
        // vừa vô hiệu hoá họ, và cú bấm tiếp theo áp lại lên đúng những người đó.
        this.popups.show(
          this.translate.instant(
            hong === 0 ? 'users.bulk.done' : 'users.bulk.donePartly',
            { count: danhSach.length - hong, failed: hong },
          ) as string,
        );

        this.load();

        return;
      }

      this.goiMotNguoi(viec, danhSach[i]).subscribe({
        next: () => {
          this.bulkDone.set(i + 1);
          chay(i + 1);
        },

        // Một người hỏng KHÔNG dừng cả loạt: người thứ 7 lỗi mà bỏ dở thì ba người sau
        // không được xử lý, và không có gì trên màn hình nói ra điều đó. Đếm lại rồi báo
        // một câu ở cuối.
        error: () => {
          hong++;
          this.bulkDone.set(i + 1);
          chay(i + 1);
        },
      });
    };

    chay(0);
  }

  private goiMotNguoi(viec: BulkAction, member: MemberListItem) {
    switch (viec) {
      case 'phongban':
        return this.org.transferEmployee(member.employeeId!, this.bulkTarget() || null);
      case 'vaitro':
        return this.users.changeRole(member.userId!, this.bulkTarget());
      default:
        return this.users.setActive(member.userId!, false);
    }
  }

  /** Trải cây phòng ban thành danh sách phẳng cho ô chọn. `depth` chỉ dùng để thụt lề. */
  private loadDepartments(): void {
    this.org.departmentTree().subscribe({
      next: (cay) => {
        const phang: DepartmentOption[] = [];

        const di = (nut: readonly DepartmentTreeItem[], depth: number): void => {
          for (const item of nut) {
            phang.push({ id: item.id, name: item.name, depth });
            di(item.children, depth + 1);
          }
        };

        di(cay, 0);
        this.departments.set(phang);
      },

      // Không có cây thì chỉ mất ô chọn phòng ban ở hộp hàng loạt; bảng vẫn dùng được.
      error: () => this.departments.set([]),
    });
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

    const body = this.createForm.getRawValue();

    this.users.create({ ...body, fullName: body.fullName.trim() }).subscribe({
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
    await this.chep(this.created()?.temporaryPassword);
  }

  /** Dùng chung cho cả hai chỗ sinh mật khẩu tạm: tạo tài khoản và đặt lại mật khẩu. */
  private async chep(password: string | undefined): Promise<void> {
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

  /**
   * Mở ngăn kéo chi tiết.
   *
   * CHỈ mở cho dòng có TÀI KHOẢN. Ngăn kéo này sửa vai trò và tên của một tài khoản đăng
   * nhập (`PATCH /api/users/{id}`) — với dòng chỉ có hồ sơ thì không có gì để sửa ở đây,
   * và mở ra một biểu mẫu không lưu được là cách chắc chắn nhất làm người dùng bực.
   */
  protected openDetail(member: MemberListItem): void {
    if (member.userId === null) {
      this.popups.show(this.translate.instant('users.noAccountYet') as string);

      return;
    }

    this.detail.set(member);
    this.detailTab.set('tt');

    const role = this.roles().find((item) => item.name === member.roleName);

    this.detailForm.setValue({ fullName: member.fullName, roleId: role?.id ?? '' });
  }

  protected saveDetail(): void {
    const user = this.detail();

    if (!user?.userId || this.detailForm.invalid) {
      this.detailForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);

    this.users.update(user.userId, this.detailForm.getRawValue()).subscribe({
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

  // ── Đặt lại mật khẩu hộ ─────────────────────────────────────────────

  /**
   * Mở hộp HỎI TRƯỚC, không gọi backend ngay.
   *
   * Thao tác này đá người đó ra khỏi mọi thiết bị đang đăng nhập. Làm ngay khi bấm thì
   * một cú bấm nhầm trong ngăn kéo là đủ để một đồng nghiệp đang họp bị đăng xuất giữa
   * chừng, và không có đường hoàn tác.
   */
  protected openReset(member: MemberListItem): void {
    // Đặt lại mật khẩu là thao tác lên TÀI KHOẢN. Dòng chưa có tài khoản thì việc cần làm
    // là cấp tài khoản, và việc đó nằm ở chỗ khác.
    if (member.userId === null) {
      return;
    }

    // Xoá kết quả cũ NGAY khi mở, không đợi lúc đóng: mở lại cho người khác mà còn thấy
    // mật khẩu của người trước thì vừa khó hiểu vừa là rò rỉ.
    this.resetResult.set(null);
    this.resetFor.set(member);
  }

  protected confirmReset(): void {
    const member = this.resetFor();

    if (member?.userId == null) {
      return;
    }

    this.saving.set(true);

    this.users.resetPassword(member.userId).subscribe({
      next: (response) => {
        this.saving.set(false);
        this.resetResult.set(response);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.showError(error);
      },
    });
  }

  /**
   * Đóng hộp và nạp lại danh sách.
   *
   * Sau khi đặt lại, người đó mang cờ "chờ nhận tài khoản" — cột Trạng thái phải đổi theo.
   * Không nạp lại thì bảng vẫn ghi "Đang hoạt động", và quản trị viên tưởng thao tác trượt.
   */
  protected async copyResetPassword(): Promise<void> {
    await this.chep(this.resetResult()?.temporaryPassword);
  }

  protected closeReset(): void {
    // Chỉ nạp lại khi THẬT SỰ có gì đổi. Bấm Huỷ ở bước hỏi thì chưa có gì để nạp.
    if (this.resetResult() !== null) {
      this.load();
    }

    this.resetFor.set(null);
    this.resetResult.set(null);
  }

  // ── Nối hồ sơ ↔ tài khoản ───────────────────────────────────────────

  /**
   * Ứng viên để nối — luôn là những dòng CHƯA nối của phía ĐỐI DIỆN.
   *
   * Đang đứng ở một hồ sơ thì cần một tài khoản, và ngược lại. Lọc theo `allMembers` chứ
   * không theo `page()`: ứng viên có thể đang nằm ở trang khác hoặc bị bộ lọc hiện tại
   * giấu đi, mà việc nối thì không liên quan gì tới bộ lọc đang bật.
   *
   * Chỉ hiện dòng chưa nối: cho chọn một tài khoản đã có chủ là bắt người dùng bấm rồi
   * mới biết bị từ chối, trong khi ta đã có sẵn cả danh sách trong tay. Backend vẫn chặn
   * bằng `Employee.UserAlreadyLinked` — hai lớp, vì danh sách trên màn có thể cũ vài giây.
   */
  protected readonly linkCandidates = computed<readonly MemberListItem[]>(() => {
    const nguon = this.linkFor();

    if (nguon === null) {
      return [];
    }

    // Đứng ở dòng thiếu TÀI KHOẢN thì đi tìm dòng thiếu HỒ SƠ, và ngược lại.
    return nguon.userId === null
      ? this.allMembers().filter((m) => m.employeeId === null)
      : this.allMembers().filter((m) => m.userId === null);
  });

  protected openLink(member: MemberListItem): void {
    this.linkFor.set(member);
    this.linkTarget.set('');
    this.createStep.set('nhap');
    this.created.set(null);
    this.rejectedEmail.set(null);
    this.employeeForm.reset({ code: '', jobTitle: '' });

    // Không còn ai để nối thì mở thẳng chế độ TẠO MỚI thay vì một danh sách xổ rỗng.
    // Đóng hộp lại là bảo người dùng rằng người này vĩnh viễn không thể có tài khoản,
    // trong khi vẫn còn đúng một đường đi.
    this.setLinkMode(this.linkCandidates().length === 0 ? 'tao' : 'noi');
  }

  /** Đổi thẻ, và điền sẵn biểu mẫu tạo mới từ chính dòng đang đứng. */
  protected setLinkMode(mode: 'noi' | 'tao'): void {
    this.linkMode.set(mode);

    const nguon = this.linkFor();

    if (mode !== 'tao' || nguon === null) {
      return;
    }

    // Điền sẵn từ dòng đang đứng. Bắt gõ lại cái tên đang hiện ngay trên màn hình là việc
    // thừa, và mỗi lần gõ lại là một cơ hội gõ khác đi — rồi hồ sơ và tài khoản của cùng
    // một người mang hai cái tên, mà không có gì báo.
    if (nguon.userId === null) {
      this.createForm.patchValue({ fullName: nguon.fullName, email: nguon.email ?? '' });
      this.createForm.markAsUntouched();
    } else {
      this.employeeForm.patchValue({ code: '', jobTitle: nguon.jobTitle ?? '' });
      this.employeeForm.markAsUntouched();
    }
  }

  protected onLinkModeChange(mode: 'noi' | 'tao'): void {
    this.setLinkMode(mode);
  }

  protected closeLink(): void {
    this.linkFor.set(null);
  }

  protected onLinkTargetChange(event: Event): void {
    this.linkTarget.set((event.target as HTMLSelectElement).value);
  }

  /**
   * Gửi lệnh nối.
   *
   * Endpoint là `POST /api/employees/{employeeId}/link-account` — nó GHI vào hồ sơ, nên
   * <b>hồ sơ luôn là tham số đầu</b> dù người dùng mở hộp thoại từ phía nào. Đảo hai tham
   * số khi mở từ dòng tài khoản thì lời gọi đi tới một `employeeId` không tồn tại, và lỗi
   * trả về nói "không tìm thấy nhân viên" trong khi người dùng đang đứng ở một tài khoản.
   */
  protected submitLink(): void {
    const nguon = this.linkFor();

    if (nguon === null) {
      return;
    }

    if (this.linkMode() === 'tao') {
      this.taoRoiNoi(nguon);

      return;
    }

    const chon = this.linkTarget();

    if (chon === '') {
      return;
    }

    const dich = this.linkCandidates().find((m) => this.rowKey(m) === chon);

    if (dich === undefined) {
      return;
    }

    const employeeId = nguon.employeeId ?? dich.employeeId;
    const userId = nguon.userId ?? dich.userId;

    if (employeeId === null || employeeId === undefined || userId === null || userId === undefined) {
      return;
    }

    this.saving.set(true);

    this.org.linkAccount(employeeId, userId).subscribe({
      next: () => {
        this.saving.set(false);
        this.linkFor.set(null);
        this.popups.show(this.translate.instant('users.link.done') as string);
        this.load();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.showError(error);
      },
    });
  }

  /**
   * Tạo nửa còn thiếu rồi nối luôn — HAI lời gọi HTTP nối tiếp, không phải một giao dịch.
   *
   * Đây là đánh đổi có chủ ý. Làm ở backend thì cần một cổng GHI liên module (nay
   * `IUserDirectory` mới chỉ đọc), mà `CompositeUnitOfWork` cũng chỉ chốt hai transaction
   * nối tiếp chứ không phải một — nên hỏng giữa chừng vẫn để lại đúng trạng thái đó, chỉ
   * là giấu vào chỗ người dùng không thấy.
   *
   * Ở đây thì nó <b>hỏng lành và nhìn thấy được</b>: bước nối trượt thì có một dòng
   * chỉ-tài-khoản nằm ngay cạnh dòng chỉ-hồ-sơ, và nút "Nối" ở ngay đó sửa được.
   */
  private taoRoiNoi(nguon: MemberListItem): void {
    // Thiếu tài khoản → tạo tài khoản. Thiếu hồ sơ → tạo hồ sơ.
    if (nguon.userId === null) {
      this.capTaiKhoan(nguon);

      return;
    }

    if (this.employeeForm.invalid) {
      this.employeeForm.markAllAsTouched();

      return;
    }

    const body = this.employeeForm.getRawValue();

    this.saving.set(true);

    this.org
      .createEmployee({
        code: body.code.trim(),
        fullName: nguon.fullName,
        jobTitle: body.jobTitle.trim() === '' ? null : body.jobTitle.trim(),
        workEmail: nguon.email,
        phone: null,
        departmentId: null,
      })
      .subscribe({
        next: (hoSo) => this.noiSauKhiTao(hoSo.id, nguon.userId!),
        error: (error: unknown) => {
          this.saving.set(false);
          this.showError(error);
        },
      });
  }

  /**
   * Tạo TÀI KHOẢN cho một hồ sơ, rồi nối.
   *
   * Vẫn đi qua bước hai hiện mật khẩu tạm, y như luồng "Thêm người" — mật khẩu đó chỉ tồn
   * tại đúng một lần trong phản hồi. Nối xong rồi đóng hộp thoại luôn thì người vừa được
   * cấp tài khoản không có cách nào đăng nhập lần đầu.
   */
  private capTaiKhoan(nguon: MemberListItem): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();

      return;
    }

    this.saving.set(true);
    this.rejectedEmail.set(null);

    const body = this.createForm.getRawValue();

    this.users.create({ ...body, fullName: body.fullName.trim() }).subscribe({
      next: (taiKhoan) => {
        this.created.set(taiKhoan);
        this.createStep.set('xong');
        this.noiSauKhiTao(nguon.employeeId!, taiKhoan.id);
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

  /**
   * Bước hai của "tạo rồi nối".
   *
   * KHÔNG đóng hộp thoại ở đây khi vừa tạo tài khoản: hộp đang ở bước hiện mật khẩu tạm.
   * Đóng nó là vứt mất thứ duy nhất người dùng cần lấy ra.
   */
  private noiSauKhiTao(employeeId: string, userId: string): void {
    this.org.linkAccount(employeeId, userId).subscribe({
      next: () => {
        this.saving.set(false);

        if (this.createStep() !== 'xong') {
          this.linkFor.set(null);
          this.popups.show(this.translate.instant('users.link.done') as string);
        }

        this.load();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.showError(error);
        this.load();
      },
    });
  }

  /**
   * Gỡ liên kết — hai dòng tách lại như trước khi nối.
   *
   * Không mất gì cả: hồ sơ còn nguyên, tài khoản còn nguyên. Vì vậy nó KHÔNG nằm trong
   * "vùng nguy hiểm" của ngăn kéo — xếp cạnh "vô hiệu hoá tài khoản" thì nó mượn một sắc
   * thái nguy hiểm mà nó không có, và người ta ngại bấm đúng cái nút để sửa một liên kết sai.
   */
  protected unlink(member: MemberListItem): void {
    if (member.employeeId === null) {
      return;
    }

    this.saving.set(true);

    this.org.unlinkAccount(member.employeeId).subscribe({
      next: () => {
        this.saving.set(false);
        this.detail.set(null);
        this.popups.show(this.translate.instant('users.link.undone') as string);
        this.load();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.showError(error);
      },
    });
  }

  protected setActive(user: MemberListItem, isActive: boolean): void {
    if (user.userId === null) {
      return;
    }

    this.saving.set(true);

    this.users.setActive(user.userId, isActive).subscribe({
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
    this.linkFor.set(null);
    this.bulkAction.set(null);

    // `closeReset` chứ không phải `resetFor.set(null)`: Escape ở bước hiện mật khẩu vẫn
    // phải nạp lại bảng, nếu không cột Trạng thái nói sai cho tới lần nạp sau.
    this.closeReset();
  }

  /**
   * Nút thao tác của một dòng làm gì — phụ thuộc vào dòng đó THIẾU gì.
   *
   * Dòng đủ cả hai thì mở ngăn kéo chi tiết. Dòng còn thiếu một nửa thì việc cần làm rõ
   * ràng là nối nửa còn lại — mở ngăn kéo cho nó là dẫn vào một biểu mẫu sửa vai trò của
   * một tài khoản không tồn tại.
   */
  protected rowAction(member: MemberListItem): void {
    if (member.employeeId === null || member.userId === null) {
      this.openLink(member);

      return;
    }

    this.openDetail(member);
  }

  /** Nhãn của nút thao tác — phải nói ĐÚNG việc nó sắp làm, vì cùng một biểu tượng ba chấm. */
  protected rowActionKey(member: MemberListItem): string {
    if (member.userId === null) {
      return 'users.link.linkAccount';
    }

    return member.employeeId === null ? 'users.link.linkProfile' : 'users.openDetail';
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
  protected canDisable(user: MemberListItem): boolean {
    // Dòng chưa có tài khoản thì không có gì để vô hiệu hoá — vô hiệu hoá là thao tác lên
    // TÀI KHOẢN, không phải lên hồ sơ. Người đã nghỉ việc thì đóng hồ sơ, việc khác.
    return (
      user.userId !== null &&
      user.userId !== this.auth.user()?.userId &&
      user.roleName !== 'Owner'
    );
  }

  /**
   * Câu trạng thái của một dòng — bốn ca, và thứ tự kiểm có nghĩa.
   *
   * "Chưa có tài khoản" phải đứng TRƯỚC mọi ca khác: người đó chưa đăng nhập được lần nào,
   * nên nói họ "đang hoạt động" là sai, mà nói "đã vô hiệu hoá" cũng sai.
   */
  protected statusKey(user: MemberListItem): string {
    if (user.userId === null) {
      return 'users.status.noAccount';
    }

    if (!user.isActive) {
      // Có hồ sơ mà hồ sơ đã đóng → nghỉ việc. Không có hồ sơ → tài khoản bị vô hiệu.
      return user.employeeId !== null ? 'users.status.left' : 'users.status.disabled';
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
