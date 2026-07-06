namespace DecisionOS.Distribution.Domain.Catalog;

public interface ICatalogKpiDefinitionSyncService
{
    /// <summary>Ensures each <see cref="CatalogKpi"/> has a global <see cref="KpiDefinition"/> row for scoring snapshots.</summary>
    Task<int> SyncGlobalDefinitionsAsync(CancellationToken ct = default);
}
