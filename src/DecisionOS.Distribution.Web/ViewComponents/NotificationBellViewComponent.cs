using System.Security.Claims;
using DecisionOS.Distribution.Domain.Workflow;
using Microsoft.AspNetCore.Mvc;

namespace DecisionOS.Distribution.Web.ViewComponents;

public sealed class NotificationBellViewComponent : ViewComponent
{
    private readonly INotificationService _notifications;

    public NotificationBellViewComponent(INotificationService notifications) => _notifications = notifications;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var count = 0;
        if (HttpContext.User.Identity?.IsAuthenticated == true)
        {
            var userId = GetUserId();
            if (userId is not null)
                count = await _notifications.GetUnreadCountAsync(userId.Value, HttpContext.RequestAborted);
        }

        return View(count);
    }

    private Guid? GetUserId()
    {
        var raw = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
