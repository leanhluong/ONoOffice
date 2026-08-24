# 05 — API

> Cập nhật: 2026-08-24 · Gốc: `http://localhost:5000` (máy lập trình viên)
> Quyết định nền: [ADR-0002](../02-kien-truc/adr/ADR-0002-xac-thuc-va-phan-quyen.md) · [ADR-0003](../02-kien-truc/adr/ADR-0003-controller-thay-vi-minimal-api.md) · [ADR-0004](../02-kien-truc/adr/ADR-0004-luu-token-o-frontend.md)

Tài liệu này trả lời: **frontend gọi endpoint nào, gửi gì, nhận gì.**

## Thử ngay ở máy mình

```bash
docker compose up -d                        # Postgres 16, cổng 5433
cd backend && dotnet run --project src/ONoOffice.Api
```

Lần chạy đầu với database trống sẽ tự gieo một workspace dùng được:

| | |
|---|---|
| Workspace | `demo` — Công ty Demo |
| Email | `chu@demo.vn` |
| Mật khẩu | `MatKhauDemo!2026` |
| Vai trò | `Owner` — đủ cả 12 quyền |

Tài khoản này **chỉ có ở môi trường phát triển**: `Seed:Enabled` mặc định TẮT, và
`appsettings.Development.json` là nơi duy nhất bật nó.

---

## Luật chung — đọc một lần, áp cho mọi endpoint

### 1. Mọi lỗi có CÙNG một hình dạng

Chuẩn RFC 7807, `Content-Type: application/problem+json`:

```json
{
  "type": "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401,
  "errors": [
    { "code": "Auth.InvalidCredentials", "description": "Email hoặc mật khẩu không đúng." }
  ]
}
```

**`errors` LUÔN là một mảng**, kể cả khi chỉ có một lỗi. Frontend nhờ vậy viết đúng
một nhánh xử lý, không phải hỏi "lần này là một hay nhiều?".

| Trường | Dùng để làm gì |
|---|---|
| `code` | **Frontend rẽ nhánh theo trường này.** Nó ổn định, không đổi theo ngôn ngữ |
| `description` | Câu hiển thị cho người dùng, đã dịch sẵn theo `Accept-Language` |
| `status` | Trùng với mã HTTP của phản hồi |

> Không rẽ nhánh theo `description` — nó đổi khi đổi ngôn ngữ, và đổi khi ai đó sửa câu chữ.

### 2. Mã HTTP suy ra từ LOẠI lỗi, không do endpoint tự chọn

| Loại | HTTP | Nghĩa |
|---|---|---|
| `Validation` | 400 | Dữ liệu gửi lên sai. Sửa rồi gửi lại thì được |
| `Unauthorized` | 401 | Chưa biết anh là ai. **Frontend đưa về màn đăng nhập** |
| `Forbidden` | 403 | Biết anh là ai, nhưng không đủ quyền. **KHÔNG đá về đăng nhập** — đăng nhập lại cũng vậy thôi |
| `NotFound` | 404 | Không có bản ghi đó |
| `Conflict` | 409 | Dữ liệu đúng, nhưng trạng thái hiện tại không cho phép. Thử lại y hệt cũng vô ích |
| còn lại | 500 | Lỗi ngoài dự kiến. Thân phản hồi cố ý **không** nói gì thêm, chỉ kèm `correlationId` |

### 3. Đa ngôn ngữ

Gửi `Accept-Language: en` thì `description` trả về tiếng Anh. Không gửi, hoặc gửi ngôn
ngữ chưa hỗ trợ → tiếng Việt.

Hiện hỗ trợ: **`vi`** (mặc định) · **`en`**.

### 4. Mã lần vết

Mọi phản hồi mang header `X-Correlation-Id`. Gửi kèm sẵn thì server **giữ nguyên** mã đó.

Frontend nên hiện mã này ở màn báo lỗi — từ nó tìm ra đúng dòng log phía server.

### 5. Xác thực

```http
Authorization: Bearer <access_token>
```

Access token sống **15 phút**. Hết hạn thì gọi `POST /api/auth/refresh` để lấy cặp mới,
**không** bắt người dùng đăng nhập lại.

### 6. Phân quyền kiểm `permission`, không kiểm `role`

