namespace DecisionOS.Distribution.Domain.Normalized;

public enum RowStatus
{
    Valid = 0,
    Corrected = 1,
    Warning = 2,
    Rejected = 3,
    NeedsReview = 4
}

