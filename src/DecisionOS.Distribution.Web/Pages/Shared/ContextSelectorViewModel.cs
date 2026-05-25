using Microsoft.AspNetCore.Mvc.Rendering;

namespace DecisionOS.Distribution.Web.Pages.Shared;

public sealed class ContextSelectorViewModel
{
    public List<SelectListItem> Tenants { get; init; } = new();
    public List<SelectListItem> Customers { get; init; } = new();
    public List<SelectListItem> Weeks { get; init; } = new();
    public string? SelectedClientId { get; init; }
    public string? SelectedCustomerId { get; init; }
    public string? SelectedPeriodEnd { get; init; }
    public bool ShowGoButton { get; init; }
}
