using Luong.Kernel.Domain;

namespace ONoOffice.Identity.Domain.Events;

/// <summary>
/// Sự kiện của <c>Tenant</c>. Tên luôn ở thì QUÁ KHỨ — chúng ghi lại chuyện ĐÃ xảy ra,
/// không phải mệnh lệnh yêu cầu làm gì.
///
/// Gốc tổng hợp chỉ ghi lại; ai quan tâm thì tự lắng nghe. Nhờ vậy <c>Tenant</c> không
/// cần biết rằng việc tạo workspace sẽ kéo theo gửi mail chào mừng, gieo sẵn 4 vai trò,
/// và ghi một dòng nhật ký kiểm toán.
/// </summary>
public sealed record TenantCreated(Guid TenantId, string Code, string Name) : DomainEvent;

public sealed record TenantOwnershipTransferred(Guid TenantId, Guid PreviousOwnerId, Guid NewOwnerId) : DomainEvent;

public sealed record TenantDeactivated(Guid TenantId) : DomainEvent;

public sealed record TenantActivated(Guid TenantId) : DomainEvent;
