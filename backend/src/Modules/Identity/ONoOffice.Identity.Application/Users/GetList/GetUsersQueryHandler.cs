using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Pagination;
using Luong.Kernel.Primitives;
using ONoOffice.Identity.Application.Abstractions;

namespace ONoOffice.Identity.Application.Users.GetList;

/// <summary>
/// Bộ lọc của màn Nhân sự. Mọi trường đều tuỳ chọn — không chọn gì là xem tất cả.
/// </summary>
public sealed record GetUsersQuery(
    string? Search = null,
    UserStatusFilter Status = UserStatusFilter.Any,
    Guid? RoleId = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedList<UserListItem>>;

/// <summary>
/// Danh sách nhân sự.
///
/// Handler mỏng có chủ ý: lọc, sắp xếp và phân trang là việc của database. Kéo hết lên rồi
/// lọc bằng LINQ-to-Objects thì với 38 người vẫn chạy, và với 3.800 người thì sập — mà
/// không có gì trong mã báo trước điều đó.
///
/// Việc DUY NHẤT nó làm là <b>chặn những con số client gửi lên</b>. Xem từng lý do ở dưới.
/// </summary>
internal sealed class GetUsersQueryHandler(IUserRepository users)
    : IQueryHandler<GetUsersQuery, PagedList<UserListItem>>
{
    private const int DefaultPageSize = 20;

    /// <summary>
    /// Trần cứng. Không có nó thì <c>?pageSize=1000000</c> kéo cả bảng lên bộ nhớ trong
    /// một request — rẻ tiền để gửi, đắt để phục vụ, và không đòi hỏi quyền gì đặc biệt.
    /// </summary>
    private const int MaxPageSize = 100;

    public async Task<Result<PagedList<UserListItem>>> Handle(
        GetUsersQuery query,
        CancellationToken cancellationToken)
    {
        // `page=0` cho `OFFSET -20` và Postgres từ chối bằng một lỗi 500 khó hiểu;
        // `pageSize=0` cho `LIMIT 0`, tức danh sách trống, và người dùng tưởng công ty
        // không có ai. Cả hai đều là lỗi của client, nhưng cả hai đều hiện ra như lỗi
        // của chúng ta.
        var page = query.Page < 1 ? 1 : query.Page;

        var pageSize = query.PageSize switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => query.PageSize,
        };

        // Người dùng dán email từ chỗ khác vào thì gần như luôn kèm một dấu cách ở cuối.
        // Không cắt thì họ tìm đúng email của mình mà ra kết quả rỗng.
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();

        return await users.SearchAsync(
            new UserSearch(search, query.Status, query.RoleId, page, pageSize),
            cancellationToken);
    }
}
