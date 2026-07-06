namespace DecisionOS.Distribution.Domain.Workflow;

public interface INotificationService
{
    Task NotifyAssignmentAsync(Guid assigneeUserId, string title, string body, string? linkUrl, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<UserNotification>> GetRecentAsync(Guid userId, int take = 20, CancellationToken ct = default);
    Task MarkReadAsync(long notificationId, Guid userId, CancellationToken ct = default);
}
