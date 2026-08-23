# 07 — Giao diện

> Cập nhật: 2026-08-23

Thư mục này trả lời: **màn hình nào tồn tại, làm gì, và trông ra sao ở mọi trạng thái.**

Nó **không** chép lại code Angular. Code thay đổi hằng ngày; tài liệu ở đây chỉ giữ những
thứ đổi chậm: mục đích của màn, ai vào được, có bao nhiêu trạng thái, và lỗi hiện thế nào.

---

## Chia theo module, rồi theo màn

```
07-giao-dien/
├── he-thong-thiet-ke.md      · bộ màu · chữ · khoảng cách · component dùng chung
├── da-ngon-ngu.md            · tiếng Việt + tiếng Anh — cách làm và bốn thứ hay quên
├── wireframes.html           · bố cục 6 màn, đơn sắc có chú thích — MỞ BẰNG TRÌNH DUYỆT
├── chung/                    · thứ không thuộc module nào
│   ├── khung-man-hinh.md     · sidebar + topbar
│   └── trang-thai-chung.md   · rỗng · lỗi · 403 · 404 · đang tải
├── identity/                 · tài khoản, đăng nhập, phân quyền
│   └── dang-nhap.md          🟢
└── org/                      · nhân sự, phòng ban
    ├── danh-ba.md            ⬜
    ├── so-do-to-chuc.md      ⬜
    └── quan-tri-nhan-vien.md ⬜
```

Cùng cách chia với backend (`02-kien-truc`) là có chủ ý: mở một module ra thì **thấy cả
hai phía cùng lúc**, không phải nhảy qua lại giữa hai cách sắp xếp khác nhau.

## Khuôn một tài liệu màn hình

Mọi file màn hình đi theo đúng 8 mục dưới đây. Khuôn cố định để **so sánh được** giữa các
màn, và để không ai quên mục nào:

```
1. Màn này để làm gì      · một câu, viết theo góc người dùng
2. Ai vào được            · quyền cần có — theo permission, KHÔNG theo vai trò
3. Đường dẫn              · route + có cần đăng nhập không
4. Bố cục                 · phác thảo bằng chữ, không cần ảnh
5. Các trạng thái         · mặc định · đang tải · rỗng · lỗi · không đủ quyền
6. Dữ liệu cần            · gọi API nào, lấy gì
7. Lỗi hiện thế nào       · mã lỗi từ backend → câu chữ người dùng đọc
8. Trên điện thoại        · gì đổi, gì ẩn
```

**Mục 5 và 7 là hai mục hay bị bỏ quên nhất** — và cũng là hai mục tốn thời gian nhất khi
phát hiện muộn. Màn hình đẹp mà lúc rỗng thì trống trơn, lúc lỗi thì hiện chữ tiếng Anh
của thư viện, là chuyện xảy ra hoài.

## Trạng thái tài liệu

| Màn | Module | Trạng thái |
|---|---|---|
| [Đăng nhập](./identity/dang-nhap.md) | identity | 🟢 Đã thiết kế, chờ chốt bộ màu |
| [Khung màn hình](./chung/khung-man-hinh.md) | chung | 🟡 Có khung Angular, chưa chốt thiết kế |
| [Trạng thái chung](./chung/trang-thai-chung.md) | chung | 🟡 Đã định nghĩa, chưa dựng |
| [Đa ngôn ngữ](./da-ngon-ngu.md) | chung | 🟡 Đã chốt cách làm, chưa dựng |
| [Wireframe 6 màn](./wireframes.html) | chung | 🟢 Đã vẽ |
| [Danh bạ](./org/danh-ba.md) | org | ⬜ Chưa tới lượt |
| [Sơ đồ tổ chức](./org/so-do-to-chuc.md) | org | ⬜ Chưa tới lượt |
| [Quản trị nhân viên](./org/quan-tri-nhan-vien.md) | org | ⬜ Chưa tới lượt |
