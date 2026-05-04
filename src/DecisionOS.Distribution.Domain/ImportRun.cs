namespace DecisionOS.Distribution.Domain;

/// <summary>
/// Audit row for a KPI/driver import (PDF ops & idempotency traceability).
/// </summary>
public class ImportRun
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public DateOnly PeriodEnd { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string Status { get; set; } = null!;
    /// <summary>
    /// Readiness status rolled up from validation: ReadyToRun / ReadyWithLimitations / NotReadyYet.
    /// </summary>
    public string? ReadinessStatus { get; set; }
    public int KpiRowsProcessed { get; set; }
    public int DriverRowsProcessed { get; set; }
    public string? ErrorMessage { get; set; }
    /// <summary>SHA-256 hex over client, period, and source file fingerprints for idempotent re-runs.</summary>
    public string? SourceFingerprint { get; set; }
    /// <summary>Rolled-up validation messages (warnings/errors) from the last attempt.</summary>
    public string? ValidationSummary { get; set; }
}
