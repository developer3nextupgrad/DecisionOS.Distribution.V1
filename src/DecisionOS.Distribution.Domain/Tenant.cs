namespace DecisionOS.Distribution.Domain;

public class Tenant
{
    public Guid Id { get; set; }
    public string ClientId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Archetype { get; set; }
}
