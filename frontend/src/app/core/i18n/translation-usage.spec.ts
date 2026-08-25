import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

/**
 * Mọi khoá dịch mà template GỌI TỚI đều phải tồn tại thật.
 *
 * ═══════════════════════════════════════════════════════════════════════
 *  VÌ SAO `translation-parity` KHÔNG ĐỦ
 * ═══════════════════════════════════════════════════════════════════════
 *
 * Bộ đó đối chiếu <c>vi</c> ↔ <c>en</c> ↔ <c>.resx</c> của backend — nó bảo đảm hai ngôn
 * ngữ có cùng bộ khoá, và mã lỗi nào cũng có bản dịch. Nhưng nó <b>không biết template
 * đang gọi khoá nào</b>: gõ <c>'action.delete'</c> trong khi file JSON chỉ có
 * <c>action.cancel</c> thì cả hai ngôn ngữ vẫn khớp nhau hoàn hảo.
 *
 * Và ngx-translate hỏng theo kiểu tệ nhất: thiếu khoá thì nó in ra <b>chính chuỗi khoá</b>.
 * Người dùng nhìn thấy <c>action.delete</c> trên một cái nút. Đó đúng là thứ đã xảy ra khi
 * làm màn Phòng ban — ba khoá (<c>delete</c>, <c>rename</c>, <c>collapse</c>) chưa từng
 * được khai, mà build, lint và 119 test đều xanh.
 *
 * Nhật ký dự án đã ghi đúng cảnh báo này từ trước, về một hệ thật:
 * <i>"129 khoá khai trong code nhưng 18 khoá không có bản dịch nào cả… người dùng nhìn
 * thấy đúng chuỗi <c>dms.checklist.template_not_found</c> trên màn hình"</i>. Bộ canh cũ
 * bắt chiều đó cho backend; bộ này bắt chiều còn lại cho frontend.
 *
 * ═══════════════════════════════════════════════════════════════════════
 *  CANH GÌ, KHÔNG CANH GÌ
 * ═══════════════════════════════════════════════════════════════════════
 *
 * Canh: mọi khoá dạng chuỗi HẰNG trong template — <c>{{ 'a.b' | translate }}</c> và
 * <c>[attr.x]="'a.b' | translate"</c>.
 *
 * KHÔNG canh: khoá dựng động (<c>'users.status.' + s</c>) hay khoá nằm trong file
 * TypeScript. Chúng có thật và cũng hỏng được, nhưng bắt chúng cần phân tích luồng dữ
 * liệu — và một bộ canh biết rõ mình không canh gì thì tốt hơn một bộ canh giả vờ canh tất.
 */

const SRC = join(process.cwd(), 'src');
const I18N = join(SRC, 'assets', 'i18n');

/**
 * Ba file dịch của một ngôn ngữ, gộp lại thành một cây phẳng `a.b.c` → chuỗi.
 *
 * <b>CHỈ đọc `.json`, và cái lọc đó không thừa.</b> Bản đầu đọc mọi file trong thư mục,
 * nên một file rác sót lại (`common.json.bak` do chính tôi tạo lúc thử phá) vẫn được tính
 * là nguồn khoá — bộ canh xanh trong khi khoá thật đã bị gỡ.
 *
 * Kiểu mù này tệ hơn nó nghe: nó làm bộ canh nói dối đúng vào lúc có người đang sửa file
 * dịch, tức là đúng lúc nó cần nói thật nhất.
 */
function khoaCuaNgonNgu(lang: string): Set<string> {
  const out = new Set<string>();

  for (const file of readdirSync(join(I18N, lang)).filter((f) => f.endsWith('.json'))) {
    const cay = JSON.parse(readFileSync(join(I18N, lang, file), 'utf8')) as unknown;

    phang(cay, '', out);
  }

  return out;
}

function phang(node: unknown, tienTo: string, out: Set<string>): void {
  if (typeof node !== 'object' || node === null) {
    return;
  }

  for (const [khoa, giaTri] of Object.entries(node)) {
    const day = tienTo ? `${tienTo}.${khoa}` : khoa;

    if (typeof giaTri === 'string') {
      out.add(day);
    } else {
      phang(giaTri, day, out);
    }
  }
}

function moiTemplate(thuMuc: string): string[] {
  return readdirSync(thuMuc).flatMap((ten) => {
    const duongDan = join(thuMuc, ten);

    if (statSync(duongDan).isDirectory()) {
      return moiTemplate(duongDan);
    }

    return ten.endsWith('.html') ? [duongDan] : [];
  });
}

/** Khoá HẰNG mà template gọi qua ống `translate`. */
function khoaDangDung(): Map<string, string[]> {
  const ketQua = new Map<string, string[]>();

  for (const duongDan of moiTemplate(join(SRC, 'app'))) {
    const html = readFileSync(duongDan, 'utf8');
    const ten = duongDan.slice(SRC.length + 1).replaceAll('\\', '/');

    for (const [, khoa] of html.matchAll(/'([\w.]+)'\s*\|\s*translate/g)) {
      const noi = ketQua.get(khoa) ?? [];

      noi.push(ten);
      ketQua.set(khoa, noi);
    }
  }

  return ketQua;
}

describe('khoá dịch mà template gọi tới', () => {
  for (const lang of ['vi', 'en']) {
    it(`${lang}: mọi khoá đều tồn tại`, () => {
      const co = khoaCuaNgonNgu(lang);

      const thieu = [...khoaDangDung().entries()]
        .filter(([khoa]) => !co.has(khoa))
        .map(([khoa, noi]) => `${khoa} — dùng ở ${[...new Set(noi)].join(', ')}`)
        .sort();

      expect(thieu).toEqual([]);
    });
  }

  // Bẫy tự thân: biểu thức đọc hỏng thì danh sách rỗng và test trên xanh vĩnh viễn.
  it('bộ đọc tìm thấy đủ khoá để đáng tin', () => {
    expect(khoaDangDung().size).toBeGreaterThan(60);
    expect(khoaCuaNgonNgu('vi').size).toBeGreaterThan(60);
  });
});
