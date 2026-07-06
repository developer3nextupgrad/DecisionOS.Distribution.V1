namespace DecisionOS.Distribution.Domain.Workflow;

public interface IHoldoverWorkflowService
{
    Task<IReadOnlyList<AssignableUserDto>> GetAssignableUsersAsync(CancellationToken ct = default);

    Task<HoldoverWorkflowDto?> GetDriverWorkflowAsync(
        Guid tenantId,
        int driverId,
        Guid currentUserId,
        CancellationToken ct = default);

    Task<HoldoverWorkflowDto> AssignDriverAsync(
        Guid tenantId,
        string clientId,
        DateOnly periodEnd,
        int driverId,
        Guid assigneeUserId,
        Guid assignedByUserId,
        CancellationToken ct = default);

    Task<HoldoverCommentDto> AddCommentAsync(
        Guid tenantId,
        string clientId,
        DateOnly periodEnd,
        int driverId,
        Guid authorUserId,
        string body,
        CancellationToken ct = default);

    Task<IReadOnlyDictionary<int, Guid?>> GetAssigneeIdsForDriversAsync(
        Guid tenantId,
        IReadOnlyList<int> driverIds,
        CancellationToken ct = default);
}
