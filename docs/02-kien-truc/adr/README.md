# ADR — Nhật ký quyết định kiến trúc

Mỗi quyết định lớn = 1 file, ngắn, theo đúng 5 mục:

```
## Bối cảnh      · đang vướng chuyện gì
## Các lựa chọn  · đã cân nhắc những cách nào
## Chốt          · chọn cách nào
## Đánh đổi      · MẤT gì khi chọn cách đó
## Học được gì   · nối về Q&A/Ontap nếu có
```

Mục **Đánh đổi** là bắt buộc. Một quyết định không mất gì cả thì đó không phải quyết định, chỉ là chuyện hiển nhiên.

## Danh sách

| # | Quyết định | Ngày | Trạng thái |
|---|---|---|---|
| [0001](./ADR-0001-chien-luoc-multi-tenant.md) | Chiến lược multi-tenant — chung DB + cột `tenant_id` + 4 lớp cô lập | 2026-08-23 | Đã chốt |
| [0002](./ADR-0002-xac-thuc-va-phan-quyen.md) | Xác thực & phân quyền — JWT ngắn hạn + refresh xoay vòng, kiểm `permission` không kiểm `role` | 2026-08-23 | Đã chốt |
| [0003](./ADR-0003-controller-thay-vi-minimal-api.md) | Dùng Controller thay vì Minimal API — nhiều endpoint, cần filter + versioning | 2026-08-23 | Đã chốt |
