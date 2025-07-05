using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RYH2025_Qubic.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "raiseyourhack");

            migrationBuilder.CreateTable(
                name: "SecurityAuditResults",
                schema: "raiseyourhack",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompleteAnalysisResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AuditDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecommendationsJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAuditResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractAnalyses",
                schema: "raiseyourhack",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Namespace = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SecurityAuditId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractAnalyses_SecurityAuditResults_SecurityAuditId",
                        column: x => x.SecurityAuditId,
                        principalSchema: "raiseyourhack",
                        principalTable: "SecurityAuditResults",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SecurityRisks",
                schema: "raiseyourhack",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SecurityAuditResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FactorsJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityRisks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityRisks_SecurityAuditResults_SecurityAuditResultId",
                        column: x => x.SecurityAuditResultId,
                        principalSchema: "raiseyourhack",
                        principalTable: "SecurityAuditResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VulnerabilitiesFound",
                schema: "raiseyourhack",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SecurityAuditResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Impact = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExploitScenariosJson = table.Column<string>(type: "jsonb", nullable: false),
                    RecommendationsJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VulnerabilitiesFound", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VulnerabilitiesFound_SecurityAuditResults_SecurityAuditResu~",
                        column: x => x.SecurityAuditResultId,
                        principalSchema: "raiseyourhack",
                        principalTable: "SecurityAuditResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractMethods",
                schema: "raiseyourhack",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractAnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProcedureIndex = table.Column<int>(type: "integer", nullable: true),
                    InputStructJson = table.Column<string>(type: "jsonb", nullable: false),
                    OutputStructJson = table.Column<string>(type: "jsonb", nullable: false),
                    FeesJson = table.Column<string>(type: "jsonb", nullable: false),
                    ValidationsJson = table.Column<string>(type: "jsonb", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsAssetRelated = table.Column<bool>(type: "boolean", nullable: false),
                    IsOrderBookRelated = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractMethods_ContractAnalyses_ContractAnalysisId",
                        column: x => x.ContractAnalysisId,
                        principalSchema: "raiseyourhack",
                        principalTable: "ContractAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SecurityTestCases",
                schema: "raiseyourhack",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SecurityAuditResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractMethodId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MethodName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetVariable = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    VulnerabilityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExpectedBehavior = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ActualRisk = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TestInputsJson = table.Column<string>(type: "jsonb", nullable: false),
                    MitigationStepsJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityTestCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityTestCases_ContractMethods_ContractMethodId",
                        column: x => x.ContractMethodId,
                        principalSchema: "raiseyourhack",
                        principalTable: "ContractMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SecurityTestCases_SecurityAuditResults_SecurityAuditResultId",
                        column: x => x.SecurityAuditResultId,
                        principalSchema: "raiseyourhack",
                        principalTable: "SecurityAuditResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractAnalyses_ContractName",
                schema: "raiseyourhack",
                table: "ContractAnalyses",
                column: "ContractName");

            migrationBuilder.CreateIndex(
                name: "IX_ContractAnalyses_CreatedAt",
                schema: "raiseyourhack",
                table: "ContractAnalyses",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContractAnalyses_SecurityAuditId",
                schema: "raiseyourhack",
                table: "ContractAnalyses",
                column: "SecurityAuditId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractMethods_ContractAnalysisId",
                schema: "raiseyourhack",
                table: "ContractMethods",
                column: "ContractAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractMethods_Name",
                schema: "raiseyourhack",
                table: "ContractMethods",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditResults_AuditDate",
                schema: "raiseyourhack",
                table: "SecurityAuditResults",
                column: "AuditDate");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityRisks_SecurityAuditResultId",
                schema: "raiseyourhack",
                table: "SecurityRisks",
                column: "SecurityAuditResultId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityTestCases_ContractMethodId",
                schema: "raiseyourhack",
                table: "SecurityTestCases",
                column: "ContractMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityTestCases_MethodName",
                schema: "raiseyourhack",
                table: "SecurityTestCases",
                column: "MethodName");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityTestCases_SecurityAuditResultId",
                schema: "raiseyourhack",
                table: "SecurityTestCases",
                column: "SecurityAuditResultId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityTestCases_Severity",
                schema: "raiseyourhack",
                table: "SecurityTestCases",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityTestCases_TargetVariable",
                schema: "raiseyourhack",
                table: "SecurityTestCases",
                column: "TargetVariable");

            migrationBuilder.CreateIndex(
                name: "IX_VulnerabilitiesFound_SecurityAuditResultId",
                schema: "raiseyourhack",
                table: "VulnerabilitiesFound",
                column: "SecurityAuditResultId");

            migrationBuilder.CreateIndex(
                name: "IX_VulnerabilitiesFound_Severity",
                schema: "raiseyourhack",
                table: "VulnerabilitiesFound",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_VulnerabilitiesFound_Type",
                schema: "raiseyourhack",
                table: "VulnerabilitiesFound",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SecurityRisks",
                schema: "raiseyourhack");

            migrationBuilder.DropTable(
                name: "SecurityTestCases",
                schema: "raiseyourhack");

            migrationBuilder.DropTable(
                name: "VulnerabilitiesFound",
                schema: "raiseyourhack");

            migrationBuilder.DropTable(
                name: "ContractMethods",
                schema: "raiseyourhack");

            migrationBuilder.DropTable(
                name: "ContractAnalyses",
                schema: "raiseyourhack");

            migrationBuilder.DropTable(
                name: "SecurityAuditResults",
                schema: "raiseyourhack");
        }
    }
}
