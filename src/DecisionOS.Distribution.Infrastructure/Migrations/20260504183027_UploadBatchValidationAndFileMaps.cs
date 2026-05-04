using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DecisionOS.Distribution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UploadBatchValidationAndFileMaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UploadBatchIssues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UploadBatchId = table.Column<long>(type: "bigint", nullable: false),
                    UploadedFileId = table.Column<long>(type: "bigint", nullable: true),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Field = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadBatchIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadBatchIssues_UploadBatches_UploadBatchId",
                        column: x => x.UploadBatchId,
                        principalTable: "UploadBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UploadBatchIssues_UploadedFiles_UploadedFileId",
                        column: x => x.UploadedFileId,
                        principalTable: "UploadedFiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UploadedFileColumnMaps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UploadedFileId = table.Column<long>(type: "bigint", nullable: false),
                    SourceColumn = table.Column<string>(type: "text", nullable: false),
                    SystemField = table.Column<string>(type: "text", nullable: true),
                    Ignore = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadedFileColumnMaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadedFileColumnMaps_UploadedFiles_UploadedFileId",
                        column: x => x.UploadedFileId,
                        principalTable: "UploadedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UploadBatchIssues_UploadBatchId_Severity",
                table: "UploadBatchIssues",
                columns: new[] { "UploadBatchId", "Severity" });

            migrationBuilder.CreateIndex(
                name: "IX_UploadBatchIssues_UploadedFileId",
                table: "UploadBatchIssues",
                column: "UploadedFileId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFileColumnMaps_UploadedFileId_SourceColumn",
                table: "UploadedFileColumnMaps",
                columns: new[] { "UploadedFileId", "SourceColumn" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UploadBatchIssues");

            migrationBuilder.DropTable(
                name: "UploadedFileColumnMaps");
        }
    }
}
