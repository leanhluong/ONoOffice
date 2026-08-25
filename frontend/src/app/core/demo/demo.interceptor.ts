import type { HttpEvent, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { type Observable, of, throwError } from 'rxjs';
import { delay } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { Permissions } from './demo.permissions';
import { kho } from './demo.state';
import { UserStatusFilter, type UserListItem } from '../models/user.model';

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

  const khop = /^\/api\/users\/([^/]+)(\/(enable|disable))?$/.exec(duong);

  if (khop) {
    const nguoi = kho.users.find((u) => u.id === khop[1]);

    if (!nguoi) {
      return loi(404, 'User.NotFound', 'Không tìm thấy tài khoản.');
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
