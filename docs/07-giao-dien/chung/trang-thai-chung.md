# Trạng thái chung

> Trạng thái: 🟡 **Đã định nghĩa, chưa dựng**

Năm trạng thái mà **mọi màn có tải dữ liệu** đều phải có. Ghi ra một chỗ để không màn nào
quên, và để chúng trông giống nhau ở mọi nơi.

---

## Đang tải

Dùng **khung xám mô phỏng nội dung** (skeleton), không dùng con quay giữa màn.

Vì sao: con quay không nói gì về thứ sắp hiện ra, và làm màn hình nhảy một cái khi dữ liệu
về. Khung xám giữ đúng chỗ, nên nội dung hiện ra **không đẩy gì đi đâu**.

## Rỗng

Không bao giờ để trống trơn. Luôn có ba phần:

```
Chuyện gì đang xảy ra   "Chưa có nhân viên nào trong phòng ban này."
Vì sao (nếu cần)        "Phòng ban vừa được tạo."
Làm gì tiếp             [ Thêm nhân viên ]   ← chỉ hiện nếu người này CÓ quyền
```

Nút hành động phải qua `*hasPermission` — hiện một nút mà bấm vào bị từ chối còn tệ hơn
là không hiện.

## Lỗi

```
Câu chữ theo mã lỗi từ backend (xem bảng ở từng màn)
Nút [ Thử lại ]  — nếu thao tác lặp lại được
Mã tham chiếu: {correlationId}  — chữ nhỏ, màu mờ
```

**Luôn kèm `correlationId`.** Người dùng đọc cho bộ phận hỗ trợ, và từ mã đó lần ra đúng
dòng log — thay vì hỏi qua hỏi lại "lúc mấy giờ, bạn bấm gì".

## Không đủ quyền — 403

```
"Bạn không có quyền xem mục này."
"Liên hệ quản trị viên của công ty nếu bạn cần truy cập."
[ Về trang chủ ]
```

**KHÔNG** liệt kê quyền còn thiếu tên là gì — đó là thông tin về cấu trúc hệ thống, không
phải thứ người dùng cần.

Tốt hơn hết: **đừng để họ tới được đây.** Menu và nút đã ẩn theo quyền rồi; màn 403 là lưới
an toàn cho trường hợp gõ thẳng đường dẫn.

## Không tìm thấy — 404

```
"Không tìm thấy trang này."
[ Về trang chủ ]
```

Lưu ý về multi-tenant: xem dữ liệu của công ty khác cũng ra **404**, không phải 403. Trả
403 là **xác nhận rằng thứ đó có tồn tại** — kẻ dò chỉ cần thử hàng loạt id là biết được
công ty khác có bao nhiêu bản ghi.
