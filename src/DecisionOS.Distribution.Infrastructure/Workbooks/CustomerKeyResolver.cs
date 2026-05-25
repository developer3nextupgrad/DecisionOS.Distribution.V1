namespace DecisionOS.Distribution.Infrastructure.Workbooks;

public static class CustomerKeyResolver
{
    public const string NameKeyPrefix = "name:";

    public static (string? Id, string? DisplayName) Resolve(string? customerId, string? customerName)
    {
        var id = customerId?.Trim();
        var name = customerName?.Trim();

        if (!string.IsNullOrEmpty(id))
            return (id, string.IsNullOrEmpty(name) ? id : name);

        if (!string.IsNullOrEmpty(name))
            return ($"{NameKeyPrefix}{WorkbookParseHelper.NormalizeHeader(name)}", name);

        return (null, null);
    }
}
