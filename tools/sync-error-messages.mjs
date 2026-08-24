/**
 * Sinh `frontend/src/assets/i18n/{vi,en}/errors.json` THẲNG từ file `.resx` của backend.
 *
 * Vì sao: backend gửi MÃ lỗi, frontend tự tra câu chữ (xem `docs/07-giao-dien/da-ngon-ngu.md`).
 * Quyết định đó mở ra một khe hở — backend thêm một mã, frontend không biết, và người dùng
 * nhận câu tiếng Việt viết cứng trong code backend thay vì bản dịch. Không lỗi nào báo.
 *
 * Có `translation-parity.spec.ts` canh khe hở đó, nhưng nó chỉ BÁO. Bộ này VÁ: chép thẳng
 * từ nguồn nên hai bên không thể lệch.
 *
 *   node tools/sync-error-messages.mjs
 */
import { readFileSync, writeFileSync } from 'node:fs';

const LANGUAGES = ['vi', 'en'];

for (const lang of LANGUAGES) {
  const source = `backend/src/ONoOffice.Api/Resources/Messages.${lang}.resx`;
  const target = `frontend/src/assets/i18n/${lang}/errors.json`;

  const xml = readFileSync(source, 'utf8');

  const entries = [...xml.matchAll(/<data name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g)].map(
    ([, key, value]) => [key, value.trim()],
  );

  if (entries.length < 30) {
    throw new Error(`Chỉ đọc được ${entries.length} khoá từ ${source} — biểu thức tìm kiếm hỏng.`);
  }

  // Sắp xếp để diff giữa hai lần sinh chỉ hiện đúng thứ thật sự đổi.
  const sorted = Object.fromEntries(entries.sort(([a], [b]) => a.localeCompare(b, 'en')));

  writeFileSync(target, JSON.stringify(sorted, null, 2) + '\n');

  console.log(`${target}  ←  ${source}  (${entries.length} khoá)`);
}
