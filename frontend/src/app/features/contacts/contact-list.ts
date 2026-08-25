import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { OrgService } from '../../core/org/org.service';
import type { ContactListItem, DepartmentTreeItem } from '../../core/models/org.model';
import type { PagedList } from '../../core/models/user.model';

/** Một dòng trên cột lọc: phòng ban đã được ép phẳng, kèm độ sâu để thụt lề. */
interface FilterRow {
  readonly id: string;
  readonly name: string;
  readonly count: number;
  readonly depth: number;
}

/**
 * Màn <b>Danh bạ</b> — khung app, ai đăng nhập cũng vào được.
 *
 * <b>Đừng nhầm với màn Thành viên bên quản trị.</b> Cùng nói về con người, nhưng:
 * ở đây mọi nhân viên TRA CỨU hồ sơ đồng nghiệp (module Org, quyền `employee.read`);
 * ở đó quản trị viên SỬA tài khoản đăng nhập của người khác (module Identity, `user.read`).
 * Một người có thể có hồ sơ mà chưa có tài khoản, hoặc ngược lại.
 *
 * <b>Thẻ, không phải bảng.</b> Người ta mở danh bạ để tìm MỘT người rồi gọi điện, không
 * phải để so sánh 38 dòng. Thẻ cho chỗ đặt số điện thoại ở kích thước bấm được bằng ngón
 * tay; bảng thì nhét nó vào ô hẹp rồi cắt bớt.
 *
 * Nguồn thiết kế: `docs/07-giao-dien/org/danh-ba.html`.
 */
@Component({
  selector: 'app-contact-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, TranslatePipe],
  templateUrl: './contact-list.html',
  styleUrl: './contact-list.scss',
})
export class ContactList {
  private readonly org = inject(OrgService);

  protected readonly page = signal<PagedList<ContactListItem> | null>(null);
  protected readonly tree = signal<DepartmentTreeItem[]>([]);

  /** `null` = "Tất cả". Chuỗi rỗng thì không dùng được vì id là GUID. */
  protected readonly departmentId = signal<string | null>(null);

  protected readonly includeInactive = signal(false);

  protected readonly search = new FormControl('', { nonNullable: true });

  /**
   * Cột lọc: cây ép phẳng, giữ độ sâu để thụt lề.
   *
   * Ép phẳng chứ không dựng cây lồng nhau như màn Phòng ban: ở đây cây chỉ là một danh
   * sách để BẤM CHỌN, không phải thứ người ta vào để ngắm. Một danh sách phẳng có thụt lề
   * đọc nhanh hơn và không cần cơ chế gập mở.
   */
  protected readonly filterRows = computed(() => epPhang(this.tree(), 0));

  /** Tên phòng đang chọn, để hiện ở tiêu đề trang. */
  protected readonly currentName = computed(() => {
    const id = this.departmentId();

    return id === null ? null : (this.filterRows().find((r) => r.id === id)?.name ?? null);
  });

  constructor() {
    this.org.departmentTree().subscribe({
      next: (tree) => this.tree.set(tree),
      // Cột lọc hỏng thì vẫn cho xem danh bạ đầy đủ. Chặn cả màn vì mất một BỘ LỌC là
      // phản ứng quá tay — người dùng còn ô tìm, và đó mới là thứ họ dùng nhiều nhất.
      error: () => this.tree.set([]),
    });

    // Chờ 300ms sau khi ngừng gõ. Không chờ thì mỗi ký tự là một request, và với người gõ
    // nhanh thì kết quả về không đúng thứ tự — chữ cuối cùng hiện ra lại là của lần gõ
    // giữa chừng.
    this.search.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(() => this.load());

    this.load();
  }

  protected chonPhong(id: string | null): void {
    this.departmentId.set(id);
    this.load();
  }

  protected doiHienNghi(checked: boolean): void {
    this.includeInactive.set(checked);
    this.load();
  }

  protected load(): void {
    this.org
      .contacts({
        search: this.search.value.trim() || undefined,
        departmentId: this.departmentId() ?? undefined,
        includeInactive: this.includeInactive(),
        pageSize: 60,
      })
      .subscribe({
        next: (page) => this.page.set(page),
        error: () => this.page.set(null),
      });
  }

  /**
   * Chữ cái đầu làm ảnh đại diện tạm: chữ đầu của TỪ ĐẦU và TỪ CUỐI.
   *
   * Không cắt hai ký tự đầu chuỗi — tên Việt "Lê Anh Lượng" sẽ ra "Lê", tức là cả họ, mà
   * họ thì trùng nhau đầy công ty.
   */
  protected initials(name: string): string {
    const words = name.trim().split(/\s+/).filter(Boolean);

    if (words.length === 0) {
      return '?';
    }

    return (words[0][0] + words[words.length - 1][0]).toUpperCase();
  }
}

function epPhang(nodes: readonly DepartmentTreeItem[], depth: number): FilterRow[] {
  return nodes.flatMap((n) => [
    { id: n.id, name: n.name, count: n.employeeCount, depth },
    ...epPhang(n.children, depth + 1),
  ]);
}
