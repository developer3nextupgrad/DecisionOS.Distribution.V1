using DecisionOS.Distribution.Domain;
using Xunit;

namespace DecisionOS.Distribution.Tests;

public class OwnerLanguageTests
{
    [Fact]
    public void ExpandFinanceAbbreviations_SpellsOutFinanceTermsInPlainEnglish()
    {
        var result = OwnerLanguage.ExpandFinanceAbbreviations("Check DSO, DIO, DPO and COGS by SKU.");
        Assert.Contains("how long customers take to pay you", result);
        Assert.Contains("how long inventory sits before it sells", result);
        Assert.Contains("how long you take to pay vendors", result);
        Assert.Contains("product cost", result);
        Assert.DoesNotContain("DSO", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CccDiagnosticChecks_UsesPlainLanguage()
    {
        Assert.Contains("customers pay you", OwnerLanguage.CccDiagnosticChecks);
        Assert.DoesNotContain("DSO", OwnerLanguage.CccDiagnosticChecks, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("GREEN", "On track")]
    [InlineData("YELLOW", "Needs attention")]
    [InlineData("RED", "Urgent")]
    [InlineData("GRAY", "Not enough data")]
    public void PlainStatusLabel_ReturnsOwnerFriendlyText(string status, string expected)
    {
        Assert.Equal(expected, OwnerLanguage.PlainStatusLabel(status));
    }

    [Fact]
    public void FormatGap_UsesPercentagePointsNotAbbreviation()
    {
        var def = new KpiDefinition { Unit = "pct", Direction = KpiDirection.HigherIsBetter };
        var gap = OwnerLanguage.FormatGap(def, 0.019m, "below", "28.0%");
        Assert.Contains("percentage points", gap);
        Assert.DoesNotContain("pp", gap);
        Assert.Contains("your goal", gap);
    }

    [Fact]
    public void PlainMissingDataItems_AvoidTechnicalColumnNames()
    {
        var items = OwnerLanguage.PlainMissingDataItems("GrossMargin%");
        Assert.All(items, i =>
        {
            Assert.DoesNotContain("Net_Sales", i);
            Assert.DoesNotContain("COGS", i);
        });
        Assert.Contains(items, i => i.Contains("sales", StringComparison.OrdinalIgnoreCase));
    }
}
