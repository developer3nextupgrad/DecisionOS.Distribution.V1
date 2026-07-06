namespace DecisionOS.Distribution.Domain.Workflow;

public class HoldoverComment
{
    public long Id { get; set; }
    public int DriverValueId { get; set; }
    public DriverValue DriverValue { get; set; } = null!;
    public Guid AuthorUserId { get; set; }
    public string Body { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
}

public class HoldoverStatusHistory
{
    public long Id { get; set; }
    public int DriverValueId { get; set; }
    public DriverValue DriverValue { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int? FixProgressPercent { get; set; }
    public Guid ChangedByUserId { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}

public class WorkAssignment
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public DateOnly PeriodEnd { get; set; }
    public string TargetType { get; set; } = null!;
    public string TargetId { get; set; } = null!;
    public Guid AssigneeUserId { get; set; }
    public Guid AssignedByUserId { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
}

public class UserNotification
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = null!;
    public string Body { get; set; } = "";
    public string? LinkUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
