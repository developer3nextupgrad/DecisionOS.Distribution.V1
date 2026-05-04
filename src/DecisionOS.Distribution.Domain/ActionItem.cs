namespace DecisionOS.Distribution.Domain;

public static class ActionStatuses
{
    public const string NotStarted = "NotStarted";
    public const string InProgress = "InProgress";
    public const string AtRisk = "AtRisk";
    public const string Completed = "Completed";
    public const string Deferred = "Deferred";
    public const string Blocked = "Blocked";
}

/// <summary>
/// Execution tracking object: turns weekly insight into accountable follow-up.
/// </summary>
public sealed class ActionItem
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public DateOnly PeriodEnd { get; set; }
    public int? KpiDefinitionId { get; set; }
    public KpiDefinition? KpiDefinition { get; set; }

    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string Owner { get; set; } = null!;
    public DateOnly? DueDate { get; set; }
    public string Status { get; set; } = ActionStatuses.NotStarted;
    public int Priority { get; set; } = 50;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? Notes { get; set; }
}

