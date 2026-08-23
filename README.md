# ONoOffice

App văn phòng nội bộ cho doanh nghiệp Việt Nam — danh bạ, phòng ban, đơn từ, trao đổi nội
bộ. Multi-tenant: nhiều công ty dùng chung một hệ, mỗi bên chỉ thấy dữ liệu của mình.

**.NET 10 · Angular 22 · PostgreSQL 16 · Clean Architecture · Modular Monolith**

---

## Vào việc ngay

```bash
# Backend — 161 test, 0 warning
cd backend && dotnet build && dotnet test

# Frontend — 8 test
cd frontend && npm install && npm run build
```

## Đọc gì trước

| Bạn là | Đọc |
|---|---|
| **Người/agent tiếp quản công việc** | 👉 [`HANDOFF.md`](./HANDOFF.md) — đang ở đâu, làm gì tiếp |
| Muốn hiểu sản phẩm | [`docs/01-tong-quan/`](./docs/01-tong-quan/) |
| Sắp viết code | [`docs/02-kien-truc/`](./docs/02-kien-truc/) + [`adr/`](./docs/02-kien-truc/adr/) |
| Làm giao diện | [`docs/07-giao-dien/`](./docs/07-giao-dien/) |

## Cấu trúc

```
docs/        7 thư mục, mỗi thư mục trả lời đúng một câu hỏi
backend/     src/ (9 project: 2 module × 4 tầng + Api) · tests/
frontend/    Angular 22 — standalone, signal, không NgModule
```

Thư viện dùng chung nằm ở repo riêng: [`leanhluong/libNetCore`](https://github.com/leanhluong/libNetCore)
— phát hành thành các gói `Luong.Kernel.*`.

## Xem thiết kế mà không cần chạy gì

```
docs/07-giao-dien/wireframes.html            · bố cục 6 màn, đơn sắc, có chú thích
docs/07-giao-dien/identity/dang-nhap.html    · bản dựng màu — 4 bộ, 4 trạng thái
```

Mở thẳng bằng trình duyệt là chạy.
