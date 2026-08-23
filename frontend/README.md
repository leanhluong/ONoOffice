# ONoOffice — Frontend

Giao diện web của ONoOffice, viết bằng **Angular 22** với standalone component
(không dùng NgModule), SCSS, strict mode, không SSR.

Tài liệu này giải thích **vì sao** mọi thứ được sắp như hiện tại, chứ không chỉ
liệt kê có gì. Đọc mục nào cũng được, nhưng nếu bạn mới vào dự án thì đọc theo
thứ tự sẽ dễ hiểu hơn.

---

## 1. Chạy dự án

```bash
npm install          # lần đầu
npm start            # dev server tại http://localhost:4200
```

| Lệnh                   | Việc nó làm                                 |
| ---------------------- | ------------------------------------------- |
| `npm start`            | Dev server, tự nạp lại khi sửa file         |
| `npm run build`        | Build production vào `dist/onooffice-web`   |
| `npm run watch`        | Build lại liên tục ở chế độ development     |
| `npm test`             | Chạy unit test (Vitest)                     |
| `npm run lint`         | ESLint cho cả `.ts` lẫn template `.html`    |
| `npm run lint:fix`     | ESLint + tự sửa những lỗi sửa được          |
| `npm run format`       | Prettier định dạng toàn bộ mã nguồn         |
| `npm run format:check` | Kiểm định dạng mà không sửa — dùng trong CI |

**Yêu cầu:** Node 20+ (đang phát triển trên Node 24). npm đi kèm Node là đủ.

### Trỏ tới backend

Địa chỉ backend nằm ở `src/environments/`:

| File                         | Dùng khi        | `apiBaseUrl`            |
| ---------------------------- | --------------- | ----------------------- |
| `environment.development.ts` | `npm start`     | `http://localhost:5000` |
| `environment.ts`             | `npm run build` | `''` (cùng origin)      |

Bản production để rỗng, nghĩa là request đi tới `/api/...` trên chính domain
đang chạy và do reverse proxy chuyển tiếp về backend .NET. Cách này tránh được
CORS, và quan trọng hơn: đổi domain backend không phải build lại frontend. Nếu
sau này FE và BE tách hẳn hai domain thì điền URL tuyệt đối vào đó.

Kiểu `AppEnvironment` (trong `environment.model.ts`) được cả hai file dùng chung,
nên thêm biến mới mà quên cập nhật một file thì TypeScript báo lỗi ngay — không
đợi tới lúc chạy production mới phát hiện thiếu.

---

## 2. Cấu trúc thư mục

```
src/app/
├── core/              · khởi tạo MỘT lần cho cả app (singleton)
│   ├── auth/          · auth.service, auth.store, token.storage, jwt.util
│   ├── http/          · 3 interceptor + bộ chuyển đổi Problem Details
│   ├── guards/        · auth.guard, permission.guard
│   └── models/        · api-error.model, auth.model
├── shared/            · thứ dùng lại nhiều nơi, KHÔNG giữ trạng thái
│   ├── ui/alert/      · khối thông báo lỗi/cảnh báo
│   └── directives/    · *appHasPermission
├── features/          · từng màn hình nghiệp vụ, lazy load
│   ├── auth/login/
│   ├── dashboard/
│   ├── employees/     · trang giữ chỗ, dùng để chứng minh permission.guard chạy thật
│   └── errors/        · forbidden (403), not-found (404)
└── layout/shell/      · khung sidebar + topbar của phần đã đăng nhập
```

### Vì sao chia ba tầng core / shared / features

Ranh giới giữa ba thư mục này không phải để cho gọn mắt, mà là để trả lời được
một câu hỏi: **sửa file này thì hỏng những đâu?**

- **`core/`** — thứ chỉ tồn tại đúng một bản trong cả app: phiên đăng nhập, cấu
  hình HTTP, guard. Sửa ở đây ảnh hưởng toàn hệ thống, nên phải cẩn thận. Đổi
  lại, nó là chỗ duy nhất chứa những quyết định chung, không rải rác khắp nơi.
- **`shared/`** — component, directive, pipe không giữ trạng thái, ai gọi cũng
  được. Quy tắc: nếu một thứ trong `shared/` cần biết "người dùng hiện tại là
  ai" thì nó đặt sai chỗ — trừ `*appHasPermission`, thứ cố ý phá luật này vì
  bản chất công việc của nó là đọc quyền.
