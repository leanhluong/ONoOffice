/**
 * Hai phép tính thuần của màn đăng ký, tách khỏi component để kiểm được riêng.
 *
 * Cả hai chép đúng từ bản dựng đã duyệt `docs/07-giao-dien/identity/dang-ky.html`. Đây là
 * phần LOGIC của bản dựng — phần hình ảnh thì `register.scss` sinh tự động, còn chỗ này
 * phải chép tay, nên có bộ kiểm riêng canh cho khỏi lệch.
 */

/**
 * Ba hằng dưới đây là BẢN SAO của luật ở backend, không phải luật riêng của giao diện.
 *
 * Chép luật là một chỗ dễ lệch, nên `register.contract.spec.ts` đọc thẳng file C# ra mà
 * so — sửa một bên mà quên bên kia thì test đỏ ngay.
 */
export const WORKSPACE_CODE_MIN_LENGTH = 3;
export const WORKSPACE_CODE_MAX_LENGTH = 30;

/**
 * Chép nguyên từ `[GeneratedRegex]` của `TenantCode`.
 *
 * Chỗ `-(?=[a-z0-9])` mới là phần dễ bỏ sót: nó cấm hai gạch nối liền nhau. Viết gọn
 * thành `[a-z0-9-]*` thì trông vẫn đúng nhưng cho lọt `cong--ty` — và backend từ chối.
 */
export const WORKSPACE_CODE_PATTERN = /^[a-z](?:[a-z0-9]|-(?=[a-z0-9]))*[a-z0-9]$/;

/** Khớp `RegisterWorkspaceCommandValidator.MinimumLength`. */
export const PASSWORD_MIN_LENGTH = 10;

/**
 * Gợi ý mã workspace từ tên công ty.
 *
 * Người Việt gõ tên công ty có dấu; mã thì backend chỉ nhận `[a-z0-9-]`. Không gợi ý thì
 * họ phải tự nghĩ ra mã, và đó là ô hay bị bỏ dở nhất trong biểu mẫu.
 */
export function suggestWorkspaceCode(companyName: string): string {
  return (
    companyName
      // NFD tách dấu thành ký tự riêng, rồi cắt cả dải dấu đi. Rẻ hơn nhiều so với một
      // bảng tra "à→a, á→a…" mà lại không bỏ sót chữ nào.
      .normalize('NFD')
      .replace(/[̀-ͯ]/g, '')

      // `đ` KHÔNG phải `d` cộng dấu — nó là một ký tự Latin riêng, nên NFD không đụng tới.
      // Thiếu dòng này thì "Đường Sắt" ra "ung-st".
      .replace(/đ/g, 'd')
      .replace(/Đ/g, 'D')

      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '')
      .slice(0, WORKSPACE_CODE_MAX_LENGTH)

      // Cắt ở đúng con số có thể để lại một gạch nối treo ở cuối — mà mã kết thúc bằng
      // gạch nối thì chính backend của ta từ chối. Gợi ý một mã không dùng được còn tệ
      // hơn không gợi ý gì.
      .replace(/-+$/, '')
  );
}

/**
 * Chấm độ mạnh mật khẩu, thang 0–4 khớp bốn vạch của thanh đo.
 *
 * <b>Đếm theo ĐỘ DÀI và sự đa dạng, không bắt buộc ký tự đặc biệt.</b> Luật bắt buộc ký
 * tự đặc biệt đẻ ra toàn `Matkhau@123`: đủ mọi loại ký tự, ngắn, và nằm trong mọi từ điển
 * dò mật khẩu. Một câu tiếng Việt không dấu dài hai mươi ký tự an toàn hơn nhiều.
 *
 * Đây chỉ là <b>lời khuyên hiển thị</b>. Luật chặn thật nằm ở backend
 * (`RegisterWorkspaceCommandValidator`) — điểm thấp vẫn gửi được nếu đủ 10 ký tự.
 */
export function passwordStrength(password: string): number {
  if (password.length === 0) {
    return 0;
  }

  let score = 0;

  if (password.length >= 10) score++;
  if (password.length >= 14) score++;
  if (/[a-z]/.test(password) && /[A-Z0-9]/.test(password)) score++;
  if (password.length >= 20 || /[^\w\s]/.test(password)) score++;

  return score;
}
