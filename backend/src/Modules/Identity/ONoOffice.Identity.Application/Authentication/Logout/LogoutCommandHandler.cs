using Luong.Kernel.Abstractions;
using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Identity.Application.Abstractions;

namespace ONoOffice.Identity.Application.Authentication.Logout;

public sealed record LogoutCommand(string RefreshToken) : ICommand;

/// <summary>
/// Thu hồi vé gia hạn để phiên không kéo dài được nữa.
///
/// <b>LUÔN trả về thành công</b>, kể cả khi vé không tồn tại hoặc đã bị thu hồi. Hai lý do:
/// người dùng muốn thoát và họ đã thoát rồi — báo lỗi chẳng giúp được gì; và báo "vé này
/// không tồn tại" là tiết lộ vé nào từng tồn tại.
///
/// Access token đang cầm vẫn dùng được tới khi hết hạn (tối đa 15 phút) — đó là bản chất
/// của token không tra database. Muốn cắt ngay lập tức thì phải thêm danh sách đen trong
/// Redis; chưa cần ở lát 1.
/// </summary>
internal sealed class LogoutCommandHandler(
    IRefreshTokenRepository refreshTokens,
    ITokenService tokenService,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        string hash = tokenService.HashRefreshToken(command.RefreshToken);
        var existing = await refreshTokens.GetByHashAsync(hash, cancellationToken);

        // Bỏ qua kết quả Revoke: "đã thu hồi rồi" không phải lỗi ở đây.
        existing?.Revoke(dateTimeProvider.UtcNow);

        return Result.Success();
    }
}
