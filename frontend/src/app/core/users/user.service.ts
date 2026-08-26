import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import type {
  ChangePasswordRequest,
  CreateUserRequest,
  CreateUserResponse,
  PagedList,
  RoleListItem,
  UpdateUserRequest,
  UserListItem,
  MyProfile,
  UserQuery,
} from '../models/user.model';
import { UserStatusFilter } from '../models/user.model';

/**
 * Cầu nối tới `/api/users` và `/api/roles`.
 *
 * Cùng ranh giới trách nhiệm với <c>AuthService</c>: gọi HTTP, không điều hướng, không giữ
 * trạng thái màn hình. Trạng thái của bảng (đang lọc gì, trang mấy) thuộc về component —
 * để service còn dùng lại được ở chỗ khác mà không kéo theo bộ lọc của màn Nhân sự.
 */
@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);

  list(query: UserQuery): Observable<PagedList<UserListItem>> {
    let params = new HttpParams();

    // Chỉ gửi tham số CÓ giá trị. Gửi `search=` rỗng hay `status=0` thì URL dài thêm mà
    // không đổi kết quả — và nó làm người dùng tưởng đang có bộ lọc bật khi nhìn thanh
    // địa chỉ.
    if (query.search) {
      params = params.set('search', query.search);
    }

    if (query.status !== undefined && query.status !== UserStatusFilter.Any) {
      params = params.set('status', String(query.status));
    }

    if (query.roleId) {
      params = params.set('roleId', query.roleId);
    }

    if (query.page && query.page > 1) {
      params = params.set('page', String(query.page));
    }

    if (query.pageSize) {
      params = params.set('pageSize', String(query.pageSize));
    }

    return this.http.get<PagedList<UserListItem>>(this.url('/api/users'), { params });
  }

  create(request: CreateUserRequest): Observable<CreateUserResponse> {
    return this.http.post<CreateUserResponse>(this.url('/api/users'), request);
  }

  update(id: string, request: UpdateUserRequest): Observable<void> {
    return this.http.patch<void>(this.url(`/api/users/${id}`), request);
  }

  /**
   * Đổi CHỈ vai trò, không đụng tới tên.
   *
   * Đường riêng chứ không dùng `update()`: `PATCH` đòi cả `fullName`, nên đổi vai hàng
   * loạt qua nó nghĩa là gửi lại cái tên đã tải về vài giây trước — ai vừa được đổi tên
   * trong khoảng đó sẽ bị **ghi đè ngược** bằng tên cũ, mà không có gì báo.
   */
  changeRole(id: string, roleId: string): Observable<void> {
    return this.http.post<void>(this.url(`/api/users/${id}/role`), { roleId });
  }

  /**
   * Vô hiệu hoá hoặc bật lại.
   *
   * Hai đường dẫn khác nhau chứ không phải một `PATCH { isActive }`: đây là hai HÀNH ĐỘNG
   * có hậu quả khác nhau, và backend chặn chúng bằng những luật khác nhau (không tự khoá
   * mình, không khoá chủ sở hữu). Một endpoint nhận cờ boolean thì hai luật đó nằm chung
   * một chỗ và dễ thiếu một nhánh.
   */
  setActive(id: string, isActive: boolean): Observable<void> {
    const action = isActive ? 'enable' : 'disable';

    return this.http.post<void>(this.url(`/api/users/${id}/${action}`), {});
  }

  // ── Tài khoản của tôi ─────────────────────────────────────────────
  //
  // Ở cùng service với /api/users vì cùng một vùng nghiệp vụ (tài khoản người dùng), và
  // tách ra thành service riêng chỉ để có thêm một file thì không đổi được gì.

  myProfile(): Observable<MyProfile> {
    return this.http.get<MyProfile>(this.url('/api/me'));
  }

  updateMyProfile(fullName: string): Observable<void> {
    return this.http.patch<void>(this.url('/api/me'), { fullName });
  }

  /** Thành công thì MỌI phiên khác bị thu hồi — kể cả điện thoại của chính người dùng. */
  changeMyPassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.post<void>(this.url('/api/me/password'), request);
  }

  roles(): Observable<RoleListItem[]> {
    return this.http.get<RoleListItem[]>(this.url('/api/roles'));
  }

  private url(path: string): string {
    return `${environment.apiBaseUrl}${path}`;
  }
}
