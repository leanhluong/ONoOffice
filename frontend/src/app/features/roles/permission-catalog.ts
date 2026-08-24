/**
 * Nhãn tiếng Việt và cách nhóm cho từng mã quyền.
 *
 * <b>Vì sao bảng này nằm ở frontend chứ không đến từ API:</b> nó là CÂU CHỮ hiển thị, và
 * câu chữ thì phải dịch được sang từng ngôn ngữ. Trả từ server thì hoặc server phải biết
 * ngôn ngữ của người xem, hoặc mọi người nhận cùng một thứ tiếng.
 *
 * ⚠️ Đổi lại: bảng này có thể lạc khỏi <c>Permissions.cs</c>. Thêm một quyền ở backend mà
 * quên thêm nhãn ở đây thì màn Vai trò hiện ra một mã trần giữa những dòng có chữ — trông
 * như lỗi hiển thị. <c>permission-catalog.spec.ts</c> đọc thẳng file C# ra và so hai bên.
 */

/** Nhóm quyền, theo đúng thứ tự hiện trên màn hình. */
export const PERMISSION_GROUPS = ['workspace', 'employee', 'account', 'department'] as const;

export type PermissionGroup = (typeof PERMISSION_GROUPS)[number];

export interface PermissionInfo {
  readonly group: PermissionGroup;

  /** Khoá dịch cho tên ngắn. */
  readonly labelKey: string;

  /**
   * Khoá dịch cho câu nói HẬU QUẢ. Chỉ có ở những quyền nguy hiểm.
   *
   * Người bật một công tắc quyền cần biết mình vừa trao đi cái gì. Với `workspace.read`
   * thì tên quyền đã đủ; với `workspace.transfer-ownership` thì không.
   */
  readonly consequenceKey?: string;
}

export const PERMISSION_CATALOG: Readonly<Record<string, PermissionInfo>> = {
  'workspace.read': { group: 'workspace', labelKey: 'roles.perm.workspaceRead' },
  'workspace.manage': { group: 'workspace', labelKey: 'roles.perm.workspaceManage' },
  'workspace.transfer-ownership': {
    group: 'workspace',
    labelKey: 'roles.perm.workspaceTransfer',
    consequenceKey: 'roles.perm.workspaceTransferWhy',
  },

  'employee.read': { group: 'employee', labelKey: 'roles.perm.employeeRead' },
  'employee.write': { group: 'employee', labelKey: 'roles.perm.employeeWrite' },
  'employee.delete': {
    group: 'employee',
    labelKey: 'roles.perm.employeeDelete',
    consequenceKey: 'roles.perm.employeeDeleteWhy',
  },

  'user.read': { group: 'account', labelKey: 'roles.perm.userRead' },
  'user.manage': { group: 'account', labelKey: 'roles.perm.userManage' },
  'role.read': { group: 'account', labelKey: 'roles.perm.roleRead' },
  'role.manage': {
    group: 'account',
    labelKey: 'roles.perm.roleManage',
    consequenceKey: 'roles.perm.roleManageWhy',
  },

  'department.read': { group: 'department', labelKey: 'roles.perm.departmentRead' },
  'department.manage': { group: 'department', labelKey: 'roles.perm.departmentManage' },
};

/** Mọi mã quyền, theo thứ tự nhóm rồi tới thứ tự khai trong bảng. */
export function permissionsByGroup(group: PermissionGroup): string[] {
  return Object.entries(PERMISSION_CATALOG)
    .filter(([, info]) => info.group === group)
    .map(([code]) => code);
}
