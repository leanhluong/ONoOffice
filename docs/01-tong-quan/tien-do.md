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
| `02-kien-truc` — cấu trúc BE, ranh giới module | 🟢 | Sơ đồ · 2 module Identity/Org · 4 luật ranh giới · ADR-0001 (multi-tenant) · ADR-0002 (xác thực) |
| `03-quy-uoc` — luật viết code | ⬜ | |
| `04-database` — bảng, quan hệ | ⬜ | |
| `05-api` — danh sách endpoint | ⬜ | |
| `06-deploy` — docker, CI, Cloudflare | ⬜ | |

## Giai đoạn 1 — `Luong.Kernel` (repo riêng `leanhluong/libNetCore`)

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
| `IDateTimeProvider` · `ICurrentUser` | 🟢 | Bản `ICurrentUser` đọc claim từ HTTP xong · không có `HttpContext` thì trả "không có ai", không ném lỗi |
| `PagedList<T>` | 🟢 | Mang theo `TotalCount` để vẽ được "Trang 2/17" · làm tròn LÊN |
| EF: quy ước snake_case | 🟢 | Đổi cả bảng, cột, khoá chính, khoá ngoại, chỉ mục — không sót cái nào |
| EF: interceptor tự điền `CreatedAt`/`UpdatedAt` | 🟢 | Một chỗ duy nhất, không handler nào phải nhớ |
| EF: xoá mềm + bộ lọc toàn cục | 🟢 | `Remove()` thành `UPDATE is_deleted` · query thường tự động không thấy hàng đã xoá |
| EF: bảng Outbox + ghi cùng transaction | 🟢 | Bịt lỗ "ghi hai nơi" · RabbitMQ chết thì nghiệp vụ vẫn chạy |
| `Messaging` — Outbox/Inbox + RabbitMQ | 🟢 | `OutboxDispatcher` · `InboxGuard` · publisher · consumer gốc (ack tay, prefetch, thư chết) · hosted service 10s |
| `Jobs` — Hangfire cho việc có lịch | 🟢 | **Không** dùng cho outbox (cron nhỏ nhất 1 phút) · chặn cửa dashboard · ghim Newtonsoft.Json vá lỗ hổng |
| `Caching` — Redis + distributed lock | 🟢 | `ICacheService` (cache cả giá trị rỗng) · lock nhả đúng mã bằng Lua · `CacheKey` |
| `Realtime` — SignalR + backplane | 🟢 | Backplane Redis · `ClaimsUserIdProvider` nhận cả `sub` lẫn `NameIdentifier` |
| `Application` — CQRS trên MediatR | 🟢 | `ICommand`/`IQuery` + 3 behavior: Validation → Transaction → Logging · `IUnitOfWork` một phương thức |
| CI: build + test mỗi lần push | 🟢 | `.github/workflows/ci.yml` — restore→build→test→pack thử |
| Phát hành lên GitHub Packages | 🟢 | `release.yml` chạy khi đẩy tag `v*` · đối chiếu tag ↔ `<Version>` trước khi phát hành |

**Số test hiện tại: 165 · tất cả xanh** · **8 package** `Luong.Kernel.*` pack được ở `0.1.0`.

## Giai đoạn 2 — Backend lát 1 (ONoOffice)

| Việc | Trạng thái | Ghi chú |
|---|---|---|
| Khởi tạo solution ONoOffice | 🟢 | backend/ + frontend/ · 9 project · 6 test kiến trúc canh 4 luật ranh giới |
| Module `Identity` — tầng Domain | 🟢 | Email · TenantCode · Tenant · User · Role · Permissions · RefreshToken — **104 test** |
| Module `Identity` — đăng nhập, token, vai trò | ⬜ | |
| Module `Org` — phòng ban, nhân viên | ⬜ | |
| Test ranh giới module | ⬜ | |
| Docker Compose (API + Postgres) | ⬜ | |

## Giai đoạn 3 — Frontend lát 1

