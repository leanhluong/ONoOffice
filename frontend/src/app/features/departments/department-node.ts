import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import type { DepartmentTreeItem } from '../../core/models/org.model';

/**
 * Một nút trên cây phòng ban, và các nút con của nó.
 *
 * <b>Vì sao tách thành component riêng:</b> cây là cấu trúc đệ quy, và template Angular
 * không tự gọi lại chính nó được. Một component gọi chính nó trong template của mình là
 * cách duy nhất dựng cây sâu tuỳ ý mà không phải viết sẵn N cấp lồng nhau.
 *
 * Nó KHÔNG gọi API và không giữ dữ liệu — chỉ nhận một nút và vẽ. Mọi thao tác đẩy ngược
 * lên `DepartmentTree` bằng output, để chỉ có đúng MỘT chỗ biết cách nói chuyện với server.
 */
@Component({
  selector: 'app-department-node',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslatePipe],
  templateUrl: './department-node.html',

  /*
    CÙNG file style với `DepartmentTree`, và đây là chỗ đã hỏng một lần.

    Angular bó style theo TỪNG component. Đánh dấu của một nút nằm ở đây, nhưng luật CSS
    của nó (`.cay__nut`, `.cay__bieu`, `.cay__con`) được sinh vào `department-tree.scss`
    — nên nếu component này không khai file đó, nó không nhận được một luật nào: biểu
    tượng phình to hết màn hình vì `.cay__bieu { width: 17px }` không hề áp.

    Hỏng theo kiểu không có lỗi nào: build xanh, test xanh, `ui-parity` xanh (nó so tập
    lớp, mà lớp thì có đủ). Chỉ mở trình duyệt ra nhìn mới thấy.

    Cả hai trỏ vào MỘT file vì cả hai được sinh từ MỘT bản dựng
    (`docs/07-giao-dien/org/phong-ban.html`) — tách đôi thì lại có hai nguồn để lệch.
  */
  styleUrl: './department-tree.scss',
})
export class DepartmentNode {
  readonly node = input.required<DepartmentTreeItem>();

  /** Bấm một nút thao tác chưa làm. Đẩy lên cha để chỉ một chỗ dựng popup. */
  readonly notBuilt = output<{ event: Event; labelKey: string }>();

  /**
   * Mở sẵn ở mọi cấp.
   *
   * Gập sẵn thì người mở màn này thấy đúng một dòng và phải bấm từng cái để biết công ty
   * có gì — mà nhìn thấy toàn bộ cấu trúc chính là lý do họ vào đây. Một công ty 20–40
   * phòng vẫn vừa một màn hình.
   */
  protected readonly open = signal(true);

  protected toggle(): void {
    this.open.update((v) => !v);
  }

  protected chuaLam(event: Event, labelKey: string): void {
    this.notBuilt.emit({ event, labelKey });
  }
}
