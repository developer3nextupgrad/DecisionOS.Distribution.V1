using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DecisionOS.Distribution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CatalogEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CatalogDriverId",
                table: "DriverValues",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CatalogDrivers",
                columns: table => new
                {
                    DriverId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Definition = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: true),
                    EvidenceFields = table.Column<string>(type: "text", nullable: true),
                    RelatedKpis = table.Column<string>(type: "text", nullable: true),
                    PrimaryModules = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogDrivers", x => x.DriverId);
                });

            migrationBuilder.CreateTable(
                name: "CatalogInfluencers",
                columns: table => new
                {
                    InfluencerId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Definition = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: true),
                    EvidenceFields = table.Column<string>(type: "text", nullable: true),
                    DefaultSeverity = table.Column<string>(type: "text", nullable: true),
                    RelatedKpis = table.Column<string>(type: "text", nullable: true),
                    PrimaryModules = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogInfluencers", x => x.InfluencerId);
                });

            migrationBuilder.CreateTable(
                name: "CatalogKpis",
                columns: table => new
                {
                    KpiId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Definition = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: true),
                    EntityScope = table.Column<string>(type: "text", nullable: true),
                    Cadence = table.Column<string>(type: "text", nullable: true),
                    PrimaryDataNeeds = table.Column<string>(type: "text", nullable: true),
                    DefaultStatusModel = table.Column<string>(type: "text", nullable: true),
                    MgmtLayerCandidate = table.Column<bool>(type: "boolean", nullable: false),
                    DeveloperNotes = table.Column<string>(type: "text", nullable: true),
                    PrimaryModules = table.Column<string>(type: "text", nullable: true),
                    LegacyCode = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogKpis", x => x.KpiId);
                });

            migrationBuilder.CreateTable(
                name: "CatalogModules",
                columns: table => new
                {
                    ModuleCode = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PrimaryKpis = table.Column<string>(type: "text", nullable: true),
                    DefaultOutput = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogModules", x => x.ModuleCode);
                });

            migrationBuilder.CreateTable(
                name: "CatalogOutputAreas",
                columns: table => new
                {
                    OutputAreaCode = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    RoutingNotes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogOutputAreas", x => x.OutputAreaCode);
                });

            migrationBuilder.CreateTable(
                name: "CatalogScoreComponents",
                columns: table => new
                {
                    Component = table.Column<string>(type: "text", nullable: false),
                    ValueRange = table.Column<string>(type: "text", nullable: true),
                    WeightPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    RequirementLevel = table.Column<string>(type: "text", nullable: true),
                    ImplementationNotes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogScoreComponents", x => x.Component);
                });

            migrationBuilder.CreateTable(
                name: "HoldoverComments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverValueId = table.Column<int>(type: "integer", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoldoverComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HoldoverComments_DriverValues_DriverValueId",
                        column: x => x.DriverValueId,
                        principalTable: "DriverValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HoldoverStatusHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverValueId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    FixProgressPercent = table.Column<int>(type: "integer", nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoldoverStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HoldoverStatusHistories_DriverValues_DriverValueId",
                        column: x => x.DriverValueId,
                        principalTable: "DriverValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoutingQueueItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    QueueType = table.Column<string>(type: "text", nullable: false),
                    CatalogKpiId = table.Column<string>(type: "text", nullable: true),
                    CatalogDriverId = table.Column<string>(type: "text", nullable: true),
                    ModuleCode = table.Column<string>(type: "text", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: true),
                    FinalScore = table.Column<decimal>(type: "numeric", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutingQueueItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoutingQueueItems_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserNotifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    LinkUrl = table.Column<string>(type: "text", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkAssignments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    TargetType = table.Column<string>(type: "text", nullable: false),
                    TargetId = table.Column<string>(type: "text", nullable: false),
                    AssigneeUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkAssignments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CatalogDriverInfluencerMaps",
                columns: table => new
                {
                    DriverId = table.Column<string>(type: "text", nullable: false),
                    InfluencerId = table.Column<string>(type: "text", nullable: false),
                    RelationshipType = table.Column<string>(type: "text", nullable: true),
                    DefaultWeight = table.Column<decimal>(type: "numeric", nullable: true),
                    RuleNotes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogDriverInfluencerMaps", x => new { x.DriverId, x.InfluencerId });
                    table.ForeignKey(
                        name: "FK_CatalogDriverInfluencerMaps_CatalogDrivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "CatalogDrivers",
                        principalColumn: "DriverId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CatalogDriverInfluencerMaps_CatalogInfluencers_InfluencerId",
                        column: x => x.InfluencerId,
                        principalTable: "CatalogInfluencers",
                        principalColumn: "InfluencerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InfluencerEvidences",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    DriverValueId = table.Column<int>(type: "integer", nullable: false),
                    InfluencerId = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: true),
                    EvidenceSummary = table.Column<string>(type: "text", nullable: false),
                    Confidence = table.Column<string>(type: "text", nullable: true),
                    Weight = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfluencerEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InfluencerEvidences_CatalogInfluencers_InfluencerId",
                        column: x => x.InfluencerId,
                        principalTable: "CatalogInfluencers",
                        principalColumn: "InfluencerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InfluencerEvidences_DriverValues_DriverValueId",
                        column: x => x.DriverValueId,
                        principalTable: "DriverValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InfluencerEvidences_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CatalogKpiDriverMaps",
                columns: table => new
                {
                    KpiId = table.Column<string>(type: "text", nullable: false),
                    DriverId = table.Column<string>(type: "text", nullable: false),
                    MapType = table.Column<string>(type: "text", nullable: true),
                    PrimaryModules = table.Column<string>(type: "text", nullable: true),
                    RuleNotes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogKpiDriverMaps", x => new { x.KpiId, x.DriverId });
                    table.ForeignKey(
                        name: "FK_CatalogKpiDriverMaps_CatalogDrivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "CatalogDrivers",
                        principalColumn: "DriverId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CatalogKpiDriverMaps_CatalogKpis_KpiId",
                        column: x => x.KpiId,
                        principalTable: "CatalogKpis",
                        principalColumn: "KpiId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IssuePriorityScores",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    KpiDefinitionId = table.Column<int>(type: "integer", nullable: true),
                    CatalogKpiId = table.Column<string>(type: "text", nullable: true),
                    SeverityScore = table.Column<decimal>(type: "numeric", nullable: false),
                    CashScore = table.Column<decimal>(type: "numeric", nullable: false),
                    FinancialScore = table.Column<decimal>(type: "numeric", nullable: false),
                    UrgencyScore = table.Column<decimal>(type: "numeric", nullable: false),
                    ActionabilityScore = table.Column<decimal>(type: "numeric", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric", nullable: false),
                    FinalScore = table.Column<decimal>(type: "numeric", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssuePriorityScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssuePriorityScores_CatalogKpis_CatalogKpiId",
                        column: x => x.CatalogKpiId,
                        principalTable: "CatalogKpis",
                        principalColumn: "KpiId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_IssuePriorityScores_KpiDefinitions_KpiDefinitionId",
                        column: x => x.KpiDefinitionId,
                        principalTable: "KpiDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_IssuePriorityScores_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantKpiSelections",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CatalogKpiId = table.Column<string>(type: "text", nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    IsExcluded = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantKpiSelections", x => new { x.TenantId, x.CatalogKpiId });
                    table.ForeignKey(
                        name: "FK_TenantKpiSelections_CatalogKpis_CatalogKpiId",
                        column: x => x.CatalogKpiId,
                        principalTable: "CatalogKpis",
                        principalColumn: "KpiId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenantKpiSelections_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogDriverInfluencerMaps_InfluencerId",
                table: "CatalogDriverInfluencerMaps",
                column: "InfluencerId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogKpiDriverMaps_DriverId",
                table: "CatalogKpiDriverMaps",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_HoldoverComments_DriverValueId",
                table: "HoldoverComments",
                column: "DriverValueId");

            migrationBuilder.CreateIndex(
                name: "IX_HoldoverStatusHistories_DriverValueId",
                table: "HoldoverStatusHistories",
                column: "DriverValueId");

            migrationBuilder.CreateIndex(
                name: "IX_InfluencerEvidences_DriverValueId",
                table: "InfluencerEvidences",
                column: "DriverValueId");

            migrationBuilder.CreateIndex(
                name: "IX_InfluencerEvidences_InfluencerId",
                table: "InfluencerEvidences",
                column: "InfluencerId");

            migrationBuilder.CreateIndex(
                name: "IX_InfluencerEvidences_TenantId_PeriodEnd_DriverValueId",
                table: "InfluencerEvidences",
                columns: new[] { "TenantId", "PeriodEnd", "DriverValueId" });

            migrationBuilder.CreateIndex(
                name: "IX_IssuePriorityScores_CatalogKpiId",
                table: "IssuePriorityScores",
                column: "CatalogKpiId");

            migrationBuilder.CreateIndex(
                name: "IX_IssuePriorityScores_KpiDefinitionId",
                table: "IssuePriorityScores",
                column: "KpiDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_IssuePriorityScores_TenantId_PeriodEnd_CatalogKpiId",
                table: "IssuePriorityScores",
                columns: new[] { "TenantId", "PeriodEnd", "CatalogKpiId" });

            migrationBuilder.CreateIndex(
                name: "IX_IssuePriorityScores_TenantId_PeriodEnd_KpiDefinitionId",
                table: "IssuePriorityScores",
                columns: new[] { "TenantId", "PeriodEnd", "KpiDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoutingQueueItems_TenantId_PeriodEnd_QueueType",
                table: "RoutingQueueItems",
                columns: new[] { "TenantId", "PeriodEnd", "QueueType" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantKpiSelections_CatalogKpiId",
                table: "TenantKpiSelections",
                column: "CatalogKpiId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkAssignments_TenantId_PeriodEnd_TargetType_TargetId",
                table: "WorkAssignments",
                columns: new[] { "TenantId", "PeriodEnd", "TargetType", "TargetId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogDriverInfluencerMaps");

            migrationBuilder.DropTable(
                name: "CatalogKpiDriverMaps");

            migrationBuilder.DropTable(
                name: "CatalogModules");

            migrationBuilder.DropTable(
                name: "CatalogOutputAreas");

            migrationBuilder.DropTable(
                name: "CatalogScoreComponents");

            migrationBuilder.DropTable(
                name: "HoldoverComments");

            migrationBuilder.DropTable(
                name: "HoldoverStatusHistories");

            migrationBuilder.DropTable(
                name: "InfluencerEvidences");

            migrationBuilder.DropTable(
                name: "IssuePriorityScores");

            migrationBuilder.DropTable(
                name: "RoutingQueueItems");

            migrationBuilder.DropTable(
                name: "TenantKpiSelections");

            migrationBuilder.DropTable(
                name: "UserNotifications");

            migrationBuilder.DropTable(
                name: "WorkAssignments");

            migrationBuilder.DropTable(
                name: "CatalogDrivers");

            migrationBuilder.DropTable(
                name: "CatalogInfluencers");

            migrationBuilder.DropTable(
                name: "CatalogKpis");

            migrationBuilder.DropColumn(
                name: "CatalogDriverId",
                table: "DriverValues");
        }
    }
}
