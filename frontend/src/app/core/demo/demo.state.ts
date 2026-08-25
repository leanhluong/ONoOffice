import { Permissions } from './demo.permissions';
import type { RoleListItem, UserListItem } from '../models/user.model';

/**
 * Dữ liệu giả của CHẾ ĐỘ DEMO — sống trong bộ nhớ, mất khi tải lại trang.
 *
 * ⚠️ Đọc `demo.interceptor.ts` trước khi sửa gì ở đây; nó giải thích vì sao chế độ này
 * tồn tại và những gì nó cố ý KHÔNG mô phỏng.
 *
 * Con số và tên người chép thẳng từ bản dựng `docs/07-giao-dien/org/nhan-su.html` và
 * `docs/07-giao-dien/khung/quan-tri.html`, để bấm thử trên app thật thấy đúng thứ đã
 * duyệt trên bản dựng.
 */

/** Bốn vai hệ thống, quyền chép ĐÚNG từ `SystemRoles.cs`. */
export const VAI_TRO: RoleListItem[] = [
  {
    id: 'r-owner',
    name: 'Owner',
    isSystem: true,
    permissions: [...Permissions.ALL],
    memberCount: 1,
  },
  {
    id: 'r-admin',
    name: 'Admin',
    isSystem: true,
    // Admin = tất cả TRỪ đúng một quyền: chuyển nhượng quyền sở hữu. Đó là toàn bộ ranh
    // giới giữa Admin và Owner — cho Admin chuyển nhượng thì hai vai thành một.
    permissions: Permissions.ALL.filter((p) => p !== Permissions.TRANSFER_OWNERSHIP),
    memberCount: 1,
  },
  { id: 'r-manager', name: 'Manager', isSystem: true, permissions: ['employee.read'], memberCount: 1 },
  { id: 'r-member', name: 'Member', isSystem: true, permissions: ['employee.read'], memberCount: 35 },
];

/** Ngày tạo tính LÙI từ hôm nay, để thẻ "Cần bạn xử lý" luôn có số ngày hợp lý. */
function ngayTruoc(soNgay: number): string {
  return new Date(Date.now() - soNgay * 86_400_000).toISOString();
}

export const NGUOI_DUNG: UserListItem[] = [
  {
    id: 'u-owner',
    email: 'chu@congty.vn',
    fullName: 'Lê Anh Lượng',
    isActive: true,
    mustChangePassword: false,
    roleName: 'Owner',
    createdAtUtc: ngayTruoc(600),
  },
  {
    id: 'u-binh',
    email: 'binh.tran@congty.vn',
    fullName: 'Trần Bình',
    isActive: true,
    mustChangePassword: false,
    roleName: 'Manager',
    createdAtUtc: ngayTruoc(180),
  },
  {
    id: 'u-an',
    email: 'an.nguyen@congty.vn',
    fullName: 'Nguyễn An',
    isActive: true,
    mustChangePassword: false,
    roleName: 'Member',
    createdAtUtc: ngayTruoc(90),
  },
  {
    id: 'u-ha',
    email: 'ha.pham@congty.vn',
    fullName: 'Phạm Hà',
    isActive: true,
    mustChangePassword: false,
    roleName: 'Admin',
    createdAtUtc: ngayTruoc(150),
  },

  // Ba người còn mật khẩu tạm — đây là thứ thẻ "Cần bạn xử lý" ở màn Tổng quan đọc.
  {
    id: 'u-ngocha',
    email: 'ha.do@congty.vn',
    fullName: 'Đỗ Ngọc Hà',
    isActive: true,
    mustChangePassword: true,
    roleName: 'Member',
    createdAtUtc: ngayTruoc(6),
  },
  {
    id: 'u-linh',
    email: 'linh.tran@congty.vn',
    fullName: 'Trần Thuỳ Linh',
    isActive: true,
    mustChangePassword: true,
    roleName: 'Member',
    createdAtUtc: ngayTruoc(4),
  },
  {
    id: 'u-minh',
    email: 'minh.vu@congty.vn',
    fullName: 'Vũ Đức Minh',
    isActive: true,
    mustChangePassword: true,
    roleName: 'Member',
    createdAtUtc: ngayTruoc(1),
  },

  // Hai người bị vô hiệu hoá.
  {
    id: 'u-mai',
    email: 'mai.vu@congty.vn',
    fullName: 'Vũ Thị Mai',
    isActive: false,
    mustChangePassword: false,
    roleName: 'Member',
    createdAtUtc: ngayTruoc(300),
  },
  {
    id: 'u-khoa',
    email: 'khoa.do@congty.vn',
    fullName: 'Đỗ Minh Khoa',
    isActive: false,
    mustChangePassword: false,
    roleName: 'Member',
    createdAtUtc: ngayTruoc(220),
  },
];

/**
 * Kho dữ liệu của phiên demo.
 *
 * Là một object có thể sửa tại chỗ chứ không phải hằng: tạo người mới, đổi vai, vô hiệu
 * hoá đều phải THẤY được ngay trên bảng — nếu không thì nửa số thao tác của màn Thành
 * viên không bấm thử được, và đó chính là nửa quan trọng hơn.
 */
export const kho = {
  users: [...NGUOI_DUNG],
  roles: [...VAI_TRO],
  toi: {
    id: 'u-owner',
    tenantId: 't-acme',
    email: 'chu@congty.vn',
    fullName: 'Lê Anh Lượng',
    roleName: 'Owner',
    isOwner: true,
    mustChangePassword: false,
  },
};
