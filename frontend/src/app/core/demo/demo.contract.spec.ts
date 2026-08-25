import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import { Permissions } from './demo.permissions';
import { VAI_TRO } from './demo.state';

/**
 * Dữ liệu giả của chế độ demo phải khớp <b>nguồn sự thật ở backend</b>.
 *
 * <b>Vì sao đáng canh một thứ chỉ dùng để bấm thử:</b> chế độ demo là nơi người ta xem
 * giao diện và CHỐT thiết kế. Nếu nó hiện một danh sách quyền khác với thực tế thì người
 * duyệt gật đầu cho một màn hình không tồn tại — đúng cái lỗi bản dựng đã mắc hai lần
 * (màn Vai trò bịa Manager 6 quyền; ngăn kéo Nhân sự bịa `conversation.*`).
 *
 * Đọc THẲNG file C# ra rồi so, cùng cách `user-contract.spec.ts` canh enum
 * `UserStatusFilter`. Không có bước này thì `demo.permissions.ts` là một bản chép tay, và
 * bản chép tay nào cũng lệch sau vài tháng.
 */

const BACKEND = join(
  process.cwd(),
  '..',
  'backend',
  'src',
  'Modules',
  'Identity',
  'ONoOffice.Identity.Domain',
);

/** Mọi hằng chuỗi trong các lớp lồng của `Permissions.cs`. */
function quyenCuaBackend(): string[] {
  const cs = readFileSync(join(BACKEND, 'Permissions.cs'), 'utf8');

  return [...cs.matchAll(/public const string \w+ = "([^"]+)"/g)].map(([, giaTri]) => giaTri);
}

describe('chế độ demo ↔ backend', () => {
  it('danh sách quyền khớp Permissions.cs, không thừa không thiếu', () => {
    expect([...Permissions.ALL].sort()).toEqual(quyenCuaBackend().sort());
  });

  it('Owner có TẤT CẢ quyền', () => {
    const owner = VAI_TRO.find((r) => r.name === 'Owner');

    expect(owner?.permissions.sort()).toEqual(quyenCuaBackend().sort());
  });

  it('Admin có tất cả TRỪ ĐÚNG chuyển nhượng quyền sở hữu', () => {
    const admin = VAI_TRO.find((r) => r.name === 'Admin');
    const mong = quyenCuaBackend()
      .filter((q) => q !== Permissions.TRANSFER_OWNERSHIP)
      .sort();

    expect(admin?.permissions.sort()).toEqual(mong);

    // Đó là toàn bộ ranh giới giữa hai vai. Thiếu phép kiểm này thì một ngày nào đó demo
    // cho Admin đủ 12 quyền, và người duyệt kết luận hai vai là một.
    expect(admin?.permissions).not.toContain(Permissions.TRANSFER_OWNERSHIP);
  });

  it('Manager trùng khít Member — đúng một quyền, cho tới khi có leave.approve', () => {
    const manager = VAI_TRO.find((r) => r.name === 'Manager');
    const member = VAI_TRO.find((r) => r.name === 'Member');

    expect(manager?.permissions).toEqual(['employee.read']);
    expect(member?.permissions).toEqual(['employee.read']);
  });
});
