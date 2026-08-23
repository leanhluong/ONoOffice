# Hệ thống thiết kế

> Trạng thái: 🟡 **Chờ chốt bộ màu** — chữ và khoảng cách thì đã chốt

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

## Màu — CHỜ CHỐT

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

Bốn phương án đang chờ chọn: **Mực · Hải đăng · Giấy · Rêu** — xem
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
