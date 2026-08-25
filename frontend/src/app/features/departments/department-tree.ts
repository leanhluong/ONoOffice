import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { PopupService } from '../../core/ui/popup.service';
import { OrgService } from '../../core/org/org.service';
import type { DepartmentTreeItem } from '../../core/models/org.model';
import { DepartmentNode } from './department-node';

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
  imports: [TranslatePipe, DepartmentNode],
  templateUrl: './department-tree.html',
  styleUrl: './department-tree.scss',
})
export class DepartmentTree {
  private readonly org = inject(OrgService);
  private readonly popups = inject(PopupService);
  private readonly translate = inject(TranslateService);

  protected readonly tree = signal<DepartmentTreeItem[] | null>(null);
  protected readonly failed = signal(false);

  /**
   * Tổng số phòng, đếm ĐỆ QUY cả cây.
   *
   * Khác con số bên cạnh mỗi nút (số người trực tiếp, không cộng dồn): ở tiêu đề trang thì
   * "6 phòng" phải là tổng thật, vì đó là câu trả lời cho "công ty tôi có mấy phòng".
   */
  protected readonly departmentCount = computed(() => dem(this.tree() ?? []));

  /** Tổng số người, cũng đếm đệ quy — mỗi người chỉ thuộc đúng một phòng nên không trùng. */
  protected readonly peopleCount = computed(() => demNguoi(this.tree() ?? []));

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

  /** Nút chưa làm: nói thẳng, đừng im lặng. */
  protected notBuiltYet(event: Event, labelKey: string): void {
    event.preventDefault();

    const label = this.translate.instant(labelKey) as string;
    const suffix = this.translate.instant('login.comingSoon') as string;

    this.popups.show(`${label} — ${suffix}`);
  }
}

function dem(nodes: readonly DepartmentTreeItem[]): number {
  return nodes.reduce((tong, n) => tong + 1 + dem(n.children), 0);
}

function demNguoi(nodes: readonly DepartmentTreeItem[]): number {
  return nodes.reduce((tong, n) => tong + n.employeeCount + demNguoi(n.children), 0);
}
