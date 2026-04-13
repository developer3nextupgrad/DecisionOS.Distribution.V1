namespace DecisionOS.Distribution.Domain;

/// <summary>
/// A broad business family library (e.g., Distribution, Retail, Service) that contains multiple Business Profiles.
/// </summary>
public class VerticalLibrary
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

