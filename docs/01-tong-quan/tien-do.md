# Tiến độ

> File này được sửa nhiều nhất trong repo. **Xong việc gì thì sửa ở đây ngay trong cùng commit.**
> Cập nhật lần cuối: 2026-08-23

## Ký hiệu

`⬜` chưa bắt đầu · `🟡` đang làm · `🟢` xong · `🔴` đang vướng

---

## Giai đoạn 0 — Tài liệu & thiết kế

| Việc | Trạng thái | Ghi chú |
|---|---|---|
| Dựng khung 6 thư mục tài liệu | 🟢 | |
| `01-tong-quan` — ý tưởng, phạm vi, tiến độ | 🟢 | Còn treo 6 câu hỏi nghiệp vụ ở cuối README |
| `02-kien-truc` — cấu trúc BE, ranh giới module | ⬜ | Chờ chốt 6 câu hỏi |
| `03-quy-uoc` — luật viết code | ⬜ | |
| `04-database` — bảng, quan hệ | ⬜ | |
| `05-api` — danh sách endpoint | ⬜ | |
| `06-deploy` — docker, CI, Cloudflare | ⬜ | |

## Giai đoạn 1 — `libNetCore` (repo riêng)

Bản đồ 8 package, phát hành dần khi có nội dung thật. Hiện dựng 3.

| Việc | Trạng thái | Ghi chú |
|---|---|---|
| Solution + 3 project + test project | 🟢 | `dotnet build` 0 warning |
| `Directory.Build.props` — một `<Version>` cho cả bộ | 🟢 | `0.1.0` · lockstep · TreatWarningsAsErrors · Nullable |
| `Error` + `ErrorType` | 🟢 | 6 loại lỗi · mã dạng `Vùng.ChuyệnGì` · record nên so sánh được |
| `Result` + `Result<T>` | 🟢 | Chặn 3 trạng thái vô lý · 2 phép chuyển ngầm |
| `ValidationError` — ôm nhiều lỗi con | 🟢 | Để form 10 ô sai báo cả 10 lượt |
| Middleware Problem Details (`AspNetCore`) | 🟢 | `Error` → HTTP + RFC 7807 · trường `errors` luôn là danh sách |
| Middleware bắt exception lọt lưới | 🟢 | Log đủ stack trace, trả ra ngoài không lộ gì · kèm `correlationId` |
| `Result` → `IResult` cho endpoint | 🟢 | Endpoint còn một dòng, không tự quyết định mã HTTP |
| Middleware correlation-id | 🟢 | Giữ nguyên mã đến từ gateway, không sinh mã mới |
| `Entity` · `AggregateRoot` · `IDomainEvent` | 🟢 | So sánh bằng danh tính · gốc tổng hợp ghi lại sự kiện, không tự gọi ai |
| `IDateTimeProvider` · `ICurrentUser` | 🟡 | Interface + đồng hồ hệ thống xong · bản `ICurrentUser` đọc từ HTTP chưa làm |
| `PagedList<T>` | 🟢 | Mang theo `TotalCount` để vẽ được "Trang 2/17" · làm tròn LÊN |
| EF: quy ước snake_case | 🟢 | Đổi cả bảng, cột, khoá chính, khoá ngoại, chỉ mục — không sót cái nào |
| EF: interceptor tự điền `CreatedAt`/`UpdatedAt` | 🟢 | Một chỗ duy nhất, không handler nào phải nhớ |
| EF: xoá mềm + bộ lọc toàn cục | 🟢 | `Remove()` thành `UPDATE is_deleted` · query thường tự động không thấy hàng đã xoá |
| EF: bảng Outbox + ghi cùng transaction | 🟢 | Bịt lỗ "ghi hai nơi" · RabbitMQ chết thì nghiệp vụ vẫn chạy |
| `Messaging` — Outbox/Inbox + RabbitMQ | 🟢 | `OutboxDispatcher` · `InboxGuard` · publisher · consumer gốc (ack tay, prefetch, thư chết) · hosted service 10s |
| `Jobs` — Hangfire cho việc có lịch | 🟢 | **Không** dùng cho outbox (cron nhỏ nhất 1 phút) · chặn cửa dashboard · ghim Newtonsoft.Json vá lỗ hổng |
| `Caching` — Redis + distributed lock | ⬜ | Package thứ 6 |
| `Realtime` — SignalR + backplane | ⬜ | Package thứ 7 |
| CI: build + test mỗi lần push | ⬜ | |
| Phát hành lên GitHub Packages | ⬜ | |

