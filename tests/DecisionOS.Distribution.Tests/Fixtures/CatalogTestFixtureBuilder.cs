using System.IO.Compression;
using System.Text;

namespace DecisionOS.Distribution.Tests.Fixtures;

/// <summary>Builds a minimal Open XML xlsx for catalog import tests (no external package).</summary>
public static class CatalogTestFixtureBuilder
{
    public static byte[] BuildMinimalCatalogWorkbook()
    {
        var kpiRows = Enumerable.Range(1, 24).Select(i => new[]
        {
            $"KPI-{i:D3}",
            i switch
            {
                1 => "Gross Margin %",
                2 => "A/R Health",
                3 => "A/P & Purchasing Efficiency",
                4 => "Inventory Health (DOH)",
                5 => "Cash Conversion Cycle (CCC)",
                6 => "Net Profit %",
                7 => "Service / Fulfillment (Perfect Order)",
                _ => $"Catalog KPI {i}"
            },
            $"Definition {i}",
            "Financial",
            "Company",
            "Weekly",
            "Sales data",
            "R/Y/G",
            i <= 7 ? "Y" : "N",
            "",
            "Finance",
            ""
        }).ToList();

        var driverRows = Enumerable.Range(1, 36).Select(i => new[]
        {
            $"DRV-{i:D3}",
            $"Driver {i}",
            $"Driver def {i}",
            "Operational",
            "OpenBalance,Customer_ID",
            "KPI-002",
            "AR"
        }).ToList();

        var influencerRows = Enumerable.Range(1, 60).Select(i => new[]
        {
            $"INF-{i:D3}",
            $"Influencer {i}",
            $"Influencer def {i}",
            "Root cause",
            "DisputeReason",
            "Medium",
            "KPI-002",
            "AR"
        }).ToList();

        var kpiDriverMaps = new List<string[]>();
        var seenPairs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 1; i <= 84; i++)
        {
            var kpi = ((i - 1) % 24) + 1;
            var drv = ((i - 1) % 36) + 1;
            var key = $"KPI-{kpi:D3}|DRV-{drv:D3}";
            if (!seenPairs.Add(key)) continue;
            kpiDriverMaps.Add([$"KPI-{kpi:D3}", $"DRV-{drv:D3}", "Primary", "AR", ""]);
        }
        while (kpiDriverMaps.Count < 84)
        {
            var kpi = (kpiDriverMaps.Count % 24) + 1;
            var drv = ((kpiDriverMaps.Count * 7) % 36) + 1;
            var key = $"KPI-{kpi:D3}|DRV-{drv:D3}";
            if (!seenPairs.Add(key)) { drv = (drv % 36) + 1; key = $"KPI-{kpi:D3}|DRV-{drv:D3}"; if (!seenPairs.Add(key)) continue; }
            kpiDriverMaps.Add([$"KPI-{kpi:D3}", $"DRV-{drv:D3}", "Primary", "AR", ""]);
        }

        var driverInfMaps = Enumerable.Range(1, 60).Select(i => new[]
        {
            $"DRV-{((i - 1) % 36) + 1:D3}",
            $"INF-{i:D3}",
            "Evidence",
            "50",
            ""
        }).ToList();

        var scoreRows = new[]
        {
            new[] { "Severity", "0-100", "30", "Required", "" },
            new[] { "Cash", "0-100", "20", "Required", "" },
            new[] { "Financial", "0-100", "20", "Required", "" },
            new[] { "Urgency", "0-100", "15", "Required", "" },
            new[] { "Actionability", "0-100", "10", "Required", "" },
            new[] { "Confidence", "0-100", "5", "Required", "" }
        };

        var moduleRows = Enumerable.Range(1, 9).Select(i => new[]
        {
            $"MOD-{i}",
            $"Module {i}",
            "KPI-002",
            "ModuleAction",
            ""
        }).ToList();

