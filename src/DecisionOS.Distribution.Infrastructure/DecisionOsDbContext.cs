using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Domain.Catalog;
using DecisionOS.Distribution.Domain.Normalized;
using DecisionOS.Distribution.Domain.Routing;
using DecisionOS.Distribution.Domain.Scoring;
using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Domain.Workflow;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Infrastructure;

public class DecisionOsDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public DecisionOsDbContext(DbContextOptions<DecisionOsDbContext> options) : base(options)
    {
    }

    public DbSet<VerticalLibrary> VerticalLibraries => Set<VerticalLibrary>();
    public DbSet<BusinessProfile> BusinessProfiles => Set<BusinessProfile>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<KpiDefinition> KpiDefinitions => Set<KpiDefinition>();
    public DbSet<KpiSnapshot> KpiSnapshots => Set<KpiSnapshot>();
    public DbSet<DriverValue> DriverValues => Set<DriverValue>();
    public DbSet<DriverDefinition> DriverDefinitions => Set<DriverDefinition>();
    public DbSet<InfluencerDefinition> InfluencerDefinitions => Set<InfluencerDefinition>();
    public DbSet<TenantKpiOverride> TenantKpiOverrides => Set<TenantKpiOverride>();
    public DbSet<TenantDriverOverride> TenantDriverOverrides => Set<TenantDriverOverride>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<WeeklyFocus> WeeklyFocuses => Set<WeeklyFocus>();
    public DbSet<ImportRun> ImportRuns => Set<ImportRun>();
    public DbSet<ImportRunIssue> ImportRunIssues => Set<ImportRunIssue>();
    public DbSet<ActionItem> ActionItems => Set<ActionItem>();
    public DbSet<UploadBatch> UploadBatches => Set<UploadBatch>();
    public DbSet<UploadedFile> UploadedFiles => Set<UploadedFile>();
    public DbSet<MappingTemplate> MappingTemplates => Set<MappingTemplate>();
    public DbSet<MappingRule> MappingRules => Set<MappingRule>();
    public DbSet<UploadedFileColumnMap> UploadedFileColumnMaps => Set<UploadedFileColumnMap>();
    public DbSet<UploadBatchIssue> UploadBatchIssues => Set<UploadBatchIssue>();

    public DbSet<NormalizedSalesRow> NormalizedSalesRows => Set<NormalizedSalesRow>();
    public DbSet<NormalizedInventoryRow> NormalizedInventoryRows => Set<NormalizedInventoryRow>();
    public DbSet<NormalizedArRow> NormalizedArRows => Set<NormalizedArRow>();
    public DbSet<NormalizedApRow> NormalizedApRows => Set<NormalizedApRow>();

    public DbSet<CatalogKpi> CatalogKpis => Set<CatalogKpi>();
    public DbSet<CatalogDriver> CatalogDrivers => Set<CatalogDriver>();
    public DbSet<CatalogInfluencer> CatalogInfluencers => Set<CatalogInfluencer>();
    public DbSet<CatalogKpiDriverMap> CatalogKpiDriverMaps => Set<CatalogKpiDriverMap>();
    public DbSet<CatalogDriverInfluencerMap> CatalogDriverInfluencerMaps => Set<CatalogDriverInfluencerMap>();
    public DbSet<CatalogScoreComponent> CatalogScoreComponents => Set<CatalogScoreComponent>();
    public DbSet<CatalogModule> CatalogModules => Set<CatalogModule>();
    public DbSet<CatalogOutputArea> CatalogOutputAreas => Set<CatalogOutputArea>();

    public DbSet<IssuePriorityScore> IssuePriorityScores => Set<IssuePriorityScore>();
    public DbSet<TenantKpiSelection> TenantKpiSelections => Set<TenantKpiSelection>();
    public DbSet<InfluencerEvidence> InfluencerEvidences => Set<InfluencerEvidence>();
    public DbSet<RoutingQueueItem> RoutingQueueItems => Set<RoutingQueueItem>();
    public DbSet<HoldoverComment> HoldoverComments => Set<HoldoverComment>();
    public DbSet<HoldoverStatusHistory> HoldoverStatusHistories => Set<HoldoverStatusHistory>();
    public DbSet<WorkAssignment> WorkAssignments => Set<WorkAssignment>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasIndex(x => x.ClientId).IsUnique();
        });

        modelBuilder.Entity<KpiDefinition>(entity =>
        {
            entity.HasIndex(x => new { x.BusinessProfileId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<KpiSnapshot>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.PeriodEnd, x.KpiDefinitionId }).IsUnique();
        });

        modelBuilder.Entity<DriverValue>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.PeriodEnd, x.PillarCode, x.DriverName, x.Rank });
        });

        modelBuilder.Entity<DriverDefinition>(entity =>
        {
            entity.HasIndex(x => new { x.BusinessProfileId, x.PillarCode, x.DriverCode }).IsUnique();
        });

        modelBuilder.Entity<BusinessProfile>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<VerticalLibrary>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<InfluencerDefinition>(entity =>
        {
            entity.HasIndex(x => new { x.BusinessProfileId, x.PillarCode, x.DriverCode, x.InfluencerCode }).IsUnique();
        });

        modelBuilder.Entity<TenantKpiOverride>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.KpiCode }).IsUnique();
        });

        modelBuilder.Entity<TenantDriverOverride>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.PillarCode, x.DriverCode }).IsUnique();
        });

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.PeriodEnd }).IsUnique();
        });

        modelBuilder.Entity<WeeklyFocus>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.PeriodEnd }).IsUnique();
        });

        modelBuilder.Entity<ActionItem>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.PeriodEnd, x.KpiDefinitionId });
            entity.HasIndex(x => new { x.TenantId, x.Status });
        });

        modelBuilder.Entity<ImportRun>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.PeriodEnd, x.StartedAt });
            entity.HasIndex(x => new { x.TenantId, x.PeriodEnd, x.SourceFingerprint });
        });

        modelBuilder.Entity<ImportRunIssue>(entity =>
        {
            entity.HasIndex(x => new { x.ImportRunId, x.Severity });
        });

        modelBuilder.Entity<UploadBatch>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.PeriodEnd });
        });

        modelBuilder.Entity<UploadedFile>(entity =>
        {
            entity.HasIndex(x => new { x.UploadBatchId, x.ReportType });
            entity.HasIndex(x => x.Sha256Hex);
        });

        modelBuilder.Entity<MappingTemplate>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.ReportType, x.Name }).IsUnique();
        });

        modelBuilder.Entity<MappingRule>(entity =>
        {
            entity.HasIndex(x => new { x.MappingTemplateId, x.SourceColumn }).IsUnique();
        });

        modelBuilder.Entity<UploadedFileColumnMap>(entity =>
        {
            entity.HasIndex(x => new { x.UploadedFileId, x.SourceColumn }).IsUnique();
        });

        modelBuilder.Entity<UploadBatchIssue>(entity =>
        {
            entity.HasIndex(x => new { x.UploadBatchId, x.Severity });
        });

        modelBuilder.Entity<NormalizedSalesRow>(entity =>
        {
            entity.HasIndex(x => new { x.UploadBatchId, x.UploadedFileId });
            entity.HasIndex(x => new { x.TenantId, x.PeriodEnd });
        });

        modelBuilder.Entity<NormalizedInventoryRow>(entity =>
        {
            entity.HasIndex(x => new { x.UploadBatchId, x.UploadedFileId });
            entity.HasIndex(x => new { x.TenantId, x.PeriodEnd });
        });

        modelBuilder.Entity<NormalizedArRow>(entity =>
        {
            entity.HasIndex(x => new { x.UploadBatchId, x.UploadedFileId });
            entity.HasIndex(x => new { x.TenantId, x.PeriodEnd });
        });

        modelBuilder.Entity<NormalizedApRow>(entity =>
        {
            entity.HasIndex(x => new { x.UploadBatchId, x.UploadedFileId });
            entity.HasIndex(x => new { x.TenantId, x.PeriodEnd });
        });

        modelBuilder.Entity<CatalogKpi>(entity => entity.HasKey(x => x.KpiId));
        modelBuilder.Entity<CatalogDriver>(entity => entity.HasKey(x => x.DriverId));
        modelBuilder.Entity<CatalogInfluencer>(entity => entity.HasKey(x => x.InfluencerId));
        modelBuilder.Entity<CatalogScoreComponent>(entity => entity.HasKey(x => x.Component));
        modelBuilder.Entity<CatalogModule>(entity => entity.HasKey(x => x.ModuleCode));
        modelBuilder.Entity<CatalogOutputArea>(entity => entity.HasKey(x => x.OutputAreaCode));

        modelBuilder.Entity<CatalogKpiDriverMap>(entity =>
        {
            entity.HasKey(x => new { x.KpiId, x.DriverId });
            entity.HasOne(x => x.Kpi).WithMany().HasForeignKey(x => x.KpiId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Driver).WithMany().HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CatalogDriverInfluencerMap>(entity =>
        {
            entity.HasKey(x => new { x.DriverId, x.InfluencerId });
            entity.HasOne(x => x.Driver).WithMany().HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Influencer).WithMany().HasForeignKey(x => x.InfluencerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IssuePriorityScore>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.PeriodEnd, x.KpiDefinitionId }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.PeriodEnd, x.CatalogKpiId });
            entity.HasOne(x => x.KpiDefinition).WithMany().HasForeignKey(x => x.KpiDefinitionId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.CatalogKpi).WithMany().HasForeignKey(x => x.CatalogKpiId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TenantKpiSelection>(entity =>
        {
            entity.HasKey(x => new { x.TenantId, x.CatalogKpiId });
            entity.HasOne(x => x.CatalogKpi).WithMany().HasForeignKey(x => x.CatalogKpiId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InfluencerEvidence>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.PeriodEnd, x.DriverValueId });
            entity.HasOne(x => x.DriverValue).WithMany().HasForeignKey(x => x.DriverValueId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Influencer).WithMany().HasForeignKey(x => x.InfluencerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoutingQueueItem>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.PeriodEnd, x.QueueType });
        });

        modelBuilder.Entity<HoldoverComment>(entity =>
        {
            entity.HasIndex(x => x.DriverValueId);
            entity.HasOne(x => x.DriverValue).WithMany().HasForeignKey(x => x.DriverValueId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HoldoverStatusHistory>(entity =>
        {
            entity.HasIndex(x => x.DriverValueId);
            entity.HasOne(x => x.DriverValue).WithMany().HasForeignKey(x => x.DriverValueId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkAssignment>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.PeriodEnd, x.TargetType, x.TargetId });
        });
    }
}
