import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import type { PagedList } from '../models/user.model';
import type {
  ContactListItem,
  ContactQuery,
  CreateDepartmentRequest,
  CreateEmployeeRequest,
  CreateEmployeeResponse,
  DepartmentTreeItem,
  MemberListItem,
} from '../models/org.model';

/**
 * Cầu nối tới module Org: `/api/departments` và `/api/contacts`.
 *
 * Cùng ranh giới trách nhiệm với `UserService`: gọi HTTP, không điều hướng, không giữ
 * trạng thái màn hình. Bộ lọc đang bật và phòng đang chọn thuộc về component — để service
 * còn dùng lại được ở chỗ khác mà không kéo theo trạng thái của màn Danh bạ.
 */
@Injectable({ providedIn: 'root' })
export class OrgService {
  private readonly http = inject(HttpClient);

  // ── Phòng ban ─────────────────────────────────────────────────────

  /** Toàn bộ cây, đã nối sẵn cha con. Không phân trang: cây bị cắt trang thì hết là cây. */
  departmentTree(): Observable<DepartmentTreeItem[]> {
    return this.http.get<DepartmentTreeItem[]>(this.url('/api/departments'));
  }

  createDepartment(request: CreateDepartmentRequest): Observable<unknown> {
    return this.http.post(this.url('/api/departments'), request);
  }

  renameDepartment(id: string, name: string): Observable<void> {
    return this.http.patch<void>(this.url(`/api/departments/${id}`), { name });
  }

  /**
   * Điều chuyển sang phòng cha khác, hoặc nâng lên làm gốc (`parentId = null`).
   *
   * Đường dẫn riêng chứ không gộp vào `PATCH`: đổi tên và điều chuyển là hai hành động có
   * hậu quả khác hẳn nhau — cái sau có thể tạo vòng lặp và làm cả một nhánh biến mất khỏi
   * cây. Backend chặn ca đó bằng `Department.WouldCreateCycle`.
   */
  moveDepartment(id: string, parentId: string | null): Observable<void> {
    return this.http.post<void>(this.url(`/api/departments/${id}/move`), { parentId });
  }

  deleteDepartment(id: string): Observable<void> {
    return this.http.delete<void>(this.url(`/api/departments/${id}`));
  }

  // ── Thành viên (gộp tài khoản + hồ sơ) ────────────────────────────

  /**
   * Một danh sách người duy nhất, gộp từ hai module.
   *
   * <b>Chỉ ĐỌC.</b> Mọi thao tác sửa vẫn đi về đúng module sở hữu dữ liệu: `UserService`
   * cho tài khoản, `OrgService` cho hồ sơ. Gộp cả phần ghi vào một endpoint thì nó phải
   * biết luật của cả hai module, và trở thành chỗ hai bộ luật lệch nhau.
   *
   * Không phân trang: bên gọi cần toàn bộ để lọc theo loại dòng.
   */
  members(): Observable<MemberListItem[]> {
    return this.http.get<MemberListItem[]>(this.url('/api/members'));
  }

  /**
   * Nối một hồ sơ nhân sự với một tài khoản đăng nhập.
   *
   * Đường dẫn nằm dưới `/api/employees` chứ không phải `/api/members`: nó GHI vào hồ sơ —
   * `Employee.UserId` — nên nó thuộc về module sở hữu dữ liệu đó. `/api/members` chỉ đọc.
   *
   * Backend đòi `user.manage` chứ không phải `employee.write`: nối là quyết định về việc
   * AI ĐĂNG NHẬP ĐƯỢC dưới danh nghĩa hồ sơ nào — một quyết định về tài khoản.
   */
  linkAccount(employeeId: string, userId: string): Observable<void> {
    return this.http.post<void>(this.url(`/api/employees/${employeeId}/link-account`), { userId });
  }

  /** Gỡ liên kết. Không mất gì: hồ sơ còn, tài khoản còn, chỉ tách lại thành hai dòng. */
  unlinkAccount(employeeId: string): Observable<void> {
    return this.http.post<void>(this.url(`/api/employees/${employeeId}/unlink-account`), {});
  }

  /**
   * Điều chuyển một nhân viên sang phòng khác, hoặc rút khỏi mọi phòng (`null`).
   *
   * Đường riêng chứ không gộp vào `PATCH /api/employees/{id}`: điều chuyển có hậu quả
   * riêng (đổi người quản lý, đổi luồng duyệt), và backend cũng tách nó ra vì thế.
   */
  transferEmployee(id: string, departmentId: string | null): Observable<void> {
    return this.http.post<void>(this.url(`/api/employees/${id}/transfer`), { departmentId });
  }

  /** Tạo hồ sơ nhân sự. Trả về `id` để bên gọi nối luôn vào một tài khoản nếu cần. */
  createEmployee(request: CreateEmployeeRequest): Observable<CreateEmployeeResponse> {
    return this.http.post<CreateEmployeeResponse>(this.url('/api/employees'), request);
  }

  // ── Danh bạ ───────────────────────────────────────────────────────

  contacts(query: ContactQuery): Observable<PagedList<ContactListItem>> {
    let params = new HttpParams();

    // Chỉ gửi tham số CÓ giá trị. Gửi `search=` rỗng thì URL dài thêm mà không đổi kết
    // quả — và nó làm người dùng tưởng đang có bộ lọc bật khi nhìn thanh địa chỉ.
    if (query.search) {
      params = params.set('search', query.search);
    }

    if (query.departmentId) {
      params = params.set('departmentId', query.departmentId);
    }

    if (query.includeInactive) {
      params = params.set('includeInactive', 'true');
    }

    if (query.page && query.page > 1) {
      params = params.set('page', String(query.page));
    }

    if (query.pageSize) {
      params = params.set('pageSize', String(query.pageSize));
    }

    return this.http.get<PagedList<ContactListItem>>(this.url('/api/contacts'), { params });
  }

  private url(path: string): string {
    return `${environment.apiBaseUrl}${path}`;
  }
}
