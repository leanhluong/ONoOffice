import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { type Observable, shareReplay } from 'rxjs';
import type { BaiHuongDan, NhomHuongDan } from '../models/help.model';

/**
 * Nội dung hướng dẫn — tải từ `public/huong-dan/`, KHÔNG đi qua backend.
 *
 * ═══════════════════════════════════════════════════════════════════════
 *  VÌ SAO KHÔNG PHẢI MỘT ENDPOINT
 * ═══════════════════════════════════════════════════════════════════════
 *
 * Nội dung này là **tệp tĩnh sinh ra lúc build**, không phải dữ liệu của workspace. Cho nó
 * đi qua API thì kéo theo cả một dây chuyền không cần thiết: một bảng, một migration, một
 * màn soạn thảo, và một câu hỏi "workspace nào được sửa hướng dẫn". Không ai cần thứ đó —
 * hướng dẫn giống nhau với mọi công ty, và nó đổi cùng nhịp với code.
 *
 * Hệ quả có ích: màn này chạy được **cả khi backend chết**. Người dùng gặp lỗi lạ vẫn mở
 * được hướng dẫn để tra, đúng lúc họ cần nó nhất.
 *
 * ⚠️ Vì vậy đường dẫn ở đây KHÔNG mang `environment.apiBaseUrl` — nó trỏ vào chính máy chủ
 * đang phục vụ app. Nhét base URL vào là gửi request tài liệu sang backend, và ở bản deploy
 * hai tên miền thì nó 404.
 */
@Injectable({ providedIn: 'root' })
export class HelpService {
  private readonly http = inject(HttpClient);

  /**
   * Chỉ mục dùng ở MỌI màn của phần hướng dẫn — cột trái, trang chủ, và cả nút trước/sau.
   *
   * `shareReplay(1)` để nó tải đúng một lần cho cả phiên: chuyển giữa các bài mà lần nào
   * cũng kéo lại cây bên trái thì cột đó nháy một cái mỗi lần bấm.
   */
  private readonly chiMuc$ = this.http
    .get<NhomHuongDan[]>('huong-dan/chi-muc.json')
    .pipe(shareReplay({ bufferSize: 1, refCount: false }));

  chiMuc(): Observable<NhomHuongDan[]> {
    return this.chiMuc$;
  }

  /**
   * Một bài.
   *
   * KHÔNG `shareReplay` ở đây: giữ lại mọi bài đã mở nghĩa là giữ luôn trong bộ nhớ cả
   * những bài người dùng đọc lướt rồi bỏ. Trình duyệt đã cache tệp tĩnh rồi — thêm một
   * lớp cache nữa chỉ để tiết kiệm một lần đọc từ đĩa là không đáng.
   */
  bai(ma: string): Observable<BaiHuongDan> {
    return this.http.get<BaiHuongDan>(`huong-dan/${encodeURIComponent(ma)}.json`);
  }
}
