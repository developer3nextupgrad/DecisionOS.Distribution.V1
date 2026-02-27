using DecisionOS.Distribution.Domain;

namespace DecisionOS.Distribution.Tests;

public class KpiStatusServiceTests
{
    private readonly KpiStatusService _sut = new();

    [Fact]
    public void HigherIsBetter_ComputesStatusesCorrectly()
    {
        var def = new KpiDefinition
        {
            Direction = KpiDirection.HigherIsBetter,
            Target = 0.28m,
            AmberThreshold = 0.265m,
            RedThreshold = 0.25m
        };

        Assert.Equal("GREEN", _sut.ComputeStatus(def, 0.30m));
        Assert.Equal("YELLOW", _sut.ComputeStatus(def, 0.27m));
        Assert.Equal("RED", _sut.ComputeStatus(def, 0.20m));
    }

    [Fact]
    public void LowerIsBetter_ComputesStatusesCorrectly()
    {
        var def = new KpiDefinition
        {
            Direction = KpiDirection.LowerIsBetter,
            Target = 45m,
            AmberThreshold = 55m,
            RedThreshold = 70m
        };

        Assert.Equal("GREEN", _sut.ComputeStatus(def, 40m));
        Assert.Equal("YELLOW", _sut.ComputeStatus(def, 50m));
        Assert.Equal("RED", _sut.ComputeStatus(def, 80m));
    }

    [Fact]
    public void ComputeStatus_WhenDirectionIsDefaultOrUnknown_ReturnsUnknown()
    {
        var def = new KpiDefinition
        {
            Direction = (KpiDirection)99,
            Target = 0.28m,
            AmberThreshold = 0.265m,
            RedThreshold = 0.25m
        };

        Assert.Equal("UNKNOWN", _sut.ComputeStatus(def, 0.30m));
    }

    [Theory]
    [InlineData(0.28, "GREEN")]
    [InlineData(0.265, "YELLOW")]
    [InlineData(0.25, "YELLOW")]
    [InlineData(0.24, "RED")]
    public void HigherIsBetter_AtBoundaryValues_ReturnsExpectedStatus(decimal value, string expectedStatus)
    {
        var def = new KpiDefinition
        {
            Direction = KpiDirection.HigherIsBetter,
            Target = 0.28m,
            AmberThreshold = 0.265m,
            RedThreshold = 0.25m
        };

        Assert.Equal(expectedStatus, _sut.ComputeStatus(def, value));
    }

    [Theory]
    [InlineData(45, "GREEN")]
    [InlineData(46, "YELLOW")]
    [InlineData(55, "YELLOW")]
    [InlineData(56, "RED")]
    [InlineData(70, "RED")]
    public void LowerIsBetter_AtBoundaryValues_ReturnsExpectedStatus(decimal value, string expectedStatus)
    {
        var def = new KpiDefinition
        {
            Direction = KpiDirection.LowerIsBetter,
            Target = 45m,
            AmberThreshold = 55m,
            RedThreshold = 70m
        };

        Assert.Equal(expectedStatus, _sut.ComputeStatus(def, value));
    }
}
