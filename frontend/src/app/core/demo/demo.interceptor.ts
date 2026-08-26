import type { HttpEvent, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { type Observable, of, throwError } from 'rxjs';
import { delay } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { Permissions } from './demo.permissions';
import { kho } from './demo.state';
import { UserStatusFilter, type UserListItem } from '../models/user.model';
import type { DepartmentTreeItem } from '../models/org.model';

/**
 * CHẾ ĐỘ DEMO — bắt mọi request tới `/api/…` và trả dữ liệu giả từ bộ nhớ.
 *
 * ═══════════════════════════════════════════════════════════════════════
 *  VÌ SAO THỨ NÀY TỒN TẠI
 * ═══════════════════════════════════════════════════════════════════════
 *
 * Không có backend thì `authGuard` đá mọi màn sau đăng nhập về `/login`, nên **không ai
 * xem thử được gì ngoài hai màn công khai** — kể cả người đang dựng giao diện. Dựng
 * Postgres chỉ để bấm thử một cái nút là quá đắt, và trên máy chưa cài Docker thì bất khả.
 *
 * ═══════════════════════════════════════════════════════════════════════
 *  BA HÀNG RÀO ĐỂ NÓ KHÔNG BAO GIỜ CHẠY Ở SẢN PHẨM THẬT
 * ═══════════════════════════════════════════════════════════════════════
 *
 * Một API giả lọt vào bản production là chuyện tệ nhất có thể xảy ra với tệp này: người
 * dùng thật đăng nhập được bằng mật khẩu bất kỳ và nhìn thấy dữ liệu bịa. Ba lớp chặn:
 *
 * 1. `environment.demo` là `false` trong `environment.ts` (bản production). Angular thay
 *    file environment lúc build, nên `demoDangBat()` trả `null` ngay dòng đầu và
 *    interceptor này không bao giờ được ĐĂNG KÝ — xem `app.config.ts`.
 * 2. Ngay cả ở bản dev, mặc định vẫn TẮT. Phải bật tay bằng `?demo=1`.
 * 3. Bật rồi thì có một dải màu chạy suốt bề ngang màn hình, không tắt được.
 *
 * ⚠️ **Mã này VẪN nằm trong bundle production** — khoảng 2KB code chết. Bản đầu của chú
 * thích trên viết rằng nó "bị cây rung rụng khỏi bundle"; **sai**, và một phép kiểm grep
 * trên `dist/` đã bắt được ngay. Nó không rụng vì `app.config.ts` tham chiếu tĩnh và
 * `DemoBanner` (do `App` nhập) giữ nó sống. Tính chất an toàn thật sự **không phải** "nó
 * không có mặt" mà là "nó không bao giờ kích hoạt", và điều đó do `demo-safety.spec.ts`
 * canh. Ghi rõ ở đây vì một chú thích sai còn nguy hiểm hơn không có chú thích: người
 * đọc sau sẽ tin rằng lớp bảo vệ đó đã có sẵn.
 *
 * ═══════════════════════════════════════════════════════════════════════
 *  NÓ CỐ Ý KHÔNG MÔ PHỎNG
 * ═══════════════════════════════════════════════════════════════════════
 *
 * · <b>Chữ ký JWT.</b> Token dưới đây đúng HÌNH DẠNG (ba phần, payload base64url) nhưng
 *   chữ ký là rác. Frontend không bao giờ xác minh chữ ký — nó không có khoá và cũng
 *   không nên có. Backend thật thì xác minh, và sẽ từ chối ngay.
 * · <b>Phân quyền.</b> Ở đây mọi tài khoản demo đều là Owner với đủ 12 quyền. Muốn thử
 *   ca "Member không thấy mục Quản trị" thì dùng `?demo=member`.
 * · <b>Cô lập tenant, xoay vòng vé, phát hiện trộm.</b> Ba thứ đó là luật của SERVER, và
 *   mô phỏng chúng ở client sẽ tạo cảm giác an toàn giả. Muốn kiểm thì phải chạy backend
 *   thật — có 52 test database làm đúng việc đó.
 */

/** Vai đang đóng trong phiên demo. Đọc một lần lúc bật. */
type Vai = 'owner' | 'member';

const KHOA = 'onooffice.demo';

/** Trễ giả, mili-giây. Không có nó thì mọi thứ xong tức thì và không ai thấy trạng thái
 *  chờ — mà trạng thái chờ là chỗ giao diện hay hỏng nhất. */
const TRE = 180;

/**
 * Chế độ demo có đang bật không, và đóng vai gì.
 *
 * `?demo=1` bật với vai Owner · `?demo=member` bật với vai Member · `?demo=0` tắt.
 * Lựa chọn ghi xuống `localStorage` để tải lại trang không mất — nếu không thì mỗi lần
 * chuyển màn lại phải gõ lại tham số.
 */
export function demoDangBat(): Vai | null {
  if (!environment.demo) {
    return null;
  }

  const tren = new URLSearchParams(location.search).get('demo');

  if (tren !== null) {
    const vai: Vai | null = tren === '0' ? null : tren === 'member' ? 'member' : 'owner';

    try {
      if (vai) {
        localStorage.setItem(KHOA, vai);
      } else {
        localStorage.removeItem(KHOA);
      }
    } catch {
      /* chế độ ẩn danh — bỏ qua */
    }

    return vai;
  }

  try {
    const luu = localStorage.getItem(KHOA);

    return luu === 'member' ? 'member' : luu === 'owner' ? 'owner' : null;
  } catch {
    return null;
  }
}

/**
 * `?demo=1&auto=1` — vào thẳng, bỏ qua màn đăng nhập.
 *
 * Gieo sẵn vé gia hạn và tên người dùng vào `localStorage`, đúng hai khoá mà
 * `TokenStorage` đọc. `provideAppInitializer` khôi phục phiên ngay sau đó, `/api/auth/refresh`
 * do chính chế độ demo trả lời, và `authGuard` thấy một phiên hợp lệ.
 *
 * Có mặt vì bộ chụp ảnh hướng dẫn (`tools/chup-huong-dan.mjs`) chụp bằng Chrome headless —
 * nó điều hướng tới một URL rồi chụp, không gõ được vào biểu mẫu đăng nhập. Không có lối
 * này thì mọi ảnh hướng dẫn đều là ảnh màn đăng nhập.
 *
 * ⚠️ Nằm sau ĐÚNG hàng rào của chế độ demo: `demoDangBat()` trả `null` khi
 * `environment.demo` là `false`, nên ở bản production hàm này thoát ngay dòng đầu. Nó
 * KHÔNG phải một lối tắt đăng nhập — nó chỉ gieo một vé mà duy nhất API giả chấp nhận.
 */
export function gieoPhienDemo(): void {
  const vai = demoDangBat();

  if (vai === null || new URLSearchParams(location.search).get('auto') !== '1') {
    return;
  }

  try {
    localStorage.setItem('onooffice.refresh-token', `demo-refresh-${vai}`);
    localStorage.setItem(
      'onooffice.user',
      JSON.stringify({
        userId: kho.toi.id,
        tenantId: kho.toi.tenantId,
        email: kho.toi.email,
        fullName: kho.toi.fullName,
        mustChangePassword: false,
      }),
    );
  } catch {
    /* chế độ ẩn danh — bỏ qua, người dùng đăng nhập tay như thường */
  }
}

/** Dựng một JWT đúng hình dạng. Chữ ký là rác, và đó là chủ ý — xem chú thích ở đầu tệp. */
function dungToken(vai: Vai): string {
  const b64 = (o: unknown) =>
    btoa(String.fromCharCode(...new TextEncoder().encode(JSON.stringify(o))))
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=+$/, '');

  const quyen =
    vai === 'owner'
      ? [...Permissions.ALL]
      : // Member có ĐÚNG một quyền — xem `SystemRoles.cs`. Đây là ca đáng thử nhất:
        // cột rail rụng gần hết mục, và menu tài khoản không còn lối vào Quản trị.
        ['employee.read'];

  const payload = {
    sub: kho.toi.id,
    tenant_id: kho.toi.tenantId,
    permission: quyen,
    exp: Math.floor(Date.now() / 1000) + 900,
  };

  return `${b64({ alg: 'none', typ: 'JWT' })}.${b64(payload)}.chu-ky-gia-khong-dung-duoc`;
}

