namespace DecisionOS.Distribution.Domain.Uploads;

public sealed class MappingTemplate
{
    public long Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public ReportType ReportType { get; set; }
    public string Name { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }

    public List<MappingRule> Rules { get; set; } = new();
}

public sealed class MappingRule
{
    public long Id { get; set; }

    public long MappingTemplateId { get; set; }
    public MappingTemplate MappingTemplate { get; set; } = null!;

    public string SourceColumn { get; set; } = null!;
    public string? SystemField { get; set; }
    public bool Ignore { get; set; }
}

