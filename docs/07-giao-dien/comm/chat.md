# Màn Trao đổi nội bộ

> Bản dựng: [`chat.html`](./chat.html) — mở thẳng bằng trình duyệt, hoặc
> `node tools/serve-mockups.mjs` rồi vào <http://localhost:4300/comm/chat.html>.
> Thêm `?state=offline` để xem một trạng thái cụ thể.

---

## 1. Màn này để làm gì

Chỗ để người trong công ty nhắn cho nhau, thay cho việc mỗi phòng một nhóm Zalo.

Lý do đáng làm không phải là "có chat cho đủ bộ" — Zalo miễn phí và ai cũng đã cài. Thứ
Zalo **không làm được** mới là lý do:

- **Kênh tự khớp với phòng ban.** Người mới vào phòng Kỹ thuật là tự có mặt trong kênh Kỹ
  thuật, không ai phải nhớ đi thêm vào nhóm. Người nghỉ việc là tự ra.
- **Dữ liệu ở lại công ty.** Trưởng nhóm nghỉ mang theo cả nhóm chat là chuyện thật.
- **Nói chuyện ngay tại chỗ có ngữ cảnh.** Sau này bấm từ hồ sơ một nhân viên là nhắn được
  cho họ, không phải đi tìm số điện thoại.

## 2. Ai vào được

Người đã đăng nhập, có quyền `conversation.read`. Bốn vai hệ thống đều có quyền này —
chat là thứ ai cũng dùng.

Nhưng **quyền không đủ**: còn phải là **thành viên** của đúng hội thoại đó. Hai lớp khác
nhau và cần cả hai — quyền trả lời "người này được phép chat không", thành viên trả lời
"được phép đọc CÁI NÀY không". Chỉ kiểm quyền thì ai cũng đọc được kênh Nhân sự.

## 3. Đường dẫn

```
/trao-doi                    danh sách hội thoại, chưa chọn cái nào
/trao-doi/{id}               mở một hội thoại
```

Mã hội thoại nằm trên URL để **gửi link được**: "xem lại đoạn này nhé" là câu người ta nói
hàng ngày. Trạng thái nằm trong bộ nhớ thì không có gì để dán.

## 4. Bố cục

Ba cột, **không cuộn cả trang** — mỗi cột tự cuộn phần của nó.

```
┌────────────┬───────────────────┬────────────────────────────────────┐
│ Điều hướng │ Danh sách         │ Luồng tin nhắn                     │
│  216px     │  264px            │  phần còn lại                      │
│            │                   │                                    │
│ Bảng đk    │ Trao đổi     [+]  │ # Kỹ thuật · 12 người              │
│ Nhân sự    │ [🔍 Tìm        ]  │────────────────────────────────────│
│ ▸Trao đổi 4│ PHÒNG BAN         │           ── Hôm nay ──            │
│            │ # Kỹ thuật        │  ◐ Trần Bình 09:12                 │
│            │ # Nhân sự      3  │    Sáng nay họp lúc mấy giờ?       │
│            │ # Kinh doanh      │                                    │
│            │ TIN NHẮN RIÊNG    │              09:14 Bạn ◐           │
│            │ ◐ Trần Bình    1  │              10h nhé, phòng A2     │
│            │ ◐ Nguyễn An       │────────────────────────────────────│
│            │                   │ [📎] Nhắn cho #Kỹ thuật…     [↑]   │
└────────────┴───────────────────┴────────────────────────────────────┘
```

Ô soạn tin **dính đáy**, không nằm cuối luồng cuộn. Cả trang cùng cuộn là kiểu hỏng kinh
điển của giao diện chat: gõ được nửa câu thì ô soạn trôi khỏi màn hình.

Khi ít tin nhắn, luồng **dồn xuống đáy** chứ không treo ở đỉnh — kênh mới có ba câu mà
chúng nằm tít trên cùng thì trông như phần dưới bị mất.

## 5. Các trạng thái

| Trạng thái | Trông thế nào |
|---|---|
| `idle` | Luồng tin bình thường |
| `empty` | Kênh chưa có tin nào — hình minh hoạ + lời mời nói câu đầu. **Ô soạn vẫn còn** |
| `sending` | Tin vừa gõ hiện mờ ở đúng chỗ nó sẽ nằm, kèm chữ *đang gửi…* |
| `failed` | Tin đó viền đỏ + dòng "Không gửi được. **Thử lại**" |
| `offline` | Dải đỏ dưới tiêu đề: *Mất kết nối — tin nhắn mới sẽ không tự hiện* |

Ba chỗ đáng nói:

**`empty` không được giấu ô soạn.** Kênh chưa có tin chính là lúc người dùng cần gõ câu
đầu tiên nhất. Giấu ô soạn ở đó là khoá cửa đúng lúc có người muốn vào.

**`sending` giữ tin trong luồng.** Người vừa gõ xong cần thấy câu của mình ở đúng vị trí
nó sẽ nằm. Cho biến mất rồi hiện lại là cảm giác tin bị mất.

**`offline` phải nói thẳng.** Chat mà im lặng khi đường realtime đứt là kiểu hỏng tệ nhất
của loại giao diện này: màn hình trông vẫn bình thường, người dùng ngồi chờ trả lời, còn
tin nhắn thì đang tới một chỗ họ không nhìn thấy.

## 6. Dữ liệu cần

```
GET  /api/comm/conversations                  → [{ id, kind, name, unreadCount, lastMessage }]
GET  /api/comm/conversations/{id}/messages    ?before=<cursor>&limit=50
POST /api/comm/conversations/{id}/messages    { body, attachmentKeys[] }
POST /api/comm/conversations/direct           { otherUserId } → mở sẵn hoặc tạo mới
POST /api/comm/conversations/{id}/read        { upTo }
PUT    /api/comm/messages/{id}                { body }
DELETE /api/comm/messages/{id}
POST /api/comm/attachments/upload-url         { fileName, contentType, sizeBytes }
```

