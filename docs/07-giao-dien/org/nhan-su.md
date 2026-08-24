# Màn Nhân sự

> Bản dựng: [`nhan-su.html`](./nhan-su.html) — `node tools/serve-mockups.mjs` rồi vào
> <http://localhost:4300/org/nhan-su.html>.
>
> `?state=rong` · `?state=khongthay` · `?mo=manThem` · `?mo=manThem&buoc=xong` ·
> `?mo=manChiTiet` · `?skin=giay` · `?bare=1`
>
> **Đã dựng bằng Angular và chạy thật** — `/nhan-su`.

---

## 1. Màn này để làm gì

Quản trị viên xem ai đang có tài khoản trong workspace, tạo tài khoản cho người mới, đổi
vai trò, và vô hiệu hoá tài khoản của người nghỉ việc.

Đây là màn **xương sống** của phần quản trị: mọi màn danh sách sau này (phòng ban, đơn từ,
khách hàng) dùng lại đúng bộ điều khiển ở [`../chung/_dieukhien.css`](../chung/_dieukhien.css).
Chốt hình dáng ở đây một lần thì những màn sau chỉ còn là đổi cột.

## 2. Ai vào được

Quyền `user.read` để xem, `user.manage` để tạo và sửa. **Tách hai quyền** vì gộp lại thì
ai xem được danh bạ cũng tạo được tài khoản.

Mục điều hướng cũng ẩn theo `user.read` — không có quyền thì không thấy mục, thay vì thấy
rồi bấm vào và nhận 403.

## 3. Đường dẫn

```
/nhan-su
```

Tiếng Việt vì đây là màn người dùng cuối nhìn thấy. Bộ lọc **chưa** nằm trên URL — xem mục
"chưa làm".

## 4. Bố cục

```
┌─────────────────────────────────────────────────────────────────┐
│ Nhân sự · 38 người                              [+ Thêm người]  │ ← tiêu đề, ĐỨNG YÊN
├─────────────────────────────────────────────────────────────────┤
│ [🔍 Tìm]  [Mọi vai trò ▾]  [Mọi trạng thái ▾]      [Xoá lọc]    │ ← lọc, ĐỨNG YÊN
├─────────────────────────────────────────────────────────────────┤
│ ☐ NGƯỜI              VAI TRÒ    TRẠNG THÁI      NGÀY TẠO        │ ← đầu bảng DÍNH
│ ☐ ◐ Lê Anh Lượng     Owner      ● Đang hoạt động  24/08/2026  ⋮ │
│ ☐ ◐ Trần Bình        Manager    ● Chờ nhận TK     24/08/2026  ⋮ │ ← chỉ vùng này CUỘN
├─────────────────────────────────────────────────────────────────┤
│ Hiện 1–6 trong 38                            [Trước] [Sau]      │ ← phân trang, ĐỨNG YÊN
└─────────────────────────────────────────────────────────────────┘
```

Chỉ bảng cuộn. Cuộn cả trang thì người xem hàng thứ 40 không còn thấy bộ lọc đang bật là gì
— và họ sẽ kết luận sai về những gì mình đang nhìn.

## 5. Các trạng thái

| Trạng thái | Khi nào | Nói gì |
|---|---|---|
| `idle` | Có người, không lọc | Bảng bình thường |
| `loc` | Có người, đang lọc | Bảng + nút **Xoá lọc** hiện ra |
| `khongthay` | Lọc không ra ai | *Không tìm thấy ai khớp* + nút xoá lọc |
| `rong` | Workspace chưa có ai ngoài mình | *Chưa có ai ngoài bạn* + nút thêm người đầu tiên |

**Phân biệt `rong` với `khongthay` là bắt buộc.** Hai câu cần nói khác hẳn nhau. Gộp làm
một thì người bật nhầm bộ lọc từ lần trước sẽ kết luận là công ty không có ai.

Nút **Xoá lọc** chỉ hiện khi CÓ bộ lọc đang bật. Lúc nào cũng hiện thì nó thành nhiễu, và
khi cần thì người dùng lại không nhận ra nó có nghĩa gì.

## 6. Hộp thoại thêm người — HAI BƯỚC

```
Bước 1  họ tên · email · vai trò · [công tắc] bắt đổi mật khẩu
   ↓  POST /api/users
Bước 2  ✓ Đã tạo tài khoản cho …
        Mật khẩu tạm   [ k7np-2wqx-hs4m ]  [Chép]
        ⚠️ Đây là lần duy nhất bạn thấy mật khẩu này.
        [Thêm người nữa]  [Xong]
```

**Bước hai bắt buộc phải có.** Mật khẩu tạm chỉ tồn tại đúng một lần, ngay trong phản hồi
của lời gọi tạo — không ghi log, không lưu, không endpoint nào đọc lại được. Đóng hộp thoại
mà chưa chép thì phải đặt lại mật khẩu cho người ta.

**Mở lại hộp thoại thì luôn về bước 1.** Giữ nguyên bước 2 thì lần sau người dùng mở ra
thấy mật khẩu của người trước — vừa khó hiểu vừa là rò rỉ.

**Vai trò mặc định là `Member`**, vai hẹp nhất. Mặc định vai rộng thì một cú bấm vội tạo ra
một quản trị viên — sai theo hướng nguy hiểm.