- **`features/`** — mỗi thư mục là một màn hình hoặc một nhóm màn hình nghiệp
  vụ. Feature được phép dùng `core` và `shared`, nhưng **không được import chéo
  sang feature khác**. Đây là điều luật quan trọng nhất: nó giữ cho việc xoá
  một module không kéo sập ba module khác, và cho phép nhiều người làm nhiều
  feature cùng lúc mà không đụng file của nhau.
- **`layout/`** — khung ngoài. Tách khỏi `features/` vì nó không phải nghiệp vụ,
  và tách khỏi `shared/` vì nó chỉ dùng đúng một lần.

### Vì sao `layout/shell` là route cha, không phải component bọc từng màn

Phần app đã đăng nhập nằm dưới **một** route cha rỗng gắn `Shell` + `authGuard`:

```ts
{ path: '', canActivate: [authGuard], loadComponent: () => Shell, children: [...] }
```

Hai cái lợi:

1. **Chuyển trang không dựng lại sidebar.** Chỉ phần `<router-outlet>` bên trong
   được vẽ lại, nên sidebar giữ nguyên vị trí cuộn và trạng thái đang mở.
2. **Không thể quên gắn guard.** Thêm màn mới chỉ là thêm một mục vào `children`;
   `authGuard` nằm ở cha nên tự động có hiệu lực. Chống lỗi bằng cấu trúc, chứ
   không trông vào việc người viết code nhớ ra.

Còn `app.ts` (component gốc) cố ý chỉ có `<router-outlet />`. Màn đăng nhập và
phần app đã đăng nhập có bố cục khác hẳn nhau — nhét sidebar vào gốc thì màn
login cũng dính theo.

### Vì sao mọi màn hình đều lazy load

Mỗi route dùng `loadComponent`. Người chưa đăng nhập chỉ tải bundle của màn
login, không kéo theo toàn bộ ứng dụng. Với app nội bộ sẽ phình ra nhiều module
theo thời gian, khoảng cách này lớn dần. Kết quả build hiện tại:

```
Initial total  275 kB   (77 kB sau nén)
login           47 kB · shell 5.4 kB · dashboard 3.1 kB · ...
```

---

## 3. Ba interceptor

Đăng ký trong `app.config.ts`. **Thứ tự là một phần của thiết kế, không phải
ngẫu nhiên:**

```
request:   correlationId → auth → error → [mạng]
response:  [mạng] → error → auth → correlationId
```

| Interceptor                     | Việc nó làm                                                      |
| ------------------------------- | ---------------------------------------------------------------- |
| `correlation-id.interceptor.ts` | Gắn `X-Correlation-Id` (UUID) vào mọi request                    |
| `auth.interceptor.ts`           | Gắn `Authorization: Bearer <token>` khi có phiên đăng nhập       |
| `error.interceptor.ts`          | Chuyển lỗi HTTP thành `AppError` thống nhất, xử lý 401 tập trung |

### `correlation-id.interceptor`

Sinh một UUID cho mỗi request. Khi người dùng báo "bấm nút thì lỗi", ta hỏi họ
mã hiển thị trên màn hình rồi grep đúng chuỗi đó trong log backend là ra toàn bộ
đường đi của request qua các service — nhanh hơn nhiều so với mò theo mốc thời
gian.

Nếu request đã tự đặt sẵn header thì giữ nguyên, đúng quy ước của backend: có
sẵn thì dùng lại, không có thì tự sinh.

`crypto.randomUUID` chỉ có trong secure context (https hoặc localhost), nên khi
chạy http trên IP nội bộ phải có phương án dự phòng — id này chỉ để tra log, không
cần bảo mật, chỉ cần đủ khác nhau.

### `auth.interceptor`

Gắn Bearer token. Có ba trường hợp **cố ý bỏ qua**:

1. Request đã tự đặt `Authorization` — người gọi biết rõ họ đang làm gì.
2. Endpoint đăng nhập / refresh — gửi token cũ (có thể đã hết hạn) tới đó chỉ
   gây nhiễu, thậm chí khiến backend từ chối sớm.
3. **URL trỏ ra ngoài backend của mình** (CDN, dịch vụ bên thứ ba). Đây không
   phải chi tiết vụn vặt: rò access token ra domain lạ là lỗ hổng bảo mật thật.

