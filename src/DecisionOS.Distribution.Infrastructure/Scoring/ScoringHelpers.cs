namespace DecisionOS.Distribution.Infrastructure.Scoring;

internal static class ScoringHelpers
{
    public static bool IsPastDue31(int? daysPastDue, string? bucket)
    {
        if (daysPastDue is >= 31) return true;
        if (string.IsNullOrWhiteSpace(bucket)) return false;
        var b = bucket.Trim().ToLowerInvariant();
        return b.Contains("31") || b.Contains("60") || b.Contains("90") ||
               b.Contains("past due") || b.Contains("over");
    }
}
