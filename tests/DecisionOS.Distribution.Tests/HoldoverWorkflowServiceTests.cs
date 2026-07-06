using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Workflow;
using DecisionOS.Distribution.Infrastructure;
using DecisionOS.Distribution.Infrastructure.Workflow;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace DecisionOS.Distribution.Tests;

public class HoldoverWorkflowServiceTests
{
    [Fact]
    public async Task AssignDriver_CreatesCommentAndNotification()
    {
        await using var db = CreateDb();
        var (owner, assignee, assigner) = await SeedUsersAsync(db);
        var tenantId = Guid.NewGuid();
        var driver = await SeedDriverAsync(db, tenantId);

        var notifications = new TestNotificationService();
        var workflow = CreateService(db, notifications, assignmentsEnabled: true, notificationsEnabled: true);

        var result = await workflow.AssignDriverAsync(
            tenantId,
            "DIST-TEST",
            driver.PeriodEnd,
            driver.Id,
            assignee.Id,
            assigner.Id);

        Assert.Equal(assignee.Id, result.AssigneeUserId);
        Assert.NotEmpty(result.Comments);
        Assert.Single(notifications.Sent);
        Assert.Equal(assignee.Id, notifications.Sent[0].UserId);

        var updated = await db.DriverValues.FindAsync(driver.Id);
        Assert.Equal("Assignee User", updated!.Owner);
    }

    [Fact]
    public async Task AddComment_NotifiesAssigneeWhenOwnerComments()
    {
        await using var db = CreateDb();
        var (_, assignee, owner) = await SeedUsersAsync(db);
        var tenantId = Guid.NewGuid();
        var driver = await SeedDriverAsync(db, tenantId);

        db.WorkAssignments.Add(new WorkAssignment
        {
            TenantId = tenantId,
            PeriodEnd = driver.PeriodEnd,
            TargetType = WorkflowTargetTypes.Driver,
            TargetId = driver.Id.ToString(),
            AssigneeUserId = assignee.Id,
            AssignedByUserId = owner.Id,
            AssignedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var notifications = new TestNotificationService();
        var workflow = CreateService(db, notifications, assignmentsEnabled: true, notificationsEnabled: true);

        var comment = await workflow.AddCommentAsync(
            tenantId,
            "DIST-TEST",
            driver.PeriodEnd,
            driver.Id,
            owner.Id,
            "Can you call the top 5 accounts today?");

        Assert.Contains("top 5 accounts", comment.Body);
        Assert.Single(notifications.Sent);
        Assert.Equal(assignee.Id, notifications.Sent[0].UserId);
    }

    private static HoldoverWorkflowService CreateService(
        DecisionOsDbContext db,
        INotificationService notifications,
        bool assignmentsEnabled,
        bool notificationsEnabled)
    {
        var userManager = CreateUserManager(db);
        var features = Options.Create(new DecisionOsFeatureOptions
        {
            Workflow = new WorkflowFeatureOptions
            {
                AssignmentsEnabled = assignmentsEnabled,
                NotificationsEnabled = notificationsEnabled
            }
        });
        return new HoldoverWorkflowService(db, userManager, notifications, features);
    }

    private static UserManager<ApplicationUser> CreateUserManager(DecisionOsDbContext db)
    {
        var store = new UserStore<ApplicationUser, IdentityRole<Guid>, DecisionOsDbContext, Guid>(db);
        return new UserManager<ApplicationUser>(
            store,
            null!,
            new PasswordHasher<ApplicationUser>(),
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }

    private static DecisionOsDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<DecisionOsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DecisionOsDbContext(options);
    }

    private static async Task<(ApplicationUser owner, ApplicationUser assignee, ApplicationUser assigner)> SeedUsersAsync(DecisionOsDbContext db)
    {
        var owner = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "owner@test.local",
            Email = "owner@test.local",
            DisplayName = "Business Owner",
            EmailConfirmed = true
        };
        var assignee = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "assignee@test.local",
            Email = "assignee@test.local",
            DisplayName = "Assignee User",
            EmailConfirmed = true
        };
        var assigner = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "assigner@test.local",
            Email = "assigner@test.local",
            DisplayName = "Admin User",
            EmailConfirmed = true
        };
        db.Users.AddRange(owner, assignee, assigner);
        await db.SaveChangesAsync();
        return (owner, assignee, assigner);
    }

    private static async Task<DriverValue> SeedDriverAsync(DecisionOsDbContext db, Guid tenantId)
    {
        var tenant = new Tenant
        {
            Id = tenantId,
            ClientId = "DIST-TEST",
            Name = "Test Tenant",
            Archetype = "Test"
        };
        db.Tenants.Add(tenant);

        var driver = new DriverValue
        {
            TenantId = tenantId,
            PeriodEnd = new DateOnly(2025, 11, 22),
            PillarCode = "AR_PastDue31p%",
            DriverName = "Contact largest past-due customers",
            Status = "RED",
            WhyItMatters = "Reduce past-due receivables.",
            Rank = 1
        };
        db.DriverValues.Add(driver);
        await db.SaveChangesAsync();
        return driver;
    }

    private sealed class TestNotificationService : INotificationService
    {
        public List<(Guid UserId, string Title, string Body)> Sent { get; } = new();

        public Task NotifyAssignmentAsync(Guid assigneeUserId, string title, string body, string? linkUrl, CancellationToken ct = default)
        {
            Sent.Add((assigneeUserId, title, body));
            return Task.CompletedTask;
        }

        public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default) => Task.FromResult(0);

        public Task<IReadOnlyList<UserNotification>> GetRecentAsync(Guid userId, int take = 20, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<UserNotification>>(Array.Empty<UserNotification>());

        public Task MarkReadAsync(long notificationId, Guid userId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
