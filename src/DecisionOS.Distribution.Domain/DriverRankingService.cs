namespace DecisionOS.Distribution.Domain;

public class DriverRankingService : IDriverRankingService
{
    public IReadOnlyList<DriverValue> RankDriversForPillar(IReadOnlyList<DriverValue> allDrivers, string pillarCode, int topN = 5)
    {
        var ranked = allDrivers
            .Where(d => d.PillarCode == pillarCode)
            .OrderByDescending(d => d.Current)
            .Take(topN)
            .ToList();

        for (var i = 0; i < ranked.Count; i++)
            ranked[i].Rank = i + 1;

        return ranked;
    }
}
