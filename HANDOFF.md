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
| `Luong.Kernel` (8 gói) | 🟢 Đủ dùng cho lát 1 | **200** |
| ONoOffice · Domain | 🟢 Xong | 151 |
| ONoOffice · Application | 🟢 Login · Refresh · Logout | *(trong 151)* |
| ONoOffice · Infrastructure | 🟢 EF · Argon2id · JWT · repository | *(trong 151)* |
| ONoOffice · **Api** | 🟢 **Xong** — 3 endpoint · phân quyền động · CORS · i18n · header an toàn | **29** |
| Test kiến trúc + i18n + luật Controller | 🟢 | 14 |
| **Migration + Docker Compose** | ⬜ **CHƯA LÀM — việc tiếp theo** | — |
| Frontend | 🟡 Khung Angular chạy được, chưa nối API | 8 |
| Tài liệu | 🟢 7 thư mục · 4 ADR · `05-api` · wireframe · bản dựng màu | — |

```bash
cd backend && dotnet build && dotnet test     # 194 xanh, 0 warning
cd frontend && npm run build && npm test      # 8 xanh
```

**Chưa có database nào.** 194 test đều chạy mà không cần Postgres — kể cả 29 test tích
hợp, vì `AddDbContext` chỉ ghi nhận chuỗi kết nối chứ không mở kết nối lúc khởi động.
Ranh giới đó cũng là ranh giới của bộ test hiện tại: **chưa có một câu truy vấn thật nào
từng chạy.**

---

## ⏭️ VIỆC TIẾP THEO — chạm database lần đầu

Ba việc, làm đúng thứ tự này:

```
⬜ 1. docker-compose.yml — Postgres 16 (+ pgAdmin nếu muốn)
⬜ 2. dotnet ef migrations add InitialIdentity
⬜ 3. Dữ liệu mồi: 1 workspace + 1 tài khoản Owner + 4 vai trò
```

**Vì sao Docker trước migration:** `dotnet ef migrations add` không cần database, nhưng
`database update` thì cần — và không kiểm chứng được migration bằng cách chạy thật thì
nó chỉ là một file `.cs` trông có vẻ đúng.

**Cái sẽ lộ ra ở bước này, và đó là điểm đáng giá nhất của nó:** cho tới lúc này
**chưa một câu truy vấn nào từng chạy thật.** 194 test đều dừng trước lời gọi dữ liệu đầu
tiên. Nghĩa là cấu hình EF, tên bảng snake_case, bộ lọc tenant, bốn interceptor — tất cả
mới chỉ được kiểm ở mức "mô hình dựng lên được", chưa ở mức "Postgres chấp nhận".

Cụ thể ba chỗ đáng ngờ nhất:

| Chỗ | Vì sao đáng ngờ |
|---|---|
| Bộ lọc tenant lúc **đăng nhập** | Đăng nhập chạy khi phiên CHƯA có tenant. `IgnoreQueryFilters` phải có mặt đúng ở đó — thiếu thì không ai đăng nhập được, thừa thì rò dữ liệu |
| `TenantInterceptor` khi INSERT `RefreshToken` | Nó tự điền `tenant_id` từ `ICurrentTenant`, mà lúc đăng nhập `ICurrentTenant` trả `null` |
| Hai schema `identity.*` và `org.*` | Chưa có schema nào tồn tại thật |

Sau đó là frontend:

```
⬜ Màn đăng nhập thật: gọi POST /api/auth/login, giữ access token trong biến,
   refresh token trong localStorage (ADR-0004)
⬜ 4 bộ màu + ngx-translate
⬜ Trạng thái chờ NÓI RÕ ĐANG LÀM GÌ — hạ tầng miễn phí ngủ dậy mất 30–60 giây
```

### Tầng `Api` — đã xong, đây là những gì nó có

```
src/ONoOffice.Api/
├── Program.cs                    · DI + pipeline, mỗi dòng middleware có chú thích vì sao ở đó
├── Controllers/Identity/AuthController.cs   · login · refresh · logout
├── Authorization/                · PermissionRequirement · Handler · PolicyProvider
├── Extensions/                   · ControllerSetup · AuthenticationSetup · CorsSetup · LocalizationSetup
├── Filters/                      · LocalizeProblemDetailsFilter
└── Middleware/                   · SecurityHeadersMiddleware

tests/ONoOffice.Api.IntegrationTests/    · 29 test, dựng máy chủ thật, KHÔNG cần Postgres
└── Probe/ProbeController.cs             · endpoint chỉ có lúc test — nằm ở assembly TEST,
                                           nạp vào bằng AddApplicationPart, nên bản build
                                           sản phẩm không thể chứa nó
```

Hợp đồng API đầy đủ: [`docs/05-api/README.md`](./docs/05-api/README.md).

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

## ⚠️ Tám cái bẫy đã gặp — đừng dẫm lại

### Bốn cái mới, gặp khi ráp tầng `Api` (2026-08-24)

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
docs/07-giao-dien/identity/dang-nhap.html     · bản dựng màu — 4 bộ, 4 trạng thái
```

Cả hai **mở thẳng bằng trình duyệt** là chạy.

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

VIỆC TIẾP THEO: chạm database lần đầu — docker-compose (Postgres 16) →
migration InitialIdentity → dữ liệu mồi. Xem mục "VIỆC TIẾP THEO" trong
HANDOFF.md: nó nêu sẵn ba chỗ đáng ngờ nhất sẽ lộ ra ở bước này.
Chưa duyệt thiết kế bước này — trình bày trước theo luật 1.

Bắt đầu bằng việc chạy `cd ONoOffice/backend && dotnet build && dotnet test`
để xác nhận 194 test còn xanh, rồi báo tôi trạng thái trước khi làm gì.
```

**Vì sao đoạn prompt này dài như vậy:** session mới không nhớ gì cả. Ba thứ nó **không thể
tự đoán ra** là (a) luật phải trình bày thiết kế trước, (b) TDD phải thấy đỏ thật, và
(c) commit tiếng Anh nhưng hội thoại tiếng Việt. Thiếu chúng thì session mới sẽ lao vào
code ngay và viết commit tiếng Việt.
