# Màn Trao đổi nội bộ

> Bản dựng: [`chat.html`](./chat.html) — `node tools/serve-mockups.mjs` rồi vào
> <http://localhost:4300/comm/chat.html>.
>
> `?kieu=rieng` xem luồng tin nhắn riêng · `?state=offline` xem một trạng thái ·
> `?skin=giay` mở thẳng một bộ màu · `?bare=1` giấu thanh duyệt.

---

## 1. Màn này để làm gì

Chỗ để người trong công ty nhắn cho nhau, thay cho việc mỗi phòng một nhóm Zalo.

Lý do đáng làm không phải "có chat cho đủ bộ" — Zalo miễn phí và ai cũng đã cài. Thứ Zalo
**không làm được** mới là lý do:

- **Kênh tự khớp với phòng ban.** Người mới vào phòng Kỹ thuật là tự có mặt trong kênh Kỹ
  thuật, không ai phải nhớ đi thêm vào nhóm. Người nghỉ việc là tự ra.
- **Dữ liệu ở lại công ty.** Trưởng nhóm nghỉ mang theo cả nhóm chat là chuyện thật.
- **Nói chuyện ngay tại chỗ có ngữ cảnh.** Bấm từ hồ sơ một nhân viên là nhắn được cho họ.

## 2. Ai vào được

Người đã đăng nhập, có quyền `conversation.read`. Bốn vai hệ thống đều có — chat là thứ ai
cũng dùng.

Nhưng **quyền không đủ**: còn phải là **thành viên** của đúng hội thoại đó. Hai lớp khác
nhau và cần cả hai — quyền trả lời "người này được phép chat không", thành viên trả lời
"được phép đọc CÁI NÀY không". Chỉ kiểm quyền thì ai cũng đọc được kênh Nhân sự.

## 3. Đường dẫn

```
/trao-doi                    danh sách hội thoại, chưa chọn cái nào
/trao-doi/{id}               mở một hội thoại
/trao-doi/{id}/chuoi/{msgId} mở chuỗi trả lời của một tin
```

Mã hội thoại nằm trên URL để **gửi link được**: "xem lại đoạn này nhé" là câu người ta nói
hàng ngày. Trạng thái nằm trong bộ nhớ thì không có gì để dán.

## 4. Bố cục

Bốn cột, **không cuộn cả trang** — mỗi cột tự cuộn phần của nó.

```
┌────┬─────────────────┬──────────────────────────────┬──────────────┐
│ 60 │      300px      │        phần còn lại          │    276px     │
│    │                 │                              │              │
│ O  │ Trao đổi   ✎  ⚟ │ # ky-thuat · Bàn kỹ thuật    │ Giới thiệu   │
│    │ [🔍 Tìm       ] │        ◐◐◐+9   🔍  ⓘ  ⋯      │ Thành viên   │
│ ▦  │ ▾ KÊNH PHÒNG BAN│──────────────────────────────│ Tệp          │
│ 💬•│ # ky-thuat 09:16│ 📌 Đã ghim: Họp giao ban T2  │──────────────│
│ ◐  │   Bạn: Cảm ơn…  │──────────────────────────────│ CHỦ ĐỀ       │
│ ⛬  │ # nhan-su  08:41│      ─── Hôm nay ───         │ Bàn kỹ thuật │
│    │   Phạm Hà… ⑶    │ ◐ Trần Bình 09:12            │              │
│    │ ▾ TIN NHẮN RIÊNG│ ┌────────────────────────┐   │ GẮN VỚI      │
│    │ ◐ Trần Bình  ⑴  │ │Sáng nay họp mấy giờ?   │   │ PHÒNG BAN    │
│    │ ◐ Nguyễn An     │ └────────────────────────┘   │ Phòng Kỹ     │
│ ☀  │ ◐ Phạm Hà       │   👍4  ＋                     │ thuật        │
│ ◐  │                 │   ◐◐ 3 trả lời · 10:02       │              │
└────┴─────────────────┴──────────────────────────────┴──────────────┘
      ↑ Zalo: ảnh,        ↑ Slack: gộp tin, thả cảm xúc,
        câu cuối, giờ,      chuỗi trả lời, thanh thao tác khi rê chuột
        chấm chưa đọc     ↑ Zalo: bong bóng bo tròn, "Đã xem", ảnh xem thẳng
```

