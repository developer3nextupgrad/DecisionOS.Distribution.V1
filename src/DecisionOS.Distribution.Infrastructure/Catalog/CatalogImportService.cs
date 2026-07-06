using System.Globalization;
using DecisionOS.Distribution.Domain.Catalog;
using DecisionOS.Distribution.Infrastructure.Workbooks;
using Microsoft.EntityFrameworkCore;

namespace DecisionOS.Distribution.Infrastructure.Catalog;

public sealed class CatalogImportService : ICatalogImportService
{
    private static readonly string[] LegacyKpiCodes =
    [
        "GrossMargin%", "AR_PastDue31p%", "AP_PastDue31p%", "DOH", "CCC", "NetProfit%", "PerfectOrderRate"
    ];

    private readonly DecisionOsDbContext _db;

    public CatalogImportService(DecisionOsDbContext db) => _db = db;

    public async Task<CatalogImportResult> ImportFromWorkbookAsync(Stream xlsx, CancellationToken ct = default)
    {
        var result = new CatalogImportResult();
        byte[] bytes;
        using (var ms = new MemoryStream())
        {
            await xlsx.CopyToAsync(ms, ct);
            bytes = ms.ToArray();
        }

        ParsedWorkbook workbook;
        try
        {
            workbook = WorkbookParseHelper.Parse(bytes);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse workbook: {ex.Message}");
            return result;
        }

        await ImportKpisAsync(workbook, result, ct);
        await ImportDriversAsync(workbook, result, ct);
        await ImportInfluencersAsync(workbook, result, ct);
        await ImportKpiDriverMapsAsync(workbook, result, ct);
        await ImportDriverInfluencerMapsAsync(workbook, result, ct);
        await ImportScoreComponentsAsync(workbook, result, ct);
        await ImportModulesAsync(workbook, result, ct);
        await ImportOutputAreasAsync(workbook, result, ct);

        await _db.SaveChangesAsync(ct);
        return result;
    }

    private async Task ImportKpisAsync(ParsedWorkbook workbook, CatalogImportResult result, CancellationToken ct)
    {
        var sheet = FindSheet(workbook, "KPI_Catalog");
        if (sheet is null) { result.Errors.Add("Sheet KPI_Catalog not found."); return; }

        foreach (var row in sheet.Rows)
        {
            var id = Get(row, "KPI_ID");
            if (string.IsNullOrWhiteSpace(id)) continue;

            var entity = await _db.CatalogKpis.FindAsync([id.Trim()], ct);
            var isNew = entity is null;
            entity ??= new CatalogKpi { KpiId = id.Trim() };
            entity.Name = Get(row, "KPI_Name") ?? id;
            entity.Definition = Get(row, "KPI_Definition", "Definition") ?? "";
            entity.Category = Get(row, "Category", "KPI_Category");
            entity.EntityScope = Get(row, "Entity_Scope", "EntityScope");
            entity.Cadence = Get(row, "Cadence");
            entity.PrimaryDataNeeds = Get(row, "Primary_Data_Needs", "PrimaryDataNeeds");
            entity.DefaultStatusModel = Get(row, "Default_Status_Model", "DefaultStatusModel");
            entity.MgmtLayerCandidate = ParseBool(Get(row, "Mgmt_Layer_Candidate", "MgmtLayerCandidate"));
            entity.DeveloperNotes = Get(row, "Developer_Notes", "DeveloperNotes");
            entity.PrimaryModules = Get(row, "Primary_Modules", "PrimaryModules");
            entity.LegacyCode = ResolveLegacyCode(row, entity.Name);

            if (isNew) _db.CatalogKpis.Add(entity);
            result.KpisImported++;
        }
    }

    private async Task ImportDriversAsync(ParsedWorkbook workbook, CatalogImportResult result, CancellationToken ct)
    {
        var sheet = FindSheet(workbook, "Driver_Catalog");
        if (sheet is null) { result.Errors.Add("Sheet Driver_Catalog not found."); return; }

        foreach (var row in sheet.Rows)
        {
            var id = Get(row, "Driver_ID");
            if (string.IsNullOrWhiteSpace(id)) continue;

            var entity = await _db.CatalogDrivers.FindAsync([id.Trim()], ct);
            var isNew = entity is null;
            entity ??= new CatalogDriver { DriverId = id.Trim() };
            entity.Name = Get(row, "Driver_Name") ?? id;
            entity.Definition = Get(row, "Driver_Definition", "Definition") ?? "";
            entity.Category = Get(row, "Category", "Driver_Category");
            entity.EvidenceFields = Get(row, "Evidence_Fields", "EvidenceFields");
            entity.RelatedKpis = Get(row, "Related_KPIs", "RelatedKpis");
            entity.PrimaryModules = Get(row, "Primary_Modules", "PrimaryModules");

            if (isNew) _db.CatalogDrivers.Add(entity);
            result.DriversImported++;
        }
    }

