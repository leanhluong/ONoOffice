---
ma: dang-ky-workspace
tieude: Đăng ký workspace
nhom: bat-dau
thutu: 1
tomtat: Tạo không gian làm việc cho công ty bạn, và hiểu mã workspace dùng để làm gì.
---

Workspace là không gian riêng của một công ty. Mọi người, phòng ban, tin nhắn và tài liệu
đều nằm trong đó, và **không workspace nào nhìn thấy dữ liệu của workspace khác**.

Người đăng ký trở thành **chủ sở hữu** — vai duy nhất chuyển nhượng được workspace về sau.

## Các bước

1. Mở trang đăng ký, điền **tên công ty**.
2. Xem lại **mã workspace** hệ thống gợi ý bên dưới. Mã này nằm trong đường dẫn đăng nhập
   của cả công ty, nên hãy chọn thứ đồng nghiệp gõ được từ trí nhớ.
3. Điền họ tên, email và mật khẩu của bạn — đây sẽ là tài khoản chủ sở hữu.
4. Tick đồng ý điều khoản rồi bấm **Tạo workspace**.

Xong bước này, hệ thống dựng một lần: công ty, bốn vai hệ thống, và tài khoản của bạn.

![Màn đăng ký workspace, hai cột: bên trái là sơ đồ tổ chức, bên phải là biểu mẫu](anh/dang-ky.png "Màn đăng ký — mã workspace được gợi ý từ tên công ty, sửa lại được")

## Về mã workspace

Mã chỉ gồm **chữ thường, số và dấu nối**, bắt đầu bằng chữ và không có hai dấu nối liền
nhau. `cong-ty-abc` hợp lệ; `Cong Ty ABC` và `cong--ty` thì không.

> [!canh] Mã workspace **không đổi được** sau khi tạo. Nó nằm trong đường dẫn đăng nhập mà
> cả công ty đã lưu, và đổi nó là làm hỏng dấu trang của mọi người cùng lúc.

## Nếu mã đã có người dùng

Mã là duy nhất trên toàn hệ thống, nên `acme` thường đã có chủ. Hệ thống báo ngay tại ô mã,
và bạn chỉ cần thêm một phần cho riêng mình — `acme-vn`, `acme-hcm`.

> [!luuy] Hệ thống kiểm **mã workspace trước, email sau**. Trùng cả hai thì chỉ hiện được
> một lỗi, và hiện cái dễ sửa trước là ít làm người ta nản hơn.
