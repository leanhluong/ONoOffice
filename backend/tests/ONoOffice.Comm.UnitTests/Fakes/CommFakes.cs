using Luong.Kernel.Abstractions;
using ONoOffice.Comm.Application.Abstractions;
using ONoOffice.Comm.Domain.Entities;
using ONoOffice.Identity.Contracts;

namespace ONoOffice.Comm.UnitTests.Fakes;

/// <summary>
/// Bản giả của các cổng, gom về một chỗ — cùng lý do với <c>OrgRepositoryFakes.cs</c>:
/// mỗi test tự dựng lớp giả riêng thì thêm một phương thức vào cổng là hàng loạt file test
/// đỏ vì thiếu thành viên, dù phần lớn chẳng dùng tới nó.
///
/// Mọi thành viên đều <c>virtual</c> và mặc định trả "không có gì", nên <b>những gì một
/// test ghi đè chính là những gì handler đó thật sự dùng tới</b>.
/// </summary>
internal class FakeConversationRepository : IConversationRepository
{
    public List<Conversation> Added { get; } = [];

    public virtual void Add(Conversation conversation) => Added.Add(conversation);

    public virtual Task<Conversation?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult<Conversation?>(null);

    public virtual Task<Conversation?> GetDirectAsync(
        string pairKey,
        CancellationToken cancellationToken) => Task.FromResult<Conversation?>(null);

    public virtual Task<IReadOnlyList<ConversationRow>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ConversationRow>>([]);
}

/// <summary>Kho hội thoại đã biết sẵn một vài hội thoại.</summary>
internal sealed class CoSanHoiThoai(params Conversation[] co) : FakeConversationRepository
{
    public override Task<Conversation?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(co.SingleOrDefault(c => c.Id == id));

    public override Task<Conversation?> GetDirectAsync(
        string pairKey,
        CancellationToken cancellationToken) =>
        Task.FromResult(co.SingleOrDefault(c => c.PairKey == pairKey));
}

internal class FakeMessageRepository : IMessageRepository
{
    public List<Message> Added { get; } = [];

    public virtual void Add(Message message) => Added.Add(message);

    public virtual Task<MessagePage> PageAsync(
        Guid conversationId,
        Guid? beforeMessageId,
        DateTimeOffset notBefore,
        int take,
        CancellationToken cancellationToken) => Task.FromResult(new MessagePage([], false));
}

/// <summary>Ghi lại đúng những tham số handler truyền xuống, để test soi được chặn dưới.</summary>
internal sealed class GhiLaiThamSo(params MessageRow[] tra) : FakeMessageRepository
{
    public DateTimeOffset? NotBefore { get; private set; }

    public int? Take { get; private set; }

    public Guid? BeforeMessageId { get; private set; }

    public override Task<MessagePage> PageAsync(
        Guid conversationId,
        Guid? beforeMessageId,
        DateTimeOffset notBefore,
        int take,
        CancellationToken cancellationToken)
    {
        NotBefore = notBefore;
        Take = take;
        BeforeMessageId = beforeMessageId;

        return Task.FromResult(new MessagePage(tra, false));
    }
}

/// <summary>Danh bạ tài khoản của module Identity — cổng liên module, nhìn từ phía Comm.</summary>
internal sealed class FakeUserDirectory(params UserSummary[] users) : IUserDirectory
{
    public Task<IReadOnlyList<UserSummary>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<UserSummary>>(users);

    public Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(users.Any(u => u.Id == userId));
}

internal sealed class FakeCurrentTenant(Guid? tenantId) : ICurrentTenant
{
    public Guid? TenantId { get; } = tenantId;
}

internal sealed class FakeCurrentUser(Guid? userId) : ICurrentUser
{
    public Guid? UserId { get; } = userId;

    public bool IsAuthenticated => UserId is not null;

    public IReadOnlySet<string> Permissions { get; } = new HashSet<string>();

    public bool HasPermission(string permission) => Permissions.Contains(permission);
}

/// <summary>Đồng hồ đứng yên, để test nói được "đúng lúc này".</summary>
internal sealed class FakeClock(DateTimeOffset now) : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; } = now;
}
