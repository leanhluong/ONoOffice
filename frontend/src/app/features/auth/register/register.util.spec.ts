import { describe, expect, it } from 'vitest';
import { passwordStrength, suggestWorkspaceCode } from './register.util';

/**
 * Hai hàm thuần của màn đăng ký.
 *
 * Chúng được tách ra khỏi component chính vì chỗ này: kiểm được mà không phải dựng
 * Angular, và kiểm được đúng những ca lắt léo của tiếng Việt mà nhìn mắt thường thì
 * tưởng chạy đúng.
 */
describe('suggestWorkspaceCode', () => {
  it('bỏ dấu tiếng Việt thay vì cắt bỏ chữ', () => {
    expect(suggestWorkspaceCode('Công ty Đường Sắt')).toBe('cong-ty-duong-sat');
  });

  it('đổi đ và Đ thành d — chuẩn hoá Unicode KHÔNG tách được hai chữ này', () => {
    // NFD tách dấu khỏi nguyên âm, nhưng `đ` là một ký tự riêng chứ không phải `d` + dấu.
    // Thiếu bước này thì "Đường" ra "ung" — mất luôn chữ cái đầu.
    expect(suggestWorkspaceCode('đông đủ')).toBe('dong-du');
  });

  it('gộp mọi ký tự lạ thành một gạch nối', () => {
    expect(suggestWorkspaceCode('ACME  Co., Ltd.')).toBe('acme-co-ltd');
  });

  it('không để gạch nối ở đầu hay cuối — backend từ chối mã như vậy', () => {
    expect(suggestWorkspaceCode('--- Acme ---')).toBe('acme');
    expect(suggestWorkspaceCode('Công ty 2026!')).toBe('cong-ty-2026');
  });

  it('cắt còn 30 ký tự, đúng giới hạn của TenantCode', () => {
    const code = suggestWorkspaceCode('a'.repeat(50));

    expect(code).toHaveLength(30);
  });

  it('cắt xong không được để lại gạch nối ở cuối', () => {
    // "aaaa…-b": cắt đúng 30 ký tự rơi vào ngay dấu gạch nối. Trả nguyên như vậy thì gợi
    // ý của ta tạo ra một mã mà chính backend của ta từ chối.
    const code = suggestWorkspaceCode(`${'a'.repeat(30)} b`);

    expect(code.endsWith('-')).toBe(false);
    expect(code).toBe('a'.repeat(30));
  });

  it('trả về chuỗi rỗng khi không còn gì dùng được', () => {
    expect(suggestWorkspaceCode('!!!')).toBe('');
  });
});

describe('passwordStrength', () => {
  it('chưa gõ gì thì không chấm điểm', () => {
    expect(passwordStrength('')).toBe(0);
  });

  it('ngắn hơn mức tối thiểu vẫn là nấc thấp nhất', () => {
    expect(passwordStrength('abc')).toBe(0);
  });

  it('đủ 10 ký tự được một điểm', () => {
    expect(passwordStrength('aaaaaaaaaa')).toBe(1);
  });

  it('câu dài dễ nhớ ăn điểm cao hơn mật khẩu ngắn có ký tự đặc biệt', () => {
    // Đây là lý do tồn tại của cách chấm này: luật "phải có ký tự đặc biệt" đẻ ra toàn
    // "Matkhau@1" — ngắn và đoán được.
    expect(passwordStrength('con meo ngoi tren mai nha')).toBeGreaterThan(
      passwordStrength('Matkhau@1'),
    );
  });

  it('không bao giờ vượt quá bốn nấc — thanh đo chỉ có bốn vạch', () => {
    expect(passwordStrength('X'.repeat(40) + 'y1!')).toBe(4);
  });
});
