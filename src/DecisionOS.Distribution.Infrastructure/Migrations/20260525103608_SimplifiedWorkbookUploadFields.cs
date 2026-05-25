using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DecisionOS.Distribution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifiedWorkbookUploadFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "AnchorPeriodEnd",
                table: "UploadBatches",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Cadence",
                table: "UploadBatches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DetectionSummaryJson",
                table: "UploadBatches",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImportMode",
                table: "UploadBatches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "WorkbookFingerprint",
                table: "UploadBatches",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkbookStoredRelativePath",
                table: "UploadBatches",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnchorPeriodEnd",
                table: "UploadBatches");

            migrationBuilder.DropColumn(
                name: "Cadence",
                table: "UploadBatches");

            migrationBuilder.DropColumn(
                name: "DetectionSummaryJson",
                table: "UploadBatches");

            migrationBuilder.DropColumn(
                name: "ImportMode",
                table: "UploadBatches");

            migrationBuilder.DropColumn(
                name: "WorkbookFingerprint",
                table: "UploadBatches");

            migrationBuilder.DropColumn(
                name: "WorkbookStoredRelativePath",
                table: "UploadBatches");
        }
    }
}
