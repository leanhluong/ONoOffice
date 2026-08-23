/**
 * Khai báo kiểu chung cho mọi file environment.
 * Tách riêng để khi thêm biến mới thì TypeScript bắt buộc phải cập nhật
 * cả environment.ts lẫn environment.development.ts — không thể quên một cái.
 */
export interface AppEnvironment {
  /** True khi build production. Dùng để bật/tắt log, devtools... */
  readonly production: boolean;

  /**
   * Gốc URL của backend, KHÔNG có dấu `/` ở cuối.
   * Chuỗi rỗng = cùng origin (đi qua reverse proxy).
   */
  readonly apiBaseUrl: string;
}
