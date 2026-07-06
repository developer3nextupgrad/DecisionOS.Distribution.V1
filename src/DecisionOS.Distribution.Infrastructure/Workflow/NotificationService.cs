using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DecisionOS.Distribution.Infrastructure.Workflow;

public sealed class NotificationService : INotificationService
{
    private readonly DecisionOsDbContext _db;
    private readonly DecisionOsFeatureOptions _features;

    public NotificationService(DecisionOsDbContext db, IOptions<DecisionOsFeatureOptions> features)
    {
        _db = db;
        _features = features.Value;
    }

    public async Task NotifyAssignmentAsync(
        Guid assigneeUserId,
        string title,
        string body,
        string? linkUrl,
        CancellationToken ct = default)
    {
        if (!_features.Workflow.NotificationsEnabled) return;

        _db.UserNotifications.Add(new UserNotification
        {
            UserId = assigneeUserId,
            Title = title,
            Body = body,
            LinkUrl = linkUrl,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
        => _db.UserNotifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task<IReadOnlyList<UserNotification>> GetRecentAsync(Guid userId, int take = 20, CancellationToken ct = default)
        => await _db.UserNotifications.AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

    public async Task MarkReadAsync(long notificationId, Guid userId, CancellationToken ct = default)
    {
        var n = await _db.UserNotifications.FirstOrDefaultAsync(
            x => x.Id == notificationId && x.UserId == userId, ct);
        if (n is null) return;
        n.IsRead = true;
        await _db.SaveChangesAsync(ct);
    }
}
