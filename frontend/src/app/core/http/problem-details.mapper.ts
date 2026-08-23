import { HttpErrorResponse } from '@angular/common/http';
import type {
  AppError,
  AppErrorKind,
  ProblemDetailItem,
  ProblemDetails,
} from '../models/api-error.model';

/** Câu chữ hiển thị khi backend không nói gì cụ thể. */
const FALLBACK_MESSAGES: Record<AppErrorKind, string> = {
  network: 'Không kết nối được máy chủ. Kiểm tra lại đường truyền rồi thử lại.',
  validation: 'Dữ liệu chưa hợp lệ. Vui lòng kiểm tra lại các trường đã nhập.',
  unauthorized: 'Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại.',
  forbidden: 'Bạn không có quyền thực hiện thao tác này.',
  'not-found': 'Không tìm thấy dữ liệu yêu cầu.',
  conflict: 'Thao tác bị từ chối do xung đột dữ liệu.',
  server: 'Máy chủ đang gặp sự cố. Vui lòng thử lại sau ít phút.',
  unknown: 'Đã có lỗi xảy ra. Vui lòng thử lại.',
};

/** Suy ra loại lỗi từ mã HTTP. */
export function kindFromStatus(status: number): AppErrorKind {
  if (status === 0) return 'network';
  if (status === 400 || status === 422) return 'validation';
  if (status === 401) return 'unauthorized';
  if (status === 403) return 'forbidden';
  if (status === 404) return 'not-found';
  if (status === 409) return 'conflict';
  if (status >= 500) return 'server';
  return 'unknown';
}

/**
 * Chuyển `HttpErrorResponse` thành `AppError`.
 *
 * Đây là chỗ duy nhất trong app biết hình dạng Problem Details. Nhờ vậy nếu
 * backend đổi format, chỉ phải sửa một file, không phải rà khắp component.
 */
export function toAppError(error: HttpErrorResponse, correlationId: string | null): AppError {
  const status = error.status;
  const kind = kindFromStatus(status);
  const problem = readProblemDetails(error.error);

  const details = readErrorItems(problem);
  const fieldErrors = readFieldErrors(problem);

  const first = details[0];
  const code = first?.code ?? defaultCode(kind, status);
  const message =
    first?.description ??
    firstFieldMessage(fieldErrors) ??
    problem?.detail ??
    problem?.title ??
    FALLBACK_MESSAGES[kind];

  return { kind, status, code, message, details, fieldErrors, correlationId };
}

/** Thân lỗi có thể là object, chuỗi JSON (khi responseType lệch), hoặc rác. */
function readProblemDetails(body: unknown): ProblemDetails | null {
  if (typeof body === 'string') {
    try {
      return JSON.parse(body) as ProblemDetails;
    } catch {
      return null;
    }
  }
  if (typeof body === 'object' && body !== null) {
    return body as ProblemDetails;
  }
  return null;
}

/** Lấy mảng `errors` khi backend trả đúng định dạng ONoOffice. */
function readErrorItems(problem: ProblemDetails | null): ProblemDetailItem[] {
  const errors = problem?.errors;
  if (!Array.isArray(errors)) {
    return [];
  }
  return errors.filter(
    (item): item is ProblemDetailItem =>
      typeof item === 'object' &&
      item !== null &&
      typeof item.code === 'string' &&
      typeof item.description === 'string',
  );
}

/**
 * Lấy lỗi theo trường khi backend trả dictionary ModelState.
 * Tên trường được hạ chữ cái đầu để khớp với tên control trong reactive form
 * (backend trả `Email`, form đặt tên `email`).
 */
function readFieldErrors(problem: ProblemDetails | null): Record<string, string[]> {
  const errors = problem?.errors;
  if (!errors || Array.isArray(errors)) {
    return {};
  }

  const result: Record<string, string[]> = {};
  for (const [field, messages] of Object.entries(errors)) {
    if (Array.isArray(messages) && messages.length > 0) {
      result[toCamelCase(field)] = messages.map(String);
    }
  }
  return result;
}

function firstFieldMessage(fieldErrors: Record<string, string[]>): string | null {
  for (const messages of Object.values(fieldErrors)) {
    const first = messages[0];
    if (first) {
      return first;
    }
  }
  return null;
}

function toCamelCase(value: string): string {
  return value.length > 0 ? value.charAt(0).toLowerCase() + value.slice(1) : value;
}

/** Mã lỗi tự sinh, giữ đúng phong cách `Namespace.Reason` như backend. */
function defaultCode(kind: AppErrorKind, status: number): string {
  switch (kind) {
    case 'network':
      return 'Network.Unreachable';
    case 'unauthorized':
      return 'Auth.Unauthorized';
    case 'forbidden':
      return 'Auth.Forbidden';
    case 'not-found':
      return 'Http.NotFound';
    case 'validation':
      return 'Http.ValidationFailed';
    case 'conflict':
      return 'Http.Conflict';
    case 'server':
      return 'Http.ServerError';
    default:
      return `Http.${status}`;
  }
}