### `error.interceptor`

Backend trả lỗi theo RFC 7807 Problem Details:

```json
{
  "type": "...",
  "title": "Conflict",
  "status": 409,
  "errors": [{ "code": "Employee.EmailTaken", "description": "Email đã có người dùng." }]
}
```

Interceptor chuyển nó thành `AppError` — kiểu lỗi duy nhất mà toàn app dùng:

```ts
interface AppError {
  kind:
    | 'network'
    | 'validation'
    | 'unauthorized'
    | 'forbidden'
    | 'not-found'
    | 'conflict'
    | 'server'
    | 'unknown';
  status: number;
  code: string; // errors[0].code, luôn có giá trị
  message: string; // errors[0].description, luôn có giá trị
  details: ProblemDetailItem[];
  fieldErrors: Record<string, string[]>; // từ dictionary ModelState
  correlationId: string | null; // để tra log
}
```

**Vì sao phải gom về một kiểu:** nếu không, mỗi component lại tự bóc
`err.error?.errors?.[0]?.description` theo một cách khác nhau. Backend đổi format
một lần là hỏng khắp nơi, mà lại hỏng âm thầm — chỉ hiện ra dưới dạng thông báo
lỗi trống trơn. Sau interceptor này, component chỉ cần `error.message` để hiện và
`error.code` để rẽ nhánh, còn hình dạng Problem Details chỉ có đúng một file biết
(`problem-details.mapper.ts`).

Bộ chuyển đổi nhận cả hai hình dạng `errors`: mảng `{code, description}` của
ONoOffice và dictionary `{ "Email": ["..."] }` mà validation tự động của
ASP.NET Core sinh ra. Trường hợp thứ hai được đổ vào `fieldErrors` với tên
trường hạ chữ cái đầu, để khớp thẳng với tên control trong reactive form.

**Xử lý 401:** xoá phiên rồi đưa về `/login?returnUrl=...`, để đăng nhập xong
quay lại đúng chỗ đang dở. **Ngoại trừ chính request đăng nhập** — sai mật khẩu
cũng trả 401, mà đá người dùng ra khỏi trang họ đang đứng thì vô lý; lỗi đó phải
hiện ngay trên form.

**Vì sao `error` đứng cuối:** ở chiều response nó là interceptor chạm vào lỗi
đầu tiên, nên bọc được cả lỗi phát sinh từ hai interceptor kia. Đảo thứ tự thì
sẽ có lỗi lọt ra ngoài dưới dạng `HttpErrorResponse` thô, và component nào không
lường trước sẽ hiện thông báo rỗng.

---

## 4. Trạng thái đăng nhập — `AuthStore`

Viết bằng **signal** của Angular, không dùng NgRx.

**Vì sao:** state ở đây chỉ có đúng **một** nguồn sự thật — access token — và mọi
thứ khác đều suy ra được từ nó bằng `computed`:

```ts
private readonly sessionState = signal<AuthSession | null>(this.storage.read());

readonly user            = computed(() => this.sessionState()?.user ?? null);
readonly permissions     = computed(() => new Set(this.sessionState()?.permissions ?? []));
readonly isAuthenticated = computed(() => this.sessionState() !== null);
```

Dựng cả bộ action / reducer / effect cho một object là chi phí không đổi lại được
gì. Signal còn cho phép template đọc thẳng (`store.user()`) mà không cần
`async` pipe, và tự động cập nhật khi phiên đổi.

`permissions` dùng `Set` chứ không phải mảng vì kiểm tra quyền là thao tác chạy
rất nhiều lần — mỗi mục menu, mỗi nút, mỗi lần chuyển route.

**Lưu token:** `TokenStorage` là nơi **duy nhất** trong app đụng tới
`localStorage`. Nếu sau này đổi sang cookie HttpOnly thì chỉ sửa một file;
`AuthStore` và `AuthService` không biết token nằm ở đâu. Chọn `localStorage` vì
refresh token sống 30 ngày và mở lại tab phải còn đăng nhập — đánh đổi là token
đọc được bằng JavaScript (rủi ro XSS). Cách chặn triệt để là cookie HttpOnly,
nhưng việc đó cần backend đổi cách trả token.

