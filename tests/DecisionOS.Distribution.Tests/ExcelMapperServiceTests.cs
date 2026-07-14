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
}