**Phân trang bằng con trỏ (`?before=`), không phải số trang.** Tin nhắn mới đến liên tục,
nên "trang 2" của mười giây trước không còn là trang 2 nữa — người cuộn lên sẽ thấy tin
lặp lại và bỏ sót tin khác.

Realtime qua SignalR, nhóm theo hội thoại:

```
ChatHub  ·  nhóm "conv:{id}"
  → MessageSent · MessageEdited · MessageDeleted · ReadUpTo
```

## 7. Lỗi hiện thế nào

| Mã lỗi | HTTP | Hiện cho người dùng |
|---|---|---|
| `Conversation.NotMember` | 403 | Bạn không còn trong hội thoại này. |
| `Conversation.Locked` | 409 | Kênh này đã khoá, không gửi tin được nữa. |
| `Message.TooLong` | 400 | Tin nhắn dài quá 4.000 ký tự. |
| `Message.NotAuthor` | 403 | Chỉ người gửi mới sửa hoặc xoá được tin này. |
| `Attachment.TooLarge` | 400 | Tệp vượt quá 25 MB. |
| *(mất mạng khi gửi)* | — | Tin đó viền đỏ tại chỗ + nút **Thử lại** |

Lỗi khi **gửi một tin** hiện ngay tại tin đó, **không** dùng popup nổi. Popup tự tắt sau
sáu giây, mà tin gửi hỏng thì phải nằm lại cho tới khi người dùng xử lý — hoặc gửi lại,
hoặc bỏ đi.

## 8. Trên điện thoại

```
< 1100px   ẩn cột điều hướng, danh sách hội thoại thu còn 216px
< 820px    một cột duy nhất — /trao-doi là danh sách, /trao-doi/{id} là luồng tin
```

Dưới 820px chuyển thành **hai route thật** chứ không phải ẩn hiện bằng CSS: nút Back của
điện thoại phải quay về danh sách, và đó là hành vi người dùng mong đợi nhất.

---

## Bốn quyết định trong màn này

### Kênh gắn với phòng ban, không phải nhóm tự lập

Mỗi phòng ban trong module Org có **đúng một** kênh, thành viên đồng bộ theo phòng. Đây là
toàn bộ lý do làm chat trong app nội bộ thay vì dùng Zalo.

Đánh đổi: không tạo được nhóm tuỳ ý cho một dự án chạy ngang nhiều phòng. Chấp nhận ở lát
này — thêm loại kênh thứ ba (tự tạo) sau là thêm một giá trị vào `Kind`, không phải sửa mô
hình.

### Tin nhắn là aggregate riêng, không nằm trong Conversation

`Conversation` giữ danh sách thành viên. `Message` là aggregate riêng, chỉ tham chiếu
`ConversationId`.

Vì sao: một kênh chạy một năm có hàng trăm nghìn tin. Nếu tin nằm trong aggregate hội thoại
thì mỗi lần gửi một câu, EF phải nạp cả trăm nghìn dòng lên bộ nhớ để "thêm vào danh sách".
Đây là cái bẫy DDD phổ biến nhất, và nó chỉ lộ ra khi dữ liệu đã nhiều — tức là ở môi
trường thật, không phải lúc phát triển.

### Ghi tin nhắn đi qua REST, SignalR chỉ để PHÁT

Gửi tin = `POST` như mọi thao tác khác: đi qua validation, transaction, outbox. Sau khi
commit xong mới đẩy qua hub cho các máy khác.

Vì sao không cho hub tự ghi thẳng vào database: sẽ có **hai đường ghi** cho cùng một loại
dữ liệu, với hai bộ luật kiểm — và đường thứ hai sớm muộn sẽ thiếu một luật mà đường thứ
nhất có. Hub cũng phải kiểm quyền và tư cách thành viên y hệt REST, nếu không nó là một lỗ
hổng đi vòng qua chính lớp bảo vệ của API.

### Đánh dấu đã đọc bằng MỘT mốc thời gian, không phải từng tin

`ConversationMember.LastReadAt`. Số tin chưa đọc = đếm tin sau mốc đó.

Đánh đổi: không làm được "hai dấu tích xanh" cho từng tin. Nhưng cách kia là *số thành viên
× số tin* dòng dữ liệu — một kênh 40 người, 100 nghìn tin là bốn triệu dòng chỉ để biết ai
đã xem gì. Với app nội bộ, thứ người ta thật sự cần là "còn bao nhiêu tin chưa đọc".

---

## Chưa làm — ghi rõ để không ai tưởng đã có

| Việc | Vì sao chưa |
|---|---|
| **Tìm trong nội dung tin nhắn** | Ô tìm hiện chỉ lọc tên người và tên kênh. Tìm toàn văn cần chỉ mục riêng của Postgres |
| **Đang gõ… / đang online** | Rẻ về kỹ thuật (đã có SignalR) nhưng thêm nhiễu; để sau khi luồng chính chạy ổn |
| **Trả lời một tin cụ thể (reply / thread)** | Đổi cả bố cục luồng. Cần thấy người dùng thật cần nó trước |
| **Biểu tượng cảm xúc trên tin** | Cùng lý do |
| **Ghim tin, sửa lịch sử tin** | Ghim thì cần chỗ hiển thị riêng ở đầu kênh |
| **Thông báo đẩy khi không mở app** | Cần service worker + đăng ký đẩy; là một lát riêng |