| Việc | Trạng thái | Ghi chú |
|---|---|---|
| Khởi tạo Angular | 🟢 | Angular 22.1 · standalone + signal · 3 interceptor · guard theo permission · build + lint + 8 test xanh |
| Màn đăng nhập | ⬜ | |
| Cây phòng ban + danh bạ | ⬜ | |
| Màn quản trị nhân viên (HR) | ⬜ | |

## Giai đoạn 4 — Đưa lên mạng

| Việc | Trạng thái | Ghi chú |
|---|---|---|
| CI: build + test mỗi lần push | 🟢 | `.github/workflows/ci.yml` — restore→build→test→pack thử |
| Cloudflare: có URL công khai | ⬜ | |

---

## Nhật ký

| Ngày | Làm gì |
|---|---|
| 2026-08-23 | Chốt: modular monolith · lát 1 = đăng nhập + sơ đồ tổ chức · tài liệu 6 thư mục trong repo. Viết xong `01-tong-quan`. |
| 2026-08-23 | `Luong.Kernel`: dựng solution 3 package + test. Viết `Error`/`Result`/`ValidationError` theo TDD (3 vòng đỏ→xanh, 21 test). `dotnet pack` ra 3 `.nupkg` ở `0.1.0`. |
| 2026-08-23 | Chốt quy trình git cho repo cá nhân: **làm thẳng trên nhánh `develop`**, không nhánh phụ, không PR. |
| 2026-08-23 | `Luong.Kernel`: khép kín chuẩn lỗi ra tới HTTP — ProblemDetails, correlation-id, exception lọt lưới, `Result` → `IResult`. 3 vòng TDD nữa, tổng 41 test xanh. |
| 2026-08-23 | `Luong.Kernel`: thêm `Entity`/`AggregateRoot`/domain event · `IDateTimeProvider` · `ICurrentUser` (interface) · `PagedList<T>`. Tổng 61 test xanh. Chốt: commit message viết bằng tiếng Anh. |
| 2026-08-23 | `Luong.Kernel`: xong `Luong.Kernel.EntityFrameworkCore` — snake_case, interceptor audit, xoá mềm + bộ lọc toàn cục, **Outbox ghi cùng transaction**. Test bằng SQLite in-memory chứ không dùng EF InMemory (xanh giả). Tổng 83 test xanh. |
| 2026-08-23 | `Luong.Kernel`: package thứ 4 `Messaging` — `OutboxDispatcher` (nửa "gửi" của outbox) + `InboxGuard` (chống xử lý trùng) + bản cài EF cho cả hai. Cổng đặt ở `Core` nên test được mà không cần broker. Tổng 105 test xanh. |
| 2026-08-23 | `Luong.Kernel`: xong `Messaging` (RabbitMQ publisher + consumer gốc + hosted service điều phối outbox 10 giây/vòng) và `Jobs` (Hangfire cho việc nghiệp vụ có lịch). Chốt ranh giới: **outbox dùng BackgroundService, không dùng Hangfire** — cron nhỏ nhất của Hangfire là 1 phút, quá thưa. `TreatWarningsAsErrors` bắt được lỗ hổng Newtonsoft.Json 11.0.1 do Hangfire kéo theo → ghim 13.0.3. Tổng 127 test xanh, 5 package. |
| 2026-08-23 | `Luong.Kernel`: xong `Caching` (Redis + distributed lock nhả đúng mã) và `Realtime` (SignalR + backplane). Dựng CI GitHub Actions + workflow phát hành GitHub Packages theo tag `v*`. **Đủ 7 package · 149 test xanh.** |
| 2026-08-23 | `Luong.Kernel`: thêm `Application` (CQRS trên **MediatR** + 3 pipeline behavior), `ICurrentUser` bản đọc HTTP, `IUnitOfWork`. Chốt: **KHÔNG làm repository gốc generic** — `DbSet<T>` đã là repository, bọc thêm chỉ mất `Include`/projection/`AsNoTracking`. Repository theo từng aggregate sẽ viết trong ONoOffice với tên nói đúng câu hỏi nghiệp vụ. Tổng 165 test xanh, 8 package. |
| 2026-08-23 | **Đổi tên gói:** `LibNetCore.*` → `Luong.Kernel.*`. Lý do: `LibNetCore` trên nuget.org đã có người khác lấy · chữ "Lib" thừa · ".NET Core" là tên đã lỗi thời từ 2020 · `LibNetCore.Core` lặp chữ. Đổi bây giờ vì chưa phát hành gì — sau khi đẩy lên nuget.org thì tên là vĩnh viễn. Thêm bước đẩy nuget.org vào `release.yml`. |
| 2026-08-23 | **Phát hành `Luong.Kernel` 0.1.0 lên GitHub Packages** — đủ 8 gói, đã xác minh qua API. nuget.org tạm tắt (API key bị 403). Vá 2 lỗi trong workflow: regex đọc `<Version>` bắt trúng chú thích · `contents: read` khiến bước tạo Release trả 403. |
| 2026-08-23 | **Chốt 6 câu nghiệp vụ.** Multi-tenant chung DB + `tenant_id` · mỗi người thuộc đúng 1 workspace · email unique toàn hệ thống · 4 vai `Owner`/`Admin`/`Manager`/`Member` · kiểm `permission` không kiểm `role`. Viết `02-kien-truc` + ADR-0001 + ADR-0002. |
| 2026-08-23 | **Dựng khung code.** Backend: 9 project (2 module × 4 tầng + Api), `Directory.Build.targets` có công tắc `UseLocalKernel`, 6 test kiến trúc canh 4 luật ranh giới — đã chứng minh bằng cách cố ý phá luật 2 lần rồi khôi phục. Frontend: Angular 22.1 dựng song song bởi agent nền, chỉ được chạm `frontend/` và **cấm chạy git** để không giẫm chân commit của backend. |
| 2026-08-23 | **Xong tầng `Domain` của `Identity`** — 104 test, đỏ trước xanh sau. Chốt: quyền là hằng số trong code (quét bằng phản chiếu), vai trò nằm trong DB · vai trò hệ thống bất biến · refresh token **xoay vòng, dùng một lần** — dùng lại lần hai nghĩa là bị trộm, phải thu hồi cả chuỗi. Thư viện `Luong.Kernel` bị ứng dụng vạch ra một lỗ hổng: `Result` trơn thiếu phép đổi ngầm từ `Error` → đã vá. |
| 2026-08-23 | Thêm thư mục tài liệu thứ 7: **`07-giao-dien`** — chia theo module rồi theo màn, khuôn 8 mục cố định cho mỗi màn (trong đó *các trạng thái* và *lỗi hiện thế nào* là hai mục hay bị bỏ quên nhất). Viết xong màn **Đăng nhập** + hệ thống thiết kế + trạng thái chung + khung màn hình. Dựng bản mẫu 4 bộ màu để chọn. |
| 2026-08-23 | **Đổi ý về phạm vi: làm đa ngôn ngữ NGAY từ đầu** (trước xếp vào "cố ý không làm"). Lý do: thêm sau khi đã có 40 màn là mở từng file tìm từng chuỗi viết cứng, sót thì không lỗi nào báo. Chốt `@ngx-translate` (đổi tiếng không tải lại trang, một bản build duy nhất), tiếng Việt là ngôn ngữ gốc, khoá lỗi trùng khít mã lỗi backend. |
| 2026-08-23 | **Chốt: ship CẢ BỐN bộ màu** thành tính năng đổi giao diện, không chọn một. Rẻ vì luật "component chỉ dùng token" đã có từ đầu. Mặc định theo `prefers-color-scheme`, người dùng đổi thì ghi `localStorage` — giao diện là lựa chọn của từng người, giống ngôn ngữ. Vẽ `wireframes.html`: 6 màn, đơn sắc, có chú thích đánh số ở những chỗ CÓ quyết định. |
| 2026-08-23 | Đưa bản dựng màu màn đăng nhập vào repo (`docs/07-giao-dien/identity/dang-nhap.html`) — trước đó nó chỉ nằm trên web, clone repo về là mất. Thêm **`HANDOFF.md`** ở gốc repo: đang ở đâu · việc tiếp theo (tầng `Api`, đã duyệt thiết kế) · luật bắt buộc · 10 quyết định đã chốt · **4 cái bẫy đã gặp** để người sau không dẫm lại. |
