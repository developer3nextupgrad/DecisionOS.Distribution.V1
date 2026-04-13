using DecisionOS.Distribution.Domain;
using DecisionOS.Distribution.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Tests;

public class DefinitionResolverTests
{
    private static DecisionOsDbContext MakeDb(string name)
    {
        var options = new DbContextOptionsBuilder<DecisionOsDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new DecisionOsDbContext(options);
    }

    [Fact]
    public async Task ResolveKpiDefinitions_ProfileOverridesWin_ThenTenantOverrideWins()
    {
        await using var db = MakeDb(nameof(ResolveKpiDefinitions_ProfileOverridesWin_ThenTenantOverrideWins));

        var profileId = Guid.NewGuid();
        db.BusinessProfiles.Add(new BusinessProfile { Id = profileId, Code = "P1", Name = "P1" });

        // Global
        db.KpiDefinitions.Add(new KpiDefinition
        {
            BusinessProfileId = null,
            Code = "CCC",
            Name = "CCC",
            Unit = "days",
            Direction = KpiDirection.LowerIsBetter,
            Target = 10m,
            AmberThreshold = 20m,
            RedThreshold = 30m,
            AlertPriority = 50,
            RecommendedAction = "Global Action",
            DiagnosticChecks = "Global Checks"
        });

        // Profile override
        db.KpiDefinitions.Add(new KpiDefinition
        {
            BusinessProfileId = profileId,
            Code = "CCC",
            Name = "CCC (Profile)",
            Unit = "days",
            Direction = KpiDirection.LowerIsBetter,
            Target = 11m,
            AmberThreshold = 21m,
            RedThreshold = 31m,
            AlertPriority = 40,
            RecommendedAction = "Profile Action",
            DiagnosticChecks = "Profile Checks"
        });

        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new Tenant { Id = tenantId, ClientId = "T", Name = "T", BusinessProfileId = profileId });

        // Tenant override
        db.TenantKpiOverrides.Add(new TenantKpiOverride
        {
            TenantId = tenantId,
            KpiCode = "CCC",
            Target = 99m,
            IsActive = true
        });

        await db.SaveChangesAsync();

        var resolver = new DefinitionResolver(db);
        var tenant = await db.Tenants.FirstAsync();
        var defs = await resolver.ResolveKpiDefinitionsAsync(tenant);

        Assert.True(defs.TryGetValue("CCC", out var def));
        Assert.Equal(99m, def!.Target); // tenant override wins
        Assert.Equal("Profile Action", def.RecommendedAction); // profile beats global when not overridden
    }

    [Fact]
    public async Task ResolveDriverDefinitions_ProfileOverridesWin_ThenTenantOverrideWins()
    {
        await using var db = MakeDb(nameof(ResolveDriverDefinitions_ProfileOverridesWin_ThenTenantOverrideWins));

        var profileId = Guid.NewGuid();
        db.BusinessProfiles.Add(new BusinessProfile { Id = profileId, Code = "P1", Name = "P1" });

        db.DriverDefinitions.Add(new DriverDefinition
        {
            BusinessProfileId = null,
            PillarCode = "CCC",
            DriverCode = "ccc_dso",
            DisplayName = "Global DSO",
            SortOrder = 1,
            IsActive = true
        });

        db.DriverDefinitions.Add(new DriverDefinition
        {
            BusinessProfileId = profileId,
            PillarCode = "CCC",
            DriverCode = "ccc_dso",
            DisplayName = "Profile DSO",
            SortOrder = 1,
            IsActive = true
        });

        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new Tenant { Id = tenantId, ClientId = "T", Name = "T", BusinessProfileId = profileId });

        db.TenantDriverOverrides.Add(new TenantDriverOverride
        {
            TenantId = tenantId,
            PillarCode = "CCC",
            DriverCode = "ccc_dso",
            DisplayName = "Tenant DSO",
            IsActive = false
        });

        await db.SaveChangesAsync();

        var resolver = new DefinitionResolver(db);
        var tenant = await db.Tenants.FirstAsync();
        var defs = await resolver.ResolveDriverDefinitionsAsync(tenant);

        var d = defs.First(x => x.PillarCode == "CCC" && x.DriverCode == "ccc_dso");
        Assert.Equal("Tenant DSO", d.DisplayName);
        Assert.False(d.IsActive);
    }
}

