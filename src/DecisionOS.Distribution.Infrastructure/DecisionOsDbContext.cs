using DecisionOS.Distribution.Domain;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Infrastructure;

public class DecisionOsDbContext : DbContext
{
    public DecisionOsDbContext(DbContextOptions<DecisionOsDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<KpiDefinition> KpiDefinitions => Set<KpiDefinition>();
    public DbSet<KpiSnapshot> KpiSnapshots => Set<KpiSnapshot>();
    public DbSet<DriverValue> DriverValues => Set<DriverValue>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<WeeklyFocus> WeeklyFocuses => Set<WeeklyFocus>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasIndex(x => x.ClientId).IsUnique();
        });

        modelBuilder.Entity<KpiDefinition>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<KpiSnapshot>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.PeriodEnd, x.KpiDefinitionId }).IsUnique();
        });

        modelBuilder.Entity<DriverValue>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.PeriodEnd, x.PillarCode, x.DriverName, x.Rank });
        });

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.PeriodEnd }).IsUnique();
        });

        modelBuilder.Entity<WeeklyFocus>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.PeriodEnd }).IsUnique();
        });
    }
}
