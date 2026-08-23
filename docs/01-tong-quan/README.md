# 01 — Tổng quan

> Cập nhật lần cuối: 2026-08-23 · Trạng thái: 🟢 bản nháp đầu, chờ chốt 6 câu hỏi ở cuối file

---

## Ý tưởng, gói trong một câu

**ONoOffice là app nội bộ để một công ty quản lý thông tin của chính mình** — ai là ai, thuộc phòng nào, làm việc gì — và về sau mở rộng thành chỗ mọi người trao đổi, xin phép, giao việc cho nhau.

Tham chiếu quen thuộc: Lark Suite, Base.vn, Slack + Workday ghép lại. Nhưng **không đặt mục tiêu bằng họ** — đặt mục tiêu làm đúng một lát mỏng trước, cho chạy thật, rồi mới dày lên.

---

## Vấn đề đang giải

Một công ty 50–200 người, không có hệ thống nội bộ, thì thông tin nằm ở đâu:

| Thông tin | Đang nằm ở đâu | Hậu quả |
|---|---|---|
| Danh sách nhân viên | File Excel trên máy chị HR | Người mới vào không biết hỏi ai; Excel có 3 phiên bản khác nhau |
| Sơ đồ phòng ban | Trong đầu vài người cũ | Không ai vẽ được cây tổ chức đầy đủ |
| Số điện thoại, email nội bộ | Zalo, danh bạ cá nhân | Tìm người ở phòng khác mất 15 phút hỏi vòng |
| Ai duyệt đơn của ai | Truyền miệng | Đơn gửi nhầm người, kẹt vài ngày |

ONoOffice biến bốn dòng đó thành **một nguồn dữ liệu duy nhất, có phân quyền, có lịch sử thay đổi**.

---

## Ai dùng, dùng để làm gì

| Vai | Người thật là ai | Họ vào app để làm gì |
|---|---|---|
| `Nhân viên` | Toàn bộ công ty | Tra danh bạ, xem mình thuộc phòng nào, sếp là ai |
| `Trưởng phòng` | Quản lý cấp phòng | Xem danh sách người trong phòng mình; về sau: duyệt đơn |
| `HR` | Phòng nhân sự | Thêm/sửa hồ sơ nhân viên, điều chuyển phòng ban |
| `Admin` | Người quản trị hệ thống | Cấp tài khoản, gán vai trò, khoá tài khoản |

> ❓ **Chưa chốt:** 4 vai này là tôi đề xuất. Cần xác nhận đủ chưa (xem câu hỏi #4 cuối file).

---

## Không phải là gì

Ghi ra để về sau không tự ý phình:

- **Không** phải phần mềm chấm công / tính lương — chuyện đó dính pháp lý và bảo hiểm, quy mô khác hẳn.
- **Không** phải mạng xã hội nội bộ (newsfeed, like, comment).
- **Không** phải hệ thống bán cho khách hàng bên ngoài ở giai đoạn này.
- **Không** làm mobile app riêng. Web chạy được trên điện thoại là đủ.

---

## Công nghệ và lý do chọn

| Mảng | Chọn gì | Vì sao chọn cái này |
|---|---|---|
| Backend | **.NET 10** | Ngôn ngữ chính đang dùng đi làm; muốn đào sâu chứ không muốn học lại thứ mới |
| Kiến trúc BE | **Clean Architecture + Modular Monolith** | Mục tiêu học là chỗ này. Chi tiết ở `02-kien-truc/` |
| Frontend | **Angular** | Có TypeScript chặt chẽ, hợp với người quen tư duy backend |
| Database | **PostgreSQL 16** | Miễn phí, mạnh, đúng thứ đang dùng đi làm |
| Package dùng chung | **`Luong.Kernel`** — repo riêng, đẩy lên GitHub | Học cách tách thư viện dùng chung, đánh version, phát hành. Xem ghi chú bên dưới |
| Chạy máy local | **Docker Compose** | Một lệnh dựng đủ API + DB + web |
| Đưa lên mạng | **Cloudflare** | Học thêm mảng hạ tầng. Chi tiết ở `06-deploy/` |

### Ghi chú về `Luong.Kernel`

Đây là **repo tách riêng** (`github.com/leanhluong/libNetCore`), không nằm trong ONoOffice.

- **Chứa gì:** những mảnh không dính nghiệp vụ và dùng lại được ở bất kỳ dự án .NET nào — kiểu `Result<T>` để trả lỗi mà không ném exception, lớp `Entity` gốc, đồng hồ `IDateTimeProvider`, phân trang, quy ước đặt tên bảng snake_case, middleware bắt lỗi.
- **Cấm chứa gì:** bất kỳ thứ gì có chữ `Employee`, `Department`, `Leave`… Hễ thấy từ nghiệp vụ ONoOffice xuất hiện trong `Luong.Kernel` là đã đặt sai chỗ.
- **Vì sao tách repo mà không để chung:** để bị ép sống với hệ quả thật — phải đánh số phiên bản, phải phát hành, và khi sửa thì phải nghĩ "sửa thế này có làm hỏng người dùng cũ không". Để chung thư mục thì không học được gì cả, vì sửa xong là chạy ngay.

---

## Cách làm việc: làm tới đâu, viết tới đó

Ba luật, áp dụng cho cả người lẫn agent:

1. **Không viết tài liệu cho thứ chưa làm.** Thư mục chưa tới lượt thì để nguyên chữ "chưa tới lượt". Tài liệu mô tả tương lai tưởng tượng là tài liệu nói dối.
2. **Xong một việc thì cập nhật tài liệu trong cùng lần commit đó.** Tách ra "code trước, docs sau" là cách chắc chắn nhất để docs lệch khỏi code.
3. **Quyết định lớn thì đẻ ra một ADR.** ADR ngắn thôi — bối cảnh, các lựa chọn, chốt cái nào, đánh đổi gì, học được gì. Nằm ở `02-kien-truc/adr/`.

Tiến độ thật nằm ở [`tien-do.md`](./tien-do.md) — đó là file được sửa nhiều nhất trong repo.

---

## 6 câu còn treo — chốt xong mới sang `02-kien-truc/`

| # | Câu hỏi | Mặc định tạm nếu không chốt |
|---|---|---|
| 1 | Một công ty, hay nhiều công ty cùng dùng (mỗi bên thấy dữ liệu riêng)? | Một công ty |
| 2 | Quy mô thật: bao nhiêu nhân viên, bao nhiêu phòng ban? | ~200 người, ~15 phòng |
| 3 | Phòng ban lồng mấy cấp? Một người thuộc 2 phòng được không? Có "quản lý trực tiếp" không? | Lồng nhiều cấp · mỗi người 1 phòng · có quản lý trực tiếp |
| 4 | Đủ 4 vai `Admin`/`HR`/`Trưởng phòng`/`Nhân viên` chưa? | Đủ 4 vai |
| 5 | Đăng nhập bằng email + mật khẩu? Sau này có cần Google/Microsoft không? | Email + mật khẩu; chừa chỗ cho đăng nhập ngoài |
| 6 | Sau lát 1 làm module nào tiếp — Chat / Đơn từ / Task / Lịch? | Đơn từ (nghỉ phép) |

> Câu **#1 nặng nhất**: nó quyết định mọi bảng dữ liệu có thêm cột "công ty nào" hay không. Thêm sau nghĩa là sửa lại toàn bộ.
