# ONoOffice — brand assets

Hướng logo: 2 chữ O lồng nhau (mark) + wordmark **ON**·o·office.

## Files
| File | Dùng khi |
| --- | --- |
| logo-mark.svg | mark trên nền sáng |
| logo-mark-light.svg | mark trên nền tối |
| logo-lockup.svg | logo đầy đủ, nền sáng |
| logo-lockup-light.svg | logo đầy đủ, nền tối |
| app-icon.svg | icon app 512px (bo góc sẵn) |
| favicon.svg | favicon 64px |
| tokens.css | biến màu + font |

## Màu
- Nâu đậm `#5A3E2B` — màu chính
- Caramel `#B98A5E` — nhấn
- Kem `#F4EEE6` — nền
- Mực `#2E2723` — chữ

## Font
Outfit (700 cho "ON", 300 cho "office") + IBM Plex Mono cho label/mã.

```html
<link href="https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;700&family=IBM+Plex+Mono:wght@400;500&display=swap" rel="stylesheet">
```

## Dùng
```html
<link rel="stylesheet" href="/brand/tokens.css">
<link rel="icon" type="image/svg+xml" href="/brand/favicon.svg">
<img src="/brand/logo-lockup.svg" alt="ONoOffice" height="32">
```

Lưu ý: lockup SVG dùng `<text>` với font Outfit — nếu nhúng ở nơi không load được Google Fonts, nó fallback sang Helvetica (nét vẫn ổn nhưng khác chút). Cần bản bất biến 100% thì mở SVG trong Figma/Illustrator và Convert to outlines.

## Khoảng thở
Chừa lề tối thiểu = bán kính vòng tròn (≈ 1/4 chiều cao logo) quanh mọi phía. Không kéo méo, không đổi màu ngoài palette, mark tối thiểu 20px.