Ô soạn tin **dính đáy**, không nằm cuối luồng cuộn. Cả trang cùng cuộn là kiểu hỏng kinh
điển của giao diện chat: gõ được nửa câu thì ô soạn trôi khỏi màn hình.

Khi ít tin nhắn, luồng **dồn xuống đáy** chứ không treo ở đỉnh — kênh mới có ba câu mà
chúng nằm tít trên cùng thì trông như phần dưới bị mất.

### Lấy gì từ Slack, lấy gì từ Zalo

| Từ **Slack** | Từ **Zalo** |
|---|---|
| Cột biểu tượng dọc, không có thanh ngang | Bong bóng bo tròn |
| Kênh có dấu `#`, nhóm gập được | Danh sách có ảnh, **câu cuối**, giờ, chấm chưa đọc |
| Tin liền nhau của cùng người **gộp lại** | Chấm **trực tuyến** trên ảnh đại diện |
| Thanh thao tác nổi lên khi rê chuột | **"Đã xem"** — mấy ảnh tí xíu dưới tin cuối của mình |
| Thả cảm xúc · chuỗi trả lời · tin ghim | Ảnh hiện **thẳng trong luồng**, không phải dòng tệp |
| Bảng thông tin bên phải | Nút biểu tượng cảm xúc ngay cạnh ô gõ |

## 5. ⭐ Hai kiểu luồng — chỗ lai then chốt

Đây là quyết định lớn nhất của màn này, và nó **không** giống Slack lẫn Zalo:

| | Cách xếp | Vì sao |
|---|---|---|
| **Kênh** (nhiều người) | Mọi tin **một cột bên trái**, kiểu Slack. Tin của mình chỉ khác **màu**, không đổi bên | Kênh 12 người mà đảo tin của mình sang phải thì cột đọc gãy làm đôi: mắt phải nhảy trái–phải liên tục và không quét được ai nói câu nào |
| **Tin nhắn riêng** | **Trái–phải**, kiểu Zalo | Chỉ có hai người nên trái–phải là cách đọc nhanh nhất, và đó là thứ người Việt đã quen tay |

Bấm **Kênh / Tin riêng** trên thanh duyệt của bản dựng để so hai kiểu cạnh nhau.

## 6. Các trạng thái

| Trạng thái | Trông thế nào |
|---|---|
| `idle` | Luồng bình thường, có dòng *Trần Bình đang nhập…* dưới đáy |
| `empty` | Kênh chưa có tin — hình minh hoạ + lời mời. **Ô soạn vẫn còn** |
| `sending` | Tin vừa gõ mờ đi tại đúng chỗ nó sẽ nằm, kèm *đang gửi…* |
| `failed` | Tin đó viền đỏ + dòng "Không gửi được. **Thử lại**" |
| `offline` | Dải đỏ dưới tiêu đề: *Mất kết nối — tin nhắn mới sẽ không tự hiện* |

Ba chỗ đáng nói:

**`empty` không được giấu ô soạn.** Kênh chưa có tin chính là lúc người dùng cần gõ câu đầu
tiên nhất. Giấu ô soạn ở đó là khoá cửa đúng lúc có người muốn vào.

**`sending` giữ tin trong luồng.** Người vừa gõ xong cần thấy câu của mình ở đúng vị trí nó
sẽ nằm. Cho biến mất rồi hiện lại là cảm giác tin bị mất.

**`offline` phải nói thẳng.** Chat mà im lặng khi đường realtime đứt là kiểu hỏng tệ nhất
của loại giao diện này: màn hình trông vẫn bình thường, người dùng ngồi chờ trả lời, còn
tin nhắn thì đang tới một chỗ họ không nhìn thấy.

