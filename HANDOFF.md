# HANDOFF — ONoOffice

> Cập nhật: **2026-08-24** · Nhánh làm việc: `develop`
> File này để một người (hoặc một agent) mới vào **hiểu ngay đang ở đâu và làm gì tiếp**.
> Xong việc gì thì cập nhật file này **và** `docs/01-tong-quan/tien-do.md` trong cùng commit.

---

## Đọc gì trước — theo đúng thứ tự này

```
1. docs/README.md                      · mục lục 7 thư mục tài liệu
2. docs/01-tong-quan/README.md         · sản phẩm là gì, 6 câu nghiệp vụ đã chốt
3. docs/02-kien-truc/README.md         · 2 module, 4 tầng, 4 luật ranh giới
4. docs/02-kien-truc/adr/              · 4 ADR — vì sao chọn thế này mà không chọn thế kia
5. docs/05-api/README.md               · hợp đồng API: hình dạng lỗi, quy ước phân quyền
6. docs/01-tong-quan/tien-do.md        · nhật ký từng ngày, chi tiết hơn file này
```

## Hai repo, đừng nhầm

```
D:/Nextx/2026/project-persion/
├── libNetCore/     → thư viện dùng chung, phát hành thành gói Luong.Kernel.*
│                     KHÔNG được có một chữ nghiệp vụ nào (Employee, Department…)
└── ONoOffice/      → sản phẩm
    ├── docs/       · 7 thư mục
    ├── backend/    · .NET 10 — src/ + tests/ + ONoOffice.slnx
    └── frontend/   · Angular 22
```

`Luong.Kernel` đã phát hành **`0.1.0` lên GitHub Packages** (8 gói). Lúc phát triển thì
ONoOffice dùng `ProjectReference` sang mã nguồn, đổi bằng công tắc:

```bash
dotnet build                              # ProjectReference — sửa lib là dùng ngay
dotnet build -p:UseLocalKernel=false      # PackageReference — ghim Luong.Kernel 0.1.0
```

---

## Đang ở đâu

| Phần | Trạng thái | Số test |
|---|---|---|
| `Luong.Kernel` (8 gói) | 🟢 Đủ dùng cho lát 1 | **202** |
| ONoOffice · Domain | 🟢 Identity xong · Org xong | 214 + 36 |
| ONoOffice · Application | 🟢 Auth (4) · **Users (4)** · **Me (3)** · **Roles (1)** | *(trong 214)* |
| ONoOffice · Infrastructure | 🟢 EF · Argon2id · JWT · repository · seeder · sinh mật khẩu tạm | *(trong 214)* |
| ONoOffice · Api | 🟢 **12 endpoint** · phân quyền động · CORS · i18n · header an toàn | 29 |
| Test kiến trúc + i18n + luật Controller | 🟢 | 14 |
| **Database** | 🟢 Postgres 16 · 2 migration · dữ liệu mồi — đã chạy THẬT | **52** |
| **Backend nói chung** | 🟢 **Đăng nhập được đầu-tới-cuối** | — |
| **Frontend · đăng nhập + đăng ký** | 🟢 **Cả hai đã nối API thật** · tự gia hạn khi 401 · 4 bộ màu · vi/en | **75** |
| **Bản dựng ↔ code** | 🟢 CSS **sinh** từ bản dựng · `npm run parity` so từng điểm ảnh (lệch 0,02%) | *(trong 75)* |
| **Khung ứng dụng v3** | 🟢 Cột điều hướng có chữ, sinh từ `_khung.css` · 18 biến màu chết đã sửa | *(trong 75)* |
| Bản dựng nhân sự · tài khoản · vai trò | 🟡 Đã duyệt, **chưa nối Angular** | — |
| Frontend · các màn còn lại | ⬜ Dashboard và danh sách nhân viên vẫn là khung rỗng | — |
| Tài liệu | 🟢 7 thư mục · 4 ADR · `05-api` · wireframe · bản dựng màu | — |

