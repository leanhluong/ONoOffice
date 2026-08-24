# Đăng nhập

> Module `identity` · Trạng thái: 🟢 **Đã dựng bằng Angular** (2026-08-24) — nối API thật, bốn bộ màu
> Bản dựng màu: [`dang-nhap.html`](./dang-nhap.html) · [xem trên web](https://claude.ai/code/artifact/0aaa520d-04b3-46a4-b297-c04a88482ded)
> Wireframe: [`../wireframes.html`](../wireframes.html)

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

## Bộ màu — giữ cả bốn

Cả bốn bộ đều ship, người dùng đổi trong menu avatar:

| | Nền | Điểm nhấn |
|---|---|---|
| Mực | `#0B0C0E` | Hổ phách `#D9A441` |
| Hải đăng | `#0A1220` | San hô `#FF7A5C` |
| Giấy | `#FAFAF8` | Đỏ rượu `#8C2F39` |
| Rêu | `#12160F` | Xanh xô thơm `#A3B18A` |

Token của từng bộ ghi ở
[`he-thong-thiet-ke.md`](../he-thong-thiet-ke.md).

---

## Bản dựng Angular — khớp tới đâu (2026-08-24)

`dang-nhap.html` là **nguồn duy nhất** cho mọi con số về giao diện. Bản Angular chép từ
nó, kể cả những giá trị trông lẻ (`1.15fr`, `384px`, `12.5px`, `clamp(30px, 4.4vw, 46px)`)
— chúng lẻ vì đã được cân bằng mắt trên bản dựng; làm tròn cho "đẹp" là làm khác đi thứ
đã duyệt.

**Bảng màu được sinh tự động** từ chính file này bằng `tools/sync-palette.mjs`, và có
`palette-parity.spec.ts` đối chiếu hai chiều mỗi lần chạy test. Không thể lệch.

### Hai chỗ CỐ Ý khác — và vì sao

| Chỗ | Bản dựng | Angular | Vì sao |
|---|---|---|---|
| Thanh chọn bộ màu trên cùng | Có, kèm ghi chú *"chọn xong tôi dựng bằng Angular"* | Không | Đó là khung để **duyệt thiết kế**, không phải sản phẩm |
| Bảng đổi trạng thái dưới cùng | Có (4 nút) | Không | Cùng lý do |
| Chọn bộ màu + ngôn ngữ | — | Góc trên phải của cột form | `he-thong-thiet-ke.md` hứa người dùng tự chọn; màn này chưa có menu avatar để đặt vào, mà người **chưa đăng nhập được** cũng cần đọc lỗi bằng tiếng của họ. Kiểu dáng chép đúng `.swatch` của bản dựng |

> Nếu muốn màn đăng nhập sạch đúng như bản dựng thì bỏ `<app-theme-picker />` khỏi
> `login.html` — một dòng. Lúc đó bộ màu theo cài đặt sáng/tối của máy, đổi được sau khi
> vào trong.

### Dữ liệu mẫu trên bản dựng không phải giá trị mặc định

Bản dựng điền sẵn `an.le@acme.com` và một mật khẩu để duyệt phần *chữ đã nhập trông thế
nào*. Bản Angular để trống và dùng **placeholder** — điền sẵn email của người khác vào ô
đăng nhập là chuyện không được làm.

## Ngôn ngữ mặc định là tiếng Việt, KHÔNG suy đoán từ trình duyệt

Nghe thì hợp lý là đọc `navigator.language`. Nhưng rất nhiều máy ở Việt Nam để mặc định
`en-US` — máy mua sẵn, máy công ty cấp, Windows bản tiếng Anh. Suy đoán theo cài đặt máy
thì phần lớn người dùng thật mở app lên sẽ thấy tiếng Anh rồi phải đi tìm chỗ đổi.

Đoán sai ở đây không phải lỗi kỹ thuật — nó chỉ phiền, nhưng phiền với đúng nhóm người
mà sản phẩm phục vụ.
