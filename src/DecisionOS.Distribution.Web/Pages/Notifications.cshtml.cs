using System.Security.Claims;
using DecisionOS.Distribution.Domain.Workflow;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DecisionOS.Distribution.Web.Pages;

[Authorize(Policy = "AnyDistributionRole")]
public class NotificationsModel : PageModel
{
    private readonly INotificationService _notifications;

    public NotificationsModel(INotificationService notifications) => _notifications = notifications;

    public IReadOnlyList<UserNotification> Items { get; private set; } = Array.Empty<UserNotification>();
    public int UnreadCount { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return;
        UnreadCount = await _notifications.GetUnreadCountAsync(userId.Value, ct);
        Items = await _notifications.GetRecentAsync(userId.Value, 50, ct);
    }

    public async Task<IActionResult> OnPostMarkReadAsync(long id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        await _notifications.MarkReadAsync(id, userId.Value, ct);
        return RedirectToPage();
    }

    private Guid? GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