```bash
docker compose up -d                          # Postgres 16 ở cổng 5433
cd backend && dotnet build && dotnet test     # 345 xanh, 0 warning
cd frontend && npm test && npm run parity     # 75 xanh · hai màn lệch 0,02%
```

### Đã kiểm chứng tới đâu (2026-08-24) — và chỗ nào thì CHƯA

| Việc | Bằng chứng |
|---|---|
| Màn đăng nhập vẽ đúng thiết kế | Ảnh chụp trình duyệt thật ở `localhost:4200` |
| Preflight + CORS từ đúng origin `:4200` | `curl -X OPTIONS` → 204, đủ header cho phép |
| Đăng nhập trả token đủ 12 quyền | `curl` → 200, giữ nguyên `X-Correlation-Id` gửi lên |
| Vé gia hạn xoay vòng, dùng lại thì **thu hồi cả chuỗi** | `curl` ba lần: vé cũ 401, và vé MỚI cũng 401 |
| Luồng gia hạn của FE (gộp một lần, gửi lại một lần) | 31 test đơn vị, có cố ý phá thứ tự để chứng minh |
| Đăng ký workspace tạo đủ công ty + 4 vai + chủ sở hữu | `curl` → 200, token có đủ 12 quyền; gọi lại cùng mã → 409 `TenantCode.Taken` |
| Đăng ký xong đăng nhập được bằng chính mật khẩu vừa đặt | `curl` → 200, và test `DangKyXong_ThiDangNhapDuocBangMatKhauVuaDat` trên Postgres thật |
| Hai màn Angular **giống hệt bản dựng đã duyệt** | `npm run parity` — chụp cả hai ở 1440×940, lệch 0,02% (ngưỡng 0,40%) |
| Đổi mật khẩu xong thì **vé gia hạn cũ chết** | Test database: đổi xong `/refresh` trả 401; sai mật khẩu hiện tại thì vé VẪN sống |
| Không ai vô hiệu hoá được chủ sở hữu | Test database dựng thêm một Admin rồi thử; đã phá lại luật để chứng minh nó đỏ |
| Đăng ký → thẻ xác nhận → dashboard, **bấm tay qua giao diện thật** | Ảnh chụp trình duyệt, kèm ca trùng mã (ô đỏ) và ca chưa tick điều khoản (popup) |
| Quản trị tạo tài khoản → **đăng nhập bằng chính mật khẩu tạm đó** | `UserManagementFlowTests` trên Postgres thật, đi qua Argon2 và UNIQUE thật |
| Danh sách nhân sự **không rò sang workspace khác** | Test dựng hai workspace rồi kiểm chéo; đã cố ý gỡ bộ lọc tenant để chứng minh nó đỏ |

⬜ **Chưa bấm tay qua: màn Nhân sự, Hồ sơ, Vai trò.** Ba màn đó mới có bản dựng, chưa có
Angular. Luồng đăng ký và đăng nhập thì đã bấm tay qua rồi — xem bảng trên.

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"chu@demo.vn","password":"MatKhauDemo!2026"}'
```

---

## ⏭️ VIỆC TIẾP THEO

**Việc số 0, mất năm phút:** bật `node tools/serve-mockups.mjs` rồi mở
`http://localhost:4300`, xem lại ba bản dựng đã duyệt (nhân sự · hồ sơ · vai trò) trước
khi dựng chúng bằng Angular. Backend của màn Nhân sự đã xong và có test chạy trên Postgres
thật — việc còn lại chỉ là nối giao diện.

Sau đó:

```
⬜ Angular: màn Nhân sự (bản dựng org/nhan-su.html đã duyệt, backend đã có API)
⬜ GET /api/me/sessions · DELETE — màn "thiết bị đang đăng nhập" trong bản dựng.
     RefreshToken chưa lưu user-agent nên chưa nói được "Chrome trên Windows".
     Cần thêm cột + migration, HOẶC sửa bản dựng cho khớp thực tế.
     (Bản dựng còn hứa cả vị trí "Hà Nội" — cái đó cần GeoIP, là việc riêng.)
⬜ Màn quản lý chat — bản dựng comm/chat.html đã có, chưa duyệt chốt
⬜ Module Org — Application → Infrastructure → Api (Domain đã XONG, 36 test)
⬜ Tìm nhân sự theo MỘT PHẦN email — cần đổi ánh xạ Email sang kiểu sở hữu
⬜ docs/04-database — sơ đồ bảng, quan hệ, chỉ mục
⬜ Giới hạn tần suất cho /api/auth/register-workspace — nó đang mở cho Internet
```

