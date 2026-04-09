using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DecisionOS.Distribution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BusinessProfilesAndInfluencers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KpiDefinitions_Code",
                table: "KpiDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_DriverDefinitions_PillarCode_DriverCode",
                table: "DriverDefinitions");

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessProfileId",
                table: "Tenants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessProfileId",
                table: "KpiDefinitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessProfileId",
                table: "DriverDefinitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BusinessProfiles",
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
                    table.PrimaryKey("PK_BusinessProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InfluencerDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BusinessProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    PillarCode = table.Column<string>(type: "text", nullable: false),
                    DriverCode = table.Column<string>(type: "text", nullable: false),
                    InfluencerCode = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Weight = table.Column<int>(type: "integer", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfluencerDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InfluencerDefinitions_BusinessProfiles_BusinessProfileId",
                        column: x => x.BusinessProfileId,
                        principalTable: "BusinessProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_BusinessProfileId",
                table: "Tenants",
                column: "BusinessProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiDefinitions_BusinessProfileId_Code",
                table: "KpiDefinitions",
                columns: new[] { "BusinessProfileId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverDefinitions_BusinessProfileId_PillarCode_DriverCode",
                table: "DriverDefinitions",
                columns: new[] { "BusinessProfileId", "PillarCode", "DriverCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessProfiles_Code",
                table: "BusinessProfiles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InfluencerDefinitions_BusinessProfileId_PillarCode_DriverCo~",
                table: "InfluencerDefinitions",
                columns: new[] { "BusinessProfileId", "PillarCode", "DriverCode", "InfluencerCode" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DriverDefinitions_BusinessProfiles_BusinessProfileId",
                table: "DriverDefinitions",
                column: "BusinessProfileId",
                principalTable: "BusinessProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_KpiDefinitions_BusinessProfiles_BusinessProfileId",
                table: "KpiDefinitions",
                column: "BusinessProfileId",
                principalTable: "BusinessProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_BusinessProfiles_BusinessProfileId",
                table: "Tenants",
                column: "BusinessProfileId",
                principalTable: "BusinessProfiles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DriverDefinitions_BusinessProfiles_BusinessProfileId",
                table: "DriverDefinitions");

            migrationBuilder.DropForeignKey(
                name: "FK_KpiDefinitions_BusinessProfiles_BusinessProfileId",
                table: "KpiDefinitions");

            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_BusinessProfiles_BusinessProfileId",
                table: "Tenants");

            migrationBuilder.DropTable(
                name: "InfluencerDefinitions");

            migrationBuilder.DropTable(
                name: "BusinessProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_BusinessProfileId",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_KpiDefinitions_BusinessProfileId_Code",
                table: "KpiDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_DriverDefinitions_BusinessProfileId_PillarCode_DriverCode",
                table: "DriverDefinitions");

            migrationBuilder.DropColumn(
                name: "BusinessProfileId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "BusinessProfileId",
                table: "KpiDefinitions");

            migrationBuilder.DropColumn(
                name: "BusinessProfileId",
                table: "DriverDefinitions");

            migrationBuilder.CreateIndex(
                name: "IX_KpiDefinitions_Code",
                table: "KpiDefinitions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverDefinitions_PillarCode_DriverCode",
                table: "DriverDefinitions",
                columns: new[] { "PillarCode", "DriverCode" },
                unique: true);
        }
    }
}
