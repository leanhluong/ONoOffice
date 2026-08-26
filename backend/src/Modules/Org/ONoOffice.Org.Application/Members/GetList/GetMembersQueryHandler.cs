using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Identity.Contracts;
using ONoOffice.Org.Application.Abstractions;

namespace ONoOffice.Org.Application.Members.GetList;

public sealed record GetMembersQuery : IQuery<IReadOnlyList<MemberListItem>>;

/// <summary>
/// Một người trong workspace — <b>hợp nhất</b> tài khoản đăng nhập và hồ sơ nhân sự.
///
/// Ba loại dòng, và cả ba đều có thật:
/// <list type="bullet">
/// <item><b>Cả hai</b> — người bình thường: có hồ sơ, có tài khoản, đã nối với nhau.</item>
/// <item><b>Chỉ hồ sơ</b> — nhân viên mới, chưa được cấp tài khoản.</item>
/// <item><b>Chỉ tài khoản</b> — tài khoản bot chạy sao lưu, không phải nhân viên nào.</item>
/// </list>
/// </summary>
public sealed record MemberListItem(
    Guid? EmployeeId,
    Guid? UserId,
    string FullName,
    string? Code,
    string? JobTitle,
    string? Email,
    string? Phone,
    Guid? DepartmentId,
    string? DepartmentName,
    string? RoleName,
    bool IsActive,
    bool MustChangePassword);

