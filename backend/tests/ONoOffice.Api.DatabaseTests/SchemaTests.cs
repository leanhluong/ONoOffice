using Microsoft.Extensions.DependencyInjection;
using ONoOffice.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ONoOffice.Api.DatabaseTests;

/// <summary>
/// Hỏi thẳng Postgres xem migration đã tạo ra cái gì.
///
/// <b>Vì sao không tin bản đồ EF là đủ:</b> mô hình EF dựng lên được không có nghĩa là
/// Postgres chấp nhận nó. Kiểu <c>uuid[]</c>, <c>text[]</c>, schema riêng, quy ước
/// snake_case — tất cả đều là thứ chỉ đúng hoặc sai lúc câu <c>CREATE TABLE</c> chạy thật.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class SchemaTests(DatabaseFixture fixture)
{
    [Theory]
    [InlineData("tenants")]
    [InlineData("users")]
    [InlineData("roles")]
    [InlineData("refresh_tokens")]
    public async Task Bang_TonTaiTrongSchemaIdentity_VaTenLaSnakeCase(string tenBang)
    {
        var ten = await MotGiaTri<string>(
            """
            SELECT table_name FROM information_schema.tables
            WHERE table_schema = 'identity' AND table_name = @ten
            """,
            ("ten", tenBang));

        Assert.Equal(tenBang, ten);
    }

    [Fact]
    public async Task BangOutbox_NamCungDatabaseVoiDuLieuNghiepVu()
    {
        // Đây là toàn bộ lý do outbox hoạt động: sự kiện và dữ liệu nghiệp vụ đi xuống
        // trong CÙNG một transaction. Để nó ở database khác là quay lại đúng vấn đề
        // "ghi hai nơi" mà outbox sinh ra để giải quyết.
        var soBang = await MotGiaTri<long>(
            """
            SELECT count(*) FROM information_schema.tables
            WHERE table_schema = 'identity' AND table_name IN ('outbox_messages', 'inbox_messages')
            """);

        Assert.Equal(2, soBang);
    }

    [Theory]
    [InlineData("users", "role_ids", "ARRAY")]
    [InlineData("roles", "permissions", "ARRAY")]
    [InlineData("users", "tenant_id", "uuid")]
    [InlineData("users", "email", "character varying")]
    public async Task Cot_CoDungKieuDuLieu(string bang, string cot, string kieu)
    {
        var thucTe = await MotGiaTri<string>(
            """
            SELECT data_type FROM information_schema.columns
            WHERE table_schema = 'identity' AND table_name = @bang AND column_name = @cot
            """,
            ("bang", bang), ("cot", cot));

        Assert.Equal(kieu, thucTe);
    }

    /// <summary>
    /// Email unique TOÀN HỆ THỐNG, không phải unique trong một workspace.
    ///
    /// Hệ quả bắt buộc của "mỗi người thuộc đúng một workspace" (ADR-0002): nếu email
    /// chỉ unique trong một công ty thì đăng nhập bằng email + mật khẩu là mơ hồ.
    /// </summary>
    [Fact]
    public async Task Email_CoChiMucUNIQUE_VaKhongKemTheoTenantId()
    {
        var dinhNghia = await MotGiaTri<string>(
            """
            SELECT indexdef FROM pg_indexes
            WHERE schemaname = 'identity' AND tablename = 'users' AND indexdef LIKE '%UNIQUE%email%'
            """);

        Assert.NotNull(dinhNghia);
        Assert.DoesNotContain("tenant_id", dinhNghia, StringComparison.Ordinal);
    }

    [Fact]
    public async Task KhongConMigrationNaoChuaApDung()
    {
        using var scope = fixture.CreateScope().Scope;
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        // Đỏ ở đây nghĩa là có người sửa entity hoặc cấu hình EF mà quên sinh migration.
        // Không canh thì chuyện đó chỉ lộ ra lúc chạy thật, dưới dạng "cột không tồn tại".
        var conThieu = await context.Database.GetPendingMigrationsAsync();

        Assert.Empty(conThieu);
    }

    private async Task<T?> MotGiaTri<T>(string sql, params (string Ten, object GiaTri)[] thamSo)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        foreach (var (ten, giaTri) in thamSo)
        {
            command.Parameters.AddWithValue(ten, giaTri);
        }

        object? ketQua = await command.ExecuteScalarAsync();

        return ketQua is null or DBNull ? default : (T)ketQua;
    }
}
