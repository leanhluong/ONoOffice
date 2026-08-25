/**
 * Mười hai quyền của hệ thống, chép ĐÚNG từ
 * `backend/src/Modules/Identity/ONoOffice.Identity.Domain/Permissions.cs`.
 *
 * Vì sao chép tay chứ không sinh: chế độ demo chạy hoàn toàn trong trình duyệt, không có
 * backend nào để hỏi. Nhưng chép tay thì lệch được — nên có `demo.contract.spec.ts` đọc
 * thẳng `Permissions.cs` ra rồi đối chiếu, y như cách `user-contract.spec.ts` canh enum
 * `UserStatusFilter`. Thêm một quyền ở backend mà quên ở đây thì test đỏ.
 */
export const Permissions = {
  TRANSFER_OWNERSHIP: 'workspace.transfer-ownership',

  ALL: [
    'workspace.read',
    'workspace.manage',
    'workspace.transfer-ownership',
    'user.read',
    'user.manage',
    'role.read',
    'role.manage',
    'employee.read',
    'employee.write',
    'employee.delete',
    'department.read',
    'department.manage',
  ] as const,
} as const;
