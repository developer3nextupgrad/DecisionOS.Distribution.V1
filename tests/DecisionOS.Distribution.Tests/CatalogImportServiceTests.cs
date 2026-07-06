using DecisionOS.Distribution.Infrastructure;
using DecisionOS.Distribution.Infrastructure.Catalog;
using DecisionOS.Distribution.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Tests;

public class CatalogImportServiceTests
{
    private static DecisionOsDbContext MakeDb(string name)
    {
        var options = new DbContextOptionsBuilder<DecisionOsDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new DecisionOsDbContext(options);
    }

    [Fact]
    public async Task ImportFromWorkbook_ImportsExpectedCounts()
    {
        await using var db = MakeDb(nameof(ImportFromWorkbook_ImportsExpectedCounts));
        var sut = new CatalogImportService(db);
        var bytes = CatalogTestFixtureBuilder.BuildMinimalCatalogWorkbook();

        await using var stream = new MemoryStream(bytes);
        var result = await sut.ImportFromWorkbookAsync(stream);

        Assert.Equal(24, result.KpisImported);
        Assert.Equal(36, result.DriversImported);
        Assert.Equal(60, result.InfluencersImported);
        Assert.Equal(84, result.KpiDriverMapsImported);
        Assert.Equal(60, result.DriverInfluencerMapsImported);
        Assert.True(result.ScoreComponentsImported >= 6);

        Assert.Equal(24, await db.CatalogKpis.CountAsync());
        Assert.Equal(7, await db.CatalogKpis.CountAsync(k => k.LegacyCode != null));
    }

    [Fact]
    public async Task ImportFromWorkbook_IsIdempotent()
    {
        await using var db = MakeDb(nameof(ImportFromWorkbook_IsIdempotent));
        var sut = new CatalogImportService(db);
        var bytes = CatalogTestFixtureBuilder.BuildMinimalCatalogWorkbook();

        await using (var s1 = new MemoryStream(bytes))
            await sut.ImportFromWorkbookAsync(s1);
        await using (var s2 = new MemoryStream(bytes))
            await sut.ImportFromWorkbookAsync(s2);

        Assert.Equal(24, await db.CatalogKpis.CountAsync());
        Assert.Equal(36, await db.CatalogDrivers.CountAsync());
    }
}