Endpoint đòi quyền `employee.read` thì token phải mang claim `permission` chứa đúng chuỗi
đó (không phân biệt hoa thường). Vai trò chỉ là cái túi đựng quyền — API không bao giờ hỏi
"anh có vai trò gì".

Đổi quyền của một người **có hiệu lực chậm nhất sau 15 phút** (một vòng đời access token).
Đó là cái giá của việc không tra database mỗi request — xem `ADR-0002`.

### 7. CORS

Chỉ origin nêu đích danh trong cấu hình mới gọi được từ trình duyệt. Không dùng cookie:
token đi trong thân phản hồi và do frontend tự gắn vào header ([ADR-0004](../02-kien-truc/adr/ADR-0004-luu-token-o-frontend.md)).

---

## Identity — Đăng ký workspace

### `POST /api/auth/register-workspace`

Endpoint **tạo ra một workspace mới** cùng người chủ của nó. Không cần token — người gọi
nó chính là người chưa có gì cả.

Một lần gọi tạo **ba thứ trong một transaction**: `Tenant`, bốn `Role` hệ thống
(Owner · Admin · Manager · Member), và `User` được gán vai trò Owner. Hỏng giữa chừng thì
không có gì được ghi — nếu không, một lần lỗi để lại một công ty không ai vào được.

```jsonc
// Request
{
  "companyName": "Công ty TNHH ACME",
  "workspaceCode": "acme",
  "fullName": "Lê Anh Lượng",
  "email": "chu@congty.vn",
  "password": "con meo ngoi tren mai nha"
}
```

```jsonc
// 200 OK — kèm luôn cặp token: đăng ký xong là đã đăng nhập
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs…",
  "refreshToken": "kR7n…",
  "expiresInSeconds": 900,
  "user": {
    "id": "0198e2f1-…",
    "tenantId": "0198e2f0-…",
    "email": "chu@congty.vn",
    "fullName": "Lê Anh Lượng"
  },
  "workspace": {
    "id": "0198e2f0-…",
    "code": "acme",
    "name": "Công ty TNHH ACME"
  }
}
```

| Ca hỏng | HTTP | `code` |
|---|---|---|
| Thiếu trường bắt buộc, hoặc mật khẩu ngắn hơn 10 ký tự | 400 | `Validation.Multiple` |
| Mã workspace sai định dạng | 400 | `TenantCode.Invalid` |
| Mã workspace không dài 3–30 ký tự | 400 | `TenantCode.WrongLength` |
| Email sai định dạng | 400 | `Email.Invalid` |
| **Mã workspace đã có người dùng** | 409 | `TenantCode.Taken` |
| **Email đã có tài khoản** | 409 | `Email.Taken` |

> **Trả 200 chứ không phải 201.** 201 phải kèm header `Location` trỏ tới tài nguyên vừa
> tạo, mà hiện chưa có endpoint nào đọc một workspace. Trả 201 với `Location` rỗng là nói
> dối về hợp đồng; sửa lại về sau còn dễ hơn gỡ một lời hứa đã ship.

> **Mã workspace được kiểm TRƯỚC email.** Trùng cả hai thì người dùng chỉ thấy một lỗi —
> nên cho họ thấy cái dễ sửa trước. Đổi mã workspace là gõ lại một từ; đổi email là chuyện
> khác hẳn.

> ⚠️ Endpoint này **để mở cho Internet** và mỗi lần gọi thành công là một công ty mới trong
> database. Chưa có giới hạn tần suất — xem [tien-do.md](../01-tong-quan/tien-do.md), mục
> "Chưa làm".

---

## Identity — Quản lý người dùng

Hai endpoint của màn **Nhân sự**. Cả hai đòi token, và đòi quyền khác nhau: `user.read`
để xem, `user.manage` để tạo. Gộp làm một thì ai xem được danh bạ cũng tạo được tài khoản.

### `GET /api/users`

```
?search=      tên (khớp một phần) hoặc email (khớp CHÍNH XÁC)
?status=      0 mọi trạng thái · 1 đang hoạt động · 2 chờ nhận tài khoản · 3 đã vô hiệu hoá
?roleId=      lọc theo một vai trò
?page=        mặc định 1
?pageSize=    mặc định 20, TRẦN CỨNG 100
```

