# Hướng dẫn sử dụng — nguồn của màn Trợ giúp trong app

Thư mục này là **nguồn duy nhất** của màn `/huong-dan`. Mỗi bài là một file Markdown; công
cụ `tools/sync-huongdan.mjs` đọc chúng và sinh ra `frontend/public/huong-dan/`.

```bash
node tools/sync-huongdan.mjs
```

## Vì sao là Markdown trong repo, không phải CMS hay chuỗi dịch

**Không nhét vào file dịch.** `identity.json` là nơi chứa nhãn nút, không phải nơi chứa văn
xuôi dài. Một bài hướng dẫn 400 chữ nằm trong JSON thì không ai đọc nổi diff, và mọi ký tự
xuống dòng phải viết `\n`.

**Không dựng CMS.** Nội dung hướng dẫn đi kèm tính năng: sửa màn hình mà không sửa hướng dẫn
là tài liệu nói dối. Để chúng trong cùng một commit là cách duy nhất bắt chúng đi cùng nhau —
cùng lý do `tien-do.md` phải sửa trong cùng commit với code.

**Không viết HTML.** Công cụ sinh ra một **cây khối** JSON, và Angular dựng lại bằng
`@switch`. Không có `innerHTML` ở đâu cả: nhúng HTML vào nội dung nghĩa là mở cửa XSS ở đúng
chỗ không cần nó, và mỗi bài mới lại là một cơ hội gõ hỏng thẻ.

## Khuôn một bài

```markdown
---
ma: cap-tai-khoan
tieude: Cấp tài khoản cho nhân viên mới
nhom: nhan-su
thutu: 2
tomtat: Một câu nói bài này giải quyết việc gì.
---

## Tiêu đề mục

Đoạn văn bình thường. Dùng **đậm**, `mã`, và [liên kết](https://...).

1. Bước một
2. Bước hai

- Gạch đầu dòng
- Gạch đầu dòng nữa

![Mô tả ảnh cho trình đọc màn hình](anh/thanh-vien.png "Chú thích hiện dưới ảnh")

> [!luuy] Câu cần người đọc dừng lại một nhịp.
> [!canh] Câu cảnh báo — thứ làm hỏng dữ liệu nếu làm sai.
```

Cú pháp ngoài danh sách trên sẽ làm **bộ sinh dừng lại và báo lỗi**, không phải im lặng bỏ
qua. Một bài dựng ra sai còn tệ hơn một bài chưa viết: người đọc tin nó.

## Ảnh

Ảnh nằm ở `anh/`, và phần lớn **sinh tự động** từ chính app đang chạy ở chế độ demo:

```bash
node tools/chup-huong-dan.mjs
```

Chụp tay rồi bỏ vào cũng được, nhưng ảnh sinh tự động thì không bao giờ lạc hậu so với giao
diện — đó là cùng một lý do `styles.scss` được sinh từ bản dựng chứ không chép tay.

⚠️ Bài trỏ tới một file ảnh không tồn tại thì **test đỏ**, không phải hiện ô vỡ. Xem
`huong-dan.spec.ts`.
