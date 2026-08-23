# ADR-0002 — Xác thực và phân quyền

> Ngày: 2026-08-23 · Trạng thái: **Đã chốt** · Nối tiếp [ADR-0001](./ADR-0001-chien-luoc-multi-tenant.md)

## Bối cảnh

Cần cho người dùng đăng nhập, và cần biết ai được làm gì — trong một hệ nhiều công ty, nơi mỗi công ty tự đặt vai trò của mình.

## Quyết định 1 — Một người thuộc đúng MỘT workspace

| | Mô hình A (chọn) | Mô hình B (Lark/Slack) |
|---|---|---|
| Cấu trúc | `users` có cột `tenant_id` | `users` toàn cục + bảng `tenant_members` |
| Đăng nhập | Một bước | Đăng nhập → chọn workspace |
| Một email ở nhiều công ty | Không được | Được |

**Chốt A.** Đơn giản hơn hẳn, và đúng nhu cầu: đây là app nội bộ của một công ty, không phải nơi tư vấn viên nhảy qua lại giữa nhiều khách hàng.

**Hệ quả bắt buộc: email phải unique TOÀN HỆ THỐNG.** Nếu chỉ unique trong một công ty thì đăng nhập bằng email + mật khẩu là mơ hồ — hai công ty cùng có `an@gmail.com` thì hệ thống không biết đang nói tới ai. Hai đường ra là (a) email unique toàn cục, (b) bắt nhập thêm mã công ty. Chọn (a) vì form đăng nhập giữ được hai ô.

## Quyết định 2 — JWT ngắn hạn + refresh token xoay vòng

```
POST /auth/login  { email, password }
   ├─ tìm user theo email
   ├─ kiểm mật khẩu bằng Argon2id
   ├─ kiểm user còn hoạt động · workspace còn hoạt động
   │
   ├─▶ access token   15 phút · claims: sub · tenant_id · permission[]
   └─▶ refresh token  30 ngày · lưu dạng BĂM trong DB · xoay vòng mỗi lần dùng
```

**Vì sao access token chỉ 15 phút:** token đã phát ra thì không thu hồi được — server không giữ danh sách token hợp lệ, đó chính là điểm mạnh (không phải tra DB mỗi request) và cũng là điểm yếu. Khoá tài khoản lúc 10h00 mà token sống 24 giờ thì người đó vẫn dùng được tới 10h00 hôm sau. 15 phút thu hẹp cửa sổ đó xuống mức chấp nhận được.

**Vì sao refresh token lưu dạng băm:** nó sống 30 ngày. Lộ bảng DB mà token nằm dạng thô thì kẻ tấn công đăng nhập được vào mọi tài khoản. Băm thì bảng lộ cũng vô dụng — cùng lý do không bao giờ lưu mật khẩu thô.

**Vì sao xoay vòng (rotation):** mỗi lần đổi refresh token lấy access token mới thì refresh token cũ bị huỷ, cấp cái mới. Nhờ vậy phát hiện được trộm: nếu một refresh token **đã dùng rồi** lại được dùng lần nữa, nghĩa là có hai bên đang giữ cùng một token → huỷ toàn bộ phiên của người đó.

**Argon2id chứ không phải SHA-256:** hàm băm thường được thiết kế để chạy **nhanh** — đó chính xác là điều không muốn cho mật khẩu, vì nhanh nghĩa là dò được hàng tỉ tổ hợp mỗi giây. Argon2id cố tình chậm và tốn RAM.

## Quyết định 3 — Kiểm `permission`, KHÔNG kiểm `role`

```csharp
// ❌ SAI
if (user.IsInRole("HR") || user.IsInRole("Admin")) { ... }

// ✅ ĐÚNG
if (user.HasPermission("employee.write")) { ... }
```

**Vì sao:** hôm nào công ty muốn thêm vai trò *"Trợ lý nhân sự"* — sửa được hồ sơ nhưng không xoá được — mà code kiểm role thì phải đi sửa **mọi chỗ** có `IsInRole("HR")`. Kiểm permission thì chỉ việc tạo vai trò mới rồi tick vào các quyền, **không đụng một dòng code nào**.

```
Role là cái TÚI đựng permission:

  Owner    → tất cả
  Admin    → tất cả trừ chuyển nhượng quyền sở hữu
  Manager  → employee.read · leave.approve  (trong phạm vi phòng mình)
  Member   → employee.read

Permission là HẰNG SỐ trong code:  "employee.read" · "employee.write" · "department.manage"
Role thuộc về TENANT:              mỗi công ty tự đặt tên vai trò của mình
```

Bốn vai trên được **gieo sẵn khi tạo workspace**, và `Owner` có luật riêng: đúng một người, không xoá được, không tự bỏ vai được — muốn đổi thì phải chuyển nhượng.

## Đánh đổi

- **Quyền nằm trong token** nên kiểm quyền không phải tra DB — nhanh, nhưng **đổi quyền không có hiệu lực ngay**, phải chờ tối đa 15 phút hoặc bắt đăng nhập lại. Chấp nhận được với app nội bộ; nếu sau này cần tức thì thì thêm một danh sách đen trong Redis.
- **Token phình ra** nếu một người có hàng trăm quyền. Ngưỡng cần xem lại: token vượt ~4KB (giới hạn header của nhiều proxy). Lúc đó chuyển sang mang `role_id` rồi tra quyền từ cache.
- **Chưa làm đăng nhập ngoài** (Google/Microsoft). Thiết kế chừa chỗ: bảng `users` cho phép `password_hash` rỗng, để sau này thêm được tài khoản chỉ đăng nhập qua nhà cung cấp ngoài.

## Học được gì

- Ba nấc **hết hạn ngắn → băm khi lưu → xoay vòng** đều trả lời cùng một câu hỏi: *"nếu thứ này lọt ra ngoài thì thiệt hại tới đâu, và trong bao lâu?"*
- Phân biệt **authentication** (bạn là ai) với **authorization** (bạn được làm gì) — hai việc khác nhau, hai tầng khác nhau.
- Nối về [`Q&A/Ontap/Chang-8-security-phan-quyen.md`](../../../../Q&A/Ontap/Chang-8-security-phan-quyen.md) — JWT/JWKS/refresh, permission-based vs RBAC vs ABAC, Argon2id.