```jsonc
// 200 OK
{
  "items": [{
    "id": "0198e2f1-…",
    "email": "an@congty.vn",
    "fullName": "Nguyễn Văn An",
    "isActive": true,
    "mustChangePassword": false,
    "roleName": "Member",
    "createdAtUtc": "2026-08-24T07:12:00+00:00"
  }],
  "page": 1, "pageSize": 20, "totalCount": 38, "totalPages": 2,
  "hasPreviousPage": false, "hasNextPage": true
}
```

> **Trần cứng 100 dòng.** Không có nó thì `?pageSize=1000000` kéo cả bảng lên bộ nhớ trong
> một request — rẻ tiền để gửi, đắt để phục vụ, và không đòi hỏi quyền gì đặc biệt.

> **Tìm theo email chỉ khớp CHÍNH XÁC**, không khớp một phần. Cột `email` ánh xạ qua một
> phép chuyển đổi giá trị (`Email` ↔ `text`) nên EF không dịch nổi `Contains` trên nó.
> Đổi sang kiểu sở hữu là một thay đổi riêng — xem "Chưa làm".

### `POST /api/users`

Quản trị viên tạo tài khoản **hộ** một đồng nghiệp.

```jsonc
// Request
{
  "fullName": "Nguyễn Văn An",
  "email": "an@congty.vn",
  "roleId": "0198e2ef-…",
  "mustChangePassword": true
}
```

```jsonc
// 200 OK
{
  "id": "0198e2f1-…",
  "email": "an@congty.vn",
  "fullName": "Nguyễn Văn An",
  "roleName": "Member",
  "temporaryPassword": "k7np-2wqx-hs4m"
}
```

| Ca hỏng | HTTP | `code` |
|---|---|---|
| Email sai định dạng | 400 | `Email.Invalid` |
| Vai trò không tồn tại, hoặc thuộc workspace khác | 404 | `Role.NotFound` |
| **Email đã có tài khoản** | 409 | `Email.Taken` |

> ⭐ **`temporaryPassword` chỉ trả về ĐÚNG MỘT LẦN.** Đây là lần duy nhất chuỗi thô tồn tại
> ngoài đầu người tạo — nó không được ghi log, không lưu, và **không endpoint nào đọc lại
> được**. Quên thì phải đặt lại mật khẩu.
>
> Vì sao không gửi email lời mời: lát này chưa nối dịch vụ gửi mail. Làm một luồng "đã gửi
> lời mời" mà thật ra không gửi gì là kiểu nói dối tệ nhất — quản trị viên ngồi chờ, đồng
> nghiệp không nhận được gì, và không chỗ nào báo lỗi.

> **Mật khẩu tạm đọc được qua điện thoại.** Bảng chữ bỏ `0/O` và `1/l/I`, chia cụm bằng dấu
> nối. Nó đi qua Zalo hoặc lời nói, nên một chuỗi base64 32 ký tự là đúng về mật mã và hỏng
> về thực tế. Đổi lại: nó chỉ sống tới lần đăng nhập đầu, vì `mustChangePassword` bắt đổi.

> **Vẫn là 200 chứ không phải 201** — cùng lý do với `register-workspace`: chưa có
> `GET /api/users/{id}` để header `Location` trỏ tới.

### `PATCH /api/users/{id}`

```jsonc
{ "fullName": "Nguyễn Văn An", "roleId": "0198e2ef-…" }   // → 204
```

| Ca hỏng | HTTP | `code` |
|---|---|---|
| Không có tài khoản đó trong workspace | 404 | `User.NotFound` |
| Vai trò không tồn tại, hoặc của workspace khác | 404 | `Role.NotFound` |
| **Đổi vai trò của chủ sở hữu** | 409 | `User.CannotChangeOwnerRole` |

> **Đổi vai trò là THAY, không phải THÊM.** Một người một vai (ADR-0002). Thêm mà không gỡ
> thì quyền chỉ có tăng — hạ ai đó từ Admin xuống Member sẽ không lấy lại được quyền nào.

### `POST /api/users/{id}/disable` · `/enable`

Không có thân request. Trả `204`.

