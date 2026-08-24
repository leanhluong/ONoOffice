# Đa ngôn ngữ

> Trạng thái: 🟢 **Đã dựng** · Chốt 2026-08-23 · Dựng xong 2026-08-24

Ban đầu `01-tong-quan/pham-vi.md` xếp đa ngôn ngữ vào **"cố ý không làm ở lát 1"**. Nay
đổi ý: **làm ngay từ đầu.**

Và đổi ý ở thời điểm này là **rẻ nhất có thể**. Lý do rất cụ thể:

> Thêm đa ngôn ngữ vào một ứng dụng đã có 40 màn nghĩa là mở **từng file** ra, tìm **từng
> chuỗi** đang viết cứng, đặt tên khoá cho nó, rồi cầu cho không sót chỗ nào. Sót thì
> chẳng có lỗi nào báo — chỉ có một câu tiếng Việt nằm giữa giao diện tiếng Anh, và
> thường là người dùng phát hiện trước.
>
> Làm từ đầu thì chỉ là một thói quen gõ phím.

---

## Hai nửa, hai cách làm khác nhau

Đây là điểm dễ nhầm nhất, và nhầm thì dịch thiếu một nửa:

```
① CHỮ TRÊN GIAO DIỆN        "Đăng nhập" · "Ghi nhớ tôi" · "Danh bạ"
   → nằm ở FRONTEND
   → backend không bao giờ biết tới chúng

② THÔNG BÁO TỪ SERVER       "Email hoặc mật khẩu không đúng."
   → sinh ở BACKEND
   → nhưng KHÔNG dịch ở backend — xem dưới
```

## Backend gửi MÃ, không gửi câu chữ

Đây là quyết định gốc, và may là **đã làm đúng từ đầu** dù lúc đó chưa nghĩ tới đa ngôn ngữ:

```json
{
  "status": 401,
  "errors": [
    { "code": "Auth.InvalidCredentials", "description": "Email hoặc mật khẩu không đúng." }
  ]
}
```

Frontend **rẽ nhánh theo `code`**, và tự tra câu chữ trong bảng dịch của mình. Trường
`description` chỉ để lập trình viên đọc log và để dự phòng khi gặp mã lạ.

**Vì sao không dịch ở backend:** backend sẽ phải biết người dùng đang chọn tiếng gì, phải
mang theo file dịch, phải phát hành lại mỗi lần sửa một dấu phẩy trong câu chữ. Trong khi
người duy nhất biết chắc người dùng muốn tiếng gì là **trình duyệt**.

Đổi lại — nói cho sòng phẳng: **mọi mã lỗi mới đều phải thêm bản dịch ở FE**, nếu không
người dùng sẽ thấy câu tiếng Việt mặc định. Có một test canh chuyện đó (xem cuối trang).

## Chọn thư viện: `@ngx-translate` hay `@angular/localize`?

| | `@angular/localize` (chính chủ) | `@ngx-translate` |
|---|---|---|
| Đổi tiếng | **Phải tải lại trang** — mỗi ngôn ngữ là một bản build riêng | Đổi **ngay lập tức**, không tải lại |
| Kích thước gói | Nhỏ hơn — chỉ nhúng đúng ngôn ngữ đó | Lớn hơn — mang theo mọi bản dịch |
| Triển khai | **N bản build**, cần định tuyến theo ngôn ngữ ở tầng máy chủ | Một bản build duy nhất |
| Dịch chuỗi động | Vụng | Tự nhiên |

**Chọn `@ngx-translate`**, vì hai lý do bám đúng hoàn cảnh:

1. **Đổi tiếng không tải lại trang.** Đây là app nội bộ dùng cả ngày; bắt tải lại chỉ để
   đổi ngôn ngữ là khó chịu.
2. **Một bản build duy nhất.** Chưa chốt được tên miền, chưa chốt cách deploy — thêm
   chuyện "định tuyến theo ngôn ngữ ở tầng máy chủ" lúc này là tự trói tay.

