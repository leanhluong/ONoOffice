import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { notBlank } from '../../core/forms/validators';
import { PopupService } from '../../core/ui/popup.service';
import { OrgService } from '../../core/org/org.service';
import type { DepartmentTreeItem } from '../../core/models/org.model';
import { DepartmentNode } from './department-node';

/** Việc đang làm trong hộp thoại. `null` = hộp đang đóng. */
type Viec = 'them' | 'doiTen' | 'chuyen' | null;

/** Một dòng trong danh sách xổ "trực thuộc": cây đã ép phẳng, giữ độ sâu để thụt lề. */
interface ChonCha {
  readonly id: string;
  readonly name: string;
  readonly depth: number;
}

/**
 * Màn <b>Phòng ban</b> — cây tổ chức, trong khung quản trị.
 *
 * Ở khung quản trị chứ không phải khung app vì màn này <b>sửa</b> cây tổ chức của cả công
 * ty. Xem cây thì ai cũng được, nhưng đó là màn Danh bạ và nó ở khung app.
 *
 * <b>Cây thật, không phải bảng phẳng có cột "phòng cha".</b> Bảng phẳng dễ dựng hơn nhiều
 * nhưng nó bắt người đọc tự nối quan hệ trong đầu — mà lý do duy nhất người ta mở màn này
 * là để NHÌN THẤY quan hệ đó.
 *
 * Nguồn thiết kế: `docs/07-giao-dien/org/phong-ban.html`.
 */
@Component({
  selector: 'app-department-tree',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, TranslatePipe, DepartmentNode],
  templateUrl: './department-tree.html',
  styleUrl: './department-tree.scss',
})
export class DepartmentTree {
  private readonly org = inject(OrgService);
  private readonly popups = inject(PopupService);
  private readonly translate = inject(TranslateService);
  private readonly fb = inject(FormBuilder);

  protected readonly tree = signal<DepartmentTreeItem[] | null>(null);
  protected readonly failed = signal(false);
  protected readonly saving = signal(false);

  /** Việc đang làm trong hộp thoại sửa. */
  protected readonly viec = signal<Viec>(null);

  /** Phòng đang được thao tác. `null` khi thêm mới ở mức gốc. */
  protected readonly target = signal<DepartmentTreeItem | null>(null);

