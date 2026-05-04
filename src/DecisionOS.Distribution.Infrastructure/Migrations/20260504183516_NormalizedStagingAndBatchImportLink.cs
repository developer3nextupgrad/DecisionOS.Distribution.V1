using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DecisionOS.Distribution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizedStagingAndBatchImportLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ImportRunId",
                table: "UploadBatches",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NormalizedApRows",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    UploadBatchId = table.Column<long>(type: "bigint", nullable: false),
                    UploadedFileId = table.Column<long>(type: "bigint", nullable: false),
                    SourceRowNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IssueSummary = table.Column<string>(type: "text", nullable: true),
                    RawJson = table.Column<string>(type: "text", nullable: false),
                    SnapshotDate = table.Column<DateOnly>(type: "date", nullable: true),
                    VendorId = table.Column<string>(type: "text", nullable: true),
                    VendorName = table.Column<string>(type: "text", nullable: true),
                    BillId = table.Column<string>(type: "text", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AgingBucket = table.Column<string>(type: "text", nullable: true),
                    DaysPastDue = table.Column<int>(type: "integer", nullable: true),
                    OpenBalance = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NormalizedApRows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NormalizedArRows",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    UploadBatchId = table.Column<long>(type: "bigint", nullable: false),
                    UploadedFileId = table.Column<long>(type: "bigint", nullable: false),
                    SourceRowNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IssueSummary = table.Column<string>(type: "text", nullable: true),
                    RawJson = table.Column<string>(type: "text", nullable: false),
                    SnapshotDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CustomerId = table.Column<string>(type: "text", nullable: true),
                    CustomerName = table.Column<string>(type: "text", nullable: true),
                    InvoiceId = table.Column<string>(type: "text", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AgingBucket = table.Column<string>(type: "text", nullable: true),
                    DaysPastDue = table.Column<int>(type: "integer", nullable: true),
                    OpenBalance = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NormalizedArRows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NormalizedInventoryRows",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    UploadBatchId = table.Column<long>(type: "bigint", nullable: false),
                    UploadedFileId = table.Column<long>(type: "bigint", nullable: false),
                    SourceRowNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IssueSummary = table.Column<string>(type: "text", nullable: true),
                    RawJson = table.Column<string>(type: "text", nullable: false),
                    SnapshotDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SkuId = table.Column<string>(type: "text", nullable: true),
                    LocationId = table.Column<string>(type: "text", nullable: true),
                    QuantityOnHand = table.Column<decimal>(type: "numeric", nullable: true),
                    InventoryValue = table.Column<decimal>(type: "numeric", nullable: true),
                    AverageCost = table.Column<decimal>(type: "numeric", nullable: true),
                    LastSaleDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NormalizedInventoryRows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NormalizedSalesRows",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    UploadBatchId = table.Column<long>(type: "bigint", nullable: false),
                    UploadedFileId = table.Column<long>(type: "bigint", nullable: false),
                    SourceRowNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IssueSummary = table.Column<string>(type: "text", nullable: true),
                    RawJson = table.Column<string>(type: "text", nullable: false),
                    TransactionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TransactionId = table.Column<string>(type: "text", nullable: true),
                    CustomerId = table.Column<string>(type: "text", nullable: true),
                    CustomerName = table.Column<string>(type: "text", nullable: true),
                    SkuId = table.Column<string>(type: "text", nullable: true),
                    ProductDescription = table.Column<string>(type: "text", nullable: true),
                    LocationId = table.Column<string>(type: "text", nullable: true),
                    QuantitySold = table.Column<decimal>(type: "numeric", nullable: true),
                    NetSales = table.Column<decimal>(type: "numeric", nullable: true),
                    Cogs = table.Column<decimal>(type: "numeric", nullable: true),
                    GrossProfit = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NormalizedSalesRows", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UploadBatches_ImportRunId",
                table: "UploadBatches",
                column: "ImportRunId");

            migrationBuilder.CreateIndex(
                name: "IX_NormalizedApRows_TenantId_PeriodEnd",
                table: "NormalizedApRows",
                columns: new[] { "TenantId", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_NormalizedApRows_UploadBatchId_UploadedFileId",
                table: "NormalizedApRows",
                columns: new[] { "UploadBatchId", "UploadedFileId" });

            migrationBuilder.CreateIndex(
                name: "IX_NormalizedArRows_TenantId_PeriodEnd",
                table: "NormalizedArRows",
                columns: new[] { "TenantId", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_NormalizedArRows_UploadBatchId_UploadedFileId",
                table: "NormalizedArRows",
                columns: new[] { "UploadBatchId", "UploadedFileId" });

            migrationBuilder.CreateIndex(
                name: "IX_NormalizedInventoryRows_TenantId_PeriodEnd",
                table: "NormalizedInventoryRows",
                columns: new[] { "TenantId", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_NormalizedInventoryRows_UploadBatchId_UploadedFileId",
                table: "NormalizedInventoryRows",
                columns: new[] { "UploadBatchId", "UploadedFileId" });

            migrationBuilder.CreateIndex(
                name: "IX_NormalizedSalesRows_TenantId_PeriodEnd",
                table: "NormalizedSalesRows",
                columns: new[] { "TenantId", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_NormalizedSalesRows_UploadBatchId_UploadedFileId",
                table: "NormalizedSalesRows",
                columns: new[] { "UploadBatchId", "UploadedFileId" });

            migrationBuilder.AddForeignKey(
                name: "FK_UploadBatches_ImportRuns_ImportRunId",
                table: "UploadBatches",
                column: "ImportRunId",
                principalTable: "ImportRuns",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UploadBatches_ImportRuns_ImportRunId",
                table: "UploadBatches");

            migrationBuilder.DropTable(
                name: "NormalizedApRows");

            migrationBuilder.DropTable(
                name: "NormalizedArRows");

            migrationBuilder.DropTable(
                name: "NormalizedInventoryRows");

            migrationBuilder.DropTable(
                name: "NormalizedSalesRows");

            migrationBuilder.DropIndex(
                name: "IX_UploadBatches_ImportRunId",
                table: "UploadBatches");

            migrationBuilder.DropColumn(
                name: "ImportRunId",
                table: "UploadBatches");
        }
    }
}
