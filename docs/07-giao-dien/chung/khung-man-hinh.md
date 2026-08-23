# Khung màn hình

> Trạng thái: 🟡 **Có khung Angular, chưa chốt thiết kế**

Vỏ bọc quanh mọi màn sau khi đăng nhập: thanh bên + thanh trên.

---

## Bố cục

```
┌──────────┬──────────────────────────────────────────┐
│ ONoOffice│  [tìm kiếm]              [chuông] [avatar]│
├──────────┼──────────────────────────────────────────┤
│ Danh bạ  │                                          │
│ Phòng ban│           nội dung màn hiện tại          │
│ Đơn từ   │                                          │
│ ─────    │                                          │
│ Quản trị │                                          │
└──────────┴──────────────────────────────────────────┘
```

## Menu ẩn hiện theo QUYỀN, không theo vai trò

```html
<a routerLink="/employees" *hasPermission="'employee.read'">Danh bạ</a>
<a routerLink="/admin"     *hasPermission="'user.manage'">Quản trị</a>
```

Kiểm theo vai trò (`*ngIf="user.role === 'HR'"`) là sai: hôm nào quản trị viên tạo vai trò
mới thì menu **lệch khỏi server ngay**, và không ai sửa vì không ai nhớ có chỗ đó.

> **Ẩn nút KHÔNG phải là phân quyền.** Nó chỉ là phép lịch sự với người dùng — đỡ phải bấm
> vào rồi bị từ chối. Phân quyền thật nằm ở server, và có test canh.

## Trên điện thoại

```
Thanh bên thu lại thành nút ☰, mở ra đè lên nội dung
Thanh trên giữ nguyên, ô tìm kiếm thu thành một nút kính lúp
```

## Chưa chốt

- Thanh bên có gập lại được (chỉ còn biểu tượng) không?
- Chuông thông báo — chưa có module Notification, tạm ẩn hay hiện mà không có gì bên trong?
- Menu avatar gồm những gì: Hồ sơ · Đổi mật khẩu · Đăng xuất?
