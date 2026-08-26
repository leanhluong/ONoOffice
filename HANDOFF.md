# HANDOFF — ONoOffice

> Cập nhật: **2026-08-26** · Nhánh làm việc: `develop`
> File này để một người (hoặc một agent) mới vào **hiểu ngay đang ở đâu và làm gì tiếp**.
> Xong việc gì thì cập nhật file này **và** `docs/01-tong-quan/tien-do.md` trong cùng commit.

---

## Đọc gì trước — theo đúng thứ tự này

```
1. docs/README.md                      · mục lục 8 thư mục tài liệu
2. docs/01-tong-quan/README.md         · sản phẩm là gì, 6 câu nghiệp vụ đã chốt
3. docs/02-kien-truc/README.md         · 2 module, 4 tầng, 4 luật ranh giới
4. docs/02-kien-truc/adr/              · 4 ADR — vì sao chọn thế này mà không chọn thế kia
5. docs/05-api/README.md               · hợp đồng API: hình dạng lỗi, quy ước phân quyền
6. docs/01-tong-quan/tien-do.md        · nhật ký từng ngày, chi tiết hơn file này
```

## Hai repo, đừng nhầm

```
D:/Luong/Person/
├── libNetCore/     → thư viện dùng chung, phát hành thành gói Luong.Kernel.*
│                     KHÔNG được có một chữ nghiệp vụ nào (Employee, Department…)
└── ONoOffice/      → sản phẩm
    ├── docs/       · 8 thư mục (08 = hướng dẫn sử dụng, nguồn của màn /huong-dan)
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
| ONoOffice · Domain | 🟢 Identity xong · Org xong | 239 + 69 |
| ONoOffice · Application | 🟢 Auth · Users · Me · Roles · **Departments · Employees · Contacts · Members** | *(trong 239)* |
| ONoOffice · Infrastructure | 🟢 EF · Argon2id · JWT · repository · seeder · sinh mật khẩu tạm | *(trong 239)* |
| ONoOffice · Api | 🟢 **33 endpoint · 9 controller** · phân quyền động · CORS · i18n · header an toàn | 32 |
| **Cổng liên module** | 🟢 `Identity.Contracts.IUserDirectory` · `CompositeUnitOfWork` chốt hai DbContext | *(trong 32)* |
| Test kiến trúc + i18n + luật Controller | 🟢 | 15 |
| **Database** | 🟢 Postgres 16 · 2 migration · dữ liệu mồi — đã chạy THẬT | **52** |
| **Backend nói chung** | 🟢 **Đăng nhập được đầu-tới-cuối** | — |
| **Frontend · đăng nhập + đăng ký** | 🟢 **Cả hai đã nối API thật** · tự gia hạn khi 401 · 4 bộ màu · vi/en | **181** |
| **Bản dựng ↔ code** | 🟢 CSS **sinh** từ bản dựng · `npm run parity` so từng điểm ảnh (lệch 0,02%) | *(trong 181)* |
| **Khung ứng dụng v4** | 🟢 **Rail 56px + cột ngữ cảnh** — khuôn Lark Messenger / Zalo PC | *(trong 181)* |
| **Khung quản trị (khuôn B)** | 🟢 **Thanh ngang + sidebar 240px** — khuôn Lark Admin, **không có rail** | *(trong 181)* |
| **Tách hai vùng** | 🟢 `/` `/me` ↔ `/admin/*` · guard ở route cha · 3 redirect từ đường dẫn cũ | *(trong 181)* |
| **Nhận diện thương hiệu** | 🟢 Logo tự đổi bản sáng/tối theo bộ màu · favicon · bộ sinh tự chép sang `public/` | *(trong 181)* |
| **Frontend · Thành viên (GỘP)** | 🟢 Đọc `/api/members` — **ba loại dòng**: có cả hai · chỉ hồ sơ · chỉ tài khoản | *(trong 181)* |
| **Nối · cấp tài khoản · tạo hồ sơ** | 🟢 Một hộp thoại, **hai chiều × hai cách** · tạo xong nối luôn · có trong demo | *(trong 181)* |
| **Thao tác hàng loạt** | 🟢 Đổi phòng ban · đổi vai trò · vô hiệu hoá — chạy **tuần tự**, nói rõ ai bị bỏ qua | *(trong 181)* |
| **Đặt lại mật khẩu hộ** | 🟢 Hai bước · thu hồi mọi phiên · **chặn Admin leo lên Owner** | *(trong 181)* |
| **Màn Hướng dẫn** | 🟢 10 bài trong app · cây trái + mục lục phải · **ảnh sinh tự động từ app** | *(trong 181)* |
| **Chuyển nhượng quyền sở hữu** | 🟢 Đòi mật khẩu · đổi cả cờ lẫn vai · đóng lại **bốn ngõ cụt** | *(trong 181)* |
| **Frontend · Phòng ban + Danh bạ** | 🟢 Cây sửa được (thêm/đổi tên/chuyển/xoá) · danh bạ lọc theo phòng | *(trong 181)* |
| **Frontend · Hồ sơ & cài đặt** | 🟢 Sửa tên · đổi mật khẩu · bộ màu/ngôn ngữ | *(trong 181)* |
| **Frontend · Vai trò & quyền** | 🟢 Vai hệ thống chỉ xem · **vai tự đặt: tạo · sửa quyền · xoá** | *(trong 181)* |
| **Frontend · Tổng quan quản trị** | 🟡 4 số THẬT (không cần endpoint mới) · gói & hạn ngạch **chưa nối**, có đeo nhãn | *(trong 181)* |
| Frontend · các màn còn lại | ⬜ Dashboard vẫn là khung rỗng · Trao đổi mới có bản dựng | — |
| Tài liệu | 🟢 8 thư mục · 4 ADR · `05-api` · wireframe · bản dựng màu | — |

```bash
docker compose up -d                          # Postgres 16 ở cổng 5433
cd backend && dotnet build && dotnet test     # 355 xanh không cần Docker · +52 test database nếu có
cd frontend && npm test && npm run parity     # 181 xanh · hai màn lệch 0,02%
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
| Màn Nhân sự chạy thật: lọc, thêm người, xem chi tiết | Ảnh chụp app đang chạy — điền biểu mẫu, API tạo thật, nhận mật khẩu tạm, bảng tự cập nhật |
| Danh sách nhân sự **không rò sang workspace khác** | Test dựng hai workspace rồi kiểm chéo; đã cố ý gỡ bộ lọc tenant để chứng minh nó đỏ |
| Màn Thành viên gộp dựng ra đủ **ba loại dòng** | Test đếm `tbody tr` trên DOM thật, không dừng ở signal — nhưng **mới ở chế độ demo** |

⬜ **Chưa bấm tay qua: đổi mật khẩu ở màn Hồ sơ.** Backend có test trên Postgres thật, và
màn hình đã chụp ảnh, nhưng chưa ai gõ tay qua ba ô đó. Mọi thứ còn lại đã bấm qua.

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"chu@demo.vn","password":"MatKhauDemo!2026"}'
```

---

## 🧭 Ba khuôn màn hình — đọc trước khi vẽ bất cứ màn nào

```
người ngoài   /login · /dang-ky        không khung nào, mỗi màn tự dựng
🟢 KHUNG A     /  ·  /me                rail 56px + cột ngữ cảnh 260px  (Lark Messenger, Zalo PC)
🔴 KHUNG B     /admin/*                 thanh ngang + sidebar 240px      (Lark Admin)
```

**Luật ranh giới, một câu, kiểm được:** màn thao tác lên **người khác** hoặc lên **cấu hình
workspace** → khung B. Màn thao tác lên **chính mình** hoặc lên dữ liệu công việc hằng ngày
→ khung A. Nên `/me` ở A còn `/admin/users` ở B, dù cả hai đều sửa một bản ghi `User`.

**Vì sao B không dùng chung khung với A** (đã thiết kế sai một lần rồi sửa): điều hướng app
thì **rộng** — 6 app ngang hàng, rail 56px là vừa. Điều hướng quản trị thì **sâu** — nhiều
nhóm × vài trang con, bắt buộc có chữ, và không có chữ thì *"Tuân thủ"* với *"Bảo mật"* là
hai biểu tượng khiên giống hệt nhau. Chi tiết ở [`_khung-quantri.css`](./docs/07-giao-dien/chung/_khung-quantri.css).

Lối vào B nằm ở **menu ảnh đại diện** (khuôn Zalo PC), không phải một biểu tượng trên rail —
rail là app dùng hằng ngày, quản trị thì mỗi tháng vào một lần.

⚠️ **Màn khung A tự dựng `.noidung` của mình** — shell KHÔNG bọc sẵn. Màn có cột ngữ cảnh
thì `.nav` phải là anh em của `.noidung`; bọc sẵn ở shell thì cột bị xếp chồng lên nội dung.
Đã xảy ra ở Danh bạ, và chỉ ảnh chụp mới lộ ra.

⚠️ **Đổi khung thì `.trangdau` / `.trang` phải ở `_dieukhien.css`, KHÔNG phải `_khung.css`.**
Chúng là chrome của *trang*, cả hai khung đều cần. Để nhầm chỗ một lần rồi: mọi màn quản trị
mất sạch tiêu đề, mà build + lint + test đều xanh.

**Luật rộng hơn, đã dẫm HAI lần:** `_khung.css` chỉ dành cho thứ **chỉ khung A** có. Cái gì
cả hai khung dùng thì phải ở `_dieukhien.css`. Lần đầu là `.trangdau` / `.trang`; lần hai là
`.popover` — menu cài đặt và menu tài khoản của khung B hiện ra không nền, không viền, chữ
đè lên trang.

Khó thấy vì **app Angular vẫn đúng**: `styles.scss` gộp cả năm file, nên chỉ BẢN DỰNG hỏng.
Bản dựng và sản phẩm nói hai chuyện khác nhau, mà không bộ canh nào bắt được — `ui-parity`
so tập LỚP, và lớp thì vẫn đủ ở cả hai bên. Chỉ mở bản dựng ra nhìn mới thấy.

---

## ⏭️ VIỆC TIẾP THEO

**Việc số 0, mất năm phút — bấm thử màn Thành viên GỘP ở chế độ demo.** Không cần Docker,
không cần backend:

```bash
cd frontend && npm start        # rồi mở http://localhost:4200/login?demo=1
```

Đăng nhập bằng email/mật khẩu **bất kỳ** (demo nhận hết), vào *Quản trị → Thành viên*. Cần
nhìn đúng ba loại dòng: **Đỗ Ngọc Hà** có mã NV005 mà cột vai trò ghi *chưa có tài khoản*;
**backup-bot** có vai Admin mà cột mã ghi *chưa có hồ sơ*; còn lại là người bình thường có
cả hai. Thử ô *Lọc theo loại* — nó lọc tại chỗ, không đi hỏi server lần nào.

Rồi bấm thử phép **nối**: Đỗ Ngọc Hà cố ý xuất hiện **hai lần** — một dòng hồ sơ NV005, một
dòng tài khoản `ha.do@congty.vn`, cùng một người mà chưa nối. Bấm nút ba chấm ở một trong
hai dòng đó → chọn dòng kia → *Nối*. Hai dòng gộp làm một. Mở lại ngăn kéo chi tiết của
người đó thì có nút *Gỡ liên kết* để tách ra như cũ.

Hộp thoại đó có **thẻ thứ hai**: *Tạo tài khoản mới* / *Tạo hồ sơ mới* — dùng khi nửa còn
thiếu chưa tồn tại ở đâu cả. Thử trên **backup-bot**: thẻ hai, điền mã `NV010`, xác nhận —
hồ sơ mới được tạo và nối luôn. Chiều ngược lại (cấp tài khoản cho một hồ sơ) đi qua bước
hiện **mật khẩu tạm**, y như luồng "Thêm người".

Mở ngăn kéo chi tiết của một người có tài khoản (ví dụ **Phạm Hà**): khối **Mật khẩu** nằm
trên vùng nguy hiểm, bấm *Đặt lại mật khẩu* → hộp hỏi → xác nhận → mật khẩu tạm hiện ra
đúng một lần, và cột Trạng thái của họ đổi sang *Chờ nhận tài khoản*.

⚠️ **Cửa chặn quan trọng nhất của tính năng này KHÔNG bấm thử được trên demo**: phiên demo
luôn đăng nhập bằng chính chủ sở hữu, nên không dựng được ca "Admin đặt lại mật khẩu của
Owner". Luật đó chỉ có test canh (`ResetUserPasswordCommandHandlerTests`), chưa có ai bấm
tay qua. Ghi ra để không ai tưởng nó đã được kiểm bằng mắt.

Cuối cùng là **thao tác hàng loạt**: tick vài dòng lẫn lộn cả ba loại → thanh nổi ở đáy →
*Đổi vai trò*. Thứ cần nhìn là câu tóm tắt trong hộp xác nhận — nó phải nói **cả hai** con
số ("sẽ áp cho 4 người · 3 người bị bỏ qua vì chưa có tài khoản, hoặc là chủ sở hữu"), và
nhãn nút mang luôn con số đó.

⬜ **Vẫn CHƯA bấm tay qua màn này với backend thật**, vì Docker trên máy còn kẹt ở lớp thứ
ba (xem bảng bên dưới — cần đăng xuất Windows rồi đăng nhập lại). Chế độ demo mô phỏng phép
gộp giống hệt handler, nhưng nó không chứng minh được SQL và `IUserDirectory` chạy đúng.

Sau đó:

```
🔴 BẤM TAY QUA VÙNG QUẢN TRỊ — khung B chưa từng chạy với dữ liệu thật.
     Cần `docker compose up -d` (máy này Docker Desktop đang TẮT). Sau đó:
     đăng nhập → menu ảnh đại diện → "Quản trị & gói cước" → xem 4 con số.
⬜ Trao đổi (chat) — bản dựng đã xong và rất chi tiết, code thì CHƯA CÓ MỘT DÒNG.
     Trước khi bắt đầu phải sửa hai chỗ `chat.md` nói sai về hệ thống:
       · mục 2 hứa quyền `conversation.read` — Permissions.cs KHÔNG có quyền đó
       · mục 7 hứa SignalR — `Luong.Kernel.Realtime` chưa được tham chiếu ở đâu cả
⬜ Nối bốn gói kernel còn lại: Messaging · Realtime · Caching · Jobs.
     ⚠️ Outbox đang GHI VÀO HƯ KHÔNG: bảng `outbox_messages` được tạo và
     `InsertOutboxMessagesInterceptor` vẫn ghi vào đó, nhưng không có
     `BackgroundService`/`IHostedService` nào trong toàn backend để rút ra.
⬜ Plan / Quota / Usage — ba khái niệm mới, chưa có bảng nào. Màn Tổng quan
     đang vẽ sẵn và đeo nhãn "chưa nối backend".
⬜ Thêm `isOwner` vào `UserListItem` (tính từ `Tenant.OwnerUserId`, không cần
     migration). Frontend đang tô huy hiệu Owner bằng phép so TÊN vai — chấp
     nhận được vì nó chỉ là màu, nhưng ngày nào cần CHẶN theo chủ sở hữu thì
     bắt buộc phải có trường này.
⬜ Bảng điều khiển `/dashboard` KHÔNG có bản dựng, và nội dung hiện tại là màn
     debug (userId, tenantId, danh sách quyền thô). Hoặc vẽ bản dựng cho nó,
     hoặc bỏ hẳn và cho `/` trỏ sang Trao đổi khi màn đó xong.
⬜ `forbidden` và `not-found` cũng chưa có bản dựng — hai màn duy nhất còn lại
     nằm ngoài `ui-parity.spec`.

⬜ Xuất Excel danh sách Thành viên · ghi bộ lọc lên URL (để chia sẻ được một
     danh sách đã lọc, và F5 không mất bộ lọc đang bật)
⬜ GET /api/me/sessions · DELETE — màn "thiết bị đang đăng nhập" trong bản dựng.
     RefreshToken chưa lưu user-agent nên chưa nói được "Chrome trên Windows".
     Cần thêm cột + migration, HOẶC sửa bản dựng cho khớp thực tế.
     (Bản dựng còn hứa cả vị trí "Hà Nội" — cái đó cần GeoIP, là việc riêng.)
⬜ Màn quản lý chat — bản dựng comm/chat.html đã có, chưa duyệt chốt
⬜ RỜI workspace (`POST /api/me/leave`). Chủ sở hữu thì chuyển nhượng trước
     rồi mới rời — đường đó nay đã có. Người thường thì chưa có đường nào:
     họ chỉ "biến mất" khi quản trị viên vô hiệu hoá tài khoản, và đó là
     quyết định của người khác chứ không phải của họ.
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
| `Manager` trùng khít `Member` cho tới khi có `leave.approve` | `SystemRoles.cs`, có test canh — và màn Vai trò nay phơi nó ra: cả hai đúng **1 quyền** |
| `Member` chỉ có `employee.read`, mà quyền đó không chặn route nào | Cần chốt: Member được thấy những gì? |

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

⚠️ **Cổng 5433, không phải 5432.** Trỏ nhầm cổng thì migration chạy vào nhầm database —
và nó sẽ *thành công*, đó mới là chỗ đáng sợ.

> Lý do gốc của con số 5433 là *"máy này đã có một Postgres cài thẳng vào Windows giữ
> 5432"*. **Đo lại ngày 2026-08-25: không còn đúng** — không có service `postgresql*` nào,
> và cả 5432 lẫn 5433 đều đóng. Vẫn giữ 5433 vì đổi cổng nay chỉ tạo ra một đợt lệch giữa
> `docker-compose.yml`, chuỗi kết nối và tài liệu, mà chẳng đổi lại được gì.

### 🐳 Docker Desktop trên máy này — ba cái bẫy đã gặp (2026-08-25)

Engine chết hẳn, và phải gỡ đúng **ba** lớp, mỗi lớp có một thông báo lỗi khác nhau:

| Thông báo | Nguyên nhân thật | Cách gỡ |
|---|---|---|
| `open \\.\pipe\dockerBackendV2:`<br>`The system cannot find the file` | `com.docker.service` **Stopped**, và WSL **không còn distro nào** | `wsl --update` · `wsl --install --no-distribution` · `net start com.docker.service` |
| `Group membership check:`<br>`user is not a member of the group` | Nhóm `docker-users` tồn tại nhưng **rỗng hoàn toàn** | `net localgroup docker-users <máy>\<user> /add` |
| `open \\.\pipe\dockerBackendV2:`<br>**`Access is denied`** | Đã thêm nhóm rồi, nhưng token đăng nhập cũ chưa mang nhóm mới | **Đăng xuất rồi đăng nhập lại** — bắt buộc |

Hai bài học đáng nhớ hơn ba dòng lệnh:

**Thông báo lỗi đổi từ *"không tìm thấy"* sang *"bị từ chối"* là dấu hiệu ĐANG TIẾN TRIỂN**,
không phải vẫn hỏng như cũ. Không tìm thấy = pipe chưa tồn tại. Bị từ chối = pipe đã có,
chỉ thiếu quyền. Đọc lướt thì cả hai đều là "vẫn lỗi" và người ta quay lại làm lại từ đầu.

**Thêm nhóm xong KHÔNG có tác dụng cho tới khi đăng nhập lại.** Windows dựng danh sách nhóm
đúng một lần, lúc tạo phiên đăng nhập. Chạy `as administrator` cũng không cứu được: nâng
quyền chỉ đổi mức toàn vẹn, nó không nạp thêm nhóm mới. Mọi tiến trình con đều thừa hưởng
đúng cái token cũ đó — kể cả Docker Desktop.

### Năm bộ test, năm mục đích khác nhau

| Bộ | Số test | Cần gì | Trả lời câu hỏi |
|---|---|---|---|
| `Identity.UnitTests` | 239 | không | Luật nghiệp vụ của Identity có đúng không |
| `Org.UnitTests` | 69 | không | Luật nghiệp vụ của Org (phòng ban, nhân viên) |
| `ArchitectureTests` | 15 | không | Ranh giới tầng và luật Controller có bị phá không |
| `Api.IntegrationTests` | 32 | không | Pipeline, phân quyền, hình dạng lỗi, i18n có đúng không |
| `Api.DatabaseTests` | 52 | **Docker** | EF ánh xạ, cô lập tenant, luồng đăng nhập/đăng ký/tạo tài khoản có chạy THẬT không |
| `frontend` (vitest) | 181 | không | Hợp đồng với API, luồng gia hạn phiên, bản dịch, bảng màu và **tên biến/lớp CSS** có lệch không |
| `npm run parity` | 2 màn | **Chrome** | Bản Angular trông có **giống hệt bản dựng đã duyệt** không |

Bộ thứ tư tự dựng Postgres bằng **Testcontainers**, không nối vào `docker compose`. Cố ý:
test nối vào compose sẽ im lặng bỏ qua trên máy chưa `up` và trên CI — mà test không chạy
thì tệ hơn cả không có, vì nhìn danh sách vẫn thấy nó nằm đó.

```bash
cd backend  && dotnet build && dotnet test      # 355 xanh không cần Docker · +52 test database nếu có
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

## ⚠️ Hai mươi mốt cái bẫy đã gặp — đừng dẫm lại

### Ba cái gặp khi cho một handler chạm HAI module (2026-08-26)

**`TransactionBehavior` chốt đúng MỘT `IUnitOfWork` — module thứ hai mất dữ liệu, im lặng.**
Behavior của kernel giải một `IUnitOfWork` từ DI và gọi `SaveChanges` trên nó. Handler nào
chạm cả `IdentityDbContext` lẫn `OrgDbContext` thì một bên được lưu, bên kia **không** —
không exception, không log, thay đổi chỉ đơn giản là biến mất. Sửa bằng
`CompositeUnitOfWork` chốt cả hai, đăng ký **sau** cả hai module để nó là bản cuối cùng
thắng. Đã gỡ đăng ký ra một lần để chứng minh test đỏ.
⚠️ Nó là **hai transaction nối tiếp**, không phải một — cái thứ hai hỏng thì cái thứ nhất đã
nằm trong DB rồi. Hai pha thật thì phải qua outbox, và outbox đã có sẵn trong kernel.

**Khoá không có ràng buộc UNIQUE thì phép kiểm trùng phải nằm ở handler — và phía ĐỌC cũng
phải chịu được khi nó vẫn trùng.** `LinkAccountCommandHandler` bản đầu kiểm "hồ sơ này đã
nối chưa" và "tài khoản có thật không", nhưng **không** kiểm "tài khoản này đã có hồ sơ khác
nhận chưa" — mà `Employee.UserId` không phải khoá ngoại và không có UNIQUE, nên database
cũng không canh. Hệ quả ở đầu kia: `GetMembersQueryHandler` dựng `ToDictionary` trên
`UserId`, và hai hồ sơ trùng là **ném `ArgumentException`, cả màn Thành viên trả 500** — một
dòng hỏng làm mù toàn bộ danh sách người, đúng lúc cần nhìn vào đó để sửa. Vá cả hai đầu:
`UserLinkedAsync` ở đường ghi, `ToHashSet` ở đường đọc. Luật rút ra: **mỗi phép kiểm trùng
đặt ở tầng ứng dụng đều phải có một câu hỏi kèm theo — "nếu nó vẫn trùng thì bên đọc chết
kiểu gì?"**

**Nối hai module bằng EMAIL là bẫy, dù email trông y hệt một khoá tự nhiên.** Phòng kinh
doanh dùng chung `sales@congty.vn` thì phép gộp biến hai người thành một dòng, và người
biến mất không để lại dấu vết nào ở đâu cả. Nối bằng `Employee.UserId` — khoá thật, do
người dùng chủ động thiết lập. Cùng lý do, `/api/members` **không phân trang**: gộp hai
nguồn thì không thể phân trang từng nguồn rồi ghép, vì người ở trang sau của nguồn bên kia
sẽ bị coi là *"chưa có tài khoản"* — một câu trả lời **sai**, không phải thiếu.

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
người ngoài
  docs/07-giao-dien/identity/dang-nhap.html   · 4 bộ màu, 5 trạng thái, có LOGO thật
  docs/07-giao-dien/identity/dang-ky.html     · 4 bộ màu, 5 trạng thái

🟢 KHUNG A — rail + cột ngữ cảnh
  docs/07-giao-dien/comm/chat.html            · app MẶC ĐỊNH · 2 kiểu luồng, 5 trạng thái
  docs/07-giao-dien/identity/tai-khoan.html   · hồ sơ — chỉ có rail, không cột

  docs/07-giao-dien/org/danh-ba.html           · danh bạ — tra cứu đồng nghiệp

🔴 KHUNG B — thanh ngang + sidebar
  docs/07-giao-dien/khung/quan-tri.html       · tổng quan · gói & hạn ngạch
  docs/07-giao-dien/org/nhan-su.html          · thành viên — bản đầy đủ nhất, có đủ 3 loại dòng
  docs/07-giao-dien/org/phong-ban.html        · cây tổ chức, sửa được
  docs/07-giao-dien/identity/vai-tro.html     · vai trò & quyền

khác
  docs/07-giao-dien/wireframes.html           · bố cục 6 màn (đơn sắc, có chú thích)
  docs/07-giao-dien/brand/README.md           · logo, bảng màu, khoảng thở
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
node tools/sync-shell.mjs           # bản dựng → styles.scss + 9 file .scss + public/brand/
node tools/sync-error-messages.mjs  # .resx của backend → errors.json của FE
node tools/sync-huongdan.mjs        # docs/08-huong-dan/*.md → public/huong-dan/*.json
cd frontend && npm run parity                 # chụp cả hai rồi so từng điểm ảnh
cd frontend && node tools/chup-huong-dan.mjs  # ảnh minh hoạ ← app đang chạy demo
```

Bộ thứ tư, thêm 2026-08-25: **`ui-parity.spec.ts`** — so tập LỚP CSS giữa bản dựng và
template Angular của **tám cặp màn**, chạy cùng `npm test`. Nó lấp đúng lỗ hổng mà ba bộ
kia để lại: bộ sinh CSS bảo đảm *luật CSS* không lệch nhưng không đụng tới *đánh dấu*, còn
`npm run parity` thì không với tới màn nào sau đăng nhập. Lần chạy đầu nó đỏ ba màn.

Nó KHÔNG bắt được lệch về thứ tự khối hay câu chữ — chỗ đó vẫn phải nhìn bằng mắt. Một bộ
canh biết rõ mình không canh gì thì tốt hơn một bộ canh giả vờ canh tất.

⚠️ **`npm run parity` mới chỉ soi được HAI màn công khai** (`/login`, `/dang-ky`). Mọi màn
sau đăng nhập thì bộ so ảnh chưa với tới được: nó phục vụ bản build tĩnh rồi chụp một URL,
mà `authGuard` đá thẳng về `/login`. Muốn soi được khung A và khung B thì cần **gieo phiên
vào `localStorage` trước khi điều hướng, VÀ có một API giả** — không có API thì bản Angular
hiện trạng thái lỗi trong khi bản dựng hiện dữ liệu, và bộ so sẽ đỏ vì đúng lý do sai.
Chưa làm; ghi ra để không ai tưởng hai khung mới đã được canh.

Lệch thì `parity` ghi ba ảnh vào `frontend/.shots/parity/` — ảnh thứ ba tô đỏ đúng chỗ khác nhau.

---

## 🚀 PROMPT KHỞI ĐỘNG SESSION MỚI

Mở session Claude Code mới **tại `D:/Luong/Person`**, dán nguyên đoạn dưới:

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

VIỆC ĐẦU TIÊN, mất năm phút, KHÔNG cần Docker: cd ONoOffice/frontend && npm start
rồi mở http://localhost:4200/login?demo=1 — đăng nhập bằng email/mật khẩu bất kỳ,
vào Quản trị → Thành viên. Phải nhìn thấy đủ BA loại dòng: có cả hai · chỉ hồ sơ
(Đỗ Ngọc Hà, cột vai trò ghi "chưa có tài khoản") · chỉ tài khoản (backup-bot,
cột mã ghi "chưa có hồ sơ").

Sau đó: LinkAccount (nối hồ sơ vào tài khoản) — cổng IUserDirectory.ExistsAsync
đã sẵn sàng, còn thiếu lệnh + endpoint + hai thao tác trên dòng. Rồi tới màn
quản lý chat. Chưa duyệt thiết kế cái nào: trình bày trước theo luật 1, và vẽ
bản dựng theo luật 8.

Bắt đầu bằng việc chạy `cd ONoOffice/backend && dotnet build && dotnet test`
và `cd ONoOffice/frontend && npm test && npm run parity` để xác nhận
355 + 181 test còn xanh và hai màn vẫn khớp bản dựng, rồi báo tôi trạng thái
trước khi làm gì. 52 test database sẽ ĐỎ nếu chưa `docker compose up -d` —
đó là hỏng môi trường, không phải hỏng code.
```

**Vì sao đoạn prompt này dài như vậy:** session mới không nhớ gì cả. Bốn thứ nó **không thể
tự đoán ra** là (a) luật phải trình bày thiết kế trước, (b) TDD phải thấy đỏ thật,
(c) commit tiếng Anh nhưng hội thoại tiếng Việt, và (d) giao diện phải qua bản dựng
trong `docs/07-giao-dien` rồi mới tới Angular. Thiếu chúng thì session mới sẽ lao vào
code ngay, viết commit tiếng Việt, và vẽ lại một giao diện khác thứ đã duyệt.
