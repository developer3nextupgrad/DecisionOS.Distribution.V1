namespace DecisionOS.Distribution.Domain.Uploads;

public interface IWeeklyScoringService
{
    Task<WeeklyScoringResult> ScorePeriodAsync(WeeklyScoringRequest request, CancellationToken ct = default);
}
