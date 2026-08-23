/**
 * Cấu hình môi trường PRODUCTION.
 *
 * `apiBaseUrl` để rỗng nghĩa là "cùng origin với trang web": mọi request sẽ đi
 * tới `/api/...` và do reverse proxy (gateway) chuyển tiếp về backend .NET.
 * Cách này tránh CORS và tránh phải rebuild FE mỗi khi đổi domain backend.
 * Nếu deploy FE và BE ở hai domain khác nhau thì đổi thành URL tuyệt đối,
 * ví dụ: 'https://api.onooffice.vn'.
 */
import type { AppEnvironment } from './environment.model';

export const environment: AppEnvironment = {
  production: true,
  apiBaseUrl: '',
};
