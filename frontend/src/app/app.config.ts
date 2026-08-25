import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { catchError, firstValueFrom, of } from 'rxjs';
import { environment } from '../environments/environment';
import { routes } from './app.routes';
import { AuthService } from './core/auth/auth.service';
import { AuthStore } from './core/auth/auth.store';
import { demoInterceptor } from './core/demo/demo.interceptor';
import { authInterceptor } from './core/http/auth.interceptor';
import { correlationIdInterceptor } from './core/http/correlation-id.interceptor';
import { errorInterceptor } from './core/http/error.interceptor';
import { refreshInterceptor } from './core/http/refresh.interceptor';
import { provideAppTranslation } from './core/i18n/translation.config';
import { ThemeService } from './core/theme/theme.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(
      routes,
      // Cho phép nhận query/path param thẳng vào `input()` của component,
      // đỡ phải inject ActivatedRoute ở những màn đơn giản.
      withComponentInputBinding(),
      // Đổi trang thì cuộn lên đầu; bấm Back thì trả về đúng chỗ cũ.
      withInMemoryScrolling({ scrollPositionRestoration: 'enabled', anchorScrolling: 'enabled' }),
    ),
    provideHttpClient(
      /**
       * THỨ TỰ INTERCEPTOR RẤT QUAN TRỌNG.
       *
       * Request chạy xuôi từ trên xuống, response chạy ngược từ dưới lên:
       *
       *   correlationId → refresh → auth → error → [mạng]
       *                                              ↓
       *   correlationId ← refresh ← auth ← error ←──┘
       *
       * - `correlationId` đứng ĐẦU: mọi request, kể cả đăng nhập, đều có id để tra log.
       *   Đứng đầu cũng có nghĩa là lần GỬI LẠI sau khi gia hạn dùng lại đúng id đó —
       *   trong log của backend, hai lần gọi nối thành một hành trình.
       *
       * - `refresh` TRƯỚC `auth`, và đây là chỗ dễ đặt nhầm nhất: lần gửi lại phải đi
       *   qua `auth` một lần nữa để gắn token MỚI. Đặt sau `auth` thì request gửi lại
       *   vẫn mang đúng cái token vừa hết hạn.
       *
       * - `error` đứng CUỐI: ở chiều response nó chạm vào lỗi ĐẦU TIÊN, nên `refresh`
       *   bên ngoài nhận được `AppError` đã chuẩn hoá thay vì `HttpErrorResponse` thô.
       */
      withInterceptors([
        correlationIdInterceptor,
        refreshInterceptor,
        authInterceptor,
        errorInterceptor,

        /**
         * CHẾ ĐỘ DEMO đứng CUỐI, và chỗ đứng này là cố ý.
         *
         * Cuối chuỗi nghĩa là request đã đi qua đủ bốn interceptor thật trước khi bị chặn
         * — nó vẫn được gắn correlation-id, vẫn mang access token, và câu trả lời giả vẫn
         * đi ngược qua `errorInterceptor` để thành `AppError` chuẩn. Nói cách khác: bấm
         * thử ở chế độ demo là bấm thử ĐÚNG đường mà request thật sẽ đi, chỉ thay mỗi
         * đoạn ra mạng.
         *
         * Đặt nó lên đầu thì ngược lại: mọi lớp hạ tầng bị nhảy cóc, và demo sẽ xanh ở
         * đúng những chỗ sản phẩm thật hỏng.
         *
         * Và nó chỉ được ĐĂNG KÝ khi `environment.demo` bật. Bản production có
         * `demo: false`, nên mảng interceptor ở đó đúng bốn phần tử — demo không nằm
         * trong chuỗi xử lý, chứ không phải nằm trong đó rồi tự bỏ qua.
         */
        ...(environment.demo ? [demoInterceptor] : []),
      ]),
    ),

    provideAppTranslation(),

    /**
     * Áp bộ màu TRƯỚC khi vẽ khung hình đầu tiên.
     *
     * Đặt muộn hơn thì người dùng chọn giao diện tối sẽ thấy một nháy trắng loá mắt mỗi
     * lần mở app — kiểu lỗi ai cũng gặp mà ít ai đi sửa.
     */
    provideAppInitializer(() => {
      inject(ThemeService).initialise();
    }),

    /**
     * Khôi phục phiên trước khi router chạy guard đầu tiên.
     *
     * Access token chết theo tab (ADR-0004), nên mở lại tab thì trong bộ nhớ không có gì
     * — chỉ còn vé gia hạn trên đĩa. Không có bước này thì `authGuard` thấy "chưa đăng
     * nhập" và đá thẳng về màn đăng nhập, dù người dùng hoàn toàn còn phiên hợp lệ.
     *
     * Nuốt lỗi là CÓ CHỦ Ý: vé chết là chuyện bình thường (đi vắng một tháng). Lúc đó
     * app cứ khởi động như người chưa đăng nhập, và guard làm phần việc còn lại. Ném lỗi
     * ở initializer thì app không lên được — hỏng nặng hơn nhiều so với việc phải đăng
     * nhập lại.
     */
    provideAppInitializer(() => {
      const store = inject(AuthStore);

      if (!store.canRestore()) {
        return;
      }

      return firstValueFrom(
        inject(AuthService)
          .refresh()
          .pipe(catchError(() => of(undefined))),
      );
    }),
  ],
};
