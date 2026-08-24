import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import { UserStatusFilter } from '../../core/models/user.model';

/**
 * Bộ lọc trạng thái đi qua dây HTTP dưới dạng một CON SỐ (<c>?status=2</c>).
 *
 * <b>Vì sao phải có test:</b> con số đó không mang ý nghĩa gì tự thân. Ai đó chèn thêm một
 * giá trị vào giữa enum ở backend là mọi bộ lọc phía frontend lặng lẽ trỏ sai — người dùng
 * chọn "Đã vô hiệu hoá" và nhận về danh sách người đang hoạt động. Không có lỗi nào ở đâu,
 * chỉ có kết quả sai.
 *
 * Đây cũng chính là lý do dùng số chứ không dùng chuỗi: chuỗi thì tự nó là tài liệu, nhưng
 * hợp đồng đã trót là số (System.Text.Json tuần tự hoá enum thành số theo mặc định), nên
 * chốt nó bằng test thay vì đổi hợp đồng.
 */

const PORT = join(
  process.cwd(),
  '..',
  'backend',
  'src',
  'Modules',
  'Identity',
  'ONoOffice.Identity.Application',
  'Abstractions',
  'IUserRepository.cs',
);

/** Đọc `Ten = 3,` trong khối `enum UserStatusFilter`. */
function backendEnum(): Record<string, number> {
  const source = readFileSync(PORT, 'utf8');
  const block = /public enum UserStatusFilter\s*\{([\s\S]*?)\n\}/.exec(source);

  expect(block, 'không tìm thấy enum UserStatusFilter ở backend').not.toBeNull();

  return Object.fromEntries(
    [...block![1].matchAll(/(\w+)\s*=\s*(\d+)\s*,/g)].map(([, name, value]) => [
      name,
      Number(value),
    ]),
  );
}

describe('bộ lọc trạng thái', () => {
  it('mọi giá trị đều khớp enum của backend', () => {
    const backend = backendEnum();

    expect(backend).toEqual({
      Any: UserStatusFilter.Any,
      Active: UserStatusFilter.Active,
      PendingFirstLogin: UserStatusFilter.PendingFirstLogin,
      Disabled: UserStatusFilter.Disabled,
    });
  });

  it('backend không có giá trị nào mà frontend chưa biết', () => {
    // Thêm một trạng thái ở backend mà quên thêm ô chọn ở giao diện thì người dùng không
    // bao giờ lọc được theo nó — và không ai phát hiện ra, vì mọi thứ khác vẫn chạy.
    const known = Object.keys(UserStatusFilter).filter((key) => Number.isNaN(Number(key)));

    expect(Object.keys(backendEnum()).sort()).toEqual(known.sort());
  });
});
