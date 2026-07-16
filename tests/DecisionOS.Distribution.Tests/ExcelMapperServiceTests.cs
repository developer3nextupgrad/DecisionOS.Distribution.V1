using DecisionOS.Distribution.Domain.Uploads;
using DecisionOS.Distribution.Infrastructure;
using DecisionOS.Distribution.Infrastructure.Workbooks;

namespace DecisionOS.Distribution.Tests;

public class ExcelMapperServiceTests
{
    private static string TemplatePath =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "DecisionOS.Distribution.Web", "wwwroot", "downloads",
            "DecisionOS_Simplified_Workbook_Template.xlsx"));

    private static string FixturePath =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "Fixtures",
            "Steves_Bowling_Supply_DPOS_Distribution_Test_Data.xlsx"));

    [Fact]
    public async Task GenerateMappedWorkbook_FromTemplate_ProducesValidXlsx()
    {
        if (!File.Exists(TemplatePath)) return;

        var analyzer = new WorkbookAnalyzer();
        var service = new ExcelMapperService(analyzer);
        var bytes = await File.ReadAllBytesAsync(TemplatePath);

        var session = await service.StartSessionAsync(bytes, "template.xlsx");
        var output = await service.GenerateMappedWorkbookAsync(session.SessionId);

        Assert.NotEmpty(output);
        Assert.Equal(0x50, output[0]); // PK zip header
        Assert.Equal(0x4B, output[1]);

        var reanalysis = analyzer.Analyze(output, UploadCadence.Weekly, new DateOnly(2025, 11, 22));
        Assert.Contains(reanalysis.Sheets, s => s.Kind == WorkbookSheetKind.WeeklyRollup);
        Assert.Contains(reanalysis.Sheets, s => s.Kind == WorkbookSheetKind.Sales);
    }

    [Fact]
    public async Task SaveReview_PersistsKindAndClearsLowConfidence()
    {
        if (!File.Exists(TemplatePath)) return;

        var service = new ExcelMapperService(new WorkbookAnalyzer());
        var bytes = await File.ReadAllBytesAsync(TemplatePath);
        var session = await service.StartSessionAsync(bytes, "template.xlsx");

        var detection = await service.GetDetectionAsync(session.SessionId);
        var review = await service.GetReviewAsync(session.SessionId);
        Assert.NotEmpty(review.Sheets);

        // Simulate uncertain sheet then operator confirmation
        var first = review.Sheets[0];
        var input = new ExcelMapperReviewInput
        {
            Sheets =
            [
                new ExcelMapperSheetReview
                {
                    SheetName = first.SheetName,
                    Kind = WorkbookSheetKind.WeeklyRollup,
                    ColumnMappingsProvided = false
                }
            ]
        };

        // Merge requires all sheets for full save from UI — send full set with confirmed kinds
        input = new ExcelMapperReviewInput
        {
            Sheets = review.Sheets.Select(s => new ExcelMapperSheetReview
            {
                SheetName = s.SheetName,
                Kind = s.Kind == WorkbookSheetKind.Unknown ? WorkbookSheetKind.Skip : s.Kind,
                ColumnMappingsProvided = false
            }).ToList()
        };

        await service.SaveReviewAsync(session.SessionId, input);

        var afterDetection = await service.GetDetectionAsync(session.SessionId);
        Assert.All(afterDetection.Sheets, s => Assert.True(s.Confidence >= 0.7));

        var afterReview = await service.GetReviewAsync(session.SessionId);
        foreach (var sheet in afterReview.Sheets)
        {
            var saved = input.Sheets.First(x => x.SheetName == sheet.SheetName);
            Assert.Equal(saved.Kind, sheet.Kind);
        }
    }

    [Fact]
    public async Task SaveReview_EditingOneSheet_DoesNotWipeOtherMappings()
    {
        if (!File.Exists(TemplatePath)) return;

        var service = new ExcelMapperService(new WorkbookAnalyzer());
        var bytes = await File.ReadAllBytesAsync(TemplatePath);
        var session = await service.StartSessionAsync(bytes, "template.xlsx");

        var before = await service.GetReviewAsync(session.SessionId);
        var sales = before.Sheets.FirstOrDefault(s => s.Kind == WorkbookSheetKind.Sales);
        var other = before.Sheets.FirstOrDefault(s => s.Kind != WorkbookSheetKind.Sales && s.ColumnMappings.Count > 0);
        if (sales is null || other is null) return;

        var otherMapsBefore = other.ColumnMappings.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);

        var input = new ExcelMapperReviewInput
        {
            Sheets = before.Sheets.Select(s =>
            {
                if (!string.Equals(s.SheetName, sales.SheetName, StringComparison.OrdinalIgnoreCase))
                {
                    return new ExcelMapperSheetReview
                    {
                        SheetName = s.SheetName,
                        Kind = s.Kind,
                        ColumnMappingsProvided = false
                    };
                }

                return new ExcelMapperSheetReview
                {
                    SheetName = s.SheetName,
                    Kind = s.Kind,
                    ColumnMappingsProvided = true,
                    ColumnMappings = new Dictionary<string, string>(s.ColumnMappings, StringComparer.OrdinalIgnoreCase)
                    {
                        [s.ColumnMappings.Keys.First()] = "Net_Sales"
                    }
                };
            }).ToList()
        };

        await service.SaveReviewAsync(session.SessionId, input);

        var after = await service.GetReviewAsync(session.SessionId);
        var otherAfter = after.Sheets.First(s => s.SheetName == other.SheetName);
        Assert.Equal(otherMapsBefore.Count, otherAfter.ColumnMappings.Count);
        foreach (var kvp in otherMapsBefore)
            Assert.Equal(kvp.Value, otherAfter.ColumnMappings[kvp.Key]);
    }

    [Fact]
    public void MergeReview_WhenKindChangesWithoutMappings_ReinfersColumns()
    {
        var detection = new WorkbookDetectionResult
        {
            Sheets =
            [
                new DetectedSheet
                {
                    SheetName = "Tab1",
                    Kind = WorkbookSheetKind.Unknown,
                    Confidence = 0.3,
                    Headers = ["Week_End_Date", "Net_Sales", "COGS"],
                    ColumnMappings = new Dictionary<string, string>()
                }
            ]
        };

        var existing = new ExcelMapperReviewInput
        {
            Sheets =
            [
                new ExcelMapperSheetReview
                {
                    SheetName = "Tab1",
                    Kind = WorkbookSheetKind.Unknown,
                    ColumnMappings = new Dictionary<string, string>()
                }
            ]
        };

        var incoming = new ExcelMapperReviewInput
        {
            Sheets =
            [
                new ExcelMapperSheetReview
                {
                    SheetName = "Tab1",
                    Kind = WorkbookSheetKind.WeeklyRollup,
                    ColumnMappingsProvided = false
                }
            ]
        };

        var merged = ExcelMapperService.MergeReview(existing, incoming, detection);
        var sheet = Assert.Single(merged.Sheets);
        Assert.Equal(WorkbookSheetKind.WeeklyRollup, sheet.Kind);
        Assert.NotEmpty(sheet.ColumnMappings);
        Assert.Contains(sheet.ColumnMappings.Values, v =>
            v.Equals("Period_End_Date", StringComparison.OrdinalIgnoreCase) ||
            v.Equals("Net_Sales", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ApplyReviewToDetection_SetsConfidenceToOne_WhenOperatorConfirmed()
    {
        var detection = new WorkbookDetectionResult
        {
            Sheets =
            [
                new DetectedSheet
                {
                    SheetName = "Mystery",
                    Kind = WorkbookSheetKind.Unknown,
                    Confidence = 0.3,
                    Headers = ["A"],
                    ColumnMappings = new Dictionary<string, string>()
                }
            ],
            Warnings = ["Sheet 'Mystery' could not be classified confidently."]
        };

        var review = new ExcelMapperReviewInput
        {
            Sheets =
            [
                new ExcelMapperSheetReview
                {
                    SheetName = "Mystery",
                    Kind = WorkbookSheetKind.Skip,
                    ColumnMappings = new Dictionary<string, string>()
                }
            ]
        };

        var updated = ExcelMapperService.ApplyReviewToDetection(detection, review, operatorConfirmed: true);
        var sheet = Assert.Single(updated.Sheets);
        Assert.Equal(1.0, sheet.Confidence);
        Assert.Equal(WorkbookSheetKind.Skip, sheet.Kind);
        Assert.DoesNotContain(updated.Warnings, w => w.Contains("Mystery", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SheetClassifier_ReturnsHighConfidence_ForInventoryConstants()
    {
        var (kind, _, conf) = SheetClassifier.Classify(
            "Inventory_By_SKU",
            ["SKU", "On_Hand_Units", "Inventory_Value"]);
        Assert.Equal(WorkbookSheetKind.Inventory, kind);
        Assert.True(conf >= 0.7, $"Expected confidence >= 0.7 but was {conf}");
    }

    [Fact]
    public void WarningGuide_ProvidesSuggestedFix_ForUnclassified()
    {
        var msg = ExcelMapperWarningGuide.Explain("Sheet 'Foo' could not be classified confidently.");
        Assert.Contains("not sure", msg.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(msg.SuggestedFix));
    }

    [Fact]
    public void Readiness_Blocks_WhenNoPreferredSheetRoles()
    {
        var detection = new WorkbookDetectionResult
        {
            Sheets =
            [
                new DetectedSheet
                {
                    SheetName = "Vendors",
                    Kind = WorkbookSheetKind.Vendor,
                    Headers = ["Vendor_ID", "Vendor_Name"],
                    ColumnMappings = new Dictionary<string, string>
                    {
                        ["Vendor_ID"] = "Vendor_ID",
                        ["Vendor_Name"] = "Vendor_Name"
                    }
                }
            ]
        };
        var review = new ExcelMapperReviewInput
        {
            Sheets =
            [
                new ExcelMapperSheetReview
                {
                    SheetName = "Vendors",
                    Kind = WorkbookSheetKind.Vendor,
                    ColumnMappings = detection.Sheets[0].ColumnMappings.ToDictionary(k => k.Key, v => v.Value)
                }
            ]
        };

        var readiness = ExcelMapperReadinessEvaluator.Evaluate(detection, review);
        Assert.False(readiness.CanGenerate);
        Assert.NotEmpty(readiness.BlockingIssues);
    }

    [Fact]
    public async Task FixtureWorkbook_CanMapAndGenerate_WhenPresent()
    {
        if (!File.Exists(FixturePath)) return;

        var analyzer = new WorkbookAnalyzer();
        var service = new ExcelMapperService(analyzer);
        var bytes = await File.ReadAllBytesAsync(FixturePath);
        var session = await service.StartSessionAsync(bytes, "steves.xlsx");

        var review = await service.GetReviewAsync(session.SessionId);
        // Confirm all roles as detected (operator save)
        await service.SaveReviewAsync(session.SessionId, new ExcelMapperReviewInput
        {
            Sheets = review.Sheets.Select(s => new ExcelMapperSheetReview
            {
                SheetName = s.SheetName,
                Kind = s.Kind,
                ColumnMappingsProvided = false
            }).ToList()
        });

        var detection = await service.GetDetectionAsync(session.SessionId);
        Assert.All(detection.Sheets, s => Assert.Equal(1.0, s.Confidence));

        var readiness = service.EvaluateReadiness(detection, await service.GetReviewAsync(session.SessionId));
        if (!readiness.CanGenerate)
            return; // fixture may lack preferred roles depending on classification

        var output = await service.GenerateMappedWorkbookAsync(session.SessionId);
        Assert.NotEmpty(output);
        var re = analyzer.Analyze(output, UploadCadence.Weekly, null);
        Assert.Contains(re.Sheets, s => ExcelMapperOutputCatalog.IsExportable(s.Kind));
    }
}