    private async Task ImportInfluencersAsync(ParsedWorkbook workbook, CatalogImportResult result, CancellationToken ct)
    {
        var sheet = FindSheet(workbook, "Influencer_Catalog");
        if (sheet is null) { result.Errors.Add("Sheet Influencer_Catalog not found."); return; }

        foreach (var row in sheet.Rows)
        {
            var id = Get(row, "Influencer_ID");
            if (string.IsNullOrWhiteSpace(id)) continue;

            var entity = await _db.CatalogInfluencers.FindAsync([id.Trim()], ct);
            var isNew = entity is null;
            entity ??= new CatalogInfluencer { InfluencerId = id.Trim() };
            entity.Name = Get(row, "Influencer_Name") ?? id;
            entity.Definition = Get(row, "Influencer_Definition", "Definition") ?? "";
            entity.Category = Get(row, "Category", "Influencer_Category");
            entity.EvidenceFields = Get(row, "Evidence_Fields", "EvidenceFields");
            entity.DefaultSeverity = Get(row, "Default_Severity", "DefaultSeverity");
            entity.RelatedKpis = Get(row, "Related_KPIs", "RelatedKpis");
            entity.PrimaryModules = Get(row, "Primary_Modules", "PrimaryModules");

            if (isNew) _db.CatalogInfluencers.Add(entity);
            result.InfluencersImported++;
        }
    }

    private async Task ImportKpiDriverMapsAsync(ParsedWorkbook workbook, CatalogImportResult result, CancellationToken ct)
    {
        var sheet = FindSheet(workbook, "KPI_Driver_Map");
        if (sheet is null) { result.Errors.Add("Sheet KPI_Driver_Map not found."); return; }

        foreach (var row in sheet.Rows)
        {
            var kpiId = Get(row, "KPI_ID");
            var driverId = Get(row, "Driver_ID");
            if (string.IsNullOrWhiteSpace(kpiId) || string.IsNullOrWhiteSpace(driverId)) continue;

            kpiId = kpiId.Trim();
            driverId = driverId.Trim();

            var existing = await _db.CatalogKpiDriverMaps
                .FirstOrDefaultAsync(m => m.KpiId == kpiId && m.DriverId == driverId, ct);
            if (existing is null)
            {
                _db.CatalogKpiDriverMaps.Add(new CatalogKpiDriverMap
                {
                    KpiId = kpiId,
                    DriverId = driverId,
                    MapType = Get(row, "Map_Type", "MapType"),
                    PrimaryModules = Get(row, "Primary_Modules", "PrimaryModules"),
                    RuleNotes = Get(row, "Rule_Notes", "RuleNotes")
                });
                result.KpiDriverMapsImported++;
            }
            else
            {
                existing.MapType = Get(row, "Map_Type", "MapType");
                existing.PrimaryModules = Get(row, "Primary_Modules", "PrimaryModules");
                existing.RuleNotes = Get(row, "Rule_Notes", "RuleNotes");
                result.KpiDriverMapsImported++;
            }
        }
    }

    private async Task ImportDriverInfluencerMapsAsync(ParsedWorkbook workbook, CatalogImportResult result, CancellationToken ct)
    {
        var sheet = FindSheet(workbook, "Driver_Influencer_Map");
        if (sheet is null) { result.Errors.Add("Sheet Driver_Influencer_Map not found."); return; }

        foreach (var row in sheet.Rows)
        {
            var driverId = Get(row, "Driver_ID");
            var influencerId = Get(row, "Influencer_ID");
            if (string.IsNullOrWhiteSpace(driverId) || string.IsNullOrWhiteSpace(influencerId)) continue;

            driverId = driverId.Trim();
            influencerId = influencerId.Trim();

            var existing = await _db.CatalogDriverInfluencerMaps
                .FirstOrDefaultAsync(m => m.DriverId == driverId && m.InfluencerId == influencerId, ct);
            if (existing is null)
            {
                _db.CatalogDriverInfluencerMaps.Add(new CatalogDriverInfluencerMap
                {
                    DriverId = driverId,
                    InfluencerId = influencerId,
                    RelationshipType = Get(row, "Relationship_Type", "RelationshipType"),
                    DefaultWeight = ParseDecimal(Get(row, "Default_Weight", "DefaultWeight")),
                    RuleNotes = Get(row, "Rule_Notes", "RuleNotes")
                });
            }
            else
            {
                existing.RelationshipType = Get(row, "Relationship_Type", "RelationshipType");
                existing.DefaultWeight = ParseDecimal(Get(row, "Default_Weight", "DefaultWeight"));
                existing.RuleNotes = Get(row, "Rule_Notes", "RuleNotes");
            }
            result.DriverInfluencerMapsImported++;
        }
    }