const ok = <T>(body: T): Observable<HttpEvent<T>> =>
  of(new HttpResponse({ status: 200, body })).pipe(delay(TRE));

const trong = (): Observable<HttpEvent<null>> =>
  of(new HttpResponse<null>({ status: 204 })).pipe(delay(TRE));

const loi = (status: number, code: string, detail: string) =>
  throwError(
    () =>
      new HttpErrorResponse({
        status,
        // Đúng khuôn ProblemDetails của backend, kể cả `errors` luôn là MẢNG — frontend
        // rẽ nhánh theo hình dạng này, nên trả sai khuôn thì demo không kiểm được gì.
        error: { type: 'about:blank', title: code, status, detail, code, errors: [] },
      }),
  ).pipe(delay(TRE));

/* ── Ba phép đi cây, dùng chung cho bốn endpoint phòng ban ────────────── */

/** Tìm một nút ở BẤT KỲ độ sâu nào. */
function tim(nodes: DepartmentTreeItem[], id: string): DepartmentTreeItem | null {
  for (const n of nodes) {
    if (n.id === id) {
      return n;
    }

    const trong = tim(n.children, id);

    if (trong) {
      return trong;
    }
  }

  return null;
}

/** So tên KHÔNG phân biệt hoa thường, giống `EfDepartmentRepository.NameTakenAsync`. */
function timTheoTen(nodes: DepartmentTreeItem[], ten: string): DepartmentTreeItem | null {
  const can = ten.toLowerCase();

  for (const n of nodes) {
    if (n.name.toLowerCase() === can) {
      return n;
    }

    const trong = timTheoTen(n.children, ten);

    if (trong) {
      return trong;
    }
  }

  return null;
}

