namespace DecisionOS.Distribution.Domain.Routing;

public static class RoutingQueueTypes
{
    public const string Management = "Management";
    public const string DrillDown = "DrillDown";
    public const string Watchlist = "Watchlist";
    public const string DataGap = "DataGap";
    public const string ModuleAction = "ModuleAction";
    public const string Review = "Review";
}

public class RoutingQueueItem
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public DateOnly PeriodEnd { get; set; }
    public string QueueType { get; set; } = null!;
    public string? CatalogKpiId { get; set; }
    public string? CatalogDriverId { get; set; }
    public string? ModuleCode { get; set; }
    public string Title { get; set; } = null!;
    public string? Severity { get; set; }
    public decimal? FinalScore { get; set; }
    public string Status { get; set; } = "Open";
    public DateTimeOffset CreatedAt { get; set; }
}