        var outputRows = new[]
        {
            new[] { "Management", "Management Layer", "", "" },
            new[] { "Watchlist", "Watchlist", "", "" },
            new[] { "DataGap", "Data Gap Queue", "", "" }
        };

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(zip, "[Content_Types].xml", ContentTypesXml());
            WriteEntry(zip, "_rels/.rels", RootRelsXml());
            WriteEntry(zip, "xl/workbook.xml", WorkbookXml());
            WriteEntry(zip, "xl/_rels/workbook.xml.rels", WorkbookRelsXml());
            WriteEntry(zip, "xl/styles.xml", StylesXml());
            WriteEntry(zip, "xl/worksheets/sheet1.xml", SheetXml("KPI_Catalog",
                ["KPI_ID", "KPI_Name", "KPI_Definition", "Category", "Entity_Scope", "Cadence", "Primary_Data_Needs", "Default_Status_Model", "Mgmt_Layer_Candidate", "Developer_Notes", "Primary_Modules", "Legacy_Code"],
                kpiRows));
            WriteEntry(zip, "xl/worksheets/sheet2.xml", SheetXml("Driver_Catalog",
                ["Driver_ID", "Driver_Name", "Driver_Definition", "Category", "Evidence_Fields", "Related_KPIs", "Primary_Modules"],
                driverRows));
            WriteEntry(zip, "xl/worksheets/sheet3.xml", SheetXml("Influencer_Catalog",
                ["Influencer_ID", "Influencer_Name", "Influencer_Definition", "Category", "Evidence_Fields", "Default_Severity", "Related_KPIs", "Primary_Modules"],
                influencerRows));
            WriteEntry(zip, "xl/worksheets/sheet4.xml", SheetXml("KPI_Driver_Map",
                ["KPI_ID", "Driver_ID", "Map_Type", "Primary_Modules", "Rule_Notes"],
                kpiDriverMaps));
            WriteEntry(zip, "xl/worksheets/sheet5.xml", SheetXml("Driver_Influencer_Map",
                ["Driver_ID", "Influencer_ID", "Relationship_Type", "Default_Weight", "Rule_Notes"],
                driverInfMaps));
            WriteEntry(zip, "xl/worksheets/sheet6.xml", SheetXml("Scoring_Logic",
                ["Component", "Value_Range", "Weight_Percent", "Requirement_Level", "Implementation_Notes"],
                scoreRows));
            WriteEntry(zip, "xl/worksheets/sheet7.xml", SheetXml("Module_Routing",
                ["Module", "Module_Name", "Primary_KPIs", "Default_Output", "Description"],
                moduleRows));
            WriteEntry(zip, "xl/worksheets/sheet8.xml", SheetXml("Output_Assignment",
                ["Output_Area", "Name", "Description", "Routing_Notes"],
                outputRows));
        }

        return ms.ToArray();
    }

    public static string WriteFixtureFile(string path)
    {
        var bytes = BuildMinimalCatalogWorkbook();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private static void WriteEntry(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }

    private static string SheetXml(string name, IReadOnlyList<string> headers, IEnumerable<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
        sb.Append("<row r=\"1\">");
        for (var c = 0; c < headers.Count; c++)
            sb.Append(Cell(1, c + 1, headers[c]));
        sb.Append("</row>");
        var r = 2;
        foreach (var row in rows)
        {
            sb.Append($"<row r=\"{r}\">");
            for (var c = 0; c < row.Length; c++)
                sb.Append(Cell(r, c + 1, row[c]));
            sb.Append("</row>");
            r++;
        }
        sb.Append("</sheetData></worksheet>");
        return sb.ToString();
    }

    private static string Cell(int row, int col, string value)
    {
        var colLetter = ((char)('A' + col - 1)).ToString();
        var escaped = System.Security.SecurityElement.Escape(value) ?? "";
        return $"<c r=\"{colLetter}{row}\" t=\"inlineStr\"><is><t>{escaped}</t></is></c>";
    }

    private static string ContentTypesXml() => """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
          <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
          <Default Extension="xml" ContentType="application/xml"/>
          <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
          <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
          <Override PartName="/xl/worksheets/sheet2.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
          <Override PartName="/xl/worksheets/sheet3.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
          <Override PartName="/xl/worksheets/sheet4.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
          <Override PartName="/xl/worksheets/sheet5.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
          <Override PartName="/xl/worksheets/sheet6.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
          <Override PartName="/xl/worksheets/sheet7.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
          <Override PartName="/xl/worksheets/sheet8.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
          <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
        </Types>
        """;

    private static string RootRelsXml() => """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
        </Relationships>
        """;

    private static string WorkbookRelsXml() => """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
          <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet2.xml"/>
          <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet3.xml"/>
          <Relationship Id="rId4" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet4.xml"/>
          <Relationship Id="rId5" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet5.xml"/>
          <Relationship Id="rId6" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet6.xml"/>
          <Relationship Id="rId7" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet7.xml"/>
          <Relationship Id="rId8" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet8.xml"/>
          <Relationship Id="rId9" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
        </Relationships>
        """;

    private static string WorkbookXml() => """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
          <sheets>
            <sheet name="KPI_Catalog" sheetId="1" r:id="rId1"/>
            <sheet name="Driver_Catalog" sheetId="2" r:id="rId2"/>
            <sheet name="Influencer_Catalog" sheetId="3" r:id="rId3"/>
            <sheet name="KPI_Driver_Map" sheetId="4" r:id="rId4"/>
            <sheet name="Driver_Influencer_Map" sheetId="5" r:id="rId5"/>
            <sheet name="Scoring_Logic" sheetId="6" r:id="rId6"/>
            <sheet name="Module_Routing" sheetId="7" r:id="rId7"/>
            <sheet name="Output_Assignment" sheetId="8" r:id="rId8"/>
          </sheets>
        </workbook>
        """;

    private static string StylesXml() => """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"/>
        """;
}
