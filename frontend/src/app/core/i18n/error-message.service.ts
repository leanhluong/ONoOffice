import { Injectable, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import type { AppError } from '../models/api-error.model';

/**
 * Đổi một <c>AppError</c> thành câu chữ hiển thị cho người dùng.
 *
 * <b>Rẽ nhánh theo `code`, KHÔNG dùng `description` của backend.</b> Mã thì ổn định; câu
 * chữ thì đổi mỗi khi ai đó sửa một dấu phẩy, và nó chỉ có một thứ tiếng — tiếng mà
 * backend viết cứng trong code.
 *
 * Ba nấc dự phòng, xếp từ tốt nhất xuống:
 *
 * <ol>
 * <li><b>Có bản dịch cho mã đó</b> → dùng, đúng ngôn ngữ người dùng chọn.</li>
 * <li><b>Không có, nhưng backend gửi kèm mô tả</b> → dùng mô tả đó. Sai ngôn ngữ còn hơn
 * không có gì, và người dùng vẫn đọc hiểu.</li>
 * <li><b>Không có cả hai</b> → câu chung theo loại lỗi.</li>
 * </ol>
 *
 * Nấc cuối cùng luôn kèm <b>mã tham chiếu</b> nếu có — để người dùng đọc cho bộ phận hỗ
 * trợ, và từ đó lần ra đúng dòng log ở backend. Không có đường thoát đó thì lỗi chưa
 * lường trước trở thành ngõ cụt cho cả hai bên.
 */
@Injectable({ providedIn: 'root' })
export class ErrorMessageService {
  private readonly translate = inject(TranslateService);

  /** Câu chữ chính. */
  resolve(error: AppError): string {
    const key = `${error.code}`;
    const translated = this.translate.instant(key);

    // ngx-translate trả về CHÍNH cái khoá khi không tìm thấy — đó là cách duy nhất để
    // biết là thiếu bản dịch, vì nó không ném lỗi.
    if (translated !== key && typeof translated === 'string' && translated.length > 0) {
      return translated;
    }

    if (error.message.length > 0) {
      return error.message;
    }

    return this.translate.instant(`errorKind.${error.kind}`) as string;
  }

  /**
   * Mã tham chiếu để tra log — <b>chỉ khi ta KHÔNG giải thích được</b> chuyện gì đã xảy ra.
   *
   * Với "sai mật khẩu" thì mã kỹ thuật chẳng giúp gì: người dùng biết chính xác phải làm
   * gì rồi, và một dãy ký tự lạ chỉ khiến câu thông báo trông như lỗi hệ thống.
   *
   * Cắt còn SÁU ký tự đầu. Đủ để tìm trong log của một ngày, mà đọc qua điện thoại cho bộ
   * phận hỗ trợ thì không phải đánh vần ba mươi hai ký tự.
   */
  reference(error: AppError): string | null {
    if (error.correlationId === null) {
      return null;
    }

    const coBanDich = this.translate.instant(error.code) !== error.code;

    return coBanDich ? null : error.correlationId.slice(0, 6);
  }
}