**Giải mã JWT:** `jwt.util.ts` đọc claim `sub`, `tenant_id`, `permission[]`.
Đây **không phải** xác thực chữ ký — frontend không có public key và cũng không
nên có. Claim đọc ở đây chỉ để vẽ giao diện. Người dùng hoàn toàn có thể sửa
localStorage để "thấy" thêm menu, nhưng API sẽ trả 403 vì backend tự kiểm lại
permission từ token đã ký.

---

## 5. Phân quyền — theo permission, không theo role

Cả `permissionGuard` lẫn `*appHasPermission` đều kiểm **permission** (`employee.read`),
không kiểm role.

**Vì sao:** role chỉ là cái tên gộp nhiều quyền lại. Hôm nay "Trưởng phòng" được
xem lương, mai công ty đổi ý — nếu code viết `if (role === 'Manager')` thì phải
sửa và build lại frontend. Kiểm theo permission thì admin chỉ cần gỡ quyền trong
trang phân quyền là xong. Backend cũng kiểm theo permission, hai bên nói cùng
một ngôn ngữ.

**Chặn route:**

```ts
{ path: 'employees', canActivate: [permissionGuard('employee.read')], loadComponent: ... }

// hoặc khai trong data khi cần cấu hình động:
{ path: 'employees', canActivate: [permissionGuard()],
  data: { permissions: ['employee.read', 'employee.write'], permissionMode: 'all' } }
```

`permissionMode` mặc định là `'any'` (có một quyền là đủ), đặt `'all'` để yêu cầu
đủ mọi quyền.

**Ẩn/hiện trong template:**

```html
<button *appHasPermission="'employee.create'">Thêm nhân viên</button>
<a *appHasPermission="['employee.read', 'employee.write']">Nhân sự</a>
```

Directive này là bản song sinh của guard ở tầng template: guard chặn cả route,
directive chỉ giấu từng nút. Cả hai đọc chung `AuthStore` nên không sợ hai nơi
hiểu quyền khác nhau.

**Khi thiếu quyền, người dùng bị đưa sang `/forbidden`, không phải `/login`.**
Họ đã đăng nhập rồi — bắt đăng nhập lại chẳng giải quyết được gì, chỉ làm họ
bối rối. Màn 403 hiện luôn tên quyền còn thiếu để họ copy gửi cho admin.

Nhắc lại: guard và directive chỉ để giao diện đỡ khó chịu, **không phải hàng rào
bảo mật**. Hàng rào thật nằm ở backend.

---

## 6. Màn đăng nhập

`features/auth/login/` — reactive form, hai ô email + mật khẩu.

**Vì sao reactive chứ không template-driven:** form này cần gắn lỗi từ server vào
từng ô (`setErrors({ server: '...' })`), cần khoá toàn bộ form khi đang gửi, và
cần kiểm tra trạng thái trong code. Template-driven làm được nhưng phải luồn
`@ViewChild` lòng vòng.

Vài chi tiết cố ý:

- **Lỗi chỉ hiện sau khi người dùng chạm vào ô** (`touched`). Bôi đỏ cả form ngay
  lúc vừa mở, khi họ chưa gõ chữ nào, là kiểu trải nghiệm khó chịu.
- **Bấm gửi khi form sai thì đánh dấu `touched` toàn bộ**, để mọi lỗi hiện ra
  cùng lúc — thay vì sửa xong ô này lại lòi ra lỗi ô khác.
- **`returnUrl` chỉ nhận đường dẫn nội bộ** (bắt đầu bằng `/`, không phải `//`).
  Không chặn thì kẻ xấu gửi link `?returnUrl=https://site-gia-mao` để lừa chuyển
  hướng ngay sau khi đăng nhập thành công.
- **Nhánh mặc định không tách "sai email" với "sai mật khẩu".** Để lộ email nào
  tồn tại là giúp kẻ xấu dò danh sách tài khoản.

### ⚠️ Chưa gọi được API thật

Backend **chưa có** `POST /api/auth/login`. Luồng trong `AuthService.login()` đã
nối đầy đủ và gọi đúng URL, nhưng **chưa ai chạy thử với server thật**. Bấm nút
đăng nhập lúc này sẽ báo lỗi kết nối — đó là hành vi đúng ở giai đoạn hiện tại.

Khi backend lên, phải đối chiếu lại bốn điểm (đã ghi trong comment ở
`auth.service.ts` và `login.ts`):

