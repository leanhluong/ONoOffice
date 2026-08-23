# ADR-0001 — Chiến lược multi-tenant

> Ngày: 2026-08-23 · Trạng thái: **Đã chốt**

## Bối cảnh

ONoOffice phục vụ **nhiều công ty**, mỗi công ty chỉ được thấy dữ liệu của chính mình. Phải chọn cách cô lập dữ liệu, và chọn sai thì sửa lại là viết lại từ đầu.

Ràng buộc: một người làm, ngân sách hạ tầng gần bằng không, dự kiến vài trăm công ty × ~200 nhân viên.

## Các lựa chọn

| Cách | Cô lập | Chi phí vận hành | Hợp với |
|---|---|---|---|
| **A. Chung DB, cột `tenant_id`** | Bằng phần mềm | Rẻ nhất — một DB, một lần migration | Vài trăm tenant, dữ liệu không bị luật ép tách vật lý |
| B. Chung DB, mỗi tenant một schema | Khá | Migration nhân lên theo số tenant | Vài chục tenant lớn |
| C. Mỗi tenant một DB | Tuyệt đối | Rất đắt — backup, migration, giám sát nhân lên | Ngân hàng, y tế, hoặc khách hàng đòi trong hợp đồng |

## Chốt

**Cách A** — chung database, mọi bảng nghiệp vụ có cột `tenant_id`.

Nhưng cách A có một điểm yếu chết người: **cô lập nằm ở chỗ lập trình viên nhớ viết `WHERE tenant_id = ...`**. Quên một lần là lộ dữ liệu công ty này sang công ty khác. Nên phải thiết kế để **không thể quên**, gồm bốn lớp:

```
1. ITenantScoped          · thực thể nào có tenant_id thì đánh dấu bằng interface này
2. Global query filter    · MỌI truy vấn tự động thêm điều kiện tenant, không ai phải nhớ
3. TenantInterceptor      · INSERT tự điền tenant_id — không nhận giá trị gán tay
4. Test cô lập            · đọc bằng tenant B phải KHÔNG thấy dữ liệu tenant A
```

Và một luật tuyệt đối:

> **`tenant_id` chỉ đến từ token đã ký, KHÔNG BAO GIỜ nhận từ client** — không qua header, không qua query string, không qua body.

Để client gửi `tenantId` nghĩa là ai cũng đổi được một con số rồi đọc dữ liệu công ty khác. Đây là lỗ hổng IDOR ở mức nghiêm trọng nhất.

## Đánh đổi

**Mất gì:**
- Cô lập chỉ ở tầng phần mềm. Một câu SQL viết tay bỏ qua bộ lọc là lộ dữ liệu — trong khi cách C thì kể cả sai code cũng không với sang DB khác được.
- Một tenant khổng lồ làm chậm truy vấn của mọi tenant khác (chung bảng, chung chỉ mục).
- Không tách được backup theo từng công ty. Khách hàng đòi "trả tôi toàn bộ dữ liệu của riêng tôi" thì phải viết công cụ xuất riêng.

**Được gì:**
- Một database, một lần migration, một bản backup.
- Thêm một công ty mới = thêm **một dòng** vào bảng `tenants`, không phải dựng hạ tầng.
- Chuyển sang cách B hoặc C sau này vẫn được, vì `tenant_id` đã có sẵn ở mọi bảng — chỉ là chuyện tách dữ liệu ra.

**Ngưỡng phải xem lại quyết định này:** một tenant chiếm >30% dung lượng, hoặc có khách hàng đưa yêu cầu tách vật lý vào hợp đồng.

## Học được gì

- **"Có quy tắc" khác "quy tắc được thi hành".** Quy tắc phải nhớ mới đúng thì sớm muộn cũng có người quên — và ở đây lần quên đó là rò rỉ dữ liệu. Bốn lớp ở trên biến quy tắc thành thứ compiler và hạ tầng tự lo.
- Cùng nguyên tắc với ca thật đã gặp ở NextX: *cô lập tenant ở tầng client của Search bằng cách bắt `workspaceId` là tham số **bắt buộc**, để người gọi **không thể quên***.
- Nối về [`Q&A/Ontap/Chang-8-security-phan-quyen.md`](../../../../Q&A/Ontap/Chang-8-security-phan-quyen.md) — mục multi-tenant RLS + query filter, và mục IDOR.
