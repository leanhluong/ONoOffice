import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { routes } from './app.routes';
import { authInterceptor } from './core/http/auth.interceptor';
import { correlationIdInterceptor } from './core/http/correlation-id.interceptor';
import { errorInterceptor } from './core/http/error.interceptor';

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
       *   correlationId → auth → error → [mạng] → error → auth → correlationId
       *
       * - `correlationId` đứng đầu: mọi request, kể cả request đăng nhập, đều
       *   phải có id để tra log.
       * - `auth` ở giữa: gắn Bearer sau khi header id đã có.
       * - `error` đứng CUỐI: ở chiều response nó là thằng chạm vào lỗi ĐẦU
       *   TIÊN, nên bọc được cả lỗi phát sinh từ hai interceptor trên.
       *   Đảo thứ tự thì lỗi sẽ lọt ra ngoài dưới dạng HttpErrorResponse thô.
       */
      withInterceptors([correlationIdInterceptor, authInterceptor, errorInterceptor]),
    ),
  ],
};
