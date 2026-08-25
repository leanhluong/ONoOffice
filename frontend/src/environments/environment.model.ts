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

  /**
   * Cho phép CHẾ ĐỘ DEMO — API giả chạy trong trình duyệt, không cần backend.
   *
   * `false` ở bản production, và đó là hàng rào quan trọng nhất: Angular thay file
   * environment lúc build, nên interceptor demo không bao giờ được đăng ký.
   *
   * (Mã demo vẫn NẰM trong bundle — nó không bị cây rung rụng, vì `app.config.ts` tham
   * chiếu tĩnh. Tính chất được bảo đảm là "không bao giờ kích hoạt", không phải "không
   * có mặt". `demo-safety.spec.ts` canh đúng chỗ đó.)
   *
   * `true` chỉ mở CỬA — mặc định vẫn tắt, phải bật tay bằng `?demo=1`.
   * Xem `core/demo/demo.interceptor.ts`.
   */
  readonly demo: boolean;
}