**Số test hiện tại: 127 · tất cả xanh** (Core 50 · AspNetCore 20 · EntityFrameworkCore 32 · Messaging 25). 5 package pack được ở `0.1.0`.

## Giai đoạn 2 — Backend lát 1 (ONoOffice)

| Việc | Trạng thái | Ghi chú |
|---|---|---|
| Khởi tạo solution ONoOffice | ⬜ | Chờ 6 câu hỏi nghiệp vụ |
| Module `Identity` — đăng nhập, token, vai trò | ⬜ | |
| Module `Org` — phòng ban, nhân viên | ⬜ | |
| Test ranh giới module | ⬜ | |
| Docker Compose (API + Postgres) | ⬜ | |

## Giai đoạn 3 — Frontend lát 1

| Việc | Trạng thái | Ghi chú |
|---|---|---|
| Khởi tạo Angular | ⬜ | |
| Màn đăng nhập | ⬜ | |
| Cây phòng ban + danh bạ | ⬜ | |
| Màn quản trị nhân viên (HR) | ⬜ | |

## Giai đoạn 4 — Đưa lên mạng

| Việc | Trạng thái | Ghi chú |
|---|---|---|
| CI: build + test mỗi lần push | ⬜ | |
| Cloudflare: có URL công khai | ⬜ | |

---

## Nhật ký

| Ngày | Làm gì |
|---|---|
| 2026-08-23 | Chốt: modular monolith · lát 1 = đăng nhập + sơ đồ tổ chức · tài liệu 6 thư mục trong repo. Viết xong `01-tong-quan`. |
| 2026-08-23 | `libNetCore`: dựng solution 3 package + test. Viết `Error`/`Result`/`ValidationError` theo TDD (3 vòng đỏ→xanh, 21 test). `dotnet pack` ra 3 `.nupkg` ở `0.1.0`. |
| 2026-08-23 | Chốt quy trình git cho repo cá nhân: **làm thẳng trên nhánh `develop`**, không nhánh phụ, không PR. |
| 2026-08-23 | `libNetCore`: khép kín chuẩn lỗi ra tới HTTP — ProblemDetails, correlation-id, exception lọt lưới, `Result` → `IResult`. 3 vòng TDD nữa, tổng 41 test xanh. |
| 2026-08-23 | `libNetCore`: thêm `Entity`/`AggregateRoot`/domain event · `IDateTimeProvider` · `ICurrentUser` (interface) · `PagedList<T>`. Tổng 61 test xanh. Chốt: commit message viết bằng tiếng Anh. |
| 2026-08-23 | `libNetCore`: xong `LibNetCore.EntityFrameworkCore` — snake_case, interceptor audit, xoá mềm + bộ lọc toàn cục, **Outbox ghi cùng transaction**. Test bằng SQLite in-memory chứ không dùng EF InMemory (xanh giả). Tổng 83 test xanh. |
| 2026-08-23 | `libNetCore`: package thứ 4 `Messaging` — `OutboxDispatcher` (nửa "gửi" của outbox) + `InboxGuard` (chống xử lý trùng) + bản cài EF cho cả hai. Cổng đặt ở `Core` nên test được mà không cần broker. Tổng 105 test xanh. |
| 2026-08-23 | `libNetCore`: xong `Messaging` (RabbitMQ publisher + consumer gốc + hosted service điều phối outbox 10 giây/vòng) và `Jobs` (Hangfire cho việc nghiệp vụ có lịch). Chốt ranh giới: **outbox dùng BackgroundService, không dùng Hangfire** — cron nhỏ nhất của Hangfire là 1 phút, quá thưa. `TreatWarningsAsErrors` bắt được lỗ hổng Newtonsoft.Json 11.0.1 do Hangfire kéo theo → ghim 13.0.3. Tổng 127 test xanh, 5 package. |
