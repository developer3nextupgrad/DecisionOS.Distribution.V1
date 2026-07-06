namespace DecisionOS.Distribution.Domain.Workflow;

public static class WorkflowTargetTypes
{
    public const string Driver = "Driver";
}

public sealed class AssignableUserDto
{
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = "";
    public string Email { get; init; } = "";
}

public sealed class HoldoverCommentDto
{
    public long Id { get; init; }
    public Guid AuthorUserId { get; init; }
    public string AuthorName { get; init; } = "";
    public bool IsMine { get; init; }
    public string Body { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class HoldoverWorkflowDto
{
    public Guid? AssigneeUserId { get; init; }
    public string? AssigneeName { get; init; }
    public IReadOnlyList<HoldoverCommentDto> Comments { get; init; } = Array.Empty<HoldoverCommentDto>();
}