*Ngưỡng xem lại:* khi kích thước gói thành vấn đề thật, hoặc khi cần SEO đa ngôn ngữ
(app nội bộ thì không).

## Cấu trúc file dịch

Chia **theo module**, giống hệt cách chia của backend và của thư mục này:

```
frontend/src/assets/i18n/
├── vi/
│   ├── common.json      · nút, nhãn, ngày giờ — dùng ở mọi nơi
│   ├── errors.json      · MÃ LỖI từ backend  ⭐
│   ├── identity.json    · đăng nhập, tài khoản, quyền
│   └── org.json         · nhân sự, phòng ban
└── en/
    └── (bốn file y hệt)
```

**Tiếng Việt là ngôn ngữ gốc.** Viết `vi` trước, `en` dịch theo — không phải ngược lại.
Sản phẩm cho công ty Việt Nam, người dùng chính là người Việt; viết tiếng Anh trước rồi
dịch sang tiếng Việt luôn cho ra thứ tiếng Việt nghe như dịch máy.

## Quy ước đặt khoá

```
{module}.{màn}.{thành phần}

identity.login.title              "Đăng nhập"
identity.login.emailLabel         "Email công ty"
identity.login.submit             "Đăng nhập"
common.action.cancel              "Huỷ"
common.state.empty                "Chưa có dữ liệu"
errors.Auth.InvalidCredentials    "Email hoặc mật khẩu không đúng."
```

Khoá của lỗi **trùng khít mã lỗi backend** — nên tra bản dịch chỉ là:

```ts
translate.instant('errors.' + apiError.code)
```

Không cần bảng ánh xạ trung gian, và **không có chỗ nào để lệch**.

## Bốn thứ hay bị quên

**① Ngày giờ và số.** `Intl.DateTimeFormat` và `Intl.NumberFormat` theo đúng ngôn ngữ đang
chọn, không tự nối chuỗi. `dd/MM/yyyy` ở Việt Nam nhưng `MM/dd/yyyy` ở Mỹ — nối tay là sai
im lặng, và `03/04` thì không ai biết là ngày 3 tháng 4 hay ngày 4 tháng 3.

**② Số nhiều.** Tiếng Việt không đổi dạng số nhiều, tiếng Anh thì có. Đừng nối
`count + " nhân viên"` — dùng cơ chế số nhiều của thư viện, nếu không bản tiếng Anh sẽ ra
`1 employees`.

**③ Chữ dài ngắn khác nhau.** Tiếng Anh thường ngắn hơn tiếng Việt khoảng 20–30%. Nút vừa
khít chữ "Save" sẽ vỡ khi thành "Lưu thay đổi". **Đừng đặt bề ngang cố định cho nút.**

**④ Thuộc tính `lang` của thẻ `<html>`.** Phải đổi theo ngôn ngữ đang chọn — trình đọc màn
hình dựa vào nó để phát âm cho đúng.

## Ngôn ngữ chọn thế nào

```
1. Người dùng đã chọn thủ công  → localStorage['lang']
2. Chưa chọn                    → navigator.language
3. Không khớp ngôn ngữ nào      → 'vi'
```

Ngôn ngữ là **lựa chọn của từng người**, không phải cấu hình của workspace — hai người
trong cùng công ty được phép dùng hai thứ tiếng khác nhau.

## Test canh

Hai test, và cả hai đều bắt lỗi mà mắt người rất dễ bỏ sót:

```
① Mọi khoá trong vi/ đều phải có trong en/  và ngược lại
   → chặn ca "thêm chuỗi mới mà quên dịch"

② Mọi mã lỗi khai trong IdentityErrors.cs đều phải có khoá trong errors.json
   → chặn ca "thêm lỗi mới ở backend, FE hiện ra một mã trần"
```

Test ② đọc thẳng file `IdentityErrors.cs` để lấy danh sách mã — nên nó **không bao giờ
lệch** khỏi backend.
