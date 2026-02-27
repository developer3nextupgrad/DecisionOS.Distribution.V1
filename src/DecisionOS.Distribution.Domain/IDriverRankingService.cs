namespace DecisionOS.Distribution.Domain;

public interface IDriverRankingService
{
    IReadOnlyList<DriverValue> RankDriversForPillar(IReadOnlyList<DriverValue> allDrivers, string pillarCode, int topN = 5);
}
