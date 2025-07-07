using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RYH2025_Qubic.Migrations
{
    /// <inheritdoc />
    public partial class AddedResultToSecurityTestCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExecutionResultJson",
                schema: "raiseyourhack",
                table: "SecurityTestCases",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExecutionStatus",
                schema: "raiseyourhack",
                table: "SecurityTestCases",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastExecutedAt",
                schema: "raiseyourhack",
                table: "SecurityTestCases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RiskLevel",
                schema: "raiseyourhack",
                table: "SecurityTestCases",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "VulnerabilityConfirmed",
                schema: "raiseyourhack",
                table: "SecurityTestCases",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExecutionResultJson",
                schema: "raiseyourhack",
                table: "SecurityTestCases");

            migrationBuilder.DropColumn(
                name: "ExecutionStatus",
                schema: "raiseyourhack",
                table: "SecurityTestCases");

            migrationBuilder.DropColumn(
                name: "LastExecutedAt",
                schema: "raiseyourhack",
                table: "SecurityTestCases");

            migrationBuilder.DropColumn(
                name: "RiskLevel",
                schema: "raiseyourhack",
                table: "SecurityTestCases");

            migrationBuilder.DropColumn(
                name: "VulnerabilityConfirmed",
                schema: "raiseyourhack",
                table: "SecurityTestCases");
        }
    }
}
