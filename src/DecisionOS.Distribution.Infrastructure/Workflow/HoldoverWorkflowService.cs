using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Workflow;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DecisionOS.Distribution.Infrastructure.Workflow;

public sealed class HoldoverWorkflowService : IHoldoverWorkflowService
{
    private readonly DecisionOsDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly INotificationService _notifications;
    private readonly DecisionOsFeatureOptions _features;

    public HoldoverWorkflowService(
        DecisionOsDbContext db,
        UserManager<ApplicationUser> users,
        INotificationService notifications,
        IOptions<DecisionOsFeatureOptions> features)
    {
        _db = db;
        _users = users;
        _notifications = notifications;
        _features = features.Value;
    }

    public async Task<IReadOnlyList<AssignableUserDto>> GetAssignableUsersAsync(CancellationToken ct = default)
    {
        var users = await _users.Users.AsNoTracking()
            .OrderBy(u => u.DisplayName ?? u.Email)
            .ToListAsync(ct);

        return users.Select(u => new AssignableUserDto
        {
            UserId = u.Id,
            DisplayName = DisplayName(u),
            Email = u.Email ?? u.UserName ?? ""
        }).ToList();
    }

    public async Task<HoldoverWorkflowDto?> GetDriverWorkflowAsync(
        Guid tenantId,
        int driverId,
        Guid currentUserId,
        CancellationToken ct = default)
    {
        var driver = await _db.DriverValues.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == driverId && d.TenantId == tenantId, ct);
        if (driver is null) return null;

        var assignment = await FindAssignmentAsync(tenantId, driverId, ct);
        var comments = await _db.HoldoverComments.AsNoTracking()
            .Where(c => c.DriverValueId == driverId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

        var authorIds = comments.Select(c => c.AuthorUserId).Distinct().ToList();
        if (assignment?.AssigneeUserId is Guid assigneeId)
            authorIds.Add(assigneeId);
        var names = await ResolveUserNamesAsync(authorIds, ct);

        return new HoldoverWorkflowDto
        {
            AssigneeUserId = assignment?.AssigneeUserId,
            AssigneeName = assignment?.AssigneeUserId is Guid id && names.TryGetValue(id, out var n) ? n : null,
            Comments = comments.Select(c => new HoldoverCommentDto
            {
                Id = c.Id,
                AuthorUserId = c.AuthorUserId,
                AuthorName = names.TryGetValue(c.AuthorUserId, out var author) ? author : "User",
                IsMine = c.AuthorUserId == currentUserId,
                Body = c.Body,
                CreatedAt = c.CreatedAt
            }).ToList()
        };
    }