## 7. Dữ liệu cần

```
GET  /api/comm/conversations                  → [{ id, kind, name, unreadCount, lastMessage, muted }]
GET  /api/comm/conversations/{id}/messages    ?before=<cursor>&limit=50
POST /api/comm/conversations/{id}/messages    { body, attachmentKeys[], replyToId? }
POST /api/comm/conversations/direct           { otherUserId } → mở sẵn hoặc tạo mới
POST /api/comm/conversations/{id}/read        { upTo }
PUT    /api/comm/messages/{id}                { body }
DELETE /api/comm/messages/{id}
POST   /api/comm/messages/{id}/reactions      { emoji }        thả / gỡ
POST /api/comm/attachments/upload-url         { fileName, contentType, sizeBytes }
```

**Phân trang bằng con trỏ (`?before=`), không phải số trang.** Tin nhắn mới đến liên tục,
nên "trang 2" của mười giây trước không còn là trang 2 nữa — người cuộn lên sẽ thấy tin lặp
lại và bỏ sót tin khác.

Realtime qua SignalR, nhóm theo hội thoại:

```
ChatHub  ·  nhóm "conv:{id}"
  → MessageSent · MessageEdited · MessageDeleted · ReactionChanged · ReadUpTo · Typing
```

## 8. Lỗi hiện thế nào

| Mã lỗi | HTTP | Hiện cho người dùng |
|---|---|---|
| `Conversation.NotMember` | 403 | Bạn không còn trong hội thoại này. |
| `Conversation.Locked` | 409 | Kênh này đã khoá, không gửi tin được nữa. |
| `Message.TooLong` | 400 | Tin nhắn dài quá 4.000 ký tự. |
| `Message.NotAuthor` | 403 | Chỉ người gửi mới sửa hoặc xoá được tin này. |
| `Attachment.TooLarge` | 400 | Tệp vượt quá 25 MB. |
| *(mất mạng khi gửi)* | — | Tin đó viền đỏ **tại chỗ** + nút **Thử lại** |

Lỗi khi gửi **một tin** hiện ngay tại tin đó, **không** dùng popup nổi. Popup tự tắt sau
sáu giây, mà tin gửi hỏng phải nằm lại cho tới khi người dùng xử lý — gửi lại hoặc bỏ đi.

## 9. Trên màn hẹp

```
< 1280px   ẩn bảng thông tin bên phải
< 1000px   danh sách hội thoại thu còn 260px
<  820px   một cột duy nhất
<  720px   cột biểu tượng thành thanh NGANG dưới đáy
```

Dưới 820px chuyển thành **hai route thật** chứ không ẩn hiện bằng CSS: nút Back của điện
thoại phải quay về danh sách, đó là hành vi người dùng mong đợi nhất.

Dưới 720px điều hướng xuống đáy màn hình — ngón cái với tới được, và đó là chỗ mọi ứng dụng
điện thoại đặt nó. Giữ cột dọc thì nó ăn mất 1/6 bề ngang.

---

## Sáu quyết định trong màn này

### 1. Khung ứng dụng bỏ thanh ngang, đổi thành cột biểu tượng

Bản trước là khuôn "trang quản trị": thanh ngang 52px chạy hết bề rộng, dưới nó thanh dọc
216px có chữ. Ba vấn đề:

- **Ăn mất chiều cao ở đúng chỗ thiếu nhất.** Laptop 13" cao 700–800px; thanh ngang lấy
  52px của mọi màn, kể cả màn chat vốn cần chiều cao hơn bất cứ thứ gì.
- **Hai vùng điều hướng cho một việc** — mắt phải quét hai nơi.
- **216px chữ vĩnh viễn** để hiển thị bốn từ.

Nay là một cột 60px suốt chiều cao: dấu sản phẩm trên, biểu tượng trang ở giữa, tuỳ chọn và
tài khoản dưới đáy. Mỗi trang tự dựng tiêu đề riêng — tiêu đề màn chat nói về **kênh đang
mở**, không phải về sản phẩm.

