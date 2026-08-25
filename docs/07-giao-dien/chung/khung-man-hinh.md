# Khung màn hình

> Trạng thái: 🟡 **Bản dựng đã có, chờ duyệt** — `khung/nguoi-dung.html` · `khung/quan-tri.html`
> Xem bằng: `node tools/serve-mockups.mjs` → http://localhost:4300

Vỏ bọc quanh mọi màn sau khi đăng nhập. **Có hai cái, không phải một.**

---

## Vì sao hai

Khung v3 dựng ra một cột điều hướng duy nhất cho cả sản phẩm, và ba màn đã duyệt nằm
chung trong đó: Nhân sự · Vai trò & quyền · Hồ sơ. Nhìn kỹ thì hai trong ba là màn
**quản trị** — chúng thao tác lên người khác.

Hệ quả đo được, không phải phỏng đoán:

| Mục trên cột | Điều kiện quyền | `Member` thấy? |
|---|---|---|
| Bảng điều khiển | *(không đòi gì)* | ✅ — nhưng nó là **khung rỗng** |
| Nhân sự | `user.read` | ❌ ẩn |
| Vai trò | `role.read` | ❌ ẩn |

Một nhân viên thường đăng nhập vào thấy cột điều hướng còn đúng một mục, và mục đó trống.
Sản phẩm không phải "app nội bộ có phần quản trị" — nó là "trang quản trị bị nhầm là app".

Đáng chú ý: **bản thiết kế đầu tiên của chính file này đã vẽ đúng** — cột nav là
*Danh bạ · Phòng ban · Đơn từ · ───── · Quản trị*, có sẵn một vạch ngăn tách vùng quản trị.
Code v3 mới là chỗ đi lệch.

---

## Luật ranh giới — một câu, kiểm được

> Màn thao tác lên **người khác** hoặc lên **cấu hình workspace** → vùng quản trị.
> Màn thao tác lên **chính mình** hoặc lên **dữ liệu công việc hằng ngày** → vùng làm việc.

Có luật thì lần sau thêm màn không phải bàn lại. Áp vào những gì đang có:

| Màn | Vùng | Vì sao |
|---|---|---|
| Trang chủ | 🟢 làm việc | |
| Trao đổi nội bộ | 🟢 làm việc | dữ liệu công việc hằng ngày |
| Hồ sơ & cài đặt | 🟢 làm việc | **chính mình** — không ai tự đổi vai trò của mình |
| Danh bạ | 🟢 làm việc | *chưa làm* — xem đồng nghiệp, không sửa ai |
| Tổng quan | 🔴 quản trị | |
| Tài khoản | 🔴 quản trị | tạo tài khoản, đổi vai, vô hiệu hoá **người khác** |
| Vai trò & quyền | 🔴 quản trị | cấu hình của cả workspace |
| Nhật ký · Cấu hình workspace | 🔴 quản trị | *chưa làm* |
| Phòng ban | **cả hai** | bên làm việc để **xem** mình thuộc phòng nào; bên quản trị để **sửa** cây |

---

## Bố cục

```
🟢  /                              🔴  /admin
┌────────────┬──────────────┐      ┌───────────────┬───────────┐
│ 👤 Lê A.L. │              │      │ ▓ QUẢN TRỊ ▓  │           │  ← dải nền màu nhấn
│ 🔍 Tìm     │              │      │ ← Về làm việc │           │
│            │              │      │ 👤 Lê A.L.    │           │
│ LÀM VIỆC   │              │      │ 🔍 Tìm        │           │
│  Trang chủ │   nội dung   │      │               │  nội dung │
│  Trao đổi 4│              │      │ NGƯỜI         │           │
│  Chờ duyệt9│              │      │  Tổng quan    │           │
│ CÔNG TY    │              │      │  Tài khoản 38 │           │
│  Danh bạ   │              │      │  Vai trò      │           │
│  Phòng ban6│              │      │ CÔNG TY       │           │
│ ────────── │              │      │  Phòng ban  6 │           │
│ ⚙ Quản trị │              │      │  Nhật ký      │           │
└────────────┴──────────────┘      │  Cấu hình     │           │
       nền `--surface`             └───────────────┴───────────┘
                                     nền `--surface-2`
```