> **Trước khi vẽ màn mới:** làm bản dựng HTML trong `docs/07-giao-dien/` và chờ user duyệt — đây là luật user đặt, không phải gợi ý. Bật xem bằng
> `node tools/serve-mockups.mjs` (cổng 4300). Duyệt xong mới sang Angular, và sang bằng
> `node tools/sync-shell.mjs` + `npm run parity`.

Ba việc nhỏ đang nợ, đã ghi rõ trong code:

| Nợ | Ở đâu |
|---|---|
| Ô "Ghi nhớ tôi" có mặt nhưng **chưa nối gì** | `login.html` |
| Điều khoản / Chính sách riêng tư chỉ là link rỗng | `register.html` |
| Nút "Vào workspace" sau đăng ký đi thẳng `/dashboard` — chưa có màn mời đồng nghiệp | `register.ts` |
| Quên mật khẩu / Google / Facebook đều hiện *"đang phát triển"* | `login.ts` — `notBuiltYet()` |
| `Manager` trùng khít `Member` cho tới khi có `leave.approve` | `SystemRoles.cs`, có test canh |

### Chạy cả hệ thống

```bash
docker compose up -d                                   # Postgres 16, cổng 5433
cd backend  && dotnet run --project src/ONoOffice.Api  # http://localhost:5000
cd frontend && npm start                               # http://localhost:4200
```

Lần chạy đầu với database trống tự chạy migration và gieo dữ liệu mồi:

```
Workspace  demo · Công ty Demo
Đăng nhập  chu@demo.vn  /  MatKhauDemo!2026
```

```bash
docker compose down -v      # xoá sạch dữ liệu, lần sau gieo lại từ đầu
```

⚠️ **Cổng 5433, không phải 5432.** Máy này đã có một Postgres cài thẳng vào Windows giữ
5432. Trỏ nhầm thì migration chạy vào nhầm database — và nó sẽ *thành công*.

### Năm bộ test, năm mục đích khác nhau

| Bộ | Số test | Cần gì | Trả lời câu hỏi |
|---|---|---|---|
| `Identity.UnitTests` | 214 | không | Luật nghiệp vụ của Identity có đúng không |
| `Org.UnitTests` | 36 | không | Luật nghiệp vụ của Org (phòng ban, nhân viên) |
| `ArchitectureTests` | 14 | không | Ranh giới tầng và luật Controller có bị phá không |
| `Api.IntegrationTests` | 29 | không | Pipeline, phân quyền, hình dạng lỗi, i18n có đúng không |
| `Api.DatabaseTests` | 52 | **Docker** | EF ánh xạ, cô lập tenant, luồng đăng nhập/đăng ký/tạo tài khoản có chạy THẬT không |
| `frontend` (vitest) | 75 | không | Hợp đồng với API, luồng gia hạn phiên, bản dịch, bảng màu và **tên biến/lớp CSS** có lệch không |
| `npm run parity` | 2 màn | **Chrome** | Bản Angular trông có **giống hệt bản dựng đã duyệt** không |

Bộ thứ tư tự dựng Postgres bằng **Testcontainers**, không nối vào `docker compose`. Cố ý:
test nối vào compose sẽ im lặng bỏ qua trên máy chưa `up` và trên CI — mà test không chạy
thì tệ hơn cả không có, vì nhìn danh sách vẫn thấy nó nằm đó.

```bash
cd backend  && dotnet build && dotnet test      # 345 xanh, 0 warning
cd frontend && npm test && npm run build && npm run lint && npm run parity
```

---

## Luật bắt buộc — vi phạm là phải sửa

