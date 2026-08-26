import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import { ConversationKind } from '../../core/models/chat.model';

/**
 * Hợp đồng với module Comm, đối chiếu THẲNG với mã nguồn C#.
 *
 * Hai thứ đi qua dây HTTP mà tự nó không mang ý nghĩa gì:
 *
 * <list type="bullet">
 * <item><b>`kind` là một CON SỐ.</b> Ai đó chèn thêm một kiểu hội thoại vào giữa enum bên
 * backend là mọi hội thoại riêng lặng lẽ được vẽ bằng bố cục của nhóm — bong bóng
 * trái–phải biến thành một cột, không lỗi, không cảnh báo.</item>
 * <item><b>Tên trường.</b> Đổi một tên ở record C# thì `JSON.parse` vẫn chạy, signal vẫn
 * có giá trị, chỉ một trường là `undefined`. Đây đúng cách màn Bảng điều khiển từng chào
 * "Chào buổi chiều," cụt lủn suốt một ngày.</item>
 * </list>
 */

const COMM = join(
  process.cwd(),
  '..',
  'backend',
  'src',
  'Modules',
  'Comm',
  'ONoOffice.Comm.Application',
  'Abstractions',
  'CommViews.cs',
);

const KIND = join(
  process.cwd(),
  '..',
  'backend',
  'src',
  'Modules',
  'Comm',
  'ONoOffice.Comm.Domain',
  'Entities',
  'ConversationKind.cs',
);

/** Đọc tên các tham số của một `record` C#, bỏ qua chú thích XML xen giữa. */
function truongCua(record: string): string[] {
  const source = readFileSync(COMM, 'utf8');
  const block = new RegExp(`record ${record}\\(([\\s\\S]*?)\\);`).exec(source);

  expect(block, `không tìm thấy record ${record} ở backend`).not.toBeNull();

  /*
    Bỏ chú thích XML TRƯỚC, rồi mới tách theo dấu phẩy.

    Bản đầu khớp theo DÒNG (`^\s*Kiểu Tên,`) và trượt ngay ở `MessagePageResponse` —
    record đó viết cả hai tham số trên một dòng. Một bộ đọc chỉ hiểu được cách trình bày
    mà nó tình cờ gặp đầu tiên thì sẽ im lặng đúng vào lúc ai đó gộp dòng.

    Dấu phẩy bên trong `<>` (như `IReadOnlyList<Dictionary<string, int>>`) không phải chỗ
    tách, nên phải đếm độ sâu chứ không `split(',')`.
  */
  const than = block![1].replace(/\/\/\/[^\n]*/g, '');
  const phan: string[] = [];
  let sau = 0;
  let dang = '';

  for (const ky of than) {
    if (ky === '<') {
      sau++;
    } else if (ky === '>') {
      sau--;
    }

    if (ky === ',' && sau === 0) {
      phan.push(dang);
      dang = '';
    } else {
      dang += ky;
    }
  }

  phan.push(dang);

  return phan
    .map((p) => /(\w+)\s*$/.exec(p.trim())?.[1])
    .filter((ten): ten is string => ten !== undefined)
    // C# viết hoa chữ đầu, JSON của ASP.NET viết thường — mặc định của
    // `JsonNamingPolicy.CamelCase`.
    .map((ten) => ten.charAt(0).toLowerCase() + ten.slice(1));
}

describe('hợp đồng với module Comm', () => {
  it('ConversationKind khớp enum của backend', () => {
    const source = readFileSync(KIND, 'utf8');
    const doc = Object.fromEntries(
      [...source.matchAll(/^\s{4}(\w+)\s*=\s*(\d+),/gm)].map(([, ten, so]) => [ten, Number(so)]),
    );

    expect(doc).toEqual({
      Rieng: ConversationKind.Rieng,
      Nhom: ConversationKind.Nhom,
    });
  });

  it('ConversationSummary: hai bên khai đúng cùng bộ trường', () => {
    expect(truongCua('ConversationSummary').sort()).toEqual(
      [
        'id',
        'kind',
        'displayName',
        'otherUserId',
        'participantCount',
        'lastMessageBody',
        'lastMessageSenderName',
        'lastMessageAtUtc',
        'unreadCount',
      ].sort(),
    );
  });

  it('MessageItem: hai bên khai đúng cùng bộ trường', () => {
    expect(truongCua('MessageItem').sort()).toEqual(
      ['id', 'senderUserId', 'senderName', 'body', 'sentAtUtc'].sort(),
    );
  });

  it('MessagePageResponse: hai bên khai đúng cùng bộ trường', () => {
    expect(truongCua('MessagePageResponse').sort()).toEqual(['items', 'hasMore'].sort());
  });

  /**
   * Bẫy tự thân: phép đọc phải THẬT SỰ đọc được.
   *
   * Đường dẫn hỏng thì mọi `expect` trên so hai mảng rỗng với nhau và tất cả đều xanh —
   * một bộ canh im lặng còn tệ hơn không có, vì nhìn danh sách vẫn thấy nó nằm đó.
   */
  it('đọc được cả ba record ở backend', () => {
    for (const record of ['ConversationSummary', 'MessageItem', 'MessagePageResponse']) {
      expect(truongCua(record).length, `record ${record} đọc ra rỗng`).toBeGreaterThan(1);
    }
  });
});
