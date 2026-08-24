# Tiến độ

> File này được sửa nhiều nhất trong repo. **Xong việc gì thì sửa ở đây ngay trong cùng commit.**
> Cập nhật lần cuối: 2026-08-24

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
| `05-api` — danh sách endpoint | 🟢 | 3 endpoint đăng nhập · hình dạng lỗi chung · quy ước phân quyền |
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
| Module `Identity` — tầng Application | 🟢 | Login · Refresh (xoay vòng + phát hiện trộm) · Logout |
| Module `Identity` — tầng Infrastructure | 🟢 | EF Core · Argon2id · JWT HS256 · repository theo từng aggregate |
| Tầng `Api` — host, middleware, phân quyền | 🟢 | 3 endpoint · policy sinh lúc chạy theo `permission` · CORS · header an toàn · i18n · **29 test tích hợp** |
| Module `Org` — phòng ban, nhân viên | ⬜ | |
| Test ranh giới module | 🟢 | 4 luật ranh giới + 4 luật Controller — đều đã cố ý phá một lần để chứng minh |
| Migration đầu tiên (`InitialIdentity`) | 🟢 | 6 bảng + schema `identity` · đã chạy thật trên Postgres 16 |
| Docker Compose (Postgres) | 🟢 | Cổng **5433** — máy đã có Postgres khác giữ 5432 |
| Dữ liệu mồi (seeder) | 🟢 | Workspace demo + 4 vai hệ thống + chủ sở hữu · công tắc `Seed:Enabled`, mặc định TẮT |
| Test có database thật | 🟢 | 26 test Testcontainers — luồng đăng nhập, xoay vòng vé, phát hiện trộm, cô lập tenant |
| Docker image cho API | ⬜ | Tới lúc deploy mới cần |

## Giai đoạn 3 — Frontend lát 1