1. Tên trường response: `accessToken` / `refreshToken` / `expiresIn`
2. Tên claim trong token: `sub` / `tenant_id` / `permission`
3. Mã lỗi khi sai mật khẩu — đối chiếu với `mapServerError()` trong `login.ts`
4. Hình dạng `errors` trong Problem Details (mảng hay dictionary)

`AuthService.refresh()` cũng đã viết sẵn nhưng **chưa được gọi ở đâu**: việc tự
động làm mới token khi gặp 401 sẽ làm sau, khi đã chốt được hành vi thật của
backend (có xoay vòng refresh token hay không).

---

## 7. Thêm một feature mới

Ví dụ thêm module "Phòng họp":

**1. Tạo component.** Đặt trong `features/`, một thư mục một feature:

```bash
npx ng generate component features/meeting-rooms/meeting-room-list --change-detection OnPush
```

**2. Khai route** trong `app.routes.ts`, đặt vào `children` của route cha có
`Shell`:

```ts
{
  path: 'meeting-rooms',
  canActivate: [permissionGuard('meetingroom.read')],
  loadComponent: () =>
    import('./features/meeting-rooms/meeting-room-list').then((m) => m.MeetingRoomList),
}
```

Dùng `loadComponent` (lazy), không import trực tiếp — giữ bundle khởi động nhỏ.

**3. Thêm vào menu** trong `layout/shell/shell.ts`, mảng `navItems`:

```ts
{ label: 'Phòng họp', path: '/meeting-rooms', icon: '▤', permissions: ['meetingroom.read'] }
```

Menu khai báo bằng dữ liệu chứ không viết tay từng thẻ `<a>`, nên quyền của mục
nằm ngay cạnh đường dẫn — khó mà quên gắn.

**4. Gọi API.** Viết service ngay trong thư mục feature (không nhét vào `core/`
— `core/` chỉ dành cho thứ singleton dùng chung):

```ts
@Injectable({ providedIn: 'root' })
export class MeetingRoomService {
  private readonly http = inject(HttpClient);

  list(): Observable<MeetingRoom[]> {
    return this.http.get<MeetingRoom[]>(`${environment.apiBaseUrl}/api/meeting-rooms`);
  }
}
```

Không cần tự gắn token, không cần tự gắn correlation id, không cần tự bóc lỗi —
ba interceptor đã lo. Chỉ cần bắt `AppError` ở chỗ hiển thị:

```ts
this.service.list().subscribe({
  error: (err: AppError) => this.error.set(err),
});
```

**5. Quy tắc phải giữ:** feature mới **không được import từ feature khác**. Cần
dùng chung thì đưa thứ đó lên `shared/` (component/pipe không trạng thái) hoặc
`core/` (dịch vụ singleton).

**6. Trước khi giao việc:** chạy `npm run lint` và `npm run build`, cả hai phải
sạch.

---

## 8. Chuẩn mã nguồn

**ESLint lo nội dung, Prettier lo hình thức.** `eslint-config-prettier` đặt cuối
mảng `extends` để tắt mọi rule ESLint đụng tới định dạng — nếu đặt trước, các
preset sau lại bật chúng lên và hai công cụ sẽ đánh nhau ở mỗi lần lưu file.

Vài rule đáng chú ý trong `eslint.config.js`:

- `@typescript-eslint/no-explicit-any: error` — khi hợp đồng API còn chưa chốt,
  `unknown` buộc phải kiểm kiểu trước khi dùng. Đó chính là chỗ dễ sinh lỗi lúc
  backend đổi tên field.
- `no-console` — chỉ cho `warn` và `error`. `console.log` lọt lên production là
  rác, có khi còn lộ dữ liệu người dùng.
- Selector bắt buộc tiền tố `app-` để không đụng tên với thư viện nhúng sau này.

TypeScript chạy `strict` + `noUnusedLocals` + `noUnusedParameters`, Angular bật
`strictTemplates` để bắt lỗi kiểu ngay trong file HTML.

**Về màu sắc:** dùng CSS custom property (`--color-accent`) khai trong
`styles.scss`, không dùng biến SCSS. Biến SCSS bị "nướng" cứng lúc build, còn
custom property đọc được lúc chạy — sau này làm chế độ tối hoặc đổi màu theo
tenant chỉ cần ghi đè trên `:root`.
