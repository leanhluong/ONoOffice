# 02 — Kiến trúc

> Cập nhật: 2026-08-23 · Quyết định nền: [ADR-0001](./adr/ADR-0001-chien-luoc-multi-tenant.md) · [ADR-0002](./adr/ADR-0002-xac-thuc-va-phan-quyen.md)

---

## Bức tranh tổng thể

```
   Trình duyệt
        │  HTTPS
        ▼
┌─────────────────────────────────────────────────────────┐
│  Angular  (SPA)                                         │
│    giữ access token trong bộ nhớ, refresh token ở cookie│
└───────────────────────┬─────────────────────────────────┘
                        │  REST + JWT
                        ▼
┌─────────────────────────────────────────────────────────┐
│  ONoOffice.Api        MỘT tiến trình .NET 10            │
│  ┌───────────────────┐   ┌───────────────────┐          │
│  │  Module Identity  │   │   Module Org      │          │
│  │  tài khoản        │   │   hồ sơ nhân viên │          │
│  │  vai trò · quyền  │   │   phòng ban       │          │
│  └───────────────────┘   └───────────────────┘          │
│         nói chuyện qua Contracts, KHÔNG qua DbContext    │
└───────────────────────┬─────────────────────────────────┘
                        ▼
┌─────────────────────────────────────────────────────────┐
│  PostgreSQL 16 — MỘT database                           │
│    schema identity.*      schema org.*                  │
│    mọi bảng nghiệp vụ có cột tenant_id                  │
└─────────────────────────────────────────────────────────┘
```

Redis, RabbitMQ, Hangfire **chưa có ở lát 1** — thư viện đã sẵn sàng nhưng chưa module nào cần tới. Thêm khi có nhu cầu thật.

---

## Vì sao Modular Monolith

Một tiến trình, bên trong chia theo *bounded context*. Không phải microservices.

| | Lý do |
|---|---|
| Thứ đang cần học là **Clean Architecture / DDD**, không phải điều phối nhiều dịch vụ | Microservices đã làm ở NextX rồi; chỗ còn thiếu là tầng thiết kế bên trong một dịch vụ |
| Deploy rẻ | Một VPS 5$/tháng chạy được |
| Vẫn cắt ra microservice sau được | Nếu ranh giới module đúng — xem bốn luật bên dưới |

---

## Hai module ở lát 1, và vì sao tách đôi

> **`Identity`** sở hữu **tài khoản đăng nhập** — user, mật khẩu, refresh token, vai trò, quyền.
> **`Org`** sở hữu **hồ sơ nhân viên** — họ tên, mã NV, phòng ban, chức danh, ngày vào làm.

Cùng một con người, **hai khái niệm khác nhau**:

- Một người nghỉ việc → `Org` đóng hồ sơ, nhưng tài khoản vẫn tồn tại để tra lại lịch sử thao tác.
- Một tài khoản hệ thống (bot chạy backup) tồn tại mà **không phải nhân viên nào cả**.
- Đổi mật khẩu là chuyện của `Identity`; điều chuyển phòng ban là chuyện của `Org`. Chúng đổi vì những lý do khác nhau, vào những lúc khác nhau.

Gộp chúng vào một bảng `users` 30 cột chính là cái bẫy *"gộp vì ngại đồng bộ"*.

> Ranh giới đúng: **gộp vì cohesion là ĐÚNG, gộp vì ngại đồng bộ là SAI.**

---

## Cấu trúc thư mục

```
src/
├── ONoOffice.Api/                       ← host duy nhất: DI, middleware, map endpoint
│
├── Modules/
│   ├── Identity/
│   │   ├── Identity.Contracts/          ← ⭐ MẶT TIỀN: interface + DTO. Module khác chỉ được thấy cái này
│   │   ├── Identity.Domain/             ← entity, value object, luật nghiệp vụ. KHÔNG tham chiếu ai
│   │   ├── Identity.Application/         ← use case: command/query + handler
│   │   └── Identity.Infrastructure/      ← EF Core, repository, băm mật khẩu, phát JWT
│   │
│   └── Org/                              ← 4 project y hệt
│
└── tests/
    ├── Identity.UnitTests/
    ├── Org.UnitTests/
    └── ArchitectureTests/                ← test canh bốn luật bên dưới
```

### Bốn tầng bên trong một module

```
        Domain              ← không tham chiếu gì cả. Luật nghiệp vụ thuần
           ▲
       Application          ← điều phối use case. Biết Domain, KHÔNG biết EF/HTTP
           ▲
     Infrastructure         ← EF Core, băm mật khẩu, gửi mail. Cài các cổng Application khai
           ▲
          Api               ← chỉ nhận request, gọi use case, trả kết quả
```

