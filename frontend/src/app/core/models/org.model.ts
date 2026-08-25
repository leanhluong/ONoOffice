/**
 * Hợp đồng dữ liệu với `/api/departments` và `/api/contacts` — module Org.
 *
 * Nguồn sự thật là các record trong `ONoOffice.Org.Application`. Có
 * `org-contract.spec.ts` đọc thẳng file C# ra rồi đối chiếu tên trường, cùng cách
 * `user-contract.spec.ts` canh enum `UserStatusFilter`.
 */

/**
 * Một nút trên cây phòng ban, đã nối sẵn con.
 *
 * `employeeCount` là số người TRỰC TIẾP thuộc phòng này, <b>không cộng dồn phòng con</b>.
 * Cộng dồn thì tổng công ty hiện ở phòng gốc và mọi phòng con bị đếm hai lần khi mắt
 * lướt qua.
 */
export interface DepartmentTreeItem {
  id: string;
  name: string;
  parentId: string | null;
  headEmployeeId: string | null;
  headName: string | null;
  employeeCount: number;
  children: DepartmentTreeItem[];
}

/** Thân của `POST /api/departments`. */
export interface CreateDepartmentRequest {
  name: string;
  parentId: string | null;
}

/** Thân của `PATCH /api/departments/{id}`. */
export interface RenameDepartmentRequest {
  name: string;
}

/** Thân của `POST /api/departments/{id}/move`. `null` = nâng lên làm phòng gốc. */
export interface MoveDepartmentRequest {
  parentId: string | null;
}

/**
 * Một dòng trên màn Danh bạ.
 *
 * ⚠️ Đây là HỒ SƠ NHÂN SỰ (module Org), không phải tài khoản đăng nhập (module Identity).
 * Một người có thể có hồ sơ mà chưa có tài khoản — nhân viên mới; hoặc có tài khoản mà
 * không phải nhân viên — tài khoản bot chạy sao lưu.
 */
export interface ContactListItem {
  id: string;
  code: string;
  fullName: string;
  jobTitle: string | null;
  workEmail: string | null;
  phone: string | null;
  departmentId: string | null;
  departmentName: string | null;
  isActive: boolean;
}

/** Tham số của `GET /api/contacts`. Mọi trường đều tuỳ chọn. */
export interface ContactQuery {
  search?: string;
  departmentId?: string;
  includeInactive?: boolean;
  page?: number;
  pageSize?: number;
}
