---
ma: phong-ban
tieude: Dựng cây phòng ban
nhom: nhan-su
thutu: 5
tomtat: Thêm, đổi tên, điều chuyển và xoá phòng — cùng bốn luật hệ thống không cho phá.
---

Cây phòng ban ở **Quản trị → Phòng ban**. Mỗi phòng có thể có phòng con, sâu bao nhiêu cấp
cũng được.

![Cây phòng ban với ba cấp lồng nhau](anh/phong-ban.png "Số bên phải mỗi phòng là số người TRỰC TIẾP, không cộng dồn phòng con")

## Bốn việc, ba hộp thoại

**Thêm phòng** · **Đổi tên** · **Điều chuyển** dùng chung một hộp thoại, đổi tiêu đề và ô
nhập theo việc. **Xoá** thì tách riêng, vì xoá cần một câu hỏi khẳng định chứ không phải một
biểu mẫu — người dùng phải đọc chứ không phải điền.

## Bốn luật hệ thống không cho phá

**Tên phòng không trùng nhau** trong cùng một workspace, không phân biệt hoa thường. Hai
phòng cùng tên thì mọi câu "chuyển sang phòng Kinh doanh" đều mơ hồ.

**Không tạo vòng.** Không chuyển một phòng vào chính nhánh con của nó — làm được thì cả
nhánh đó biến mất khỏi cây, và không màn nào hiện ra để sửa. Danh sách xổ *trực thuộc* đã
tự loại bỏ chính nó và cả nhánh của nó, nên bạn cũng không chọn nhầm được.

**Phòng còn phòng con thì không xoá.** Chuyển hoặc xoá các phòng con trước.

**Phòng còn nhân viên thì không xoá** — kể cả người **đã nghỉ việc**. Hồ sơ của họ vẫn trỏ
vào phòng này, và xoá phòng đi là mất luôn thông tin "từng làm ở đâu".

> [!luuy] Con số bên phải mỗi phòng là số người **trực tiếp**, không cộng dồn phòng con.
> Một khối có 3 người và hai phòng con 12 + 5 người thì khối đó vẫn ghi 3.