## 7. Ngăn kéo chi tiết

Trượt từ **phải**, không phải hộp thoại giữa màn: xem chi tiết một người thì vẫn cần thấy
mình đang ở dòng nào trong danh sách.

Hai thẻ: **Thông tin** (sửa họ tên, xem email/ngày tạo/trạng thái, vùng nguy hiểm) và
**Vai trò & quyền** (đổi vai, xem bộ quyền của vai đó).

**Nút "Vô hiệu hoá" ẨN khi backend chắc chắn từ chối** — với chính mình và với chủ sở hữu.
Hiện nút rồi báo lỗi khi bấm là cách chắc chắn nhất làm người dùng bực; họ không biết mình
đã làm gì sai. Thay bằng một câu ngắn giải thích.

## 8. Dữ liệu cần

```
GET   /api/users?search=&status=&roleId=&page=&pageSize=
POST  /api/users                      → { …, temporaryPassword }
PATCH /api/users/{id}                 { fullName, roleId }
POST  /api/users/{id}/disable · /enable
GET   /api/roles                      danh sách vai trò cho ô chọn
```

Chi tiết ở [`../../05-api/README.md`](../../05-api/README.md).

**Lọc, sắp xếp, phân trang đều ở SERVER.** Lọc trong bộ nhớ thì với 38 người vẫn chạy, và
với 3.800 người thì sập — mà không có gì trong mã báo trước điều đó.

Ô tìm kiếm **chờ 300ms sau khi ngừng gõ**. Gọi theo từng phím thì một cái tên mười ký tự là
mười lượt đi về.

## 9. Lỗi hiện thế nào

| Mã lỗi | Hiện thế nào |
|---|---|
| `Email.Taken` | Đỏ **ngay tại ô email** trong hộp thoại, kèm popup |
| `User.CannotDisableSelf` · `CannotDisableOwner` | Không xảy ra — nút đã ẩn. Nếu vẫn tới thì popup |
| `Role.NotFound` | Popup |
| *(mất mạng)* | Popup, bảng giữ nguyên dữ liệu cũ |

## 10. Trên màn hẹp

```
< 900px   ẩn cột "Ngày tạo"; cột điều hướng tự thu về biểu tượng
```

---

## Bốn quyết định trong màn này

### 1. Vô hiệu hoá, không phải xoá

Người nghỉ việc vẫn còn tin nhắn, còn tên trên bản ghi cũ, còn là người duyệt của một đơn
từ năm ngoái. Xoá đi thì mọi chỗ đó thành khoảng trống và không ai khôi phục lại được ngữ
cảnh.

### 2. Ô "chọn tất cả" có ba trạng thái

Tick · không tick · **chọn một phần**. Chỉ có hai trạng thái đầu thì nó nói dối — nhìn vào
tưởng chưa chọn ai trong khi đang chọn dở ba dòng.

Và nạp lại danh sách thì **bỏ chọn những người không còn trên trang**. Giữ lại thì thanh
"đã chọn 3 người" nói về những người đang không nhìn thấy, và thao tác hàng loạt sẽ chạm
vào người mà quản trị viên không hề định chạm.

### 3. Nền tối của hộp thoại là một NÚT, không phải một div

Bấm ra ngoài để đóng là thói quen của người dùng chuột. Nhưng một `div` bắt sự kiện bấm thì
người dùng bàn phím và trình đọc màn hình không chạm tới được — họ không biết là có cách
đóng ở đó. Một `<button>` phủ kín thì Tab tới được, Enter bấm được, và đọc đúng là "Đóng".
Kèm theo đó, `Escape` cũng đóng.

### 4. Đổi bộ lọc thì quay về trang một

Đang ở trang 3 mà lọc lại thì kết quả mới thường không có tới trang 3 — người dùng nhận về
một trang trống và tưởng không tìm thấy ai.

---

## Chưa làm — ghi rõ để không ai tưởng đã có

| Việc | Vì sao chưa |
|---|---|
| **Cột "Chức danh" và "Phòng ban"** | Dữ liệu đó thuộc module Org, hiện mới xong tầng Domain nên chưa có endpoint. Vẽ sẵn hai cột luôn rỗng thì bảng đầy dấu gạch ngang — trông như dữ liệu thiếu, không như tính năng chưa tới |
| **Cột "Hoạt động"** (đăng nhập cuối) | `refresh_tokens` chưa lưu mốc và thiết bị. Cột hiện là **Ngày tạo**, là thứ đang có thật |
| **Thao tác hàng loạt** | Thanh "đã chọn N người" mới có nút bỏ chọn. Đổi vai trò / vô hiệu hoá hàng loạt cần endpoint nhận nhiều mã một lúc |
| **Xuất Excel** | Cần chọn thư viện và chốt định dạng cột |
| **Bộ lọc trên URL** | Để dán link "danh sách phòng Kế toán đang chờ nhận tài khoản" cho đồng nghiệp |
| **Tìm theo MỘT PHẦN email** | Cột `email` ánh xạ qua phép chuyển đổi giá trị nên EF không dịch nổi `Contains`. Hiện chỉ khớp email chính xác |
| **Đặt lại mật khẩu cho người khác** | Cần một endpoint riêng, và cùng luồng hai bước như lúc tạo |
