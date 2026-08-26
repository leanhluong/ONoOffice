import { Permissions } from './demo.permissions';
import type { RoleListItem, UserListItem } from '../models/user.model';
import type { ContactListItem, DepartmentTreeItem } from '../models/org.model';

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

  /*
    Đỗ Ngọc Hà xuất hiện HAI LẦN trong danh sách gộp, và đó là chủ ý.

    Tài khoản này (`u-ngocha`) và hồ sơ `e-ngocha` là cùng một con người, nhưng CHƯA NỐI
    với nhau — người này tạo tài khoản, người kia dựng hồ sơ, không ai nối lại. Đây là
    trạng thái có thật và phổ biến nhất mà tính năng "Nối hồ sơ ↔ tài khoản" sinh ra để sửa.

    Nó cũng là bằng chứng sống cho việc KHÔNG tự ghép theo email: hai dòng có cùng email
    `ha.do@congty.vn` mà hệ thống vẫn để riêng, chờ người quyết định. Tự ghép thì tiện hơn
    đúng ở ca này và sai ở ca phòng kinh doanh dùng chung `sales@`.
  */
  {
    id: 'u-ngocha',
    email: 'ha.do@congty.vn',
    fullName: 'Đỗ Ngọc Hà',
    isActive: true,
    mustChangePassword: true,
    roleName: 'Member',
    createdAtUtc: ngayTruoc(6),
  },

  // Hai người nữa còn mật khẩu tạm — thẻ "Cần bạn xử lý" ở màn Tổng quan đọc chính chỗ này.
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

  /*
    Một tài khoản KHÔNG PHẢI người — loại dòng thứ ba của màn Thành viên gộp.

    Tài khoản máy chạy sao lưu đêm. Nó không có hồ sơ nhân sự và sẽ không bao giờ có: nó
    không đi làm, không thuộc phòng nào, không có mã nhân viên. Đây là lý do màn gộp phải
    lấy cả những tài khoản không nối được hồ sơ — bỏ chúng đi thì một tài khoản quyền
    Admin biến mất khỏi mọi bảng, và đó đúng là thứ không được phép mất dấu.
  */
  {
    id: 'u-bot',
    email: 'bot@congty.vn',
    fullName: 'backup-bot',
    isActive: true,
    mustChangePassword: false,
    roleName: 'Admin',
    createdAtUtc: ngayTruoc(400),
  },
];

/**
 * Kho dữ liệu của phiên demo.
 *
 * Là một object có thể sửa tại chỗ chứ không phải hằng: tạo người mới, đổi vai, vô hiệu
 * hoá đều phải THẤY được ngay trên bảng — nếu không thì nửa số thao tác của màn Thành
 * viên không bấm thử được, và đó chính là nửa quan trọng hơn.
 */
/**
 * Cây phòng ban, chép từ bản dựng `docs/07-giao-dien/org/phong-ban.html`.
 *
 * `employeeCount` là số người TRỰC TIẾP, không cộng dồn phòng con — đúng như backend trả
 * về. Cộng dồn ở đây thì bấm thử trên demo sẽ thấy một hành vi mà sản phẩm thật không có.
 */
export const PHONG_BAN: DepartmentTreeItem[] = [
  {
    id: 'd-bgd',
    name: 'Ban giám đốc',
    parentId: null,
    headEmployeeId: 'e-luong',
    headName: 'Lê Anh Lượng',
    employeeCount: 2,
    children: [
      {
        id: 'd-kythuat',
        name: 'Khối Kỹ thuật',
        parentId: 'd-bgd',
        headEmployeeId: 'e-binh',
        headName: 'Trần Bình',
        employeeCount: 3,
        children: [
          {
            id: 'd-sanpham',
            name: 'Phát triển sản phẩm',
            parentId: 'd-kythuat',
            headEmployeeId: null,
            headName: null,
            employeeCount: 12,
            children: [],
          },
          {
            id: 'd-hatang',
            name: 'Hạ tầng',
            parentId: 'd-kythuat',
            headEmployeeId: 'e-an',
            headName: 'Nguyễn An',
            employeeCount: 5,
            children: [],
          },
        ],
      },
      {
        id: 'd-kinhdoanh',
        name: 'Kinh doanh',
        parentId: 'd-bgd',
        headEmployeeId: 'e-ha',
        headName: 'Phạm Hà',
        employeeCount: 9,
        children: [],
      },
    ],
  },
  {
    id: 'd-nhansu',
    name: 'Nhân sự',
    parentId: null,
    headEmployeeId: null,
    headName: null,
    employeeCount: 4,
    children: [],
  },
];

