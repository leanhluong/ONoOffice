/**
 * Hợp đồng dữ liệu với `/api/users` và `/api/roles`.
 *
 * Nguồn sự thật là `docs/05-api/README.md`. Đã đối chiếu với backend chạy thật
 * ngày 2026-08-24.
 */

/**
 * Lọc theo trạng thái. Con số phải khớp enum `UserStatusFilter` ở backend — có test
 * đối chiếu canh, xem `user-contract.spec.ts`.
 */
export enum UserStatusFilter {
  Any = 0,
  Active = 1,

  /** Đã tạo tài khoản nhưng chưa từng đăng nhập — vẫn còn mật khẩu tạm. */
  PendingFirstLogin = 2,

  Disabled = 3,
}

/** Một dòng trên bảng Nhân sự. */
export interface UserListItem {
  id: string;
  email: string;
  fullName: string;
  isActive: boolean;
  mustChangePassword: boolean;

  /** Tên vai trò, không phải mã — backend đã tra hộ để tránh N+1. */
  roleName: string;

  createdAtUtc: string;
}

/** Khuôn phân trang chung của `Luong.Kernel.Pagination.PagedList`. */
export interface PagedList<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

/** Tham số của `GET /api/users`. Mọi trường đều tuỳ chọn. */
export interface UserQuery {
  search?: string;
  status?: UserStatusFilter;
  roleId?: string;
  page?: number;
  pageSize?: number;
}

/** Thân của `POST /api/users`. */
export interface CreateUserRequest {
  fullName: string;
  email: string;
  roleId: string;
  mustChangePassword: boolean;
}

/**
 * `POST /api/users` → 200.
 *
 * ⚠️ `temporaryPassword` là lần DUY NHẤT chuỗi thô đó tồn tại ngoài đầu người tạo. Không
 * ghi log, không lưu, và không endpoint nào đọc lại được. Quên thì phải đặt lại mật khẩu.
 */
export interface CreateUserResponse {
  id: string;
  email: string;
  fullName: string;
  roleName: string;
  temporaryPassword: string;
}

/** Thân của `PATCH /api/users/{id}`. */
export interface UpdateUserRequest {
  fullName: string;
  roleId: string;
}

/** Một vai trò, từ `GET /api/roles`. */
export interface RoleListItem {
  id: string;
  name: string;

  /** Vai hệ thống thì giao diện khoá bảng quyền — sửa sẽ bị bản nâng cấp sau ghi đè. */
  isSystem: boolean;

  permissions: string[];
  memberCount: number;
}
