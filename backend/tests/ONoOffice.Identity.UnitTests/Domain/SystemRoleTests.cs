using ONoOffice.Identity.Domain;

namespace ONoOffice.Identity.UnitTests.Domain;

/// <summary>
/// Canh bốn vai trò được gieo sẵn khi tạo workspace — định nghĩa ở
/// <c>ADR-0002</c>, mục "Bốn vai mặc định".
///
/// <b>Vì sao đáng canh bằng test:</b> đây là bộ quyền mà <i>mọi</i> workspace nhận lúc
/// sinh ra. Gõ thừa một quyền vào <c>Member</c> thì mọi nhân viên bình thường của mọi
/// công ty có quyền đó — và không ai nhận ra, vì thừa quyền không gây lỗi nào cả.
/// Thiếu quyền thì có người kêu; thừa quyền thì im lặng.
/// </summary>
public class SystemRoleTests
{
    [Fact]
    public void CoDungBonVai()
    {
        Assert.Equal(4, SystemRoles.All.Count);
    }

    [Fact]
    public void Owner_CoTATCAQuyen()
    {
        // Owner phải theo kịp mọi quyền mới thêm vào hệ thống. Liệt kê tay thì thêm quyền
        // xong quên cập nhật, và chủ workspace không dùng được tính năng mình vừa mua.
        Assert.Equal(
            Permissions.All.OrderBy(p => p, StringComparer.Ordinal),
            SystemRoles.Owner.Permissions.OrderBy(p => p, StringComparer.Ordinal));
    }

    [Fact]
    public void Admin_CoTatCaTruDungMotQuyen_ChuyenNhuongQuyenSoHuu()
    {
        var thieu = Permissions.All.Except(SystemRoles.Admin.Permissions).ToList();

        // Ranh giới DUY NHẤT giữa Owner và Admin. Cho Admin chuyển nhượng quyền sở hữu
        // nghĩa là Admin tự trao workspace cho mình được — lúc đó hai vai là một.
        Assert.Equal([Permissions.Workspace.TransferOwnership], thieu);
    }

    [Fact]
    public void MoiQuyenTrongVaiHeThong_DeuPhaiCoThat()
    {
        var khongCoThat = SystemRoles.All
            .SelectMany(vai => vai.Permissions.Select(quyen => $"{vai.Name}: {quyen}"))
            .Where(dong => !Permissions.Contains(dong.Split(": ")[1]))
            .ToList();

        // Quyền gõ sai chính tả không gây lỗi nào — nó chỉ đơn giản là không bao giờ khớp
        // với thứ endpoint đòi. Người có vai đó nhận 403 và không hiểu vì sao.
        Assert.True(khongCoThat.Count == 0, "Vai hệ thống chứa quyền không tồn tại:\n  " + string.Join("\n  ", khongCoThat));
    }

    [Fact]
    public void KhongCoHaiVaiTrungTen()
    {
        // Ràng buộc UNIQUE (tenant_id, name) ở database sẽ chặn — nhưng chặn lúc INSERT,
        // tức là giữa chừng việc tạo workspace, để lại một workspace dựng dở.
        Assert.Equal(4, SystemRoles.All.Select(r => r.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    /// <summary>
    /// Dựng vai trò thật từ định nghĩa — chứng minh định nghĩa đi lọt qua được mọi luật
    /// của <see cref="Role.CreateSystem"/> (tên không rỗng, không quá 100 ký tự, quyền hợp lệ).
    /// </summary>
    [Fact]
    public void MoiVai_DungDuocThanhRoleThat()
    {
        var tenantId = Guid.NewGuid();

        foreach (var dinhNghia in SystemRoles.All)
        {
            var role = dinhNghia.CreateFor(tenantId);

            Assert.True(role.IsSuccess, $"Không dựng được vai '{dinhNghia.Name}': {role.Error.Code}");
            Assert.True(role.Value.IsSystem, $"Vai '{dinhNghia.Name}' phải là vai hệ thống — vai hệ thống mới bất biến.");
            Assert.Equal(tenantId, role.Value.TenantId);
        }
    }

    /// <summary>
    /// Ghi lại một chỗ LỆCH có thật giữa ADR và code, để nó không bị tưởng là lỗi.
    ///
    /// ADR-0002 viết <c>Manager → employee.read · leave.approve</c>. Chưa có module nghỉ
    /// phép nên <c>leave.approve</c> không tồn tại, và Manager tạm thời trùng khít Member.
    /// Test này sẽ ĐỎ đúng vào ngày ai đó thêm <c>leave.approve</c> — và đỏ là đúng: đó
    /// là lúc phải quay lại cấp quyền đó cho Manager.
    /// </summary>
    [Fact]
    public void Manager_TamThoiTrungKhitMember_ChoToiKhiCoModuleNghiPhep()
    {
        Assert.False(
            Permissions.Contains("leave.approve"),
            "Đã có quyền 'leave.approve' — hãy cấp nó cho Manager và xoá test này.");

        Assert.Equal(
            SystemRoles.Member.Permissions.OrderBy(p => p, StringComparer.Ordinal),
            SystemRoles.Manager.Permissions.OrderBy(p => p, StringComparer.Ordinal));
    }
}