/**
 * Hồ sơ nhân sự — KHÁC tài khoản đăng nhập ở `NGUOI_DUNG`. Xem `Employee.cs`.
 *
 * `userId` KHÔNG có trong `ContactListItem` thật, và đó là chủ ý: API danh bạ không lộ
 * khoá của module Identity ra ngoài. Ở đây thêm vào vì kho demo phải mô phỏng cả phép
 * NỐI mà backend làm qua `Employee.UserId` — nếu không thì màn Thành viên gộp sẽ hiện mọi
 * người hai lần.
 */
type DemoHoSo = ContactListItem & { userId: string | null };

export const HO_SO: DemoHoSo[] = [
  {
    id: 'e-luong', code: 'NV001', fullName: 'Lê Anh Lượng', jobTitle: 'Giám đốc',
    workEmail: 'chu@congty.vn', phone: '090 123 4567',
    departmentId: 'd-bgd', departmentName: 'Ban giám đốc', isActive: true, userId: 'u-owner',
  },
  {
    id: 'e-binh', code: 'NV002', fullName: 'Trần Bình', jobTitle: 'Trưởng khối Kỹ thuật',
    workEmail: 'binh.tran@congty.vn', phone: '091 234 5678',
    departmentId: 'd-kythuat', departmentName: 'Khối Kỹ thuật', isActive: true, userId: 'u-binh',
  },
  // Không có điện thoại — ca để thử rằng thẻ BỎ HẲN dòng chứ không vẽ ô rỗng.
  {
    id: 'e-an', code: 'NV003', fullName: 'Nguyễn An', jobTitle: 'Kỹ sư hạ tầng',
    workEmail: 'an.nguyen@congty.vn', phone: null,
    departmentId: 'd-hatang', departmentName: 'Hạ tầng', isActive: true, userId: 'u-an',
  },
  {
    id: 'e-ha', code: 'NV004', fullName: 'Phạm Hà', jobTitle: 'Trưởng phòng Kinh doanh',
    workEmail: 'ha.pham@congty.vn', phone: '098 765 4321',
    departmentId: 'd-kinhdoanh', departmentName: 'Kinh doanh', isActive: true, userId: 'u-ha',
  },
  // Chưa xếp phòng là trạng thái BÌNH THƯỜNG của người mới, không phải lỗi dữ liệu.
  {
    id: 'e-ngocha', code: 'NV005', fullName: 'Đỗ Ngọc Hà', jobTitle: 'Thực tập sinh',
    workEmail: 'ha.do@congty.vn', phone: null,
    departmentId: null, departmentName: null, isActive: true, userId: null,
  },
  {
    id: 'e-minh', code: 'NV006', fullName: 'Vũ Đức Minh', jobTitle: 'Lập trình viên',
    workEmail: 'minh.vu@congty.vn', phone: '097 111 2222',
    departmentId: 'd-sanpham', departmentName: 'Phát triển sản phẩm', isActive: true, userId: 'u-minh',
  },
  // Đã nghỉ: mặc định KHÔNG hiện, chỉ ra khi bật công tắc.
  {
    id: 'e-mai', code: 'NV007', fullName: 'Vũ Thị Mai', jobTitle: 'Nhân viên kinh doanh',
    workEmail: 'mai.vu@congty.vn', phone: null,
    departmentId: 'd-kinhdoanh', departmentName: 'Kinh doanh', isActive: false, userId: 'u-mai',
  },
  {
    id: 'e-linh', code: 'NV008', fullName: 'Trần Thuỳ Linh', jobTitle: 'Chuyên viên nhân sự',
    workEmail: 'linh.tran@congty.vn', phone: '093 456 7890',
    departmentId: 'd-nhansu', departmentName: 'Nhân sự', isActive: true, userId: 'u-linh',
  },
  /*
    Nghỉ việc: đóng CẢ hồ sơ lẫn tài khoản.

    `MemberListItem` chỉ có một cờ `isActive` cho cả dòng — nó là phép AND của hai bên,
    nên bật một tắt một sẽ ra một dòng nói "đã nghỉ" trong khi hồ sơ vẫn đang mở. Muốn
    demo được ca "khoá đăng nhập tạm nhưng chưa nghỉ" thì phải tách cờ ở DTO trước đã.
  */
  {
    id: 'e-khoa', code: 'NV009', fullName: 'Đỗ Minh Khoa', jobTitle: 'Lập trình viên',
    workEmail: 'khoa.do@congty.vn', phone: null,
    departmentId: 'd-sanpham', departmentName: 'Phát triển sản phẩm', isActive: false, userId: 'u-khoa',
  },
];

export const kho = {
  users: [...NGUOI_DUNG],
  roles: [...VAI_TRO],
  phongBan: PHONG_BAN,
  hoSo: [...HO_SO],
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