/** Gỡ một nút khỏi cha hiện tại, giữ nguyên nhánh con của nó. */
function go(nodes: DepartmentTreeItem[], id: string): boolean {
  const i = nodes.findIndex((n) => n.id === id);

  if (i >= 0) {
    nodes.splice(i, 1);

    return true;
  }

  return nodes.some((n) => go(n.children, id));
}

/** Mật khẩu tạm sinh theo ràng buộc ĐỌC ĐƯỢC QUA ĐIỆN THOẠI — bỏ `0/O` và `1/l/I`. */
function matKhauTam(): string {
  const bang = 'abcdefghjkmnpqrstuvwxyz23456789';
  const cum = () =>
    Array.from({ length: 4 }, () => bang[Math.floor(Math.random() * bang.length)]).join('');

  return `${cum()}-${cum()}-${cum()}`;
}

function locNguoiDung(req: HttpRequest<unknown>) {
  const p = req.params;
  const search = (p.get('search') ?? '').trim().toLowerCase();
  const status = Number(p.get('status') ?? UserStatusFilter.Any) as UserStatusFilter;
  const page = Math.max(1, Number(p.get('page') ?? 1));
  const pageSize = Math.min(100, Math.max(1, Number(p.get('pageSize') ?? 20)));

  let items = kho.users.filter((u) => {
    if (search && !`${u.fullName} ${u.email}`.toLowerCase().includes(search)) {
      return false;
    }

    switch (status) {
      case UserStatusFilter.Active:
        return u.isActive && !u.mustChangePassword;
      case UserStatusFilter.PendingFirstLogin:
        return u.mustChangePassword;
      case UserStatusFilter.Disabled:
        return !u.isActive;
      default:
        return true;
    }
  });

  // Sắp theo TÊN rồi tới id, giống hệt `EfUserRepository`: sắp xếp phải ổn định, nếu
  // không hai trang liên tiếp có thể trả cùng một người và bỏ sót một người khác.
  items = [...items].sort((a, b) => a.fullName.localeCompare(b.fullName, 'vi') || a.id.localeCompare(b.id));

  const totalCount = items.length;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  return {
    items: items.slice((page - 1) * pageSize, page * pageSize),
    page,
    pageSize,
    totalCount,
    totalPages,
    hasPreviousPage: page > 1,
    hasNextPage: page < totalPages,
  };
}

