using Luong.Kernel.Domain;

namespace ONoOffice.Identity.Domain.Events;

public sealed record UserCreated(Guid UserId, Guid TenantId, string Email) : DomainEvent;

/// <summary>Nơi khác lắng nghe để thu hồi mọi refresh token đang sống của người này.</summary>
public sealed record UserPasswordChanged(Guid UserId, Guid TenantId) : DomainEvent;

/// <summary>Cũng dùng để thu hồi phiên — khoá tài khoản mà token cũ còn dùng được thì việc khoá là nửa vời.</summary>
public sealed record UserDeactivated(Guid UserId, Guid TenantId) : DomainEvent;