    private async Task ImportScoreComponentsAsync(ParsedWorkbook workbook, CatalogImportResult result, CancellationToken ct)
    {
        var sheet = FindSheet(workbook, "Scoring_Logic");
        if (sheet is null) return;

        foreach (var row in sheet.Rows)
        {
            var component = Get(row, "Component");
            if (string.IsNullOrWhiteSpace(component)) continue;

            component = component.Trim();
            var entity = await _db.CatalogScoreComponents.FindAsync([component], ct);
            var isNew = entity is null;
            entity ??= new CatalogScoreComponent { Component = component };
            entity.ValueRange = Get(row, "Value_Range", "ValueRange");
            var weight = ParseDecimal(Get(row, "Weight_Percent", "WeightPercent", "Weight_%"));
            if (weight is not null) entity.WeightPercent = weight.Value;
            entity.RequirementLevel = Get(row, "Requirement_Level", "RequirementLevel");
            entity.ImplementationNotes = Get(row, "Implementation_Notes", "ImplementationNotes");

            if (isNew) _db.CatalogScoreComponents.Add(entity);
            result.ScoreComponentsImported++;
        }
    }

    private async Task ImportModulesAsync(ParsedWorkbook workbook, CatalogImportResult result, CancellationToken ct)
    {
        var sheet = FindSheet(workbook, "Module_Routing");
        if (sheet is null) return;

        foreach (var row in sheet.Rows)
        {
            var code = Get(row, "Module", "Module_Code", "ModuleCode");
            if (string.IsNullOrWhiteSpace(code)) continue;

            code = code.Trim();
            var entity = await _db.CatalogModules.FindAsync([code], ct);
            var isNew = entity is null;
            entity ??= new CatalogModule { ModuleCode = code };
            entity.Name = Get(row, "Module_Name", "Name") ?? code;
            entity.PrimaryKpis = Get(row, "Primary_KPIs", "PrimaryKpis");
            entity.DefaultOutput = Get(row, "Default_Output", "DefaultOutput");
            entity.Description = Get(row, "Description");

            if (isNew) _db.CatalogModules.Add(entity);
            result.ModulesImported++;
        }
    }

    private async Task ImportOutputAreasAsync(ParsedWorkbook workbook, CatalogImportResult result, CancellationToken ct)
    {
        var sheet = FindSheet(workbook, "Output_Assignment");
        if (sheet is null) return;

        foreach (var row in sheet.Rows)
        {
            var code = Get(row, "Output_Area", "OutputArea", "Management_Layer", "Code");
            var name = Get(row, "Name", "Output_Area_Name");
            if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(name)) continue;

            code = (code ?? name)!.Trim();
            var entity = await _db.CatalogOutputAreas.FindAsync([code], ct);
            var isNew = entity is null;
            entity ??= new CatalogOutputArea { OutputAreaCode = code };
            entity.Name = name ?? code;
            entity.Description = Get(row, "Description");
            entity.RoutingNotes = Get(row, "Routing_Notes", "RoutingNotes");

            if (isNew) _db.CatalogOutputAreas.Add(entity);
            result.OutputAreasImported++;
        }
    }

    internal static string? ResolveLegacyCode(IReadOnlyDictionary<string, string?> row, string name)
    {
        var explicitCode = Get(row, "Legacy_Code", "LegacyCode", "KPI_Code", "Code");
        if (!string.IsNullOrWhiteSpace(explicitCode) && LegacyKpiCodes.Contains(explicitCode.Trim(), StringComparer.OrdinalIgnoreCase))
            return LegacyKpiCodes.First(c => c.Equals(explicitCode.Trim(), StringComparison.OrdinalIgnoreCase));

        foreach (var code in LegacyKpiCodes)
        {
            if (name.Contains(code, StringComparison.OrdinalIgnoreCase))
                return code;
        }

        var normalized = name.ToLowerInvariant();
        if (normalized.Contains("gross margin")) return "GrossMargin%";
        if (normalized.Contains("a/r") || normalized.Contains("accounts receivable") || normalized.Contains("ar health"))
            return "AR_PastDue31p%";
        if (normalized.Contains("a/p") || normalized.Contains("accounts payable"))
            return "AP_PastDue31p%";
        if (normalized.Contains("days on hand") || normalized.Contains("inventory health") || normalized == "doh")
            return "DOH";
        if (normalized.Contains("cash conversion") || normalized == "ccc")
            return "CCC";
        if (normalized.Contains("net profit"))
            return "NetProfit%";
        if (normalized.Contains("perfect order") || normalized.Contains("fulfillment"))
            return "PerfectOrderRate";

        return null;
    }

    private static ParsedSheet? FindSheet(ParsedWorkbook workbook, string name)
        => workbook.Sheets.FirstOrDefault(s =>
            s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    private static string? Get(IReadOnlyDictionary<string, string?> row, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (row.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                return val.Trim();
        }
        return null;
    }

    private static bool ParseBool(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var s = raw.Trim().ToLowerInvariant();
        return s is "y" or "yes" or "true" or "1" or "x";
    }

    private static decimal? ParseDecimal(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var s = raw.Trim().Replace("%", "");
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
    }
}
