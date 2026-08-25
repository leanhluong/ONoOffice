/**
 * Cấu hình môi trường DEV (dùng khi chạy `npm start`).
 * Backend .NET chạy ở cổng 5000 theo mặc định của dự án.
 */
import type { AppEnvironment } from './environment.model';

export const environment: AppEnvironment = {
  production: false,
  apiBaseUrl: 'http://localhost:5000',

  // Mở cửa cho chế độ demo, nhưng mặc định vẫn TẮT: `npm start` vẫn nói chuyện với
  // backend .NET thật như trước. Bật bằng `?demo=1`.
  demo: true,
};
