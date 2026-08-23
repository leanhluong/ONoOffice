# ADR-0003 — Dùng Controller thay vì Minimal API

> Ngày: 2026-08-23 · Trạng thái: **Đã chốt**

## Bối cảnh

Tầng web cần một cách khai báo endpoint. ASP.NET Core cho hai lối, và cả hai đều dùng thật được.

Một hiểu lầm phổ biến phải gạt bỏ trước: *"Minimal API là nhét hết vào `Program.cs`"*. Đó là **kiểu viết trong hướng dẫn nhập môn**, không phải ràng buộc — dự án thật tách mỗi endpoint thành một file riêng, `Program.cs` chỉ còn một dòng `app.MapEndpoints()`. Cả hai lối đều trả về phản hồi như nhau: Controller trả `IActionResult`, Minimal API trả `IResult` — hai tên gọi cho cùng một ý.

## Các lựa chọn

| | Controller | Minimal API |
|---|---|---|
| Gom nhóm | Class = nhóm, `[Route("api/auth")]` | `MapGroup("/api/auth")` |
| Gắn quyền cả nhóm | `[Authorize(Policy = "...")]` trên class | `.RequireAuthorization()` trên group |
| Đánh version | Rất chín: `[ApiVersion]` | Làm được, qua package + group |
| Filter | Action filter — nhiều tài liệu, nhiều ví dụ | Endpoint filter — mới hơn |
| Người mới đọc | Ai cũng biết | Phải giải thích |
| Tốc độ | Chậm hơn một chút | Nhanh hơn một chút |

## Chốt

**Controller.**

Lý do bám đúng ranh giới đã học ở [`Q&A/Ontap/Chang-1-dotnet-core.md`](../../../../Q&A/Ontap/Chang-1-dotnet-core.md) Câu 14 — *Minimal API cho service nhỏ; Controller khi nhiều endpoint + cần filter/versioning*.

ONoOffice rơi vào vế sau: nhiều module, mỗi module hàng chục endpoint, phần lớn cần gắn quyền theo nhóm, và sẽ cần đánh version khi frontend đã chạy ngoài thật.

Action chỉ được phép có **một dòng** — mọi việc thật nằm ở handler:

```csharp
[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToActionResult();
}
```

`ToActionResult()` đến từ `Luong.Kernel.AspNetCore`, nên **không action nào tự quyết định mã HTTP**: sai mật khẩu → `Error.Unauthorized` → 401, email trùng → `Error.Conflict` → 409. Giống hệt nhau ở mọi endpoint, không ai phải nhớ.

## Đánh đổi

**Mất gì:**
- Chậm hơn Minimal API một chút ở khâu định tuyến và gọi action. Với app nội bộ vài trăm người thì không đo được.
- Nhiều nghi lễ hơn: một class + thuộc tính, thay vì một dòng `MapPost`.
- Dễ sa vào thói quen cũ — nhét logic vào controller. Chặn bằng luật: **action một dòng, không có `if`, không `try/catch`**.

**Được gì:**
- `[Authorize(Policy = "...")]` một dòng trên class là xong cho mọi action bên trong.
- Đánh version về sau không phải đổi kiến trúc.
- Người mới vào đọc hiểu ngay, không cần giải thích.

## Học được gì

- Hai lối này **không hơn kém nhau**; chọn theo *hình dạng của hệ thống* chứ không theo cái nào mới hơn.
- Phần cần thêm cho quyết định này rất nhỏ: một file `ResultActionExtensions` trong `Luong.Kernel.AspNetCore`. Vì luật ánh xạ lỗi → mã HTTP đã nằm ở **một chỗ dùng chung** (`ToProblemDetails`), nên thêm một lối trả kết quả chỉ là thêm vỏ bọc — Controller và Minimal API không thể lệch nhau khi báo cùng một lỗi.
