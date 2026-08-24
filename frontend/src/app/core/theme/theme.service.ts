import { Injectable, signal } from '@angular/core';

/** Bốn bộ màu ship kèm sản phẩm — chốt ở `docs/07-giao-dien/he-thong-thiet-ke.md`. */
export const THEMES = [
  { id: 'muc', name: 'Mực', hint: 'Nền đen ám xanh · điểm nhấn hổ phách', dark: true },
  { id: 'hai-dang', name: 'Hải đăng', hint: 'Nền xanh mực sâu · điểm nhấn san hô', dark: true },
  { id: 'giay', name: 'Giấy', hint: 'Nền trắng ngà · điểm nhấn đỏ rượu', dark: false },
  { id: 'reu', name: 'Rêu', hint: 'Nền xanh rêu tối · điểm nhấn xanh xô thơm', dark: true },
] as const;

export type ThemeId = (typeof THEMES)[number]['id'];

const STORAGE_KEY = 'onooffice.theme';

/** Máy thích tối thì Mực, thích sáng thì Giấy — quy ước ở he-thong-thiet-ke.md. */
const DARK_DEFAULT: ThemeId = 'muc';
const LIGHT_DEFAULT: ThemeId = 'giay';

function isThemeId(value: string | null): value is ThemeId {
  return value !== null && THEMES.some((theme) => theme.id === value);
}

/**
 * Đổi bộ màu bằng cách đặt một thuộc tính trên thẻ <html>.
 *
 * <b>Vì sao đổi thuộc tính chứ không nạp file CSS khác:</b> nạp file thì đổi giao diện
 * là một lần đi mạng, và có một khoảnh khắc trang chưa có màu. Cả bốn bộ chỉ là bốn lần
 * khai lại đúng mười biến CSS — gộp hết vào một file thì việc đổi bộ màu diễn ra tức thì,
 * và tốn thêm chưa tới một kilobyte.
 *
 * <b>Giao diện là lựa chọn của TỪNG NGƯỜI, không phải cấu hình của workspace.</b> Nên nó
 * nằm ở <c>localStorage</c> của máy đó, không nằm trong hồ sơ người dùng trên server —
 * giống hệt cách đối xử với ngôn ngữ.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly currentState = signal<ThemeId>(LIGHT_DEFAULT);

  readonly current = this.currentState.asReadonly();

  readonly themes = THEMES;

  /**
   * Gọi một lần lúc khởi động, TRƯỚC khi vẽ khung hình đầu tiên.
   *
   * Thứ tự ưu tiên: người dùng đã chọn → cài đặt của máy → sáng. Người đã chọn tay thì
   * lựa chọn đó thắng, kể cả khi máy họ đổi sang chế độ tối lúc chiều tối — họ chọn rồi,
   * đừng đổi sau lưng.
   */
  initialise(): void {
    this.apply(this.readSaved() ?? this.readSystemPreference());
  }

  set(theme: ThemeId): void {
    this.apply(theme);

    try {
      localStorage.setItem(STORAGE_KEY, theme);
    } catch {
      // Chế độ ẩn danh: giao diện vẫn đổi, chỉ là đóng tab thì quên.
    }
  }

  private apply(theme: ThemeId): void {
    this.currentState.set(theme);

    if (typeof document !== 'undefined') {
      document.documentElement.dataset['theme'] = theme;
    }
  }

  private readSaved(): ThemeId | null {
    try {
      const saved = localStorage.getItem(STORAGE_KEY);
      return isThemeId(saved) ? saved : null;
    } catch {
      return null;
    }
  }

  private readSystemPreference(): ThemeId {
    if (typeof matchMedia !== 'function') {
      return LIGHT_DEFAULT;
    }

    return matchMedia('(prefers-color-scheme: dark)').matches ? DARK_DEFAULT : LIGHT_DEFAULT;
  }
}