Đánh đổi: điều hướng chỉ còn biểu tượng, nên **bắt buộc** có nhãn khi rê chuột và nhãn cho
trình đọc màn hình. Biểu tượng trần là chỗ người dùng mới đoán sai.

### 2. Tin liền nhau của cùng một người thì gộp lại

Không gộp thì một người gửi năm câu liên tiếp sẽ thấy tên họ năm lần — luồng biến thành
danh bạ. Giờ của tin đã gộp hiện khi rê chuột, để vẫn tra được mà không chiếm chỗ.

### 3. Tin nhắn là aggregate riêng, không nằm trong Conversation

`Conversation` giữ danh sách thành viên. `Message` là aggregate riêng, chỉ tham chiếu
`ConversationId`.

Một kênh chạy một năm có hàng trăm nghìn tin. Nếu tin nằm trong aggregate hội thoại thì mỗi
lần gửi một câu, EF phải nạp cả trăm nghìn dòng lên bộ nhớ để "thêm vào danh sách". Đây là
cái bẫy DDD phổ biến nhất, và nó chỉ lộ ra khi dữ liệu đã nhiều — tức là ở môi trường thật,
không phải lúc phát triển.

### 4. Ghi tin nhắn đi qua REST, SignalR chỉ để PHÁT

Gửi tin = `POST` như mọi thao tác khác: qua validation, transaction, outbox. Commit xong mới
đẩy qua hub cho các máy khác.

Vì sao không cho hub ghi thẳng vào database: sẽ có **hai đường ghi** cho cùng một loại dữ
liệu với hai bộ luật kiểm — và đường thứ hai sớm muộn thiếu một luật mà đường thứ nhất có.
Hub cũng phải kiểm quyền và tư cách thành viên y hệt REST, nếu không nó là một lỗ hổng đi
vòng qua chính lớp bảo vệ của API.

### 5. Đánh dấu đã đọc bằng MỘT mốc thời gian, không phải từng tin

`ConversationMember.LastReadAt`. Số tin chưa đọc = đếm tin sau mốc đó.

Đánh đổi: "Đã xem" chỉ hiện dưới **tin cuối cùng** của mình, không phải từng tin. Cách kia
là *số thành viên × số tin* dòng dữ liệu — kênh 40 người, 100 nghìn tin là bốn triệu dòng
chỉ để biết ai đã xem gì. Với app nội bộ, thứ người ta thật sự cần là "còn bao nhiêu chưa
đọc".

### 6. Bảng bên phải đổi nội dung theo kiểu hội thoại

Kênh thì nói về kênh (chủ đề, phòng ban, thành viên). Tin nhắn riêng thì nói về **người**
(chức danh, phòng ban, email, trạng thái). Để nguyên "Gắn với phòng ban" khi đang chat riêng
với một người là thông tin **sai**, không phải thông tin thừa.

---

## Chưa làm — ghi rõ để không ai tưởng đã có

| Việc | Vì sao chưa |
|---|---|
| **Tìm trong nội dung tin nhắn** | Ô tìm hiện chỉ lọc tên người và tên kênh. Toàn văn cần chỉ mục riêng của Postgres |
| **Nhóm tự lập** (dự án chạy ngang nhiều phòng) | Lát này chỉ có kênh phòng ban + tin riêng. Thêm sau là thêm một giá trị vào `Kind`, không phải sửa mô hình |
| **Định dạng chữ thật sự** | Thanh B/I/S đã vẽ nhưng chưa nối gì — cần chọn cú pháp (Markdown?) và bộ kết xuất an toàn |
| **Ghim nhiều tin** | Hiện chỉ hiện một tin ghim. Nhiều thì cần chỗ xổ danh sách |
| **Thông báo đẩy khi không mở app** | Cần service worker + đăng ký đẩy; là một lát riêng |
| **Xoá / sửa có lịch sử** | Xoá là xoá mềm, nhưng chưa có màn xem lại bản cũ |