| Luật | Vì sao |
|---|---|
| **Trình bày thiết kế trước, chờ user gật, rồi mới code** | Dự án này là *vừa làm vừa học* — giá trị nằm ở chỗ hiểu từng quyết định |
| **TDD: viết test, THẤY NÓ ĐỎ, rồi mới viết code** | Test xanh ngay từ đầu không chứng minh được gì |
| Commit **bằng tiếng Anh**, hội thoại và comment bằng **tiếng Việt** | Git history là mặt tiền của dự án |
| Làm thẳng trên `develop`, **không** nhánh phụ, **không** PR | Repo cá nhân, một người làm |
| Có việc chạy song song thì `git add <đường-dẫn>`, **không bao giờ** `git add -A` | `-A` sẽ nuốt file đang viết dở của tiến trình kia |
| Xong việc → cập nhật `tien-do.md` **và** file này trong **cùng commit** | Tách ra là cách chắc chắn nhất để tài liệu lệch khỏi code |

---

## Những quyết định đã chốt — đừng bàn lại nếu không có lý do mới

| Chủ đề | Đã chốt | Ghi ở |
|---|---|---|
| Multi-tenant | Chung DB + cột `tenant_id` + **4 lớp cô lập** | `ADR-0001` |
| Danh tính | Mỗi người thuộc **đúng một** workspace · email unique **toàn hệ thống** | `ADR-0002` |
| Token | Access 15 phút · refresh 30 ngày, **lưu băm, xoay vòng** | `ADR-0002` |
| Phân quyền | Kiểm **`permission`**, KHÔNG kiểm `role` | `ADR-0002` |
| Web | **Controller**, không phải Minimal API | `ADR-0003` |
| Lưu token ở FE | Access trong biến, refresh trong `localStorage` | `ADR-0004` |
| Ký JWT | **HS256** — một tiến trình vừa phát vừa xác minh | — |
| Băm mật khẩu | **Argon2id**, m=19MiB t=2 p=1 | — |
| Đa ngôn ngữ | **Cả BE lẫn FE**. BE dùng `.resx`, FE dùng `ngx-translate` | `07-giao-dien/da-ngon-ngu.md` |
| Giao diện | **Ship cả 4 bộ màu**, người dùng tự chọn | `07-giao-dien/he-thong-thiet-ke.md` |

---

## ⚠️ Mười tám cái bẫy đã gặp — đừng dẫm lại

### Ba cái gặp khi bắt bản dựng và code phải giống nhau (2026-08-24)

**Dựng giao diện theo file `.md` mô tả, không mở `.html` ra đối chiếu → 17 giá trị màu sai, mà build, lint, test đều xanh.** Văn xuôi nói "xanh mực" thì không ai dựng lại đúng `#0b0c0e`. Nay CSS được **sinh** từ chính bản dựng (`tools/sync-shell.mjs`): không có hai file thì không lệch được. Nhưng sinh tự động chỉ bảo đảm cho lần sinh — nó không ngăn ai mở `styles.scss` ra sửa tay, nên còn hai test đối chiếu chạy mỗi lần `npm test`.

**Sinh CSS vẫn KHÔNG bắt được lệch về bố cục.** Màu đúng hết mà lề lệch 4px, thiếu một icon, hay cả một khối đặt nhầm chỗ thì mọi test vẫn xanh. Chỉ có một cách canh "trông giống nhau": nhìn cả hai rồi so. Đó là `npm run parity` — chụp bản dựng và bản Angular ở 1440×940 rồi so từng điểm ảnh, ngưỡng 0,4%.

**Bản dựng thiếu `<!doctype html>` → trình duyệt chạy chế độ *quirks* và cả trang lệch xuống 8 điểm ảnh.** Đây là lỗi đầu tiên bộ so ảnh bắt được, và nó nằm ở chính bản dựng chứ không ở code. Bài học lớn hơn con số 8px: **thứ người duyệt nhìn không phải thứ sẽ chạy**, trừ khi có cái gì đó chứng minh ngược lại.

### Ba cái gặp khi nối frontend vào API thật (2026-08-24)

