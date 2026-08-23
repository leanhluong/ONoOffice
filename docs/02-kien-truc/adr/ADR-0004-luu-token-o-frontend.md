# ADR-0004 — Lưu token ở phía frontend

> Ngày: 2026-08-23 · Trạng thái: **Đã chốt** · Nối tiếp [ADR-0002](./ADR-0002-xac-thuc-va-phan-quyen.md)

---

## Bối cảnh

Sau khi đăng nhập, frontend cầm hai thứ: **access token** sống 15 phút và **refresh token**
sống 30 ngày. Phải quyết định cất chúng ở đâu.

Đây là chỗ đánh đổi giữa **hai loại tấn công**, và **không có phương án nào miễn nhiễm cả hai**:

```
localStorage  →  chống CSRF tốt,  nhưng XSS ĐỌC ĐƯỢC
                 một dòng script lạ là lấy được token, mang về máy nó, dùng 30 ngày

Cookie        →  chống XSS tốt (HttpOnly = JavaScript không đọc nổi),
                 nhưng mở cửa CSRF (trình duyệt tự đính cookie vào mọi request)
```

## Các lựa chọn

| | **A. Cả hai trong thân phản hồi** | **B. Access trong thân + refresh trong cookie** | **C. Cả hai trong cookie** |
|---|---|---|---|
| FE lưu access token | biến trong bộ nhớ | biến trong bộ nhớ | không chạm tới được |
| FE lưu refresh token | `localStorage` | cookie `HttpOnly` | cookie `HttpOnly` |
| XSS lấy được refresh token? | ✅ **Có** | ❌ Không | ❌ Không |
| Diện CSRF | Không có | 1 endpoint | **Mọi** endpoint |
| Cần FE và BE **cùng site**? | Không | **Có** | **Có** |

## Chốt

**Phương án A.**

Không phải vì nó an toàn nhất — **nó không phải** — mà vì hai phương án kia **không chạy được**
trong hoàn cảnh hiện tại:

```
Cookie do  api.onooffice.onrender.com  đặt
Request đến từ trang  onooffice.pages.dev
        ↓
Trình duyệt xếp nó là COOKIE BÊN THỨ BA
        ↓
Safari CHẶN mặc định. Chrome đang siết dần.
```

Đây **không phải "kém an toàn hơn"** — mà là **không chạy**. Và nó áp cho **cả B lẫn C**,
vì refresh token của B cũng nằm trong cookie do API đặt.

Hạ tầng miễn phí gần như luôn cho FE và BE hai tên miền gốc khác nhau (`*.pages.dev` với
`*.onrender.com`), nên **khác site là gần như chắc chắn**, không phải rủi ro xa xôi.

### Cụ thể

```
accessToken   → biến JavaScript trong bộ nhớ, KHÔNG ghi vào localStorage
                sống 15 phút · mất khi tải lại trang (cố ý)
refreshToken  → localStorage
                sống 30 ngày · FE gọi /auth/refresh lúc khởi động để lấy lại phiên
```

Access token nằm trong **biến chứ không phải `localStorage`** là có lý do: XSS có lấy được
thì cũng chỉ dùng được **15 phút**. Còn thứ đáng giá thật — refresh token 30 ngày — thì
phương án A **không bảo vệ được**, nên phải bù bằng cách khác.

## Đánh đổi

**Mất gì:** XSS đọc được refresh token, và mang về máy khác dùng suốt 30 ngày.

**Bù bằng ba lớp đã có sẵn trong `RefreshToken`** — và đây mới là lúc chúng thật sự được
dùng tới, không phải bày cho đẹp:

```
① xoay vòng            mỗi refresh token dùng ĐÚNG MỘT LẦN
② phát hiện dùng lại   dùng lần hai = có hai bên cùng giữ = bị trộm
③ thu hồi cả chuỗi     phát hiện trộm → huỷ toàn bộ phiên của người đó
```

> Ý nghĩa thật: token có bị lấy thì **lần dùng đầu tiên của kẻ trộm sẽ tự tố giác** — vì
> nạn nhân hoặc kẻ trộm, ai dùng sau cũng đụng phải một token đã xoay. Cửa sổ thiệt hại
> co từ 30 ngày xuống còn "tới lần gia hạn tiếp theo của người dùng thật".

**Lớp phòng thủ thứ tư, và là lớp chính:** header **CSP** để giảm bề mặt XSS ngay từ đầu.
Với phương án A thì CSP không còn là "nên có" — nó là **thứ đang gánh phần lớn rủi ro**.

## Ngưỡng xem lại

Chuyển sang **phương án B** ngay khi FE và API về **cùng một tên miền gốc**, ví dụ:

```
app.onooffice.com  +  api.onooffice.com     ← SAME-SITE, cookie chạy bình thường
onooffice.com  phục vụ FE, /api  phục vụ BE ← same-origin, tốt nhất
```

Lưu ý phân biệt: **`api.x.com` và `app.x.com` LÀ same-site** (site tính theo tên miền đăng
ký, tiền tố con và cổng không tính). Rất nhiều người tưởng khác subdomain là hỏng — không phải.

Việc phải làm khi chuyển: controller đặt cookie thay vì trả `refreshToken` trong thân;
CORS bật `AllowCredentials` với `WithOrigins` cụ thể; FE bỏ `token.storage.ts` và gửi kèm
`withCredentials: true`. Khoảng nửa buổi.

## Học được gì

- **Bảo mật là hàm của hoàn cảnh triển khai**, không phải một thang điểm tuyệt đối. Phương
  án "an toàn nhất" mà bị Safari chặn thì điểm an toàn thực tế của nó bằng không.
- `HttpOnly` chặn **mang token đi**, không chặn **dùng token tại chỗ**. Kẻ có XSS vẫn gọi
  API thay bạn được, vì trình duyệt tự đính cookie vào. Nó thu hẹp thiệt hại chứ không xoá.
- Ba lớp xoay vòng / phát hiện dùng lại / thu hồi chuỗi được viết từ trước khi có quyết định
  này — và chính chúng làm phương án A **chấp nhận được** thay vì liều lĩnh.
- Nối về [`Q&A/Ontap/Chang-8-security-phan-quyen.md`](../../../../Q&A/Ontap/Chang-8-security-phan-quyen.md) — XSS, CSRF, OWASP.
