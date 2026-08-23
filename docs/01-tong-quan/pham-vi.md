# Phạm vi — làm gì trước, làm gì sau

> Cập nhật lần cuối: 2026-08-23

---

## Cách chia việc: lát cắt dọc, không phải tầng ngang

Có hai cách chia một dự án lớn:

**Cách sai (chia ngang):** tháng 1 làm hết database, tháng 2 làm hết API, tháng 3 làm hết giao diện.
→ Hết tháng 2 vẫn **chưa có gì dùng được**. Sai sót ở tháng 1 tới tháng 3 mới lộ.

**Cách đúng (chia dọc):** mỗi lát cắt đi **xuyên hết mọi tầng** nhưng chỉ cho một nhóm tính năng nhỏ — có giao diện, có API, có bảng dữ liệu, chạy được, dùng được.
→ Cuối lát 1 đã có thứ mở lên xem được. Mọi rủi ro hạ tầng (deploy, đăng nhập, CI) lộ ngay từ lát đầu, lúc còn rẻ để sửa.

Dự án này đi theo cách thứ hai.

---

## Lát 1 — Đăng nhập + Sơ đồ tổ chức

Chọn lát này làm đầu tiên vì nghiệp vụ của nó **nhẹ nhất**, nên toàn bộ công sức đổ vào dựng khung: Clean Architecture, `Luong.Kernel`, Docker, CI, Cloudflare. Khung dựng đúng một lần rồi mọi lát sau dùng lại.

### Xong lát 1 nghĩa là gì

Chưa đủ 8 gạch đầu dòng dưới đây thì **chưa được sang lát 2**:

| # | Tiêu chí | Kiểm chứng bằng cách nào |
|---|---|---|
| 1 | Nhân viên đăng nhập bằng email + mật khẩu, nhận được token | Đăng nhập thật trên trình duyệt |
| 2 | Token hết hạn thì tự làm mới, không bị đá ra ngoài giữa chừng | Chỉnh hạn token xuống 1 phút rồi ngồi dùng 5 phút |
| 3 | Xem được cây phòng ban + danh bạ, tìm theo tên/phòng ban | Mở trang, gõ tìm |
| 4 | HR thêm/sửa/khoá nhân viên, điều chuyển phòng ban | Làm thử đủ 4 thao tác |
| 5 | Nhân viên thường **gọi thẳng** API của admin thì bị chặn | Dùng Postman gọi trực tiếp, không qua giao diện |
| 6 | `docker compose up` là chạy được toàn bộ | Xoá sạch, clone lại từ GitHub, chạy 1 lệnh |
| 7 | Mỗi lần mở PR, máy tự build + chạy test | Mở 1 PR thử, xem đèn xanh/đỏ |
| 8 | Có địa chỉ web mở được bằng điện thoại 4G | Tắt wifi, mở bằng 4G |

Chỗ đáng chú ý là **tiêu chí 5**. Ẩn nút trên giao diện *không phải* là phân quyền — đó chỉ là trang trí. Phân quyền thật là server từ chối. Đây là lỗi kinh điển và cũng là câu hay bị hỏi khi phỏng vấn.

### Cố ý KHÔNG làm ở lát 1

Ghi ra để về sau không tự tiện thêm vào giữa chừng:

| Thứ bị hoãn | Hoãn tới khi nào |
|---|---|
| Chat, thông báo realtime | Lát riêng, sau này |
| Upload ảnh đại diện | Cần chỗ lưu file — để chung lát có Drive |
| Quên mật khẩu qua email | Cần dịch vụ gửi mail — lát 2 |
| Đăng nhập bằng Google/Microsoft | Chừa chỗ trong thiết kế, chưa làm |
| ~~Đa ngôn ngữ~~ | **ĐÃ ĐỔI Ý 23/08 — làm ngay từ đầu.** Thêm vào sau khi đã có 40 màn là mở từng file tìm từng chuỗi viết cứng, sót thì không có lỗi nào báo. Xem [`07-giao-dien/da-ngon-ngu.md`](../07-giao-dien/da-ngon-ngu.md) |
| Giao diện tối | Chốt bộ màu xong mới làm cả sáng lẫn tối cho bộ được chọn |
| Nhật ký thay đổi (audit log) | Lát 2, khi đã có dữ liệu đáng để ghi vết |

---

## Các lát sau — bản nháp, sẽ đổi

Chỉ ghi để định hướng, **chưa cam kết**:

| Lát | Nội dung | Cái mới học được |
|---|---|---|
| 2 | Đơn từ / phê duyệt (nghỉ phép) | Nghiệp vụ giàu: máy trạng thái, quy tắc miền, domain event |
| 3 | Thông báo | Chạy nền, hàng đợi, gửi mail |
| 4 | Chat | Realtime, SignalR, đồng bộ nhiều máy chủ |
| 5 | Task / giao việc | Frontend nặng, kéo thả |

> ❓ Thứ tự này phụ thuộc câu hỏi #6 ở [README](./README.md) — chưa chốt.