**Khung FE viết trước khi có backend thì mọi tên trường đều là phỏng đoán.** Bốn chỗ lệch,
không chỗ nào gây lỗi biên dịch: `expiresIn` thật ra là `expiresInSeconds`; tên và email
được đọc từ claim trong token nhưng backend cố ý không nhét chúng vào đó; `/refresh` không
trả `user` nên gia hạn xong là mất tên; và `TokenStorage` ghi cả access token xuống
`localStorage`, trái thẳng `ADR-0004`. Bài học: khung viết trước backend phải ghi to
"CHƯA KIỂM CHỨNG" và **đối chiếu lại từng trường** ở ngày nối thật.

**`refreshInterceptor` phải đứng TRƯỚC `authInterceptor`.** Lần gửi lại sau khi gia hạn
phải đi qua `auth` một lần nữa để gắn token MỚI. Đặt sau thì request gửi lại vẫn mang
đúng cái token vừa hết hạn — 401 lần nữa, và lần này không ai cứu. Đã cố ý đảo một lần:
test đỏ và chỉ thẳng vào chỗ token cũ.

**Mọi lời gọi gia hạn phải gộp làm MỘT, nếu không sẽ tự đá người dùng ra.** Một màn hình
mở ra bắn 5–6 request; token vừa hết hạn thì cả 5–6 cùng nhận 401. Mỗi cái tự gọi
`/refresh` thì cái thứ hai cầm vé đã tiêu — backend coi là **bị trộm** và thu hồi cả
chuỗi. Đây không phải suy đoán: đã dựng lại bằng curl trên server thật, vé cũ 401 và vé
MỚI cũng 401 theo.


### Bốn cái gặp khi chạm database lần đầu (2026-08-24)

> Cả bốn đều là **hỏng im lặng**, và cả bốn đều lọt qua 194 test trước đó. Đây chính là
> lý do bước "chạm database thật" không thể trì hoãn thêm nữa.

**Mô hình EF chưa từng dựng nổi trên Postgres, mà test vẫn xanh.** `Role._permissions` là
`HashSet<string>`; EF chỉ map primitive collection cho mảng hoặc `IList`. Test đơn vị chỉ
chạm `context.Model` nên không kích hoạt phép kiểm đó, và nó xanh suốt. Truy vấn thật đầu
tiên mới nổ. Bài học: *"mô hình dựng lên được"* và *"Postgres chấp nhận"* là hai câu hỏi
khác nhau. `User._roleIds` không dính vì nó là `List` — EF đổ dữ liệu vào tại chỗ được.

**Thiếu `ValueComparer` thì `Grant()` không lưu gì cả.** Với thuộc tính có phép chuyển
đổi, EF mặc định so bằng **tham chiếu**. `Grant()` sửa tại chỗ chính cái `HashSet` đang có
nên tham chiếu không đổi → EF kết luận "không có gì thay đổi" → không sinh câu `UPDATE`.
Cấp quyền xong, không lỗi, và quyền biến mất sau khi tải lại trang.

**`DatabaseFixture` tự dựng `DbContextOptions` = test xanh giả.** Bản tự dựng thiếu cả bốn
interceptor và thiếu bảng lịch sử migration tuỳ chỉnh. Test cô lập tenant vì thế "xanh"
trong khi lớp bảo vệ nó tưởng đang kiểm **không hề có mặt**. Nay nó lấy `DbContext` qua
đúng đường DI của ứng dụng, và giả lập phiên bằng một `HttpContext` mang claim `tenant_id`.

**Cổng 5432 đã có chủ.** Máy này có sẵn một Postgres cài thẳng vào Windows. Container
dùng **5433**. Trỏ nhầm cổng thì migration chạy vào nhầm database — và nó sẽ *thành công*.

### Bốn cái gặp khi ráp tầng `Api` (2026-08-24)