  /** Phòng sắp bị xoá. Hộp RIÊNG vì xoá cần một câu hỏi khẳng định, không phải biểu mẫu. */
  protected readonly deleting = signal<DepartmentTreeItem | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    // `notBlank` chứ không chỉ `required`: `Validators.required` cho lọt chuỗi toàn khoảng
    // trắng, và một phòng ban tên là ba dấu cách thì biến mất khỏi mọi danh sách.
    name: ['', [Validators.required, notBlank, Validators.maxLength(100)]],
    parentId: [''],
  });

  protected readonly departmentCount = computed(() => dem(this.tree() ?? []));
  protected readonly peopleCount = computed(() => demNguoi(this.tree() ?? []));

  /**
   * Các phòng chọn được làm "trực thuộc".
   *
   * <b>Loại bỏ chính nó và toàn bộ nhánh của nó</b> khi đang chuyển. Backend cũng chặn
   * bằng `Department.WouldCreateCycle`, nhưng để người dùng chọn được một thứ chắc chắn
   * bị từ chối là bắt họ bấm rồi mới biết — trong khi ta đã có sẵn cả cây trong tay.
   */
  protected readonly parentOptions = computed<ChonCha[]>(() => {
    const bo = this.viec() === 'chuyen' ? this.target()?.id : undefined;

    return epPhang(this.tree() ?? [], 0, bo);
  });

  protected readonly dialogTitleKey = computed(() => {
    switch (this.viec()) {
      case 'doiTen':
        return 'action.rename';
      case 'chuyen':
        return 'departments.move';
      default:
        return 'departments.add';
    }
  });

  constructor() {
    this.load();
  }

  protected load(): void {
    this.org.departmentTree().subscribe({
      next: (tree) => {
        this.tree.set(tree);
        this.failed.set(false);
      },
      // Hỏng thì nói HỎNG, không hiện cây rỗng. Cây rỗng đọc như "công ty chưa có phòng
      // nào" — một câu trả lời SAI, không phải một câu trả lời thiếu.
      error: () => this.failed.set(true),
    });
  }

  // ── Mở hộp thoại ──────────────────────────────────────────────────

  protected openAdd(parent: DepartmentTreeItem | null): void {
    this.target.set(parent);
    this.form.reset({ name: '', parentId: parent?.id ?? '' });
    this.viec.set('them');
  }

  protected openRename(node: DepartmentTreeItem): void {
    this.target.set(node);
    this.form.reset({ name: node.name, parentId: node.parentId ?? '' });
    this.viec.set('doiTen');
  }

  protected openMove(node: DepartmentTreeItem): void {
    this.target.set(node);
    this.form.reset({ name: node.name, parentId: node.parentId ?? '' });
    this.viec.set('chuyen');
  }

  protected openDelete(node: DepartmentTreeItem): void {
    this.deleting.set(node);
  }

  protected close(): void {
    this.viec.set(null);
    this.deleting.set(null);
  }

  // ── Lưu ───────────────────────────────────────────────────────────

  protected save(): void {
    const viec = this.viec();

    // Chuyển phòng KHÔNG đụng tới tên, nên ô tên không hợp lệ cũng không cản được nó.
    if (viec === null || (viec !== 'chuyen' && this.form.controls.name.invalid)) {
      this.form.markAllAsTouched();

      return;
    }

    const name = this.form.controls.name.value.trim();
    const parentId = this.form.controls.parentId.value || null;
    const node = this.target();

    this.saving.set(true);

    const request =
      viec === 'them'
        ? this.org.createDepartment({ name, parentId })
        : viec === 'doiTen'
          ? this.org.renameDepartment(node!.id, name)
          : this.org.moveDepartment(node!.id, parentId);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.close();
        // Nạp lại CẢ cây, không sửa tại chỗ: chuyển một phòng đổi vị trí của cả một nhánh,
        // và số người của phòng cha cũ lẫn mới đều đổi. Sửa tại chỗ thì phải chép lại
        // đúng những luật server vừa áp, và hai bản luật sẽ lệch nhau.
        this.load();
        this.popups.show(this.translate.instant('departments.saved') as string);
      },
      // Không hiện popup: `errorInterceptor` đã dựng thông báo từ mã lỗi của server
      // (`Department.NameTaken`, `Department.WouldCreateCycle`…). Hiện thêm một câu của
      // riêng màn này thì người dùng nhận hai popup nói hai chuyện.
      error: () => this.saving.set(false),
    });
  }

  protected confirmDelete(): void {
    const node = this.deleting();

    if (!node) {
      return;
    }

    this.saving.set(true);

    this.org.deleteDepartment(node.id).subscribe({
      next: () => {
        this.saving.set(false);
        this.close();
        this.load();
        this.popups.show(this.translate.instant('departments.deleted') as string);
      },
      error: () => this.saving.set(false),
    });
  }
}

function dem(nodes: readonly DepartmentTreeItem[]): number {
  return nodes.reduce((tong, n) => tong + 1 + dem(n.children), 0);
}

function demNguoi(nodes: readonly DepartmentTreeItem[]): number {
  return nodes.reduce((tong, n) => tong + n.employeeCount + demNguoi(n.children), 0);
}

/** Ép cây thành danh sách phẳng, bỏ hẳn nhánh có gốc là `boQua`. */
function epPhang(
  nodes: readonly DepartmentTreeItem[],
  depth: number,
  boQua?: string,
): ChonCha[] {
  return nodes.flatMap((n) =>
    n.id === boQua
      ? []
      : [{ id: n.id, name: n.name, depth }, ...epPhang(n.children, depth + 1, boQua)],
  );
}
