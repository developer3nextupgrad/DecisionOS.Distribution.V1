using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Tests;

public class DecisionOsDbContextTests
{
    [Fact]
    public async Task DbContext_CanCreateAndSaveTenant()
    {
        var options = new DbContextOptionsBuilder<DecisionOsDbContext>()
            .UseInMemoryDatabase(databaseName: "DbContext_CanCreateAndSaveTenant")
            .Options;

        await using var db = new DecisionOsDbContext(options);
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            ClientId = "TEST-001",
            Name = "Test Distributor",
            Archetype = "Pilot"
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var loaded = await db.Tenants.FirstOrDefaultAsync(t => t.ClientId == "TEST-001");
        Assert.NotNull(loaded);
        Assert.Equal("Test Distributor", loaded.Name);
        Assert.Equal("Pilot", loaded.Archetype);
    }

    [Fact]
    public void DbContext_HasUniqueIndexOnClientId()
    {
        var options = new DbContextOptionsBuilder<DecisionOsDbContext>()
            .UseInMemoryDatabase(databaseName: "DbContext_HasUniqueIndexOnClientId")
            .Options;

        using var db = new DecisionOsDbContext(options);
        var entityType = db.Model.FindEntityType(typeof(Tenant))!;
        var index = entityType.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(Tenant.ClientId)));

        Assert.NotNull(index);
        Assert.True(index.IsUnique);
    }

    [Fact]
    public void DbContext_KpiDefinitions_HasCompositeUniqueIndexOnProfileAndCode()
    {
        var options = new DbContextOptionsBuilder<DecisionOsDbContext>()
            .UseInMemoryDatabase(databaseName: "DbContext_KpiDefinitions_HasCompositeUniqueIndexOnProfileAndCode")
            .Options;

        using var db = new DecisionOsDbContext(options);
        var entityType = db.Model.FindEntityType(typeof(KpiDefinition))!;
        var index = entityType.GetIndexes()
            .FirstOrDefault(i =>
                i.IsUnique &&
                i.Properties.Count == 2 &&
                i.Properties.Any(p => p.Name == nameof(KpiDefinition.BusinessProfileId)) &&
                i.Properties.Any(p => p.Name == nameof(KpiDefinition.Code)));

        Assert.NotNull(index);
    }

    [Fact]
    public void DbContext_DriverDefinitions_HasCompositeUniqueIndexOnProfilePillarDriverCode()
    {
        var options = new DbContextOptionsBuilder<DecisionOsDbContext>()
            .UseInMemoryDatabase(databaseName: "DbContext_DriverDefinitions_HasCompositeUniqueIndexOnProfilePillarDriverCode")
            .Options;

        using var db = new DecisionOsDbContext(options);
        var entityType = db.Model.FindEntityType(typeof(DriverDefinition))!;
        var index = entityType.GetIndexes()
            .FirstOrDefault(i =>
                i.IsUnique &&
                i.Properties.Count == 3 &&
                i.Properties.Any(p => p.Name == nameof(DriverDefinition.BusinessProfileId)) &&
                i.Properties.Any(p => p.Name == nameof(DriverDefinition.PillarCode)) &&
                i.Properties.Any(p => p.Name == nameof(DriverDefinition.DriverCode)));

        Assert.NotNull(index);
    }

    [Fact]
    public async Task DbContext_CanSaveKpiDefinitionAndSnapshot()
    {
        var options = new DbContextOptionsBuilder<DecisionOsDbContext>()
            .UseInMemoryDatabase(databaseName: "DbContext_CanSaveKpiDefinitionAndSnapshot")
            .Options;

        await using var db = new DecisionOsDbContext(options);
        var tenant = new Tenant { Id = Guid.NewGuid(), ClientId = "KPI-TEST", Name = "KPI Test" };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var def = new KpiDefinition
        {
            Code = "CCC",
            Name = "Cash Conversion Cycle",
            Unit = "days",
            Direction = KpiDirection.LowerIsBetter,
            Target = 45m,
            AmberThreshold = 55m,
            RedThreshold = 70m,
            RecommendedAction = "Action",
            DiagnosticChecks = "Checks"
        };
        db.KpiDefinitions.Add(def);
        await db.SaveChangesAsync();

        var snapshot = new KpiSnapshot
        {
            TenantId = tenant.Id,
            PeriodEnd = new DateOnly(2025, 2, 28),
            KpiDefinitionId = def.Id,
            Value = 50m,
            Status = "YELLOW"
        };
        db.KpiSnapshots.Add(snapshot);
        await db.SaveChangesAsync();

        var loaded = await db.KpiSnapshots
            .Include(s => s.KpiDefinition)
            .Include(s => s.Tenant)
            .FirstOrDefaultAsync(s => s.TenantId == tenant.Id);
        Assert.NotNull(loaded);
        Assert.Equal("CCC", loaded.KpiDefinition.Code);
        Assert.Equal(50m, loaded.Value);
        Assert.Equal("YELLOW", loaded.Status);
    }
}
