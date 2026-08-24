import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import viIdentity from '../../../assets/i18n/vi/identity.json';
import enIdentity from '../../../assets/i18n/en/identity.json';
import { PERMISSION_CATALOG } from './permission-catalog';

/**
 * Bảng nhãn quyền của frontend phải khớp <c>Permissions.cs</c> ở backend, cả hai chiều.
 *
 * <b>Thiếu ở frontend</b> → màn Vai trò hiện một mã trần (<c>leave.approve</c>) giữa những
 * dòng có chữ tiếng Việt. Trông như lỗi hiển thị, và không ai biết quyền đó làm gì.
 *
 * <b>Thừa ở frontend</b> → một dòng quyền không tồn tại. Người quản trị đọc xong tưởng hệ
 * thống có tính năng đó, và đi tìm nó ở đâu đó.
 *
 * Cả hai đều không có lỗi nào báo, và cả hai đều chỉ lộ ra khi có người mở màn đó ra xem.
 */

const PERMISSIONS_CS = join(
  process.cwd(),
  '..',
  'backend',
  'src',
  'Modules',
  'Identity',
  'ONoOffice.Identity.Domain',
  'Permissions.cs',
);

/** Đọc mọi `public const string X = "…";` trong Permissions.cs. */
function backendPermissions(): string[] {
  const source = readFileSync(PERMISSIONS_CS, 'utf8');

  return [...source.matchAll(/public const string \w+ = "([^"]+)"/g)]
    .map(([, code]) => code)
    .sort();
}

/** Lấy giá trị của một khoá lồng nhau kiểu `roles.perm.userRead`. */
function lookup(json: unknown, key: string): unknown {
  return key
    .split('.')
    .reduce<unknown>((node, part) => (node as Record<string, unknown>)?.[part], json);
}

describe('bảng nhãn quyền', () => {
  it('phủ đúng những mã quyền backend khai, không thiếu không thừa', () => {
    expect(Object.keys(PERMISSION_CATALOG).sort()).toEqual(backendPermissions());
  });

  it('mọi nhãn và câu giải thích đều có bản dịch ở CẢ HAI ngôn ngữ', () => {
    // Thiếu bản dịch thì ngx-translate hiện ra chính cái khoá — `roles.perm.userRead` nằm
    // giữa bảng, còn khó hiểu hơn cả mã quyền gốc.
    const keys = Object.values(PERMISSION_CATALOG).flatMap((info) =>
      info.consequenceKey ? [info.labelKey, info.consequenceKey] : [info.labelKey],
    );

    const missing = keys.flatMap((key) => [
      ...(lookup(viIdentity, key) ? [] : [`vi: ${key}`]),
      ...(lookup(enIdentity, key) ? [] : [`en: ${key}`]),
    ]);

    expect(missing).toEqual([]);
  });

  it('mọi quyền đều thuộc về một nhóm có thật', () => {
    const groups = new Set(Object.values(PERMISSION_CATALOG).map((info) => info.group));

    // Gõ sai tên nhóm thì quyền đó không rơi vào khối nào và biến mất khỏi màn hình —
    // không lỗi, chỉ là một dòng lặng lẽ không có ở đó.
    expect([...groups].sort()).toEqual(['account', 'department', 'employee', 'workspace']);
  });
});
