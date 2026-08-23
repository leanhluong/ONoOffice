using Luong.Kernel.Abstractions;
using Luong.Kernel.Application.Messaging;
using Luong.Kernel.Primitives;
using ONoOffice.Identity.Application.Abstractions;
using ONoOffice.Identity.Domain;
using ONoOffice.Identity.Domain.Entities;

namespace ONoOffice.Identity.Application.Authentication.Refresh;

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<RefreshTokenResponse>;

public sealed record RefreshTokenResponse(string AccessToken, string RefreshToken, int ExpiresInSeconds);

/// <summary>
/// Đổi một refresh token còn sống lấy cặp token mới, và <b>xoay vòng</b> vé cũ.
///
/// Đây là nơi luật chống trộm thật sự phát huy tác dụng — xem <see cref="Handle"/>.
/// </summary>
internal sealed class RefreshTokenCommandHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    ITokenService tokenService,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public async Task<Result<RefreshTokenResponse>> Handle(
        RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        var now = dateTimeProvider.UtcNow;

        string hash = tokenService.HashRefreshToken(command.RefreshToken);
        var existing = await refreshTokens.GetByHashAsync(hash, cancellationToken);

        if (existing is null)
        {
            return IdentityErrors.Auth.InvalidRefreshToken;
        }

        // ⭐ PHÁT HIỆN TRỘM.
        //
        // Vé ĐÃ BỊ THU HỒI mà vẫn được đem ra dùng thì chỉ có một cách giải thích:
        // HAI bên đang cùng giữ nó. Vé chỉ bị thu hồi khi đã xoay vòng (hoặc khi đăng
        // xuất) — nghĩa là bản sao hợp lệ đã được cấp cho ai đó rồi.
        //
        // Lúc đó KHÔNG tin bên nào cả: huỷ toàn bộ chuỗi, bắt đăng nhập lại bằng mật khẩu.
        // Chỉ thu hồi mỗi vé này là vô dụng — nó vốn đã bị thu hồi; kẻ trộm chỉ cần dùng
        // vé KẾ TIẾP trong chuỗi mà nó đã lấy được.
        if (existing.RevokedAtUtc is not null)
        {
            await refreshTokens.RevokeAllForUserAsync(existing.UserId, now, cancellationToken);

            return IdentityErrors.Auth.InvalidRefreshToken;
        }

        // Hết hạn là chuyện BÌNH THƯỜNG — người dùng đi vắng một tháng. Không phải dấu
        // hiệu bị trộm, nên không huỷ chuỗi.
        if (!existing.IsActiveAt(now))
        {
            return IdentityErrors.Auth.InvalidRefreshToken;
        }

        // Nạp LẠI quyền và trạng thái, không tin những gì token cũ mang theo. Giữa hai
        // lần gia hạn, người này có thể đã bị khoá hoặc bị thu hồi quyền — đây chính là
        // chỗ những thay đổi đó có hiệu lực, và cũng là lý do access token chỉ sống 15 phút.
        var data = await users.GetByIdAsync(existing.UserId, cancellationToken);

        if (data is null)
        {
            return IdentityErrors.Auth.InvalidRefreshToken;
        }

        if (!data.IsTenantActive)
        {
            return IdentityErrors.Auth.WorkspaceDisabled;
        }

        if (!data.IsUserActive)
        {
            return IdentityErrors.Auth.AccountDisabled;
        }

        var accessToken = tokenService.IssueAccessToken(data.UserId, data.TenantId, data.Permissions);
        var refreshPair = tokenService.IssueRefreshToken();

        var replacement = RefreshToken.Create(
            data.UserId, data.TenantId, refreshPair.Hash, now, RefreshTokenLifetime);

        if (replacement.IsFailure)
        {
            return replacement.Error;
        }

        var rotated = existing.RotateTo(replacement.Value.Id, now);

        if (rotated.IsFailure)
        {
            return rotated.Error;
        }

        refreshTokens.Add(replacement.Value);

        return new RefreshTokenResponse(
            accessToken.Value,
            refreshPair.Raw,
            (int)accessToken.Lifetime.TotalSeconds);
    }
}
