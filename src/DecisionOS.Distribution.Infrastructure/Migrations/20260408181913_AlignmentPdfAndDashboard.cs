using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DecisionOS.Distribution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignmentPdfAndDashboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CardDetailLine1",
                table: "KpiSnapshots",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardDetailLine2",
                table: "KpiSnapshots",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AlertPriority",
                table: "KpiDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<string>(
                name: "AssignedSummary",
                table: "DriverValues",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentSummary",
                table: "DriverValues",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FixProgressPercent",
                table: "DriverValues",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Owner",
                table: "DriverValues",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetSummary",
                table: "DriverValues",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ImportRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    KpiRowsProcessed = table.Column<int>(type: "integer", nullable: false),
                    DriverRowsProcessed = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportRuns_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportRuns_TenantId_PeriodEnd_StartedAt",
                table: "ImportRuns",
                columns: new[] { "TenantId", "PeriodEnd", "StartedAt" });

            migrationBuilder.Sql("""
                UPDATE "KpiDefinitions" SET "AlertPriority" = CASE "Code"
                    WHEN 'CCC' THEN 10
                    WHEN 'AR_PastDue31p%' THEN 20
                    WHEN 'NetProfit%' THEN 30
                    WHEN 'GrossMargin%' THEN 40
                    WHEN 'DOH' THEN 50
                    WHEN 'AP_PastDue31p%' THEN 60
                    WHEN 'PerfectOrderRate' THEN 70
                    ELSE "AlertPriority"
                END
                WHERE "Code" IN (
                    'CCC','AR_PastDue31p%','NetProfit%','GrossMargin%','DOH','AP_PastDue31p%','PerfectOrderRate'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportRuns");

            migrationBuilder.DropColumn(
                name: "CardDetailLine1",
                table: "KpiSnapshots");

            migrationBuilder.DropColumn(
                name: "CardDetailLine2",
                table: "KpiSnapshots");

            migrationBuilder.DropColumn(
                name: "AlertPriority",
                table: "KpiDefinitions");

            migrationBuilder.DropColumn(
                name: "AssignedSummary",
                table: "DriverValues");

            migrationBuilder.DropColumn(
                name: "CurrentSummary",
                table: "DriverValues");

            migrationBuilder.DropColumn(
                name: "FixProgressPercent",
                table: "DriverValues");

            migrationBuilder.DropColumn(
                name: "Owner",
                table: "DriverValues");

            migrationBuilder.DropColumn(
                name: "TargetSummary",
                table: "DriverValues");
        }
    }
}
