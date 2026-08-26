/**
 * Hình dạng của nội dung hướng dẫn — do `tools/sync-huongdan.mjs` sinh ra.
 *
 * ⚠️ Đổi gì ở đây thì phải đổi cả bộ sinh. Hai bên là một hợp đồng, và nó KHÔNG được
 * TypeScript canh: dữ liệu tới từ một file JSON tải lúc chạy, nên lệch nhau chỉ lộ ra khi
 * mở đúng bài đó trên trình duyệt. `huong-dan.spec.ts` đọc JSON thật để bịt chỗ đó.
 *
 * Đây là CÂY KHỐI, không phải chuỗi HTML — xem chú thích đầu `sync-huongdan.mjs` để biết
 * ba lý do. Ngắn gọn: không `innerHTML` thì không có XSS, không bị Angular khử vệ sinh, và
 * test đếm được "bài này có mấy ảnh".
 */

/** Một mẩu chữ trong dòng. `k` = loại. */
export type DoanChu =
  | { k: 'chu'; v: string }
  | { k: 'dam'; v: string }
  | { k: 'ma'; v: string }
  | { k: 'lien'; v: string; den: string };

export type KhoiBai =
  | { k: 'de'; muc: number; chu: string }
  | { k: 'doan'; chu: DoanChu[] }
  | { k: 'ds'; thutu: boolean; muc: DoanChu[][] }
  | { k: 'anh'; mota: string; tep: string; chuthich: string | null }
  | { k: 'chuy'; tong: 'luuy' | 'canh'; chu: DoanChu[] }
  | { k: 'bang'; dau: DoanChu[][]; than: DoanChu[][][] };

/** Một bài, đủ nội dung. */
export interface BaiHuongDan {
  readonly ma: string;
  readonly tieude: string;
  readonly nhom: string;
  readonly tomtat: string;
  readonly khoi: readonly KhoiBai[];
}

/** Một dòng trong cây bên trái — chỉ đủ để vẽ mục lục, không mang nội dung. */
export interface TomTatBai {
  readonly ma: string;
  readonly tieude: string;
  readonly tomtat: string;
}

export interface NhomHuongDan {
  readonly ma: string;
  readonly ten: string;
  readonly mota: string;
  readonly bai: readonly TomTatBai[];
}
