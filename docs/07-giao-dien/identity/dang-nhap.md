# Đăng nhập

> Module `identity` · Trạng thái: 🟢 **Đã thiết kế, chờ chốt bộ màu**
> Bản dựng để duyệt: https://claude.ai/code/artifact/0aaa520d-04b3-46a4-b297-c04a88482ded

---

## 1. Màn này để làm gì

Cho một người đã có tài khoản vào được workspace của họ — bằng email công ty và mật khẩu.

Đây là **màn duy nhất ai cũng vào được mà chưa cần token**, nên nó cũng là màn bị dò nhiều
nhất. Mọi lựa chọn ở dưới đều xoay quanh chuyện đó.

## 2. Ai vào được

Tất cả mọi người. Không cần quyền, không cần token.

Ngược lại: **người đã đăng nhập rồi mà mở lại màn này thì đá thẳng về `/`** — để tránh
cảnh gõ lại mật khẩu một cách vô nghĩa.

## 3. Đường dẫn

```
/login        · công khai
              · người đã đăng nhập → chuyển hướng về /
              · vào từ link cần quyền → nhớ đường cũ, đăng nhập xong quay lại đúng chỗ
```

## 4. Bố cục

Hai cột trên màn rộng, một cột trên điện thoại.

```
┌─────────────────────────────┬───────────────────────────┐
│  ONoOffice                  │                           │
│                             │   Đăng nhập               │
│                             │   Dùng email công ty…     │
│  Cả công ty,                │                           │
│  gọn trong một chỗ.         │   ┌─ Email công ty ────┐  │
│                             │   └────────────────────┘  │
│  Danh bạ, phòng ban, đơn từ │   ┌─ Mật khẩu ──── Hiện┐  │
│  và trao đổi nội bộ…        │   └────────────────────┘  │
│                             │   ☑ Ghi nhớ   Quên mật khẩu│
│  ·─·─·  nền: sơ đồ tổ chức  │   ┌────────────────────┐  │
│   ·  ·  trôi chậm, mờ       │   │     Đăng nhập      │  │
│                             │   └────────────────────┘  │
│                             │   ─── hoặc tiếp tục với ──│
│  NỘI BỘ · RIÊNG TỪNG CÔNG TY│   [ Google ] [ Facebook ] │
└─────────────────────────────┴───────────────────────────┘
```

**Nền động là sơ đồ tổ chức, không phải hoa văn.** Các chấm nối nhau chính là thứ sản phẩm
nói về — cây phòng ban và người trong công ty. Đứng yên hoàn toàn khi máy bật chế độ giảm
chuyển động.

## 5. Các trạng thái

| Trạng thái | Trông thế nào |
|---|---|
| **Mặc định** | Form sạch, con trỏ ở ô email |
| **Đang gửi** | Nút khoá, hiện con quay, chữ đổi thành *"Đang kiểm tra…"*. Hai ô nhập **không** bị khoá — để người dùng sửa được nếu đổi ý |
| **Lỗi kiểm dữ liệu** | Viền đỏ ở đúng ô sai + một dòng nhắc bên dưới. Kiểm **khi rời ô**, không kiểm khi đang gõ |
| **Sai email / mật khẩu** | Khung cảnh báo phía trên form. Hai ô **không** bị bôi đỏ — vì ta không biết ô nào sai, và cũng không được để lộ |
| **Tài khoản bị khoá** | Cùng khung cảnh báo, câu chữ khác |

**Không có trạng thái "rỗng"** — màn này không tải dữ liệu gì.

## 6. Dữ liệu cần

```
POST /api/auth/login
     { email, password }
  → 200 { accessToken, refreshToken, expiresIn, user{ id, tenantId, email, fullName } }
```

Sau khi nhận:

```
accessToken   → giữ trong BIẾN bộ nhớ, không ghi vào localStorage
refreshToken  → localStorage
                (lát 1 chọn cách này — xem ADR-0004; chuyển sang cookie HttpOnly
                 khi FE và API về chung một tên miền)
user          → nạp vào auth.store
permission[]  → giải mã từ accessToken, dùng cho guard và cho việc ẩn/hiện nút
```

## 7. Lỗi hiện thế nào

Backend trả Problem Details (RFC 7807). FE đọc `errors[0].code`, **không đọc câu chữ** —
mã ổn định, câu chữ thì đổi.

| Mã lỗi | HTTP | Hiện cho người dùng |
|---|---|---|
| `Auth.InvalidCredentials` | 401 | Email hoặc mật khẩu không đúng. |
| `Auth.AccountDisabled` | 403 | Tài khoản đã bị vô hiệu hoá. Vui lòng liên hệ quản trị viên. |
| `Auth.WorkspaceDisabled` | 403 | Workspace đã ngừng hoạt động. Vui lòng liên hệ quản trị viên. |
| *(không nối được mạng)* | — | Không kết nối được máy chủ. Kiểm tra mạng rồi thử lại. |
| *(mã lạ)* | bất kỳ | Có lỗi xảy ra. Mã tham chiếu: `{correlationId}` |

Dòng cuối quan trọng: **luôn có đường thoát cho lỗi chưa lường trước**, và luôn kèm
`correlationId` lấy từ header `X-Correlation-Id` — để người dùng đọc cho bộ phận hỗ trợ,
và từ mã đó lần ra đúng dòng log.

> **Không bao giờ hiện thông báo lỗi thô của backend.** Nó có thể chứa tên bảng, tên cột,
> đường dẫn file.

## 8. Trên điện thoại

```
Cột trái thu thành một dải cao ~240px ở trên, chỉ giữ tên sản phẩm + câu định vị
Form chiếm hết bề ngang, lề 20px
Hai nút Google/Facebook vẫn nằm cạnh nhau — chúng đủ ngắn
Bàn phím bật lên không che nút Đăng nhập (form cuộn được)
```

---

## Chưa làm — ghi rõ để không ai tưởng đã có

| Việc | Vì sao chưa |
|---|---|
| **Đăng nhập Google / Facebook** | Nút đã có, bấm vào hiện *"tính năng đang phát triển"*. Cần đăng ký ứng dụng ở Google/Meta trước |
| **Quên mật khẩu** | Cần dịch vụ gửi mail — hoãn sang lát 2 |
| **Chặn thử sai nhiều lần** | Cần Redis. Đếm trong bộ nhớ thì chạy nhiều pod là vô dụng — thà chưa làm còn hơn làm giả |
| **Ghi nhớ tôi** | Ô tick đã có nhưng **chưa nối gì**. Sẽ quyết định hạn refresh token dài/ngắn theo ô này |

## Chờ chốt

**Bộ màu** — bốn phương án trong bản dựng ở đầu file:

| | Nền | Điểm nhấn |
|---|---|---|
| Mực | `#0B0C0E` | Hổ phách `#D9A441` |
| Hải đăng | `#0A1220` | San hô `#FF7A5C` |
| Giấy | `#FAFAF8` | Đỏ rượu `#8C2F39` |
| Rêu | `#12160F` | Xanh xô thơm `#A3B18A` |

Chốt xong thì bộ được chọn mới làm **cả chế độ sáng lẫn tối**, và các token màu ghi vào
[`he-thong-thiet-ke.md`](../he-thong-thiet-ke.md).