/// <summary>
/// Danh sách người của workspace, gộp từ HAI module.
///
/// ═══════════════════════════════════════════════════════════════════════
///  VÌ SAO PHÉP GỘP NẰM Ở ĐÂY, VÀ KHÔNG THỂ NẰM CHỖ KHÁC
/// ═══════════════════════════════════════════════════════════════════════
///
/// <list type="bullet">
/// <item><b>Không ở database</b> — Luật 3 cấm JOIN xuyên schema. <c>Employee.UserId</c>
/// cố ý là <c>Guid</c> trần, không phải khoá ngoại.</item>
/// <item><b>Không ở Controller</b> — <c>ControllerRuleTests</c> bắt mỗi action là MỘT
/// biểu thức, không câu điều kiện nào.</item>
/// <item><b>Không ở Identity</b> — Identity không được biết "nhân viên" là gì; nó phục vụ
/// cả những workspace chỉ dùng đăng nhập.</item>
/// </list>
///
/// Còn lại đúng một chỗ: handler của Org, gọi Identity qua <see cref="IUserDirectory"/>.
///
/// ═══════════════════════════════════════════════════════════════════════
///  NỐI BẰNG `UserId`, KHÔNG ĐOÁN BẰNG EMAIL
/// ═══════════════════════════════════════════════════════════════════════
///
/// Ghép theo email trùng nhau nghe rất tiện và <b>sai một cách nguy hiểm</b>: hai người
/// khác nhau dùng chung một email công ty (phòng kinh doanh dùng <c>sales@</c>) sẽ bị gộp
/// thành một dòng, và mọi thao tác lên dòng đó chạm vào nhầm người. Chỉ nối khi có ai đó
/// đã nối tay bằng <c>Employee.LinkAccount</c> — chậm hơn, nhưng không bao giờ ghép nhầm.
///
/// Hệ quả nhìn thấy được: workspace mới có tài khoản mà chưa có hồ sơ nào, nên danh sách
/// ban đầu chỉ toàn dòng "chỉ tài khoản". Đó là sự thật, không phải lỗi.
/// </summary>
internal sealed class GetMembersQueryHandler(
    IEmployeeRepository employees,
    IUserDirectory users) : IQueryHandler<GetMembersQuery, IReadOnlyList<MemberListItem>>
{
    /// <summary>
    /// Trần cứng khi kéo hồ sơ nhân sự.
    ///
    /// Màn này cần TOÀN BỘ danh sách để gộp — lấy một trang thì người ở trang sau bị coi
    /// là "chưa có tài khoản", tức là một câu trả lời sai chứ không phải thiếu. Trần đặt ở
    /// đây để một workspace bất thường không kéo sập bộ nhớ; chạm trần thì phần dôi ra
    /// biến mất khỏi danh sách, và đó là lúc màn này cần phân trang thật.
    /// </summary>
    private const int TranHoSo = 2000;

    public async Task<Result<IReadOnlyList<MemberListItem>>> Handle(
        GetMembersQuery query,
        CancellationToken cancellationToken)
    {
        var hoSo = await employees.SearchAsync(
            new ContactSearch(null, null, IncludeInactive: true, Page: 1, PageSize: TranHoSo),
            cancellationToken);

        var taiKhoan = await users.GetAllAsync(cancellationToken);

        // `UserId` của những hồ sơ đã nối — dùng để biết tài khoản nào CÒN LẠI chưa có hồ sơ.
        var daNoi = await employees.LinkedUserIdsAsync(cancellationToken);

        // `ToHashSet`, KHÔNG `ToDictionary`. Hai hồ sơ trỏ vào cùng một tài khoản là trạng
        // thái không hợp lệ — `LinkAccountCommandHandler` chặn ở đường ghi — nhưng
        // `Employee.UserId` không có ràng buộc UNIQUE (Luật 3 cấm ràng buộc xuyên schema),
        // nên database không canh giúp và một lần sửa tay là đủ để nó xảy ra.
        //
        // Với `ToDictionary` thì đúng lúc đó cả màn Thành viên trả 500: một dòng hỏng làm
        // mù toàn bộ danh sách người, ngay lúc quản trị viên cần nhìn vào đó để sửa. Ở đây
        // ta chỉ cần biết "tài khoản này đã có ai nhận chưa", nên tập hợp là đủ.
        var daCoChu = daNoi.Select(x => x.UserId).ToHashSet();

        // Chiều ngược lại thì khoá là `EmployeeId` — một hồ sơ chỉ mang được MỘT `UserId`,
        // nên chiều này không thể trùng.
        var theoEmployeeId = daNoi.ToDictionary(x => x.EmployeeId, x => x.UserId);
        var tenTaiKhoan = taiKhoan.ToDictionary(u => u.Id);

        var ketQua = new List<MemberListItem>();

        foreach (var nv in hoSo.Items)
        {
            UserSummary? tk = theoEmployeeId.TryGetValue(nv.Id, out var userId)
                && tenTaiKhoan.TryGetValue(userId, out var found)
                    ? found
                    : null;

            ketQua.Add(new MemberListItem(
                nv.Id,
                tk?.Id,
                nv.FullName,
                nv.Code,
                nv.JobTitle,

                // Email công việc trên hồ sơ đứng TRƯỚC email đăng nhập: đó là email đồng
                // nghiệp dùng để liên hệ, còn email đăng nhập có thể là một địa chỉ nội bộ
                // không ai gửi thư tới.
                nv.WorkEmail ?? tk?.Email,
                nv.Phone,
                nv.DepartmentId,
                nv.DepartmentName,
                tk?.RoleName,

                // Người đã nghỉ việc HOẶC tài khoản bị vô hiệu đều là "không hoạt động".
                // Đủ một trong hai là đã không dùng được hệ thống.
                nv.IsActive && (tk?.IsActive ?? true),
                tk?.MustChangePassword ?? false));
        }

        foreach (var tk in taiKhoan.Where(u => !daCoChu.Contains(u.Id)))
        {
            ketQua.Add(new MemberListItem(
                null,
                tk.Id,
                tk.FullName,
                null,
                null,
                tk.Email,
                null,
                null,
                null,
                tk.RoleName,
                tk.IsActive,
                tk.MustChangePassword));
        }

        // Sắp theo TÊN, ổn định bằng khoá phụ. Hai nguồn ghép lại thì thứ tự của từng
        // nguồn không còn nghĩa gì — không sắp lại thì mọi hồ sơ đứng trước mọi tài khoản,
        // và danh sách đọc như hai bảng dán chồng lên nhau.
        return ketQua
            .OrderBy(m => m.FullName, StringComparer.CurrentCulture)
            .ThenBy(m => m.EmployeeId ?? m.UserId)
            .ToList();
    }
}
