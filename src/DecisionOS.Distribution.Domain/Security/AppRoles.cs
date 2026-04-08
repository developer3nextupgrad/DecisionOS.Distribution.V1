namespace DecisionOS.Distribution.Domain.Security;

/// <summary>OPSPEC-style roles for Decision OS Distribution pilot.</summary>
public static class AppRoles
{
    public const string Admin = nameof(Admin);
    public const string Operator = nameof(Operator);
    public const string Viewer = nameof(Viewer);
    public const string Developer = nameof(Developer);

    public static readonly string[] All = { Admin, Operator, Viewer, Developer };

    /// <summary>Roles that may access operational import history and diagnostics.</summary>
    public static readonly string[] Ops = { Admin, Operator, Developer };
}