    public async Task<HoldoverWorkflowDto> AssignDriverAsync(
        Guid tenantId,
        string clientId,
        DateOnly periodEnd,
        int driverId,
        Guid assigneeUserId,
        Guid assignedByUserId,
        CancellationToken ct = default)
    {
        EnsureWorkflowEnabled();

        var driver = await _db.DriverValues
            .FirstOrDefaultAsync(d => d.Id == driverId && d.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException("Holdover item not found.");

        var assignee = await _users.FindByIdAsync(assigneeUserId.ToString())
            ?? throw new InvalidOperationException("Assignee user not found.");

        var assigneeName = DisplayName(assignee);
        var assignment = await FindAssignmentAsync(tenantId, driverId, ct, tracked: true);
        if (assignment is null)
        {
            assignment = new WorkAssignment
            {
                TenantId = tenantId,
                PeriodEnd = periodEnd,
                TargetType = WorkflowTargetTypes.Driver,
                TargetId = driverId.ToString(),
                AssigneeUserId = assigneeUserId,
                AssignedByUserId = assignedByUserId,
                AssignedAt = DateTimeOffset.UtcNow
            };
            _db.WorkAssignments.Add(assignment);
        }
        else
        {
            assignment.AssigneeUserId = assigneeUserId;
            assignment.AssignedByUserId = assignedByUserId;
            assignment.AssignedAt = DateTimeOffset.UtcNow;
            assignment.PeriodEnd = periodEnd;
        }

        driver.Owner = assigneeName;

        var assignerName = await ResolveUserNameAsync(assignedByUserId, ct);
        var systemNote = $"{assignerName} assigned this to {assigneeName}.";
        _db.HoldoverComments.Add(new HoldoverComment
        {
            DriverValueId = driverId,
            AuthorUserId = assignedByUserId,
            Body = systemNote,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        if (assigneeUserId != assignedByUserId)
        {
            var link = BuildHoldoverLink(clientId, periodEnd, driverId);
            await _notifications.NotifyAssignmentAsync(
                assigneeUserId,
                "New follow-up assigned to you",
                $"{driver.DriverName} — {driver.WhyItMatters}",
                link,
                ct);
        }

        return (await GetDriverWorkflowAsync(tenantId, driverId, assignedByUserId, ct))!;
    }

    public async Task<HoldoverCommentDto> AddCommentAsync(
        Guid tenantId,
        string clientId,
        DateOnly periodEnd,
        int driverId,
        Guid authorUserId,
        string body,
        CancellationToken ct = default)
    {
        EnsureWorkflowEnabled();

        var trimmed = body.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new ArgumentException("Comment cannot be empty.", nameof(body));

        var driver = await _db.DriverValues.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == driverId && d.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException("Holdover item not found.");

        var comment = new HoldoverComment
        {
            DriverValueId = driverId,
            AuthorUserId = authorUserId,
            Body = trimmed,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.HoldoverComments.Add(comment);
        await _db.SaveChangesAsync(ct);

        var assignment = await FindAssignmentAsync(tenantId, driverId, ct);
        var authorName = await ResolveUserNameAsync(authorUserId, ct);
        var link = BuildHoldoverLink(clientId, periodEnd, driverId);

        if (assignment is not null)
        {
            if (assignment.AssigneeUserId != authorUserId)
            {
                await _notifications.NotifyAssignmentAsync(
                    assignment.AssigneeUserId,
                    $"New message on: {driver.DriverName}",
                    $"{authorName}: {trimmed}",
                    link,
                    ct);
            }

            if (assignment.AssignedByUserId != authorUserId && assignment.AssignedByUserId != assignment.AssigneeUserId)
            {
                await _notifications.NotifyAssignmentAsync(
                    assignment.AssignedByUserId,
                    $"Update on: {driver.DriverName}",
                    $"{authorName}: {trimmed}",
                    link,
                    ct);
            }
        }

        return new HoldoverCommentDto
        {
            Id = comment.Id,
            AuthorUserId = authorUserId,
            AuthorName = authorName,
            IsMine = true,
            Body = trimmed,
            CreatedAt = comment.CreatedAt
        };
    }

    public async Task<IReadOnlyDictionary<int, Guid?>> GetAssigneeIdsForDriversAsync(
        Guid tenantId,
        IReadOnlyList<int> driverIds,
        CancellationToken ct = default)
    {
        if (driverIds.Count == 0)
            return new Dictionary<int, Guid?>();

        var targetIds = driverIds.Select(id => id.ToString()).ToList();
        var rows = await _db.WorkAssignments.AsNoTracking()
            .Where(a => a.TenantId == tenantId
                && a.TargetType == WorkflowTargetTypes.Driver
                && targetIds.Contains(a.TargetId))
            .ToListAsync(ct);

        return driverIds.ToDictionary(
            id => id,
            id =>
            {
                var row = rows.FirstOrDefault(r => r.TargetId == id.ToString());
                return row?.AssigneeUserId;
            });
    }

    private async Task<WorkAssignment?> FindAssignmentAsync(
        Guid tenantId,
        int driverId,
        CancellationToken ct,
        bool tracked = false)
    {
        var targetId = driverId.ToString();
        var query = tracked ? _db.WorkAssignments : _db.WorkAssignments.AsNoTracking();
        return await query.FirstOrDefaultAsync(
            a => a.TenantId == tenantId
                && a.TargetType == WorkflowTargetTypes.Driver
                && a.TargetId == targetId,
            ct);
    }

    private async Task<Dictionary<Guid, string>> ResolveUserNamesAsync(IEnumerable<Guid> userIds, CancellationToken ct)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<Guid, string>();

        var users = await _users.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToListAsync(ct);

        return users.ToDictionary(u => u.Id, DisplayName);
    }

    private async Task<string> ResolveUserNameAsync(Guid userId, CancellationToken ct)
    {
        var user = await _users.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        return user is null ? "User" : DisplayName(user);
    }

    private static string DisplayName(ApplicationUser user)
        => string.IsNullOrWhiteSpace(user.DisplayName) ? (user.Email ?? user.UserName ?? "User") : user.DisplayName.Trim();

    private static string BuildHoldoverLink(string clientId, DateOnly periodEnd, int driverId)
        => $"/Dashboard?ClientId={Uri.EscapeDataString(clientId)}&PeriodEnd={periodEnd:yyyy-MM-dd}&view=holdover&driverId={driverId}";

    private void EnsureWorkflowEnabled()
    {
        if (!_features.Workflow.AssignmentsEnabled)
            throw new InvalidOperationException("Workflow assignments are not enabled.");
    }
}
