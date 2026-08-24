# Đăng ký workspace

> Module `identity` · Trạng thái: 🟡 **Đã dựng bản mẫu, chờ duyệt** (2026-08-24)
> Bản dựng: [`dang-ky.html`](./dang-ky.html) · [xem trên web](https://claude.ai/code/artifact/a88aab53-a6ae-437d-b108-cd0854f219f3)

---

## 1. Màn này để làm gì

Cho một người **chưa có gì cả** dựng xong không gian làm việc cho công ty họ, và trở
thành chủ sở hữu của nó.

Đây là màn duy nhất **tạo ra dữ liệu ở ba bảng cùng lúc**: `tenants`, `roles` (bốn vai hệ
thống), và `users`. Cũng chính là việc mà `IdentityDataSeeder` đang làm bằng tay ở môi
trường phát triển — nay thành một use case thật.

## 2. Ai vào được

Tất cả mọi người. Không cần quyền, không cần token.

Người **đã đăng nhập** mở lại màn này thì đá về `/` — họ đã có workspace rồi. (Một người
chỉ thuộc **đúng một** workspace, xem `ADR-0002`.)

## 3. Đường dẫn

```
/dang-ky      · công khai
              · người đã đăng nhập → chuyển hướng về /
```

## 4. Bố cục

Hai cột như màn đăng nhập, dùng lại nguyên khung. Khác ở cột trái: thay câu định vị sản
phẩm bằng **ba thứ người dùng sắp nhận được**.

```
┌─────────────────────────────┬───────────────────────────┐
│  ONoOffice                  │        [●●●●] [🇻🇳 VI ▾]  │
│                             │                           │
│  Dựng không gian làm việc   │   Đăng ký workspace       │
│  cho công ty bạn.           │   Mất khoảng một phút…    │
│                             │                           │
│  ✓ Workspace riêng, dữ liệu │   ── CÔNG TY ───────────  │
│    không nằm chung          │   ┌ Tên công ty ────────┐ │
│  ✓ Bốn vai trò sẵn sàng     │   ┌ onooffice.vn/ acme ─┐ │
│  ✓ Tài khoản chủ là của bạn │                           │
│                             │   ── TÀI KHOẢN CHỦ ─────  │
│  ·─·─·  nền: sơ đồ tổ chức  │   ┌ Họ và tên ──────────┐ │
│   ·  ·  trôi chậm, mờ       │   ┌ Email công ty ──────┐ │
│                             │   ┌ Mật khẩu ───── Hiện┐ │
│                             │   ▬▬▬▬  độ mạnh          │
│  MIỄN PHÍ · KHÔNG CẦN THẺ   │   ☐ Đồng ý điều khoản    │
│                             │   ┌────────────────────┐ │
│                             │   │  Tạo workspace     │ │
└─────────────────────────────┴───────────────────────────┘
```

> **Ràng buộc số một: vừa ĐÚNG MỘT MÀN, không phải cuộn.** Người đăng ký muốn điền cho
> nhanh; bắt họ cuộn giữa chừng là chỗ dễ bỏ dở nhất của cả luồng.

**Bản đầu xếp năm ô dọc một mạch**, cộng hai tiêu đề nhóm `CÔNG TY` / `TÀI KHOẢN CHỦ SỞ
HỮU` — cao hơn một màn laptop. Bốn chỗ đã cắt:

| Bỏ / đổi | Tiết kiệm |
|---|---|
| Hai tiêu đề nhóm | ~60px — với ba hàng thì không còn là "bức tường" để cần chia nhóm |
| Năm ô dọc → **lưới tự co hai cột** | ~150px |
| Siết nhịp dọc, ô nhập thấp hơn 2px | ~40px |
| Màn hẹp: cột trái thu thành dải mỏng chỉ còn tên sản phẩm | ~130px |

Lưới dùng `repeat(auto-fit, minmax(210px, 1fr))` nên **không cần một điểm ngắt nào**: đủ
rộng thì hai cột, hẹp thì tự xuống một cột. Vừa cả khung xem hẹp của artifact lẫn màn
hình đầy đủ.

**Vì sao nói trước ba thứ sắp nhận được:** màn đăng ký nào cũng phải trả lời câu *"bấm
xong thì có gì?"* **trước** khi bấm, chứ không phải sau.

## 5. Các trạng thái

| Trạng thái | Trông thế nào |
|---|---|
| **Mặc định** | Form sạch, con trỏ ở ô tên công ty |
| **Đang gửi** | Nút khoá, con quay, chữ đổi thành *"Đang tạo workspace…"*. Các ô **không** bị khoá |
| **Lỗi kiểm dữ liệu** | Viền đỏ ở đúng ô sai + dòng nhắc. Kiểm **khi rời ô** |
| **Bị từ chối** | **Popup nổi ở đầu màn hình**, tự biến mất — xem mục 7 |
| **Xong** | Thay cả biểu mẫu bằng thẻ xác nhận: mã workspace, email đăng nhập, vai trò |

Không có trạng thái "rỗng" — màn này không tải dữ liệu gì.

## 6. Dữ liệu cần

```
POST /api/auth/register-workspace          ← ĐÃ CÓ, chạy thật từ 2026-08-24
     { companyName, workspaceCode, fullName, email, password }
  → 200 { accessToken, refreshToken, expiresInSeconds, user{…}, workspace{ id, code, name } }
```

Backend làm đúng bốn việc, **trong một transaction**:

```
① Tenant.Create(code, name)
② SystemRoles.All → bốn Role hệ thống của tenant đó
③ User.Create(...) + AssignRole(Owner)
④ Tenant.AssignOwner(user.Id)
```

Thứ tự không đảo được — xem `IdentityDataSeeder`, nó đã làm đúng trình tự này rồi.

**Đăng ký xong thì đăng nhập luôn**, không bắt gõ lại mật khẩu vừa đặt: phản hồi trả về
cặp token y như `/login`.

> **200 chứ không phải 201.** 201 buộc phải kèm header `Location` trỏ tới tài nguyên vừa
> tạo, mà chưa có endpoint nào đọc một workspace. Trả 201 với `Location` rỗng là nói dối về
> hợp đồng — sửa lại về sau còn dễ hơn gỡ một lời hứa đã ship. Ghi ở
> [`05-api/README.md`](../../05-api/README.md).

## 7. Lỗi hiện thế nào

| Mã lỗi | HTTP | Hiện cho người dùng |
|---|---|---|
| `TenantCode.Invalid` | 400 | Mã workspace chỉ gồm chữ thường, số và gạch nối. |
| `TenantCode.Taken` | 409 | Mã này đã có công ty khác dùng. Hãy chọn mã khác. |
| `Email.Taken` | 409 | Email này đã có tài khoản. Bạn muốn [đăng nhập](./dang-nhap.md)? |
| `Validation.Multiple` | 400 | Mật khẩu phải có ít nhất 10 ký tự. |
| *(mất mạng)* | — | Không kết nối được máy chủ. |

Cả hai mã `TenantCode.Taken` và `Email.Taken` **đã có ở backend**, kèm bản dịch vi/en trong
`Messages.*.resx`.

Hai lỗi 409 hiện **theo hai cách cùng lúc**: một popup nổi ở đầu màn, và ô gây ra nó
được tô đỏ. Chỉ có popup thì người dùng biết *có gì đó sai* mà không biết sửa **ô nào** —
biểu mẫu này có năm ô.

Mã workspace được kiểm **trước** email. Trùng cả hai thì chỉ hiện được một lỗi, nên hiện
cái dễ sửa trước: đổi mã workspace là gõ lại một từ, đổi email là chuyện khác hẳn.

## 8. Trên điện thoại

```
Cột trái thu thành dải ~200px, chỉ giữ tên sản phẩm + tiêu đề (ba gạch đầu dòng ẩn đi)
Form chiếm hết bề ngang, lề 20px
Thước độ mạnh mật khẩu giữ nguyên — nó chỉ cao 3px
```

---

## Ba quyết định trong màn này

### Mã workspace tự gợi ý từ tên công ty, nhưng vẫn sửa được

Sinh xong khoá cứng thì công ty tên dài nhận một mã xấu mà không sửa được. Bắt gõ tay từ
đầu thì phần lớn người dùng gõ luôn tên công ty **có dấu**, rồi nhận lỗi.

Gợi ý rồi cho sửa là đường giữa — và **ngừng gợi ý ngay khi người dùng tự gõ vào ô đó**.
Ghi đè lên thứ họ vừa gõ là kiểu khó chịu mà ai cũng từng gặp ở form đăng ký.

Bỏ dấu bằng `String.normalize('NFD')` rồi cắt dấu, không cần bảng tra tay.

### Thước độ mạnh mật khẩu đếm ĐỘ DÀI, không bắt ký tự đặc biệt

Luật "phải có chữ hoa, số và ký tự đặc biệt" đẻ ra toàn `Matkhau@123` — dài mà đoán được,
và người dùng phải dán nó vào một file ghi chú vì không nhớ nổi.

Một câu dài dễ nhớ an toàn hơn nhiều. Thước ở đây thưởng cho độ dài trước, đa dạng ký tự
sau, và **không chặn** — nó chỉ khuyên. Ràng buộc cứng duy nhất là 10 ký tự.

Bốn vạch rời chứ không phải một thanh liền: thanh liền gợi ý "càng đầy càng tốt" và người
dùng cố nhồi cho đầy.

### Mã workspace KHÔNG đổi được sau này — và phải nói trước

Nó nằm trong URL, trong link người ta gửi cho nhau, trong bookmark. Cho đổi thì mọi link
cũ chết. Nói thẳng ngay dưới ô nhập, lúc họ còn đang gõ — chứ không phải trong điều khoản.

---

## Chưa làm — ghi rõ để không ai tưởng đã có

| Việc | Vì sao chưa |
|---|---|
| **Xác minh email** | Cần dịch vụ gửi mail — hoãn sang lát 2. Hiện đăng ký xong là vào được luôn |
| **Chặn đăng ký hàng loạt** | Cần Redis để đếm theo IP. Đếm trong bộ nhớ thì chạy nhiều pod là vô dụng. ⚠️ Endpoint đang **mở cho Internet**, mỗi lần gọi thành công là một công ty mới trong database |
| **Điều khoản / Chính sách riêng tư** | Hai link đã có, bấm hiện *"đang phát triển"*. Cần người viết nội dung pháp lý |
| **Mời đồng nghiệp ngay sau khi tạo** | Màn "Xong" mới chỉ có nút vào workspace |
