using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RYH2025_Qubic.Migrations
{
    /// <inheritdoc />
    public partial class EliminatedCompleteAnalysisResultId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompleteAnalysisResultId",
                schema: "raiseyourhack",
                table: "SecurityAuditResults");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CompleteAnalysisResultId",
                schema: "raiseyourhack",
                table: "SecurityAuditResults",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