Mũi tên **chỉ đi vào trong**. `Domain` không bao giờ biết ngoài kia có database hay có HTTP.

---

## Bốn luật ranh giới — được test canh, không phải lời hứa

**Luật 1 — Module chỉ thấy `Contracts` của module khác.**
`Org.Application` được tham chiếu `Identity.Contracts`. Tham chiếu `Identity.Domain` hay `Identity.Infrastructure` là **sai** và test kiến trúc sẽ đỏ.

**Luật 2 — Mỗi module một `DbContext`, một schema Postgres riêng.**
`identity.users`, `org.employees`. Một database, hai schema.

**Luật 3 — Cấm `JOIN` xuyên schema.**
`Org` cần biết email của user thì gọi qua `IIdentityApi` trong `Contracts` — **không** viết SQL nối bảng.

**Luật 4 — `Domain` không tham chiếu bất kỳ package hạ tầng nào.**
Chỉ được tham chiếu `Luong.Kernel` (gói này cố ý không phụ thuộc gì ngoài .NET gốc).

> Luật 2 và 3 là thứ khiến sau này muốn cắt `Org` ra thành dịch vụ riêng chỉ cần đổi chuỗi kết nối và đổi lời gọi in-process thành HTTP. **Lọt một câu `JOIN` xuyên schema thì ngày cắt là ngày viết lại.**

---

## Một request đi qua đâu

Ví dụ `POST /api/employees`:

```
① Api            nhận request, ràng buộc thành CreateEmployeeCommand
                 ↓  sender.Send(command)
② LoggingBehavior       ghi "Bắt đầu CreateEmployee"
③ ValidationBehavior    kiểm dữ liệu — hỏng thì DỪNG, không xuống tới handler
④ TransactionBehavior   mở phạm vi lưu
⑤ Handler        nạp aggregate, gọi luật nghiệp vụ, Raise(EmployeeHired)
⑥ Interceptor    SaveChanges: ghi hàng employees + hàng outbox, CÙNG transaction
                 ↑  Result<Guid>
⑦ Api            Result → IResult  →  201 hoặc Problem Details
```

② → ⑥ **đều đến từ `Luong.Kernel`**, không phải viết lại. Việc còn lại của ONoOffice chỉ là bước ⑤ — nghiệp vụ thật.

---

## Cô lập tenant — bốn lớp

Xem [ADR-0001](./adr/ADR-0001-chien-luoc-multi-tenant.md). Tóm tắt:

```
1. ITenantScoped        · đánh dấu thực thể có tenant_id
2. Global query filter  · MỌI truy vấn tự thêm điều kiện tenant
3. TenantInterceptor    · INSERT tự điền tenant_id, không nhận gán tay
4. Test cô lập          · tenant B đọc phải KHÔNG thấy dữ liệu tenant A
```

Và luật tuyệt đối: **`tenant_id` chỉ đến từ token đã ký, không bao giờ nhận từ client.**

---

## Vai trò của `Luong.Kernel`

Thư viện dùng chung, repo riêng, đã phát hành lên GitHub Packages `0.1.0`.

| ONoOffice dùng gì | Từ gói nào |
|---|---|
| `Result`/`Error`, `Entity`/`AggregateRoot`, domain event, `ICurrentUser`, `IUnitOfWork` | `Luong.Kernel` |
| CQRS + 3 pipeline behavior | `Luong.Kernel.Application` |
| `Error` → Problem Details, correlation-id, bắt exception, `Result` → `IResult` | `Luong.Kernel.AspNetCore` |
| snake_case, interceptor audit, xoá mềm, outbox | `Luong.Kernel.EntityFrameworkCore` |

**Lúc đang phát triển dùng `ProjectReference`**, khi ổn định mới chuyển sang `PackageReference` ghim phiên bản. Nếu cài qua NuGet ngay từ đầu thì mỗi lần sửa một dòng trong thư viện phải nâng version → pack → push → chờ feed → restore; sửa 20 lần một buổi là hết ngày.

---

## Chưa quyết

| Việc | Sẽ chốt khi |
|---|---|
| Bảng `employees` có cần bản sao `email` từ `Identity` không | Khi làm màn danh bạ — đo xem gọi qua `Contracts` có đủ nhanh |
| Cây phòng ban lưu kiểu nào (adjacency list / materialized path / ltree) | Khi thiết kế `04-database` |
| Đăng nhập ngoài (Google/Microsoft) | Sau lát 1. Thiết kế đã chừa chỗ: `password_hash` cho phép rỗng |