**Thứ tự `UseCors` — lời giải thích quen thuộc là SAI, dù kết luận thì đúng.** Ai cũng
bảo "preflight `OPTIONS` không mang token nên bị 401". Thực nghiệm ở đây cho thấy **không
xảy ra**: `OPTIONS` không khớp endpoint nào (định tuyến theo thuộc tính chỉ map `GET`/`POST`),
nên `UseAuthorization` chẳng có policy nào để áp. Chuyện hỏng thật là ở request **bình
thường** bị từ chối: `UseAuthorization` cắt ngang và trả 401 ngay tại chỗ nó đứng, nên nếu
`UseCors` đứng sau thì **401 đi ra không có header CORS** → trình duyệt cấm JavaScript đọc
phản hồi, kể cả mã trạng thái → frontend không phân biệt được "phiên hết hạn" với "máy chủ
hỏng". Test canh: `PhanHoi401ChoRequestXuyenOrigin_VanPhaiMangHeaderCors`.

**Bản dịch có thể nằm đó mà không ai gọi.** 41 khoá `.resx` + test đối chiếu đủ khoá làm
người ta yên tâm là i18n đã xong. Nhưng `ProblemDetails.Localize()` là thứ phải **gọi**, và
`ToActionResult()` của kernel cố ý không gọi nó (kernel không được biết sản phẩm có `.resx`
nào). Thiếu chỗ gọi thì mọi người dùng luôn nhận câu tiếng Việt viết cứng — và không test
nào cũ bắt được. Nay có `LocalizeProblemDetailsFilter` + test canh câu tiếng Anh thật.

**`[ApiController]` tự sinh một khuôn lỗi RIÊNG.** JSON hỏng → MVC trả `errors` dạng **từ
điển** theo tên trường, khác hẳn mảng `errors[]` của mọi lỗi khác. Frontend phải viết hai
nhánh, và nhánh thứ hai chỉ lộ ra khi có người gửi JSON hỏng — thường là ở môi trường thật.
Đã ép về một hình dạng bằng `InvalidModelStateResponseFactory`. Kèm theo:
`SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true`, nếu không thì MVC
tự coi mọi `string` không-null là bắt buộc và chặn **trước** khi FluentValidation kịp chạy —
tức là có hai bộ kiểm dữ liệu nói hai câu khác nhau.

**Bản dịch nằm ở satellite assembly, và `<NeutralLanguage>` sẽ giết nó.** `Messages.vi.resx`
sinh ra `vi/ONoOffice.Api.resources.dll`, không nhúng vào assembly chính. Đặt
`<NeutralLanguage>vi</NeutralLanguage>` nghe rất hợp lý nhưng nó bảo `ResourceManager` rằng
bản tiếng Việt nằm trong assembly chính → mọi phép tra tiếng Việt trả `null`. Cũng vì vậy,
hai dòng `<EmbeddedResource Update="ResourcesMessages.*.resx">` cũ (thiếu dấu `/`) là
**no-op may mắn** — "sửa" chúng cho đúng ý định ban đầu sẽ làm hỏng toàn bộ bản dịch. Đã gỡ
hẳn và ghi lý do vào `.csproj`. Có `KiemTraBanDich()` chạy lúc khởi động canh chuyện này.

### Bốn cái cũ

**`IgnoreQueryFilters` chỉ có ĐÚNG HAI chỗ hợp lệ trong toàn hệ thống:** tra cứu lúc
**đăng nhập** và lúc **gia hạn phiên**. Lý do: cả hai chạy khi phiên **chưa có tenant** —
người ta đang đi *xin* token. Thấy chỗ thứ ba là sai, phải hỏi lại.

**`SetQueryFilter` đời cũ là GÁN ĐÈ.** Gọi hai lần thì cái sau xoá mất cái trước — bộ lọc
tenant biến mất, **mọi workspace nhìn thấy dữ liệu của nhau**, im lặng. Đang dùng bộ lọc
**có tên** của EF 10 để tránh.

**Bộ lọc tenant phải đọc từ `DbContext`, không nhận `Guid` lúc dựng mô hình.** EF cache mô
hình cho cả tiến trình — nhét giá trị vào lúc đó thì mọi request sau dùng lại tenant của
request **đầu tiên**.