| Việc | Trạng thái | Ghi chú |
|---|---|---|
| Khởi tạo Angular | 🟢 | Angular 22.1 · standalone + signal · 4 interceptor · guard theo permission |
| Màn đăng nhập | 🟢 | Nối API thật · hai cột · nền sơ đồ tổ chức · hiện/ẩn mật khẩu |
| Khung ứng dụng v3 (cột điều hướng có chữ) | 🟢 | Sinh từ `_khung.css` · thu gọn được · menu tài khoản · mỗi trang tự dựng tiêu đề |
| Bản dựng nhân sự · tài khoản · vai trò | 🟢 | Đã duyệt, chưa nối Angular |
| Màn đăng ký workspace | 🟢 | Tạo công ty + 4 vai + chủ sở hữu trong một lần · gợi ý mã từ tên công ty · thanh đo độ mạnh mật khẩu |
| Bản dựng ↔ code không lệch | 🟢 | CSS **sinh** từ bản dựng · `npm run parity` so từng điểm ảnh · test đối chiếu luật kiểm dữ liệu với backend |
| Tự gia hạn phiên khi 401 | 🟢 | Gộp một lần chống bão gia hạn · gửi lại đúng một lần |
| 4 bộ màu | 🟢 | 10 token × 4 bộ · mặc định theo cài đặt sáng/tối của máy, người dùng đổi được |
| Đa ngôn ngữ (ngx-translate) | 🟢 | vi/en · 3 file theo module · test đối chiếu mã lỗi với backend |
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
| 2026-08-23 | **Chốt ADR-0004: token đi trong thân phản hồi, không dùng cookie.** Lý do không phải "đủ an toàn" mà là hoàn cảnh: deploy đầu tiên lên hạ tầng miễn phí → FE và BE khác tên miền gốc → cookie thành cookie bên thứ ba → **Safari chặn mặc định**, tức là không chạy chứ không phải kém an toàn. Bù bằng ba lớp đã có sẵn (xoay vòng · phát hiện dùng lại · thu hồi cả chuỗi) + CSP. Ngưỡng chuyển sang cookie: khi FE và API về cùng tên miền gốc. |
| 2026-08-24 | **Xong tầng `Api`** — 29 test tích hợp dựng máy chủ thật trong bộ nhớ (`WebApplicationFactory`), không cần Postgres vì `AddDbContext` không mở kết nối lúc khởi động. Ba endpoint đăng nhập · phân quyền động sinh policy lúc chạy · CORS đích danh · ba header an toàn · dịch thông báo lỗi theo `Accept-Language`. Đã cố ý phá **6 luật** rồi khôi phục để chứng minh test bắt được. |
| 2026-08-24 | **Sửa lại một hiểu sai về thứ tự `UseCors`.** Lời giải thích quen thuộc — "preflight OPTIONS không mang token nên bị 401" — thực nghiệm cho thấy KHÔNG xảy ra ở đây: `OPTIONS` không khớp endpoint nào nên `UseAuthorization` chẳng có policy nào để áp. Chuyện hỏng thật là ở request thường bị từ chối: `UseAuthorization` cắt ngang, nên nếu `UseCors` đứng sau thì **401 đi ra không có header CORS** → trình duyệt cấm JavaScript đọc phản hồi → frontend không phân biệt được "phiên hết hạn" với "máy chủ hỏng". Đã viết test canh đúng chỗ đó. |
| 2026-08-24 | **Ba lỗ hổng im lặng phát hiện khi ráp tầng `Api`.** (1) 41 khoá `.resx` chưa từng được dùng — không ai gọi `ProblemDetails.Localize`, nên mọi người dùng luôn nhận câu tiếng Việt viết cứng; đã thêm `LocalizeProblemDetailsFilter`. (2) `[ApiController]` mặc định tự sinh khuôn lỗi RIÊNG (`errors` là từ điển) cho JSON hỏng, khác hẳn mảng `errors[]` của mọi lỗi khác → frontend phải viết hai nhánh; đã ép về một hình dạng. (3) `ClockSkew` mặc định 5 phút khiến access token 15 phút thật ra sống 20 phút → đặt về 0. |
| 2026-08-24 | Gỡ hai dòng `<EmbeddedResource Update="ResourcesMessages.*.resx">` trong `ONoOffice.Api.csproj`: đường dẫn thiếu dấu `/` nên chúng là **no-op**, và quy ước mặc định của SDK mới là thứ đang chạy đúng (sinh satellite assembly `vi/` + `en/`, đúng nơi `ResourceManager` tìm). Sửa "đúng" theo ý định ban đầu sẽ làm mọi bản dịch trả `null`. Cũng đã thử `<NeutralLanguage>vi</NeutralLanguage>` và nó **làm hỏng ngay** — test khởi động bắt được. |
| 2026-08-24 | **Chạm database lần đầu** — docker-compose (Postgres 16, cổng **5433** vì máy đã có sẵn một Postgres giữ 5432) · migration `InitialIdentity` · seeder gieo workspace demo + 4 vai hệ thống + chủ sở hữu. Thêm 26 test dùng **Testcontainers** (tự dựng Postgres riêng, không nối vào compose — test nối vào compose sẽ im lặng bỏ qua trên CI). Đã gọi thật `POST /api/auth/login` bằng curl và nhận về token đủ 12 quyền. Tổng **227 test xanh**. |
| 2026-08-24 | **Ba lỗi im lặng mà 194 test trước đó không thể bắt được.** (1) `Role._permissions` là `HashSet<string>` — EF chỉ map được mảng hoặc `IList`, nên **mô hình chưa từng dựng nổi trên Postgres**; test đơn vị chỉ chạm `context.Model` nên không kích hoạt kiểm tra đó. Sửa bằng bộ chuyển đổi `HashSet ↔ string[]` kèm `ValueComparer` (thiếu comparer thì `Grant()` không sinh câu UPDATE nào — cấp quyền xong mà không lưu). (2) Cột sinh ra tên `_permissions`, `_role_ids` — gạch dưới của C# rò vào schema. (3) `DatabaseFixture` bản đầu tự dựng `DbContextOptions`, thiếu cả 4 interceptor — test cô lập tenant **xanh giả** vì lớp bảo vệ nó tưởng đang kiểm thì không có mặt. |
| 2026-08-24 | **Vá `Luong.Kernel` lần thứ hai do ứng dụng vạch ra.** `UseSnakeCaseNames()` đọc `property.Name` (tên C#) thay vì tên cột đang có, nên nó **ghi đè cả `HasColumnName`** khai tay. Tài liệu của nó nói "bổ sung cho chỗ chưa đặt tên", thực tế là ghi đè tất — với cột thường hai đường ra cùng kết quả nên chẳng ai thấy, chỉ lộ đúng ở chỗ người ta cố tình đặt tên khác. Sửa một dòng, thêm 2 test canh. Kernel: **202 test xanh**. |
| 2026-08-24 | **Frontend nối vào API thật.** Màn đăng nhập chạy đầu-tới-cuối với backend đang chạy: hai cột, nền sơ đồ tổ chức vẽ bằng SVG, nút hiện/ẩn mật khẩu, trạng thái chờ KHÔNG khoá ô nhập (người vừa nhận ra gõ nhầm email không phải ngồi chờ hết một vòng mạng). Thêm **bốn bộ màu** (10 token × 4 bộ, mặc định theo cài đặt sáng/tối của máy) và **ngx-translate** (vi/en, ba file chia theo module). FE: 31 test xanh, build + lint sạch. |
| 2026-08-24 | **Bốn chỗ hợp đồng FE lệch với backend, tìm ra trước khi gõ dòng nào** — khung FE viết hôm 23 khi backend chưa có endpoint, nên toàn bộ là phỏng đoán. (1) `expiresIn` thật ra là `expiresInSeconds`. (2) FE đọc tên và email từ claim trong token, nhưng backend cố ý KHÔNG nhét chúng vào token — chúng nằm ở `user{}` trong thân phản hồi; đọc nhầm chỗ thì không lỗi nào báo, tên chỉ đơn giản là trống. (3) `/refresh` không trả về `user` nên phải giữ lại từ phiên cũ, còn quyền thì phải nạp LẠI (đó là chỗ việc thu hồi quyền có hiệu lực). (4) `TokenStorage` ghi cả access token vào `localStorage` — **trái ADR-0004**; nay chỉ vé gia hạn và tên người dùng chạm đĩa. |
| 2026-08-24 | **Tự gia hạn phiên khi 401**, với hai luật mà thiếu một cái là hỏng. Thứ nhất: `refreshInterceptor` phải đứng TRƯỚC `authInterceptor` — đặt sau thì lần gửi lại vẫn mang đúng cái token vừa hết hạn (đã cố ý đảo một lần, test đỏ ngay và chỉ thẳng vào token cũ). Thứ hai: mọi lời gọi gia hạn phải **gộp làm một** — vé xoay vòng chỉ dùng được một lần, nên năm request cùng dính 401 mà mỗi cái tự gia hạn thì cái thứ hai cầm vé đã tiêu, backend coi là bị trộm và **thu hồi cả chuỗi**; app tự đá người dùng ra. Đã xác nhận hành vi thu hồi cả chuỗi đó bằng curl trên server đang chạy. |
| 2026-08-24 | Ba thứ nữa phải sửa để bốn bộ màu không vỡ. `Alert` viết mã màu cứng → nay chỉ dùng token, nền pha bằng `color-mix` để khỏi phải khai 16 giá trị. Thông báo lỗi trước đây chỉ phân biệt bằng MÀU → nay mỗi tông có biểu tượng riêng (khoảng 8% nam giới mù màu đỏ-lục, và không ai trong số họ báo lỗi này). Và `errors.json` được **sinh thẳng từ file `.resx` của backend** nên không thể lệch, kèm test đối chiếu ba nguồn — bản sao chiều ngược lại của `LocalizationParityTests` bên backend. |
| 2026-08-24 | **Xong luồng đăng ký workspace, đầu tới cuối.** Một lần bấm tạo `Tenant` + 4 vai hệ thống + `User` chủ sở hữu trong MỘT transaction — chính là việc seeder vẫn làm tay, nay thành use case thật mà người lạ trên Internet gọi được, nên mọi thứ phải kiểm. Chốt hai chuyện: kiểm **mã workspace trước email** (trùng cả hai thì chỉ hiện được một lỗi, nên hiện cái dễ sửa trước), và trả **200 chứ không 201** (201 phải kèm `Location`, mà chưa có endpoint nào đọc một workspace — hứa rồi rút lại còn khó hơn). Backend **287 test xanh**, frontend **72 test xanh**. |
| 2026-08-24 | **Chốt cách chống lệch giữa bản dựng và code**, sau khi bị bắt hai lần (17 giá trị màu sai vì dựng theo file `.md` thay vì mở `.html` ra đối chiếu; rồi bản trên web khác bản trong repo vì quên đăng lại). Bốn lớp: (1) `tools/sync-shell.mjs` **sinh** `styles.scss` + `login.scss` + `register.scss` thẳng từ bản dựng — không có hai file thì không lệch được; (2) `tools/sync-error-messages.mjs` sinh `errors.json` từ `.resx`; (3) hai test đối chiếu chạy mỗi lần `npm test`; (4) **`npm run parity`** chụp cả bản dựng lẫn bản Angular ở 1440×940 rồi so từng điểm ảnh — thứ duy nhất bắt được lệch về BỐ CỤC. |
| 2026-08-24 | **Bộ so ảnh bắt lỗi đầu tiên, và là lỗi trong chính bản dựng: thiếu `<!doctype html>`.** Thiếu doctype thì trình duyệt chạy chế độ *quirks*, và cả trang lệch xuống 8 điểm ảnh so với sản phẩm (sản phẩm luôn có doctype trong `index.html`). Lệch 8px thì không ai nhìn ra bằng mắt — mà nó chứng minh đúng điều cần chứng minh: **thứ người duyệt nhìn không phải thứ sẽ chạy**. Thêm doctype → cả hai màn còn lệch 0,02%. |
| 2026-08-24 | **Test hợp đồng FE↔BE cho luật mã workspace** — đọc thẳng `TenantCode.cs` ra rồi so biểu thức chính quy và giới hạn độ dài với hằng bên Angular. Lần đầu viết, luật bên FE là `^[a-z][a-z0-9-]*[a-z0-9]$`: nhìn thì đúng nhưng **cho lọt `cong--ty`** trong khi backend cấm hai gạch nối liền nhau. Lệch kiểu này hỏng theo hai chiều đều tệ — lỏng hơn thì người dùng điền xong mới bị từ chối, chặt hơn thì có mã hợp lệ mà không ai đăng ký được và chẳng có lỗi nào ở đâu để lần ra. Đã cố ý đặt lại luật lỏng một lần để chứng minh test bắt được. |
| 2026-08-24 | Bốn thay đổi giao diện theo yêu cầu duyệt: bộ màu **chỉ hiện chấm màu, bỏ tên** · ngôn ngữ thành danh sách xổ **có cờ** và mở rộng được (ja/ko đã có chỗ, đánh dấu chưa sẵn sàng) · thông báo lỗi thành **popup nổi ở đầu màn, tự tắt** (lỗi 6 giây, tin thường 3,2 giây, dừng đồng hồ khi rê chuột, có nút đóng) thay cho khối lỗi nằm lì trong biểu mẫu · bỏ hết dòng chú thích dưới ô nhập, dồn vào dấu **`?`**. Và **không hiện mã kỹ thuật ra ngoài** nữa: mã tham chiếu chỉ còn xuất hiện khi ta thật sự không giải thích được chuyện gì đã xảy ra, cắt còn 6 ký tự để đọc qua điện thoại cho bộ phận hỗ trợ. |
| 2026-08-24 | **Khung ứng dụng đổi lần thứ ba, và lần này nó nằm trong hệ thống bản dựng.** v1 là thanh ngang + thanh dọc kiểu trang quản trị; v2 là cột biểu tượng 60px kiểu Slack; v3 là cột 212px có chữ và số đếm kiểu Lark. Lý do quay lại chữ: app nội bộ có nhiều module, và **"9 việc chờ duyệt" phải đọc được ngay khi mở** — cột chỉ có biểu tượng không có chỗ nào để hiện con số đó. Giữ nguyên nguyên tắc của v2: không có thanh ngang toàn chiều rộng, mỗi trang tự dựng tiêu đề của mình. |
| 2026-08-24 | **18 chỗ trong phần đã đăng nhập gọi biến màu KHÔNG TỒN TẠI.** Bộ token cũ (`--color-surface-strong`, `--color-text`…) bị xoá khi chốt bốn bộ màu, nhưng shell, dashboard, danh sách nhân sự và trang lỗi vẫn gọi. CSS gọi biến không tồn tại thì **lặng lẽ bỏ luôn cả dòng khai báo** — thanh trên mất nền, mục đang mở mất màu nhấn, mà build, lint và 72 test đều xanh. Nay `styles.scss` sinh từ CẢ BA file bản dựng, và có hai bộ canh mới. |
| 2026-08-24 | **Một bộ canh XANH trong khi lỗi vẫn còn — bài học đắt nhất hôm nay.** `class-usage.spec` viết ra để bắt lớp CSS bịa, và nó bỏ sót đúng lớp bịa đang có: template gắn `nav__muc--dang` qua `routerLinkActive`, trong khi test chỉ quét `class="…"`. Phải quét cả thuộc tính đó thì nó mới đỏ. **Một bộ canh không soi đúng cơ chế đã gây ra lỗi thì nó chỉ tạo cảm giác an toàn** — và cảm giác đó còn tệ hơn không có gì. |
| 2026-08-24 | **Xong backend tạo tài khoản hộ + danh sách nhân sự.** Quản trị viên tạo tài khoản, nhận **mật khẩu tạm trả về đúng một lần**, tự đưa cho đồng nghiệp; cờ `MustChangePassword` bắt đổi ở lần đăng nhập đầu. Không làm luồng "gửi lời mời qua email" vì chưa có dịch vụ gửi mail — làm một luồng nói đã gửi mà thật ra không gửi gì là kiểu nói dối tệ nhất. Backend **321 test xanh**. |
| 2026-08-24 | Mật khẩu tạm sinh theo ràng buộc **đọc được qua điện thoại**: bảng chữ bỏ `0/O` và `1/l/I`, chia cụm bằng dấu nối (`k7np-2wqx-hs4m`). Một chuỗi base64 32 ký tự đúng về mật mã và hỏng về thực tế — nó đi qua Zalo và lời nói. Đánh đổi chấp nhận được vì nó chỉ sống tới lần đăng nhập đầu tiên. Dùng `RandomNumberGenerator` chứ không `Random`: `Random` gieo từ đồng hồ, hai tài khoản tạo cùng một mili-giây có thể nhận cùng mật khẩu. |
| 2026-08-24 | **Chuỗi chống lệch chạy đúng như thiết kế, ba lần trong một buổi.** (1) Thêm mã lỗi `Role.NotFound` → `LocalizationParityTests` đỏ vì thiếu bản dịch. (2) Thêm bản dịch → test đối chiếu phía frontend đỏ vì `errors.json` chưa sinh lại. (3) Thêm trường `mustChangePassword` vào hợp đồng → TypeScript đỏ ở bốn chỗ dựng dữ liệu giả. Không chỗ nào phải nhớ bằng đầu. |