export const demoInterceptor: HttpInterceptorFn = (req, next) => {
  const vai = demoDangBat();

  // Không bật, hoặc không phải request tới API (ví dụ tệp dịch `.json`) → đi tiếp bình thường.
  if (!vai || !req.url.includes('/api/')) {
    return next(req);
  }

  const duong = new URL(req.url, location.origin).pathname;
  const token = () => dungToken(vai);

  // ── Xác thực ────────────────────────────────────────────────────────
  if (duong === '/api/auth/login' && req.method === 'POST') {
    const body = req.body as { email?: string; password?: string } | null;

    // Chỉ chặn ô rỗng. Cố ý KHÔNG kiểm mật khẩu: đây là chế độ để xem giao diện, và
    // bắt nhớ một mật khẩu bịa chỉ tổ cản. Ca "sai mật khẩu" thử bằng email `sai@`.
    if (!body?.email || !body.password) {
      return loi(400, 'Auth.InvalidCredentials', 'Email hoặc mật khẩu không đúng.');
    }

    if (body.email.startsWith('sai@')) {
      return loi(401, 'Auth.InvalidCredentials', 'Email hoặc mật khẩu không đúng.');
    }

    return ok({
      accessToken: token(),
      refreshToken: `demo-refresh-${Date.now()}`,
      expiresInSeconds: 900,
      user: {
        id: kho.toi.id,
        tenantId: kho.toi.tenantId,
        email: body.email,
        fullName: vai === 'owner' ? kho.toi.fullName : 'Nguyễn An',
        mustChangePassword: false,
      },
    });
  }

  if (duong === '/api/auth/refresh' && req.method === 'POST') {
    return ok({
      accessToken: token(),
      refreshToken: `demo-refresh-${Date.now()}`,
      expiresInSeconds: 900,
    });
  }

  if (duong === '/api/auth/logout' && req.method === 'POST') {
    return trong();
  }

  if (duong === '/api/auth/register-workspace' && req.method === 'POST') {
    const body = req.body as { tenantCode?: string } | null;

    // Ca trùng mã workspace — thử được ô đỏ trên màn đăng ký mà không cần backend.
    if (body?.tenantCode === 'demo') {
      return loi(409, 'TenantCode.Taken', 'Mã workspace này đã có người dùng.');
    }

    return ok({
      accessToken: token(),
      refreshToken: `demo-refresh-${Date.now()}`,
      expiresInSeconds: 900,
      user: { ...kho.toi, mustChangePassword: false },
    });
  }

  // ── Tài khoản của tôi ───────────────────────────────────────────────
  if (duong === '/api/me' && req.method === 'GET') {
    return ok(
      vai === 'owner'
        ? kho.toi
        : { ...kho.toi, id: 'u-an', fullName: 'Nguyễn An', roleName: 'Member', isOwner: false },
    );
  }

  if (duong === '/api/me' && req.method === 'PATCH') {
    const body = req.body as { fullName?: string } | null;

    if (body?.fullName) {
      kho.toi.fullName = body.fullName;
    }

    return trong();
  }

  if (duong === '/api/me/password' && req.method === 'POST') {
    const body = req.body as { currentPassword?: string } | null;

    // Ca "mật khẩu hiện tại sai" — thử được thông báo lỗi mà không cần backend.
    if (body?.currentPassword === 'sai') {
      return loi(400, 'Auth.InvalidCredentials', 'Mật khẩu hiện tại không đúng.');
    }

    return trong();
  }

  // ── Vai trò ─────────────────────────────────────────────────────────
  if (duong === '/api/roles' && req.method === 'GET') {
    return ok(kho.roles);
  }

  // ── Thành viên: GỘP tài khoản + hồ sơ ───────────────────────────────
  if (duong === '/api/members' && req.method === 'GET') {
    // Nối bằng `userId` trên hồ sơ, KHÔNG đoán theo email — giống hệt backend. Đoán theo
    // email thì phòng kinh doanh dùng chung `sales@` sẽ bị gộp thành một dòng.
    const theoUser = new Map(
      kho.hoSo.filter((h) => h.userId).map((h) => [h.userId!, h]),
    );

    const tuHoSo = kho.hoSo.map((h) => {
      const tk = h.userId ? kho.users.find((u) => u.id === h.userId) : undefined;

      return {
        employeeId: h.id,
        userId: tk?.id ?? null,
        fullName: h.fullName,
        code: h.code,
        jobTitle: h.jobTitle,
        email: h.workEmail ?? tk?.email ?? null,
        phone: h.phone,
        departmentId: h.departmentId,
        departmentName: h.departmentName,
        roleName: tk?.roleName ?? null,
        isActive: h.isActive && (tk?.isActive ?? true),
        mustChangePassword: tk?.mustChangePassword ?? false,
      };
    });

    const tuTaiKhoan = kho.users
      .filter((u) => !theoUser.has(u.id))
      .map((u) => ({
        employeeId: null,
        userId: u.id,
        fullName: u.fullName,
        code: null,
        jobTitle: null,
        email: u.email,
        phone: null,
        departmentId: null,
        departmentName: null,
        roleName: u.roleName,
        isActive: u.isActive,
        mustChangePassword: u.mustChangePassword,
      }));

    return ok(
      [...tuHoSo, ...tuTaiKhoan].sort(
        (a, b) => a.fullName.localeCompare(b.fullName, 'vi') || (a.employeeId ?? '').localeCompare(b.employeeId ?? ''),
      ),
    );
  }

  // ── Phòng ban ───────────────────────────────────────────────────────
  if (duong === '/api/departments' && req.method === 'GET') {
    return ok(kho.phongBan);
  }

  if (duong === '/api/departments' && req.method === 'POST') {
    const body = req.body as { name?: string; parentId?: string | null } | null;
    const ten = (body?.name ?? '').trim();

    if (!ten) {
      return loi(400, 'Department.NameEmpty', 'Tên phòng ban không được để trống.');
    }

    if (timTheoTen(kho.phongBan, ten)) {
      return loi(409, 'Department.NameTaken', 'Workspace đã có phòng ban trùng tên này.');
    }

    const moi = {
      id: `d-${Date.now()}`,
      name: ten,
      parentId: body?.parentId ?? null,
      headEmployeeId: null,
      headName: null,
      employeeCount: 0,
      children: [],
    };

    if (moi.parentId === null) {
      kho.phongBan.push(moi);
    } else {
      const cha = tim(kho.phongBan, moi.parentId);

      if (!cha) {
        return loi(404, 'Department.NotFound', 'Không tìm thấy phòng ban.');
      }

      cha.children.push(moi);
    }

    return ok(moi);
  }

  const khopPhong = /^\/api\/departments\/([^/]+)(\/move)?$/.exec(duong);

  if (khopPhong) {
    const id = khopPhong[1];
    const phong = tim(kho.phongBan, id);

    if (!phong) {
      return loi(404, 'Department.NotFound', 'Không tìm thấy phòng ban.');
    }

    // ── Đổi tên ──
    if (!khopPhong[2] && req.method === 'PATCH') {
      const ten = ((req.body as { name?: string } | null)?.name ?? '').trim();

      if (!ten) {
        return loi(400, 'Department.NameEmpty', 'Tên phòng ban không được để trống.');
      }

      // Trùng tên phải LOẠI TRỪ chính nó — nếu không thì đổi "Kỹ thuật" thành "Kỹ thuật"
      // bị từ chối vì trùng với chính mình, và thông báo lỗi đọc như một lời nói dối.
      const trung = timTheoTen(kho.phongBan, ten);

      if (trung && trung.id !== id) {
        return loi(409, 'Department.NameTaken', 'Workspace đã có phòng ban trùng tên này.');
      }

      phong.name = ten;

      return trong();
    }

    // ── Điều chuyển ──
    if (khopPhong[2] && req.method === 'POST') {
      const chaMoi = (req.body as { parentId?: string | null } | null)?.parentId ?? null;

      // Luật quan trọng nhất của màn này: chuyển một phòng vào chính nhánh của nó thì
      // nhánh đó tách khỏi gốc và BIẾN MẤT khỏi cây — dựng cây bắt đầu từ những nút
      // `parentId = null`, mà vòng lặp thì không có nút nào như vậy.
      if (chaMoi !== null && (chaMoi === id || tim(phong.children, chaMoi))) {
        return loi(
          409,
          'Department.WouldCreateCycle',
          'Không thể chuyển một phòng ban vào bên trong chính nhánh của nó.',
        );
      }

      go(kho.phongBan, id);
      phong.parentId = chaMoi;

      if (chaMoi === null) {
        kho.phongBan.push(phong);
      } else {
        tim(kho.phongBan, chaMoi)!.children.push(phong);
      }

      return trong();
    }

    // ── Xoá ──
    if (!khopPhong[2] && req.method === 'DELETE') {
      if (phong.children.length > 0) {
        return loi(
          409,
          'Department.HasChildren',
          'Phòng ban còn phòng ban con. Hãy chuyển hoặc xoá các phòng con trước.',
        );
      }

      // Đếm cả người đã nghỉ: hồ sơ của họ vẫn trỏ vào phòng này, và xoá phòng đi thì
      // mất luôn thông tin "từng làm ở đâu".
      if (kho.hoSo.some((h) => h.departmentId === id)) {
        return loi(
          409,
          'Department.HasEmployees',
          'Phòng ban còn nhân viên. Hãy điều chuyển họ sang phòng khác trước.',
        );
      }

      go(kho.phongBan, id);

      return trong();
    }
  }

  // ── Tạo hồ sơ nhân sự ───────────────────────────────────────────────
  if (duong === '/api/employees' && req.method === 'POST') {
    const than = req.body as { code?: string; fullName?: string; jobTitle?: string | null } | null;
    const ma = (than?.code ?? '').trim().toUpperCase();

    // VIẾT HOA rồi mới kiểm trùng, giống `Employee.Create`. Kiểm trên chuỗi thô thì
    // "nv001" và "NV001" lọt thành hai người, và ràng buộc UNIQUE thật mới nổ sau đó.
    if (ma === '') {
      return loi(400, 'Employee.CodeEmpty', 'Mã nhân viên không được để trống.');
    }

    if (kho.hoSo.some((h) => h.code.toUpperCase() === ma)) {
      return loi(409, 'Employee.CodeTaken', 'Workspace đã có nhân viên mang mã này.');
    }

    const moi = {
      id: `e-${ma.toLowerCase()}`,
      code: ma,
      fullName: than?.fullName ?? '',
      jobTitle: than?.jobTitle ?? null,
      workEmail: null,
      phone: null,
      departmentId: null,
      departmentName: null,
      isActive: true,
      userId: null,
    };

    kho.hoSo.push(moi);

    return ok({ id: moi.id, code: moi.code, fullName: moi.fullName });
  }

  // ── Điều chuyển phòng ban ───────────────────────────────────────────
  {
    const chuyen = /^\/api\/employees\/([^/]+)\/transfer$/.exec(duong);

    if (chuyen && req.method === 'POST') {
      const hoSo = kho.hoSo.find((h) => h.id === chuyen[1]);

      if (!hoSo) {
        return loi(404, 'Employee.NotFound', 'Không tìm thấy hồ sơ nhân sự.');
      }

      const phongId = (req.body as { departmentId?: string | null } | null)?.departmentId ?? null;

      // Chuyển vào đúng phòng đang ở là bị TỪ CHỐI, không phải cho qua im lặng — cho qua
      // thì nhật ký thay đổi đầy những dòng "điều chuyển" mà không có gì đổi.
      if (phongId === hoSo.departmentId) {
        return loi(
          409,
          'Employee.AlreadyInThatDepartment',
          'Nhân viên đã ở phòng ban này rồi.',
        );
      }

      if (phongId !== null && tim(kho.phongBan, phongId) === null) {
        return loi(404, 'Department.NotFound', 'Không tìm thấy phòng ban.');
      }

      hoSo.departmentId = phongId;
      hoSo.departmentName = phongId === null ? null : tim(kho.phongBan, phongId)!.name;

      return trong();
    }
  }

  // ── Nối / gỡ hồ sơ ↔ tài khoản ──────────────────────────────────────
  //
  // Mô phỏng đủ HAI phép kiểm của backend, không chỉ phép nối. Bỏ bớt một cái thì demo dạy
  // người dùng một hành vi mà sản phẩm thật không có — và họ chỉ phát hiện ra khi bấm trên
  // hệ thống thật, lúc đó lỗi trông như hỏng hóc chứ không như một luật.
  {
    const noi = /^\/api\/employees\/([^/]+)\/(link|unlink)-account$/.exec(duong);

    if (noi && req.method === 'POST') {
      const hoSo = kho.hoSo.find((h) => h.id === noi[1]);

      if (!hoSo) {
        return loi(404, 'Employee.NotFound', 'Không tìm thấy hồ sơ nhân sự.');
      }

      if (noi[2] === 'unlink') {
        if (hoSo.userId === null) {
          return loi(409, 'Employee.NotLinked', 'Hồ sơ này chưa nối với tài khoản nào.');
        }

        hoSo.userId = null;

        return trong();
      }

      const userId = (req.body as { userId?: string } | null)?.userId ?? '';

      if (hoSo.userId !== null) {
        return loi(
          409,
          'Employee.AlreadyLinked',
          'Hồ sơ này đã nối với một tài khoản. Hãy gỡ liên kết cũ trước.',
        );
      }

      if (!kho.users.some((u) => u.id === userId)) {
        return loi(404, 'User.NotFound', 'Không tìm thấy tài khoản.');
      }

      // Một tài khoản chỉ thuộc về MỘT người. Thiếu phép kiểm này thì hai hồ sơ cùng "là"
      // một tài khoản, và mọi thao tác lên tài khoản đó hiện ở cả hai dòng.
      if (kho.hoSo.some((h) => h.userId === userId)) {
        return loi(409, 'Employee.UserAlreadyLinked', 'Tài khoản này đã nối với một hồ sơ khác.');
      }

      hoSo.userId = userId;

      return trong();
    }
  }

  // ── Danh bạ ─────────────────────────────────────────────────────────
  if (duong === '/api/contacts' && req.method === 'GET') {
    const p = req.params;
    const tim = (p.get('search') ?? '').trim().toLowerCase();
    const phong = p.get('departmentId');
    const caNghi = p.get('includeInactive') === 'true';

    const items = kho.hoSo
      .filter((h) => (caNghi || h.isActive)
        && (!phong || h.departmentId === phong)
        && (!tim
          || `${h.fullName} ${h.code} ${h.workEmail ?? ''}`.toLowerCase().includes(tim)))
      // Sắp theo TÊN rồi tới id, giống hệt `EfEmployeeRepository`: sắp xếp phải ổn định.
      .sort((a, b) => a.fullName.localeCompare(b.fullName, 'vi') || a.id.localeCompare(b.id));

    return ok({
      items,
      page: 1,
      pageSize: 60,
      totalCount: items.length,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    });
  }

  // ── Người dùng ──────────────────────────────────────────────────────
  if (duong === '/api/users' && req.method === 'GET') {
    return ok(locNguoiDung(req));
  }

  if (duong === '/api/users' && req.method === 'POST') {
    const body = req.body as { fullName: string; email: string; roleId: string } | null;

    if (!body) {
      return loi(400, 'Validation.Failed', 'Thiếu dữ liệu.');
    }

    if (kho.users.some((u) => u.email.toLowerCase() === body.email.toLowerCase())) {
      return loi(409, 'Email.Taken', 'Email này đã có người dùng.');
    }

    const roleName = kho.roles.find((r) => r.id === body.roleId)?.name ?? 'Member';
    const moi: UserListItem = {
      id: `u-${Date.now()}`,
      email: body.email,
      fullName: body.fullName,
      isActive: true,
      mustChangePassword: true,
      roleName,
      createdAtUtc: new Date().toISOString(),
    };

    kho.users.push(moi);

    return ok({
      id: moi.id,
      email: moi.email,
      fullName: moi.fullName,
      roleName,
      temporaryPassword: matKhauTam(),
    });
  }

  const khop = /^\/api\/users\/([^/]+)(\/(enable|disable|role|reset-password))?$/.exec(duong);

  if (khop) {
    const nguoi = kho.users.find((u) => u.id === khop[1]);

    if (!nguoi) {
      return loi(404, 'User.NotFound', 'Không tìm thấy tài khoản.');
    }

    // Đặt lại mật khẩu HỘ. Hai cửa chặn dưới đây đều là chuyện AN TOÀN, không phải tiện
    // dụng — bỏ chúng ở demo thì demo dạy người dùng một hành vi mà hệ thật từ chối, và
    // tệ hơn: nó dạy rằng ranh giới Admin ↔ Owner lỏng hơn thực tế.
    if (khop[3] === 'reset-password' && req.method === 'POST') {
      // Đặt lại mật khẩu của ai đó = đăng nhập được dưới danh nghĩa người đó. Admin thiếu
      // đúng một quyền so với Owner (chuyển nhượng workspace); cho họ đặt lại mật khẩu
      // của Owner thì họ đăng nhập thành Owner rồi tự chuyển nhượng.
      if (nguoi.roleName === 'Owner' && nguoi.id !== kho.toi.id) {
        return loi(
          409,
          'User.CannotResetOwnerPassword',
          'Không thể đặt lại mật khẩu của chủ sở hữu. Chỉ chính họ làm được việc đó.',
        );
      }

      if (nguoi.id === kho.toi.id && nguoi.roleName !== 'Owner') {
        return loi(
          409,
          'User.CannotResetOwnPassword',
          'Hãy đổi mật khẩu của chính bạn ở màn Hồ sơ — ở đó có kiểm mật khẩu hiện tại.',
        );
      }

      nguoi.mustChangePassword = true;

      return ok({
        id: nguoi.id,
        email: nguoi.email,
        fullName: nguoi.fullName,
        temporaryPassword: matKhauTam(),
      });
    }

    // Đổi CHỈ vai trò — đường riêng, cố ý không mang tên. Xem `UsersController.ChangeRole`.
    if (khop[3] === 'role' && req.method === 'POST') {
      // Cùng luật với `PATCH`: không hạ được vai chủ sở hữu, vì họ là người DUY NHẤT
      // chuyển nhượng được workspace. Bỏ phép kiểm này ở demo thì thao tác hàng loạt trên
      // demo làm được một việc mà hệ thật từ chối.
      if (nguoi.roleName === 'Owner') {
        return loi(409, 'User.CannotChangeOwnerRole', 'Không thể đổi vai trò của chủ sở hữu.');
      }

      const roleId = (req.body as { roleId?: string } | null)?.roleId;
      const vai = kho.roles.find((r) => r.id === roleId);

      if (!vai) {
        return loi(404, 'Role.NotFound', 'Không tìm thấy vai trò.');
      }

      nguoi.roleName = vai.name;

      return trong();
    }

    if (khop[3] && req.method === 'POST') {
      // Hai luật CHẶN WORKSPACE TỰ KHOÁ CHÍNH MÌNH, mô phỏng vì giao diện phải xử lý
      // được câu trả lời của chúng — ẩn nút, hiện thông báo.
      if (khop[3] === 'disable' && nguoi.roleName === 'Owner') {
        return loi(409, 'User.CannotDisableOwner', 'Không thể vô hiệu hoá chủ sở hữu.');
      }

      if (khop[3] === 'disable' && nguoi.id === kho.toi.id) {
        return loi(409, 'User.CannotDisableSelf', 'Không thể tự vô hiệu hoá chính mình.');
      }

      nguoi.isActive = khop[3] === 'enable';

      return trong();
    }

    if (req.method === 'PATCH') {
      const body = req.body as { fullName?: string; roleId?: string } | null;

      if (body?.fullName) {
        nguoi.fullName = body.fullName;
      }

      if (body?.roleId) {
        nguoi.roleName = kho.roles.find((r) => r.id === body.roleId)?.name ?? nguoi.roleName;
      }

      return trong();
    }
  }

  // Endpoint chưa mô phỏng: trả 501 kèm tên đường dẫn, KHÔNG im lặng đi tiếp ra mạng.
  // Đi tiếp thì request rơi vào `ERR_CONNECTION_REFUSED` và người thử tưởng giao diện
  // hỏng, trong khi thật ra chỉ là chỗ này chưa viết.
  return loi(501, 'Demo.NotImplemented', `Chế độ demo chưa mô phỏng ${req.method} ${duong}.`);
};
