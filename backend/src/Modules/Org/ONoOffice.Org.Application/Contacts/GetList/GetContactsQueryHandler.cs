using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Pagination;
using Luong.Kernel.Primitives;
using ONoOffice.Org.Application.Abstractions;

namespace ONoOffice.Org.Application.Contacts.GetList;

/// <summary>Bộ lọc của màn Danh bạ. Mọi trường đều tuỳ chọn.</summary>
public sealed record GetContactsQuery(
    string? Search = null,
    Guid? DepartmentId = null,
    bool IncludeInactive = false,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedList<ContactListItem>>;

/// <summary>
/// Danh bạ nội bộ — <b>ai cũng xem được</b>, chỉ cần <c>employee.read</c>.
///
/// Khác hẳn màn Thành viên bên quản trị dù cùng nói về con người: ở đó quản trị viên sửa
/// TÀI KHOẢN của người khác, ở đây mọi nhân viên tra số điện thoại của đồng nghiệp. Đó
/// cũng là lý do màn này nằm ở khung app còn màn kia ở khung quản trị.
///
/// Handler mỏng có chủ ý: lọc, sắp xếp và phân trang là việc của database. Việc DUY NHẤT
/// nó làm là chặn những con số client gửi lên.
/// </summary>
internal sealed class GetContactsQueryHandler(IEmployeeRepository employees)
    : IQueryHandler<GetContactsQuery, PagedList<ContactListItem>>
{
    private const int DefaultPageSize = 20;

    /// <summary>
    /// Trần cứng. Không có nó thì <c>?pageSize=1000000</c> kéo cả bảng lên bộ nhớ trong
    /// một request — rẻ để gửi, đắt để phục vụ, và không đòi hỏi quyền gì đặc biệt.
    /// </summary>
    private const int MaxPageSize = 100;

    public async Task<Result<PagedList<ContactListItem>>> Handle(
        GetContactsQuery query,
        CancellationToken cancellationToken)
    {
        // `page=0` cho `OFFSET -20` và Postgres từ chối bằng một lỗi 500 khó hiểu;
        // `pageSize=0` cho `LIMIT 0`, tức danh sách trống, và người dùng tưởng công ty
        // không có ai. Cả hai đều là lỗi của client, nhưng cả hai đều hiện ra như lỗi
        // của chúng ta.
        int page = query.Page < 1 ? 1 : query.Page;

        int pageSize = query.PageSize switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => query.PageSize,
        };

        // Người dùng dán số điện thoại hay mã nhân viên từ chỗ khác vào thì gần như luôn
        // kèm một dấu cách ở cuối. Không cắt thì họ tìm đúng thứ của mình mà ra rỗng.
        string? search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();

        return await employees.SearchAsync(
            new ContactSearch(search, query.DepartmentId, query.IncludeInactive, page, pageSize),
            cancellationToken);
    }
}
