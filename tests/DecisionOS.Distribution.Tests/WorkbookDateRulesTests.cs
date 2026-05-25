using DecisionOS.Distribution.Infrastructure.Workbooks;

namespace DecisionOS.Distribution.Tests;

public class WorkbookDateRulesTests
{
    [Theory]
    [InlineData("2026-02-28", 2026, 2, 28)]
    [InlineData("2025-11-22", 2025, 11, 22)]
    public void TryParsePeriodDate_AcceptsPlausibleWeeks(string raw, int y, int m, int d)
    {
        var parsed = WorkbookDateRules.TryParsePeriodDate(raw);
        Assert.NotNull(parsed);
        Assert.Equal(new DateOnly(y, m, d), parsed);
    }

    [Theory]
    [InlineData("8818-03-01")]
    [InlineData("9648-03-01")]
    [InlineData("9025-07-01")]
    public void TryParsePeriodDate_RejectsImplausibleYears(string raw)
    {
        Assert.Null(WorkbookDateRules.TryParsePeriodDate(raw));
    }

    [Fact]
    public void IsNonPeriodDataColumn_BlocksCustomerAndWeekNumberHeaders()
    {
        Assert.True(WorkbookDateRules.IsNonPeriodDataColumn("Customer_ID"));
        Assert.True(WorkbookDateRules.IsNonPeriodDataColumn("Week_Number"));
        Assert.False(WorkbookDateRules.IsNonPeriodDataColumn("Week_End_Date"));
    }

    [Fact]
    public void CustomerKeyResolver_UsesNameWhenIdMissing()
    {
        var (id, name) = CustomerKeyResolver.Resolve(null, "Acme Bowling");
        Assert.StartsWith(CustomerKeyResolver.NameKeyPrefix, id);
        Assert.Equal("Acme Bowling", name);
    }
}