Cột quản trị đặt dải nhận diện **lên trên cả ảnh đại diện**: mắt đọc từ trên xuống, và câu
đầu tiên phải là *"bạn đang ở vùng quản trị"* chứ không phải *"bạn là ai"*.

### Ba dấu hiệu của vùng quản trị, mạnh dần từ dưới lên

1. đường **"Về không gian làm việc"** ngay dưới dải
2. nền cột đổi sang `--surface-2` — nhìn đâu cũng thấy, không chỉ ở rìa mắt
3. dải **"QUẢN TRỊ"** nền màu nhấn — chỗ **duy nhất** trong app lấy `--accent` làm nền cho
   cả một khối, nên không lẫn được với bất cứ thứ gì

**Vì sao không ghim một màu đỏ riêng cho vùng này:** màu nhấn là danh tính của bộ màu người
dùng đã chọn. Ghim cứng một màu thì ở bộ Giấy nó chửi nhau với đỏ mận `#8C2F39`, ở bộ Rêu
nó phá cả bảng màu — tức là phải khai thêm bốn giá trị nữa và canh chúng. `--surface-2` thì
mỗi bộ đã tự khai một giá trị hợp với chính nó. Chi tiết ở [`_khung-vung.css`](./_khung-vung.css).

---

## Đường dẫn

Luật (đã ghi trong `app.routes.ts` từ đầu, rồi bị phá): **màn người ngoài công ty nhìn thấy
thì tiếng Việt, phần bên trong app thì tiếng Anh cho khớp tên module.**

```
người ngoài   /login · /dang-ky
🟢 làm việc    /            Trang chủ
              /me          Hồ sơ & cài đặt
🔴 quản trị    /admin       Tổng quan
              /admin/users   Tài khoản
              /admin/roles   Vai trò & quyền
```

`/me` chứ không `/account` — khớp thẳng endpoint `GET /api/me` đang có.

---

## Menu ẩn hiện theo QUYỀN, không theo vai trò

```html
<a routerLink="/admin" *appHasPermission="['user.read', 'role.read']">Quản trị</a>
```

Kiểm theo vai trò (`*ngIf="user.role === 'HR'"`) là sai: hôm nào quản trị viên tạo vai trò
mới thì menu **lệch khỏi server ngay**, và không ai sửa vì không ai nhớ có chỗ đó.

Người không có quyền quản trị nào thì **cả vạch ngăn cũng phải biến mất** — để lại một
đường kẻ không có gì bên dưới thì nó thành nét vẽ vô nghĩa ở đáy cột.

> **Ẩn nút KHÔNG phải là phân quyền.** Nó chỉ là phép lịch sự với người dùng — đỡ phải bấm
> vào rồi bị từ chối. Phân quyền thật nằm ở server, và có test canh.

Guard đặt ở **route cha `/admin`**, không phải từng route con — cùng lý do cấu trúc đã dùng
cho `authGuard`: thêm màn quản trị mới thì không thể quên gắn. Gõ thẳng URL mà không có
quyền → đá về `/`, không phải `/forbidden`.

---

## Trên điện thoại

Kế thừa nguyên từ khung v3 (`_khung.css`), không thêm luật mới:

```
< 900px   cột thu còn 56px, chỉ biểu tượng — dải "QUẢN TRỊ" giữ lại biểu tượng và mảng màu
< 720px   cột dọc thành thanh NGANG dưới đáy, tầm với của ngón cái
```

Dải nhận diện **không được biến mất** khi thu gọn: đúng lúc cột hẹp nhất lại là lúc không
còn dấu hiệu nào khác nói đây là vùng quản trị.

---

## Chưa chốt

- Thanh ngang dưới đáy (< 720px) chưa có chỗ nào cho lối vào quản trị — nhét mục thứ sáu
  vào thì mỗi mục còn 60px. Hay là ẩn hẳn trên điện thoại?
- Chuông thông báo — chưa có module Notification. Tạm chưa vẽ.