| Ca hỏng | HTTP | `code` |
|---|---|---|
| **Tự vô hiệu hoá chính mình** | 409 | `User.CannotDisableSelf` |
| **Vô hiệu hoá chủ sở hữu** | 409 | `User.CannotDisableOwner` |

> **Vô hiệu hoá, không phải xoá.** Người nghỉ việc vẫn còn tin nhắn, còn tên trên bản ghi
> cũ, còn là người duyệt của một đơn từ năm ngoái.
>
> Hai luật chặn ở trên đều là chặn **workspace tự khoá chính mình ra ngoài**. Người bị khoá
> mất quyền truy cập trong vòng 15 phút — `/refresh` nạp lại `IsUserActive` và từ chối, và
> đó chính là lý do access token cố tình ngắn.

---

## Identity — Tài khoản của tôi

Ba endpoint của màn **Hồ sơ & cài đặt**. Chỉ đòi token, **không đòi quyền gì** — ai cũng
được sửa hồ sơ của chính mình.

Mã người dùng KHÔNG bao giờ nhận từ ngoài vào ở đây; nó lấy từ token. Nhận từ ngoài thì
`/api/me` trở thành cửa sửa hồ sơ bất kỳ ai.

### `GET /api/me`

```jsonc
{
  "id": "0198e2f1-…", "tenantId": "0198e2f0-…",
  "email": "chu@congty.vn", "fullName": "Lê Anh Lượng",
  "roleName": "Owner", "isOwner": true, "mustChangePassword": false
}
```

> `isOwner` để giao diện **ẩn bớt lựa chọn**: chủ sở hữu không tự đổi vai trò được, không
> tự vô hiệu hoá được. Hiện nút rồi báo lỗi khi bấm là cách chắc chắn nhất làm người dùng bực.

### `PATCH /api/me`

```jsonc
{ "fullName": "Lê Anh Lượng" }   // → 204
```

**Chỉ có họ tên.** Email là định danh đăng nhập nên phải qua quản trị viên; chức danh và
phòng ban do phòng Nhân sự đặt; vai trò thì đương nhiên không ai tự nâng cho mình được.

### `POST /api/me/password`

```jsonc
{ "currentPassword": "…", "newPassword": "…" }   // → 204
```

| Ca hỏng | HTTP | `code` |
|---|---|---|
| Sai mật khẩu hiện tại | 400 | `User.WrongCurrentPassword` |
| Mật khẩu mới trùng mật khẩu cũ | 400 | `User.NewPasswordSameAsCurrent` |

> ⭐ **Thành công thì MỌI refresh token của người đó bị thu hồi.** Lý do người ta đổi mật
> khẩu gần như luôn là "tôi nghĩ nó bị lộ" — không thu hồi thì kẻ trộm vẫn ngồi trong phiên
> cũ suốt 30 ngày, và việc đổi mật khẩu chỉ là một động tác cho yên tâm.
>
> **Thất bại thì KHÔNG thu hồi gì.** Ngược lại thì bất kỳ ai ngồi vào máy đang mở cũng đá
> được người dùng ra khỏi mọi thiết bị chỉ bằng cách gõ bừa.

---

## Identity — Vai trò

### `GET /api/roles`

Cần quyền `role.read`. Không phân trang — một workspace có bốn vai hệ thống cộng vài vai
tự tạo.

```jsonc
[{
  "id": "0198e2ef-…",
  "name": "Owner",
  "isSystem": true,
  "permissions": ["department.manage", "department.read", "…"],
  "memberCount": 1
}]
```

> `isSystem` quyết định giao diện có khoá bảng quyền hay không. Bốn vai hệ thống dựng lại
> từ hằng số trong mã nguồn ở mọi workspace, nên sửa chúng sẽ bị lần nâng cấp sau ghi đè mà
> không báo gì.

---

## Identity — Đăng nhập

Cả ba endpoint dưới đây **không cần token**: người gọi chúng chính là người chưa có, hoặc
không còn, token dùng được.

### `POST /api/auth/login`

```jsonc
// Request
{ "email": "an@congty.vn", "password": "…" }
```

