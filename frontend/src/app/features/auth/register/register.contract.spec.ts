import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import {
  PASSWORD_MIN_LENGTH,
  WORKSPACE_CODE_MAX_LENGTH,
  WORKSPACE_CODE_MIN_LENGTH,
  WORKSPACE_CODE_PATTERN,
} from './register.util';

/**
 * Đối chiếu luật kiểm dữ liệu của màn đăng ký với <b>luật thật ở backend</b>.
 *
 * <pre>
 *   TenantCode.cs                       ↔  WORKSPACE_CODE_*
 *   RegisterWorkspaceCommandValidator   ↔  PASSWORD_MIN_LENGTH
 * </pre>
 *
 * <b>Vì sao cần:</b> kiểm ở phía trình duyệt chỉ để nói sớm cho người dùng; luật thật
 * luôn nằm ở backend. Hai bên lệch nhau thì hỏng theo một trong hai kiểu, và cả hai đều
 * tệ:
 *
 * <ul>
 * <li><b>Phía này lỏng hơn</b> → người dùng điền xong, bấm gửi, rồi mới bị từ chối. Họ
 * đã tin là mình điền đúng vì không có ô nào đỏ.</li>
 * <li><b>Phía này chặt hơn</b> → có những mã hợp lệ mà không ai đăng ký được, và không
 * có lỗi nào ở đâu để lần ra.</li>
 * </ul>
 *
 * Không ai nhìn ra hai kiểu đó bằng mắt, nên phải có test đọc thẳng file C# ra mà so.
 */

const BACKEND = join(process.cwd(), '..', 'backend', 'src', 'Modules', 'Identity');

const TENANT_CODE = join(BACKEND, 'ONoOffice.Identity.Domain', 'ValueObjects', 'TenantCode.cs');

const VALIDATOR = join(
  BACKEND,
  'ONoOffice.Identity.Application',
  'Authentication',
  'Register',
  'RegisterWorkspaceCommandValidator.cs',
);

describe('luật mã workspace khớp backend', () => {
  const source = readFileSync(TENANT_CODE, 'utf8');

  it('dùng đúng biểu thức chính quy của TenantCode', () => {
    const backendPattern = /\[GeneratedRegex\("([^"]+)"\)\]/.exec(source)?.[1];

    expect(backendPattern).toBeDefined();
    expect(WORKSPACE_CODE_PATTERN.source).toBe(backendPattern);
  });

  it('dùng đúng giới hạn độ dài của TenantCode', () => {
    const min = /MinLength\s*=\s*(\d+)/.exec(source)?.[1];
    const max = /MaxLength\s*=\s*(\d+)/.exec(source)?.[1];

    expect(WORKSPACE_CODE_MIN_LENGTH).toBe(Number(min));
    expect(WORKSPACE_CODE_MAX_LENGTH).toBe(Number(max));
  });

  it('từ chối hai gạch nối liền nhau, đúng như backend', () => {
    // Ca này chính là chỗ luật viết vội hay bỏ sót: `^[a-z][a-z0-9-]*[a-z0-9]$` nhìn thì
    // đúng nhưng cho lọt `cong--ty`, và backend thì không.
    expect(WORKSPACE_CODE_PATTERN.test('cong--ty')).toBe(false);
    expect(WORKSPACE_CODE_PATTERN.test('cong-ty')).toBe(true);
  });

  it('từ chối mã bắt đầu bằng số hoặc kết thúc bằng gạch nối', () => {
    expect(WORKSPACE_CODE_PATTERN.test('2026-acme')).toBe(false);
    expect(WORKSPACE_CODE_PATTERN.test('acme-')).toBe(false);
    expect(WORKSPACE_CODE_PATTERN.test('acme2026')).toBe(true);
  });
});

describe('độ dài mật khẩu khớp backend', () => {
  it('dùng đúng con số của RegisterWorkspaceCommandValidator', () => {
    const source = readFileSync(VALIDATOR, 'utf8');
    // Con số nằm trong một hằng có tên chứ không viết thẳng vào lời gọi — chính vì vậy
    // lần chạy đầu của test này đỏ, và đó là bằng chứng nó đọc file thật chứ không đoán.
    const min = /MinPasswordLength = (\d+)/.exec(source)?.[1];

    expect(min).toBeDefined();
    expect(PASSWORD_MIN_LENGTH).toBe(Number(min));
  });
});
