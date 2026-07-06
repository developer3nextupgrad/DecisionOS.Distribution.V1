namespace DecisionOS.Distribution.Domain;

public sealed class DecisionOsFeatureOptions
{
    public const string SectionName = "DecisionOs";

    public CatalogFeatureOptions Catalog { get; set; } = new();
    public ScoringFeatureOptions Scoring { get; set; } = new();
    public RoutingFeatureOptions Routing { get; set; } = new();
    public WorkflowFeatureOptions Workflow { get; set; } = new();
    public IngestionFeatureOptions Ingestion { get; set; } = new();
}

public sealed class CatalogFeatureOptions
{
    public bool Enabled { get; set; }
}

public sealed class ScoringFeatureOptions
{
    public bool UseCatalogEngine { get; set; }
    public bool UseDynamicTop7 { get; set; }
}

public sealed class RoutingFeatureOptions
{
    public bool Enabled { get; set; }
}

public sealed class WorkflowFeatureOptions
{
    public bool AssignmentsEnabled { get; set; }
    public bool NotificationsEnabled { get; set; }
}

public sealed class IngestionFeatureOptions
{
    public bool RuleBasedExpansionEnabled { get; set; }
}