```jsonc
// 200 OK
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs…",
  "refreshToken": "kR7n…",
  "expiresInSeconds": 900,
  "user": {
    "id": "0198e2f1-…",
    "tenantId": "0198e2f0-…",
    "email": "an@congty.vn",
    "fullName": "Nguyễn Văn An",
    "mustChangePassword": false
  }
}
```

> `mustChangePassword` nằm trong THÂN phản hồi, không nằm trong access token. Server không
> chặn gì dựa vào nó — nó chỉ để giao diện đưa người dùng thẳng tới màn đổi mật khẩu. Nhét
> vào token thì mọi request đều mang theo, và nó chỉ đúng tại thời điểm phát token.

| Ca hỏng | HTTP | `code` |
|---|---|---|
| Thiếu email hoặc mật khẩu | 400 | `Email` / `Password` |
| **Sai email HOẶC sai mật khẩu** | 401 | `Auth.InvalidCredentials` |
| Workspace đã ngừng hoạt động | 403 | `Auth.WorkspaceDisabled` |
| Tài khoản bị vô hiệu hoá | 403 | `Auth.AccountDisabled` |

> ⚠️ **Sai email và sai mật khẩu trả về GIỐNG HỆT NHAU** — cùng mã, cùng câu, và cùng
> khoảng thời gian xử lý. Tách bạch hai ca là tặng công cụ dò tài khoản: gõ 10.000 email,
> cái nào báo "sai mật khẩu" nghĩa là email đó có thật.
>
> Hai lỗi 403 thì cố ý nói thẳng, vì người tới được đó **đã gõ đúng mật khẩu** — gần như
> chắc chắn là chủ tài khoản thật. Giấu thì họ gọi điện cho IT hỏi "sao tôi không vào được".

### `POST /api/auth/refresh`

```jsonc
// Request
{ "refreshToken": "kR7n…" }
```

```jsonc
// 200 OK — refreshToken cũ đã bị THU HỒI, phải thay bằng cái mới
{ "accessToken": "eyJ…", "refreshToken": "9dQm…", "expiresInSeconds": 900 }
```

| Ca hỏng | HTTP | `code` |
|---|---|---|
| Không tìm thấy / hết hạn / đã thu hồi / **bị dùng lại** | 401 | `Auth.InvalidRefreshToken` |
| Workspace ngừng hoạt động | 403 | `Auth.WorkspaceDisabled` |
| Tài khoản bị vô hiệu hoá | 403 | `Auth.AccountDisabled` |

> **Refresh token dùng MỘT LẦN.** Mỗi lần gia hạn, vé cũ bị thu hồi và trả về vé mới —
> frontend phải ghi đè cái đang giữ, nếu không lần gia hạn sau sẽ bị coi là **trộm**.
>
> Vé đã thu hồi mà còn được đem dùng nghĩa là hai bên đang cùng giữ nó. Lúc đó hệ thống
> **thu hồi toàn bộ phiên của người đó** và bắt đăng nhập lại bằng mật khẩu. Bốn ca hỏng
> chung một mã là cố ý: nói rõ "vé này đã bị dùng lại" là mách cho kẻ tấn công biết
> mình đang bị theo dõi.

### `POST /api/auth/logout`

```jsonc
// Request
{ "refreshToken": "kR7n…" }
```

```
204 No Content
```

**Luôn thành công**, kể cả khi vé không tồn tại hoặc đã thu hồi. Người dùng muốn thoát và
họ đã thoát rồi — báo lỗi chẳng giúp gì, mà báo "vé này không tồn tại" là tiết lộ vé nào
từng tồn tại.

> Access token đang cầm **vẫn dùng được tới khi hết hạn** (tối đa 15 phút). Đó là bản chất
> của token không tra database. Muốn cắt tức thì thì phải có danh sách đen trong Redis —
> chưa cần ở lát 1.

---

## Chưa có

| Endpoint | Sẽ làm khi |
|---|---|
| `GET /api/auth/me` | Frontend cần thông tin người đang đăng nhập ngoài những gì `login` đã trả |
| Đăng ký workspace | Sau lát 1 |
| Đăng nhập Google / Facebook | Đang chờ đăng ký ứng dụng ở Google/Meta |
| `Org` — phòng ban, nhân viên | Lát 1, sau khi có migration đầu tiên |