**Đăng nhập sai email và sai mật khẩu phải trả lỗi GIỐNG HỆT NHAU**, và ca không tìm thấy
email **vẫn phải chạy `Verify`** một lần. Bỏ qua bước băm làm request đó nhanh hơn hẳn
(Argon2id cố ý chậm ~100ms) → đo thời gian là dò ra email nào có thật.

---

## Đang chờ user

| Việc | Ghi chú |
|---|---|
| Đăng nhập Google / Facebook | Nút đã có, bấm hiện *"đang phát triển"*. Cần đăng ký ứng dụng ở Google/Meta |
| `NUGET_API_KEY` | Đã tắt bước đẩy nuget.org trong `release.yml` vì key bị 403 |
| Ba câu hỏi giao diện | Cuối `docs/07-giao-dien/wireframes.html` |

## Về triển khai — đã chốt 2026-08-23

Hiện chạy **local**, chưa có tên miền nào. Deploy đầu tiên sẽ lên **hạ tầng miễn phí**,
nên FE và BE gần như chắc chắn **khác tên miền gốc** (`*.pages.dev` với `*.onrender.com`).

Hệ quả: **cookie không dùng được** — cookie do API đặt sẽ là cookie bên thứ ba, Safari
chặn mặc định. Vì vậy token đi trong thân phản hồi, xem [`ADR-0004`](./docs/02-kien-truc/adr/ADR-0004-luu-token-o-frontend.md).

⚠️ **Một cái bẫy của hạ tầng miễn phí:** phần lớn gói free **tắt máy khi không có ai dùng**
(Render free là ví dụ). Request đầu tiên sau khi ngủ mất **30–60 giây** để đánh thức — và
người dùng sẽ tưởng app hỏng. Màn đăng nhập phải có trạng thái chờ **nói rõ đang làm gì**,
đừng để nút quay im lặng.

## Xem nhanh thiết kế

```
docs/07-giao-dien/wireframes.html             · bố cục 6 màn (đơn sắc, có chú thích)
docs/07-giao-dien/identity/dang-nhap.html     · bản dựng màu — 4 bộ, 5 trạng thái
docs/07-giao-dien/identity/dang-ky.html       · bản dựng màu — 4 bộ, 5 trạng thái
docs/07-giao-dien/comm/chat.html              · trao đổi nội bộ — 4 bộ, 2 kiểu luồng, 5 trạng thái
```

Mở thẳng bằng trình duyệt là chạy, hoặc bật máy chủ để xem cả danh sách:

```bash
node tools/serve-mockups.mjs        # http://localhost:4300
```

Thêm `?state=invalid` để xem một trạng thái, `?kieu=rieng` đổi kiểu luồng chat,
`?skin=giay` mở thẳng một bộ màu, `?bare=1` giấu thanh duyệt.

### 🔒 Bản dựng và code KHÔNG được lệch — đây là luật user đặt

Ba công cụ giữ chúng dính nhau. Đổi giao diện thì **sửa bản dựng trước**, rồi chạy lại bộ sinh —
sửa thẳng vào `.scss` sẽ bị lần chạy sau xoá mất.

```bash
node tools/sync-shell.mjs           # bản dựng → styles.scss, login.scss, register.scss
node tools/sync-error-messages.mjs  # .resx của backend → errors.json của FE
cd frontend && npm run parity       # chụp cả hai rồi so từng điểm ảnh
```

Lệch thì `parity` ghi ba ảnh vào `frontend/.shots/parity/` — ảnh thứ ba tô đỏ đúng chỗ khác nhau.

---

## 🚀 PROMPT KHỞI ĐỘNG SESSION MỚI

Mở session Claude Code mới **tại `D:/Nextx/2026/project-persion`**, dán nguyên đoạn dưới:

```
Đọc kỹ theo thứ tự:
  ONoOffice/HANDOFF.md                     ← đọc TRƯỚC, có đủ trạng thái + việc tiếp theo
  ONoOffice/docs/02-kien-truc/README.md
  ONoOffice/docs/02-kien-truc/adr/         ← 4 ADR, đọc cả mục "Đánh đổi"
  ONoOffice/docs/05-api/README.md          ← hợp đồng API: hình dạng lỗi, quy ước phân quyền
  ONoOffice/docs/01-tong-quan/tien-do.md   ← nhật ký chi tiết

Đây là dự án VỪA LÀM VỪA HỌC. Giá trị nằm ở chỗ tôi hiểu từng quyết định,
không phải ở tốc độ ra code.

LUẬT BẮT BUỘC:
1. Trước khi code BẤT KỲ chức năng nào: trình bày luồng nghiệp vụ, pattern áp dụng
   và vì sao, file nào ở tầng nào, cổng nào lộ ra, và những chỗ cần tôi quyết
   (nêu rõ đánh đổi + đề xuất). CHỜ TÔI GẬT rồi mới viết.
2. TDD thật: viết test → chạy → THẤY NÓ ĐỎ → mới viết code. Test xanh ngay từ đầu
   không chứng minh được gì. Với test canh luật thì phải cố ý phá luật một lần
   để chứng minh nó bắt được.
3. Commit message bằng TIẾNG ANH. Hội thoại và comment trong code bằng TIẾNG VIỆT.
4. Làm thẳng trên nhánh develop, không nhánh phụ, không PR.
5. Có việc chạy song song thì git add <đường-dẫn>, KHÔNG BAO GIỜ git add -A.
6. Xong việc → cập nhật ONoOffice/docs/01-tong-quan/tien-do.md VÀ ONoOffice/HANDOFF.md
   trong CÙNG commit.
7. Comment trong code phải giải thích VÌ SAO, không mô tả lại code đang làm gì.
   Nêu rõ đánh đổi và chuyện gì hỏng nếu làm khác đi.

Cách giảng: đừng dùng thuật ngữ chưa định nghĩa; ví von đời thường + số liệu cụ thể;
mở đầu bằng sự cố thật rồi mới tới lý thuyết.

LUẬT SỐ 8 — GIAO DIỆN: mọi màn mới phải làm thành bản dựng HTML trong
ONoOffice/docs/07-giao-dien/ để tôi duyệt TRƯỚC, rồi mới viết Angular. Và bản dựng
với code phải Y HỆT nhau để lần sau tôi kiểm không bị lệch — CSS sinh bằng
node tools/sync-shell.mjs, kiểm bằng npm run parity (so từng điểm ảnh).

VIỆC ĐẦU TIÊN, mất năm phút: chạy cả hệ thống (docker compose up -d, dotnet run,
npm start) rồi ĐĂNG KÝ MỘT WORKSPACE BẰNG TAY ở http://localhost:4200/dang-ky,
xem nó có nhảy sang thẻ xác nhận không. Hai màn đã kiểm bằng curl và bằng ảnh
chụp máy, nhưng chưa ai gõ tay qua giao diện thật.

Sau đó: màn quản lý chat (tôi đã chọn đây là đích tiếp theo), rồi module Org
từ tầng Application trở lên — Domain đã xong. Chưa duyệt thiết kế cái nào:
trình bày trước theo luật 1, và vẽ bản dựng theo luật 8.

Bắt đầu bằng việc chạy `cd ONoOffice/backend && dotnet build && dotnet test`
và `cd ONoOffice/frontend && npm test && npm run parity` để xác nhận
287 + 72 test còn xanh và hai màn vẫn khớp bản dựng (cần `docker compose up -d`
trước), rồi báo tôi trạng thái trước khi làm gì.
```

**Vì sao đoạn prompt này dài như vậy:** session mới không nhớ gì cả. Bốn thứ nó **không thể
tự đoán ra** là (a) luật phải trình bày thiết kế trước, (b) TDD phải thấy đỏ thật,
(c) commit tiếng Anh nhưng hội thoại tiếng Việt, và (d) giao diện phải qua bản dựng
trong `docs/07-giao-dien` rồi mới tới Angular. Thiếu chúng thì session mới sẽ lao vào
code ngay, viết commit tiếng Việt, và vẽ lại một giao diện khác thứ đã duyệt.
