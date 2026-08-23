# HANDOFF — ONoOffice

> Cập nhật: **2026-08-23** · Nhánh làm việc: `develop`
> File này để một người (hoặc một agent) mới vào **hiểu ngay đang ở đâu và làm gì tiếp**.
> Xong việc gì thì cập nhật file này **và** `docs/01-tong-quan/tien-do.md` trong cùng commit.

---

## Đọc gì trước — theo đúng thứ tự này

```
1. docs/README.md                      · mục lục 7 thư mục tài liệu
2. docs/01-tong-quan/README.md         · sản phẩm là gì, 6 câu nghiệp vụ đã chốt
3. docs/02-kien-truc/README.md         · 2 module, 4 tầng, 4 luật ranh giới
4. docs/02-kien-truc/adr/              · 3 ADR — vì sao chọn thế này mà không chọn thế kia
5. docs/01-tong-quan/tien-do.md        · nhật ký từng ngày, chi tiết hơn file này
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
| ONoOffice · **Api** | ⬜ **CHƯA LÀM — việc tiếp theo** | — |
| Test kiến trúc + i18n | 🟢 | 10 |
| Frontend | 🟡 Khung Angular chạy được, chưa nối API | 8 |
| Tài liệu | 🟢 7 thư mục · 3 ADR · wireframe · bản dựng màu | — |

```bash
cd backend && dotnet build && dotnet test     # 161 xanh, 0 warning
cd frontend && npm run build && npm test      # 8 xanh
```

---

## ⏭️ VIỆC TIẾP THEO — tầng `Api`

Thiết kế **đã được duyệt**, chỉ còn code:

```
src/ONoOffice.Api/
├── Program.cs                       · DI + pipeline
├── Controllers/Identity/AuthController.cs    · login · refresh · logout
├── Authorization/                   · PermissionRequirement · Handler · PolicyProvider
└── Extensions/                      · AuthenticationSetup · CorsSetup
```

**Thứ tự middleware — sai thứ tự là hỏng, không phải sở thích:**

```csharp
app.UseCorrelationId();                   // ① sớm nhất — mọi log sau đó mang mã lần vết
app.UseProblemDetailsExceptionHandler();  // ② bọc ngoài mọi thứ còn lại
app.UseRequestLocalization();             // ③ đặt CultureInfo cho i18n
app.UseCors();                            // ④ TRƯỚC auth — preflight OPTIONS không mang token
app.UseAuthentication();                  // ⑤ "anh là ai"
app.UseAuthorization();                   // ⑥ "anh được làm gì" — bắt buộc SAU ⑤
app.MapControllers();
```

Đặt `UseCors` sau `UseAuthentication` thì preflight bị chặn vì không có token — mà preflight
**không bao giờ** mang token. Triệu chứng: *"Postman gọi được, trình duyệt thì không."*

**Luật của Controller:** action **chỉ được một dòng**. Không `if`, không `try/catch`,
không gọi repository. Cái bẫy lớn nhất của Controller không phải hiệu năng — mà là nó
**quá tiện để nhét logic vào**.

```csharp
[HttpPost("login")]
[AllowAnonymous]
public async Task<IActionResult> Login(LoginCommand command, CancellationToken ct)
    => (await sender.Send(command, ct)).ToActionResult();
```

**Phân quyền động:** ASP.NET đòi policy phải đăng ký trước; với permission thì không thể
(hàng chục và còn thêm). Dùng `IAuthorizationPolicyProvider` sinh policy lúc chạy:
gặp tên lạ thì dựng một `PermissionRequirement` mang đúng tên đó. Handler chỉ soi claim
`permission` trong token — **không tra database**.

Sau `Api` còn:

```
⬜ dotnet ef migrations add InitialIdentity
⬜ docker-compose.yml  (API + Postgres 16)
⬜ FE: màn đăng nhập thật + 4 theme + ngx-translate
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

## ⚠️ Bốn cái bẫy đã gặp — đừng dẫm lại

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
  ONoOffice/docs/02-kien-truc/adr/         ← 3 ADR, đọc cả mục "Đánh đổi"
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

VIỆC TIẾP THEO: tầng Api (AuthController · phân quyền động theo permission ·
Program.cs · CORS · security header). Thiết kế ĐÃ ĐƯỢC DUYỆT — xem mục
"VIỆC TIẾP THEO" trong HANDOFF.md, code luôn không cần hỏi lại thiết kế.

Bắt đầu bằng việc chạy `cd ONoOffice/backend && dotnet build && dotnet test`
để xác nhận 161 test còn xanh, rồi báo tôi trạng thái trước khi làm gì.
```

**Vì sao đoạn prompt này dài như vậy:** session mới không nhớ gì cả. Ba thứ nó **không thể
tự đoán ra** là (a) luật phải trình bày thiết kế trước, (b) TDD phải thấy đỏ thật, và
(c) commit tiếng Anh nhưng hội thoại tiếng Việt. Thiếu chúng thì session mới sẽ lao vào
code ngay và viết commit tiếng Việt.
