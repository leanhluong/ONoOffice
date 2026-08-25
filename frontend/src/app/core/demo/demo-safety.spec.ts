import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

/**
 * Chế độ demo <b>không bao giờ được kích hoạt ở bản production</b>.
 *
 * ═══════════════════════════════════════════════════════════════════════
 *  VÌ SAO CANH BẰNG CÁCH ĐỌC FILE, KHÔNG PHẢI GỌI HÀM
 * ═══════════════════════════════════════════════════════════════════════
 *
 * Vitest chạy với `environment.development.ts` (Angular chỉ thay file lúc BUILD), nên
 * `import { environment }` trong test luôn cho bản dev. Muốn kiểm bản production thì phải
 * đọc thẳng `environment.ts` ra — cùng cách `user-contract.spec.ts` đọc file C# của backend.
 *
 * ═══════════════════════════════════════════════════════════════════════
 *  ĐIỀU NÀY CANH, VÀ ĐIỀU NÀY KHÔNG
 * ═══════════════════════════════════════════════════════════════════════
 *
 * Canh: cờ ở bản production là `false`, và interceptor demo chỉ được đăng ký khi cờ bật.
 *
 * KHÔNG canh: mã demo có nằm trong bundle production hay không — <b>nó CÓ nằm trong đó</b>,
 * khoảng 2KB code chết, vì `app.config.ts` tham chiếu tĩnh và `DemoBanner` giữ nó sống.
 * Chú thích ban đầu của tôi viết rằng nó "bị cây rung rụng"; sai, và một lệnh grep trên
 * `dist/` đã bắt được. Tính chất an toàn thật sự là <b>không bao giờ kích hoạt</b>, và đó
 * là thứ ba phép kiểm dưới đây đo.
 */

const ENV = join(process.cwd(), 'src', 'environments');
const APP_CONFIG = join(process.cwd(), 'src', 'app', 'app.config.ts');

describe('an toàn của chế độ demo', () => {
  it('bản production tắt cờ demo', () => {
    const prod = readFileSync(join(ENV, 'environment.ts'), 'utf8');

    // Khớp cả `demo: false` lẫn `demo:false`; và phải KHÔNG có `demo: true` ở đâu cả.
    expect(prod).toMatch(/demo:\s*false/);
    expect(prod).not.toMatch(/demo:\s*true/);
  });

  it('bản dev bật cờ — nếu không thì không ai bấm thử được gì', () => {
    const dev = readFileSync(join(ENV, 'environment.development.ts'), 'utf8');

    expect(dev).toMatch(/demo:\s*true/);
  });

  it('interceptor demo chỉ được đăng ký khi cờ bật', () => {
    const config = readFileSync(APP_CONFIG, 'utf8');

    // Nằm sau một phép rẽ nhánh theo `environment.demo`, KHÔNG phải một phần tử trần
    // trong mảng. Ai đó gỡ điều kiện này đi thì API giả có mặt trong chuỗi xử lý của bản
    // production, và lúc đó chỉ còn đúng một dòng `if` trong interceptor đứng giữa người
    // dùng thật và dữ liệu bịa.
    expect(config).toMatch(/environment\.demo\s*\?\s*\[demoInterceptor\]\s*:\s*\[\]/);
  });
});
