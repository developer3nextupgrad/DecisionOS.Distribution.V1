using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DecisionOS.Distribution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProfileConfigFrameworkLayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActiveKpiProfileCode",
                table: "BusinessProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChannelStructure",
                table: "BusinessProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationStructure",
                table: "BusinessProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThresholdProfileCode",
                table: "BusinessProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VerticalLibraryId",
                table: "BusinessProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TenantDriverOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PillarCode = table.Column<string>(type: "text", nullable: false),
                    DriverCode = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantDriverOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantDriverOverrides_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantKpiOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    KpiCode = table.Column<string>(type: "text", nullable: false),
                    Target = table.Column<decimal>(type: "numeric", nullable: true),
                    AmberThreshold = table.Column<decimal>(type: "numeric", nullable: true),
                    RedThreshold = table.Column<decimal>(type: "numeric", nullable: true),
                    MinValue = table.Column<decimal>(type: "numeric", nullable: true),
                    MaxValue = table.Column<decimal>(type: "numeric", nullable: true),
                    AlertPriority = table.Column<int>(type: "integer", nullable: true),
                    RecommendedAction = table.Column<string>(type: "text", nullable: true),
                    DiagnosticChecks = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantKpiOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantKpiOverrides_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VerticalLibraries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerticalLibraries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessProfiles_VerticalLibraryId",
                table: "BusinessProfiles",
                column: "VerticalLibraryId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantDriverOverrides_TenantId_PillarCode_DriverCode",
                table: "TenantDriverOverrides",
                columns: new[] { "TenantId", "PillarCode", "DriverCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantKpiOverrides_TenantId_KpiCode",
                table: "TenantKpiOverrides",
                columns: new[] { "TenantId", "KpiCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerticalLibraries_Code",
                table: "VerticalLibraries",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessProfiles_VerticalLibraries_VerticalLibraryId",
                table: "BusinessProfiles",
                column: "VerticalLibraryId",
                principalTable: "VerticalLibraries",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessProfiles_VerticalLibraries_VerticalLibraryId",
                table: "BusinessProfiles");

            migrationBuilder.DropTable(
                name: "TenantDriverOverrides");

            migrationBuilder.DropTable(
                name: "TenantKpiOverrides");

            migrationBuilder.DropTable(
                name: "VerticalLibraries");

            migrationBuilder.DropIndex(
                name: "IX_BusinessProfiles_VerticalLibraryId",
                table: "BusinessProfiles");

            migrationBuilder.DropColumn(
                name: "ActiveKpiProfileCode",
                table: "BusinessProfiles");

            migrationBuilder.DropColumn(
                name: "ChannelStructure",
                table: "BusinessProfiles");

            migrationBuilder.DropColumn(
                name: "LocationStructure",
                table: "BusinessProfiles");

            migrationBuilder.DropColumn(
                name: "ThresholdProfileCode",
                table: "BusinessProfiles");

            migrationBuilder.DropColumn(
                name: "VerticalLibraryId",
                table: "BusinessProfiles");
        }
    }
}
