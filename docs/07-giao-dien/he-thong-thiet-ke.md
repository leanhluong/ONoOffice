# Hệ thống thiết kế

> Trạng thái: 🟢 **Đã chốt** — bốn bộ màu (người dùng tự chọn), chữ, khoảng cách, bo góc

Trang này giữ những thứ **mọi màn đều dùng**. Có nó thì mỗi màn mới không phải quyết định
lại từ đầu, và không có chuyện hai màn cùng một ý nhưng khác nhau vài pixel.

---

## Chữ

Chọn theo **tiếng Việt trước**, không phải theo thẩm mỹ trước.

| Vai trò | Bộ chữ | Vì sao |
|---|---|---|
| Thân bài, giao diện | **Be Vietnam Pro** | Được thiết kế riêng cho tiếng Việt — dấu má cân đối, không chồng lấn như phần lớn font phương Tây |
| Tiêu đề | **Sora** 600/700 | Hình học, hiện đại, có bộ ký tự tiếng Việt đầy đủ |
| Mã lỗi, số liệu, mã workspace | **JetBrains Mono** | Chữ đều bề ngang → cột số thẳng hàng, và `l` `1` `I` phân biệt được |

```
Cỡ chữ        12 · 13.5 · 15 · 17 · 21 · 25 · 32 · 46
Chiều cao dòng 1.6 cho thân bài · 1.12 cho tiêu đề lớn
Dãn chữ       tiêu đề -0.02em · nhãn viết hoa +0.07em
Bề ngang đọc  tối đa ~65 ký tự
```

## Khoảng cách

Bội số của **4px**. Không có ngoại lệ.

```
4 · 8 · 12 · 16 · 20 · 24 · 28 · 32 · 40 · 56
```

Lý do rất thực tế: không có thang thì sẽ có chỗ 13px, chỗ 15px, chỗ 18px — nhìn riêng
không sai, nhìn cả trang thì lệch lạc mà không chỉ ra được vì sao.

## Bo góc

```
ô nhập, nút        10px
thẻ, khung cảnh báo 10px
huy hiệu, chip     999px (bo tròn hẳn)
```

Cố ý **không dùng bo góc lớn** ở khắp nơi — nó làm giao diện trông mềm và giống mọi sản
phẩm khác. Góc 10px giữ được cảm giác gọn và chính xác.

## Màu — GIỮ CẢ BỐN BỘ, cho người dùng tự chọn

Chốt ngày 2026-08-23: **không chọn một bộ, mà ship cả bốn thành tính năng đổi giao diện.**

Quyết định này **rẻ vì đã chuẩn bị từ trước**: luật "component chỉ dùng token, không bao
giờ viết mã màu trực tiếp" đã có từ đầu. Bốn bộ chỉ là bốn lần khai lại 10 token.

```
Mực        nền đen ám xanh    · điểm nhấn hổ phách      · trầm, dùng lâu không mỏi mắt
Hải đăng   nền xanh mực sâu   · điểm nhấn san hô        · ấm và sống động hơn
Giấy       nền trắng ngà      · điểm nhấn đỏ rượu       · sáng, sắc nét, kiểu tạp chí
Rêu        nền xanh rêu tối   · điểm nhấn xanh xô thơm  · dịu nhất, ít gặp
```

**Chọn mặc định theo máy người dùng**, không ép:

```
prefers-color-scheme: dark   → Mực
prefers-color-scheme: light  → Giấy
người dùng tự chọn           → ghi localStorage['theme'], từ đó ưu tiên lựa chọn của họ
```

Giao diện là **lựa chọn của từng người**, giống ngôn ngữ — không phải cấu hình của
workspace. Hai người cùng công ty được dùng hai giao diện khác nhau.

### Cái giá — nói thẳng

| Mất gì | Cách sống chung |
|---|---|
| Mỗi component mới phải **nhìn đúng ở cả bốn bộ** | Luật token đã bắt buộc; thêm một lần soi mắt lúc review |
| Ảnh chụp trong tài liệu **nhân lên bốn** | Chỉ chụp bộ mặc định; nêu rõ đây là một trong bốn |
| Bốn bộ đều phải đủ **độ tương phản** cho chữ | Kiểm bằng công cụ, không kiểm bằng mắt — mắt quen rất nhanh |

*Ngưỡng phải xem lại:* khi thêm một bộ thứ năm bắt đầu thấy phiền, hoặc khi có bộ nào
người dùng gần như không ai chọn — lúc đó bỏ bớt.

## Mười token của mỗi bộ

Mỗi bộ khai đủ 10 token dưới đây. Component **chỉ được dùng token**, không bao giờ viết
mã màu trực tiếp — nhờ vậy đổi bộ màu là đổi một chỗ.

```
--ground      nền ngoài cùng
--surface     nền thẻ, ô nhập
--surface-2   nền chìm hơn một bậc
--line        đường kẻ, viền
--ink         chữ chính
--ink-soft    chữ phụ, nhãn
--ink-faint   chữ mờ, gợi ý
--accent      màu điểm nhấn — CHỈ dùng cho hành động chính
--accent-ink  chữ nằm trên nền accent
--danger      lỗi
```

Cả bốn bộ đều ship. Bảng màu cụ thể xem
[`identity/dang-nhap.md`](./identity/dang-nhap.md).

**Một điểm nhấn duy nhất trên nền trầm.** Cố ý tránh gradient tím-xanh — kiểu đó đang bị
dùng đến mòn, mọi sản phẩm trông giống nhau.

## Ba luật về màu

**Màu ngữ nghĩa tách khỏi màu điểm nhấn.** `--danger` không bao giờ là `--accent`. Nếu
điểm nhấn là màu đỏ (bộ Giấy) thì `--danger` phải là một sắc đỏ **khác hẳn** — nếu không
thì nút chính và thông báo lỗi trông như nhau.

**Không bao giờ dùng riêng màu để truyền tin.** Trạng thái luôn có **chữ hoặc hình** đi
kèm — khoảng 8% nam giới bị mù màu đỏ-lục, và không ai trong số họ báo lỗi này cho bạn.

**Điểm nhấn chỉ cho hành động chính.** Một màn hình có **đúng một** nút màu accent. Nhiều
nút cùng nổi bật thì không nút nào nổi bật cả.

## Component dùng chung

Nằm ở `frontend/src/app/shared/ui/`. Danh sách hiện có và cần có:

| Component | Trạng thái |
|---|---|
| `alert` — khung cảnh báo, nhận mã lỗi + câu chữ | 🟢 |
| `has-permission` — chỉ thị ẩn/hiện theo quyền | 🟢 |
| `field` — ô nhập kèm nhãn + dòng lỗi | ⬜ |
| `empty-state` — trạng thái rỗng kèm gợi ý hành động | ⬜ |
| `confirm-dialog` — hỏi lại trước hành động không hoàn tác được | ⬜ |
