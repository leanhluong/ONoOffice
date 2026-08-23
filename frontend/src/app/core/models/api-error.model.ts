/**
 * Kiểu dữ liệu lỗi trả về từ backend .NET (chuẩn RFC 7807 Problem Details)
 * và kiểu lỗi thống nhất mà toàn bộ frontend sẽ dùng.
 *
 * Lý do có hai lớp kiểu:
 * - `ProblemDetails` là thứ backend NÓI. Hình dạng của nó có thể thay đổi
 *   (mảng `errors`, dictionary ModelState, hoặc không có gì cả khi mất mạng).
 * - `AppError` là thứ UI CẦN. Luôn có `code` và `message` để hiển thị,
 *   không bao giờ null. Nhờ vậy component không phải viết `?.` chằng chịt.
 *
 * Việc chuyển đổi giữa hai lớp nằm gọn trong `error.interceptor`.
 */

/** Một mục lỗi trong mảng `errors` của backend. */
export interface ProblemDetailItem {
  /** Mã lỗi có cấu trúc, ví dụ `Employee.EmailTaken`. */
  code: string;
  /** Mô tả cho người dùng đọc, backend đã dịch sẵn. */
  description: string;
}

/** Thân lỗi RFC 7807 mà backend .NET trả về. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  /**
   * Backend ONoOffice trả mảng `ProblemDetailItem`.
   * Nhưng validation tự động của ASP.NET Core lại trả dictionary
   * `{ "Email": ["..."] }`, nên kiểu ở đây phải nhận cả hai.
   */
  errors?: ProblemDetailItem[] | Record<string, string[]>;
}

/** Phân loại lỗi để UI quyết định cách hiển thị (toast, banner, redirect...). */
export type AppErrorKind =
  | 'network' // không gọi được tới server
  | 'validation' // 400/422 — dữ liệu người dùng nhập sai
  | 'unauthorized' // 401 — chưa đăng nhập hoặc token hết hạn
  | 'forbidden' // 403 — đã đăng nhập nhưng thiếu permission
  | 'not-found' // 404
  | 'conflict' // 409 — vi phạm quy tắc nghiệp vụ
  | 'server' // 5xx
  | 'unknown';

/**
 * Lỗi đã chuẩn hoá. Mọi `catchError` trong app đều nhận đúng kiểu này.
 */
export interface AppError {
  readonly kind: AppErrorKind;
  /** Mã HTTP. 0 khi lỗi mạng / bị CORS chặn. */
  readonly status: number;
  /** `errors[0].code`, hoặc mã tự sinh khi backend không nói gì. */
  readonly code: string;
  /** `errors[0].description` — chuỗi để hiển thị thẳng lên UI. */
  readonly message: string;
  /** Toàn bộ danh sách lỗi, phòng khi UI muốn liệt kê hết. */
  readonly details: readonly ProblemDetailItem[];
  /**
   * Lỗi gắn theo từng trường form, key là tên trường (đã lowerCamelCase).
   * Lấy từ dictionary ModelState của ASP.NET Core.
   */
  readonly fieldErrors: Readonly<Record<string, string[]>>;
  /** `X-Correlation-Id` đã gửi đi — để tra log backend khi người dùng báo lỗi. */
  readonly correlationId: string | null;
}

/** Type guard: phân biệt `AppError` với lỗi JS thường. */
export function isAppError(value: unknown): value is AppError {
  return (
    typeof value === 'object' &&
    value !== null &&
    'kind' in value &&
    'code' in value &&
    'message' in value
  );
}
