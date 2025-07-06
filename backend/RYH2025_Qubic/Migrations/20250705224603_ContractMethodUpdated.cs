using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RYH2025_Qubic.Migrations
{
    /// <inheritdoc />
    public partial class ContractMethodUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ValidationsJson",
                schema: "raiseyourhack",
                table: "ContractMethods",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]",
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "OutputStructJson",
                schema: "raiseyourhack",
                table: "ContractMethods",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<bool>(
                name: "IsOrderBookRelated",
                schema: "raiseyourhack",
                table: "ContractMethods",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "IsAssetRelated",
                schema: "raiseyourhack",
                table: "ContractMethods",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "InputStructJson",
                schema: "raiseyourhack",
                table: "ContractMethods",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "FeesJson",
                schema: "raiseyourhack",
                table: "ContractMethods",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "raiseyourhack",
                table: "ContractMethods",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "InputFieldsJson",
                schema: "raiseyourhack",
                table: "ContractMethods",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "OutputFieldsJson",
                schema: "raiseyourhack",
                table: "ContractMethods",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<int>(
                name: "PackageSize",
                schema: "raiseyourhack",
                table: "ContractMethods",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ContractMethods_ContractAnalysisId_Name",
                schema: "raiseyourhack",
                table: "ContractMethods",
                columns: new[] { "ContractAnalysisId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractMethods_FeesJson",
                schema: "raiseyourhack",
                table: "ContractMethods",
                column: "FeesJson")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_ContractMethods_InputFieldsJson",
                schema: "raiseyourhack",
                table: "ContractMethods",
                column: "InputFieldsJson")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_ContractMethods_IsAssetRelated",
                schema: "raiseyourhack",
                table: "ContractMethods",
                column: "IsAssetRelated",
                filter: "\"IsAssetRelated\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_ContractMethods_IsOrderBookRelated",
                schema: "raiseyourhack",
                table: "ContractMethods",
                column: "IsOrderBookRelated",
                filter: "\"IsOrderBookRelated\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_ContractMethods_OutputFieldsJson",
                schema: "raiseyourhack",
                table: "ContractMethods",
                column: "OutputFieldsJson")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_ContractMethods_PackageSize",
                schema: "raiseyourhack",
                table: "ContractMethods",
                column: "PackageSize");

            migrationBuilder.CreateIndex(
                name: "IX_ContractMethods_ProcedureIndex",
                schema: "raiseyourhack",
                table: "ContractMethods",
                column: "ProcedureIndex",
                filter: "\"ProcedureIndex\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ContractMethods_Type",
                schema: "raiseyourhack",
                table: "ContractMethods",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ContractMethods_Type_Name",
                schema: "raiseyourhack",
                table: "ContractMethods",
                columns: new[] { "Type", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_ContractMethods_ValidationsJson",
                schema: "raiseyourhack",
                table: "ContractMethods",
                column: "ValidationsJson")
                .Annotation("Npgsql:IndexMethod", "gin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContractMethods_ContractAnalysisId_Name",
                schema: "raiseyourhack",
                table: "ContractMethods");

            migrationBuilder.DropIndex(
                name: "IX_ContractMethods_FeesJson",
                schema: "raiseyourhack",
                table: "ContractMethods");

            migrationBuilder.DropIndex(
                name: "IX_ContractMethods_InputFieldsJson",
                schema: "raiseyourhack",
                table: "ContractMethods");

            migrationBuilder.DropIndex(
                name: "IX_ContractMethods_IsAssetRelated",
                schema: "raiseyourhack",
                table: "ContractMethods");

            migrationBuilder.DropIndex(
                name: "IX_ContractMethods_IsOrderBookRelated",
                schema: "raiseyourhack",
                table: "ContractMethods");

            migrationBuilder.DropIndex(
                name: "IX_ContractMethods_OutputFieldsJson",
                schema: "raiseyourhack",
                table: "ContractMethods");

            migrationBuilder.DropIndex(
                name: "IX_ContractMethods_PackageSize",
                schema: "raiseyourhack",
                table: "ContractMethods");

            migrationBuilder.DropIndex(
                name: "IX_ContractMethods_ProcedureIndex",
                schema: "raiseyourhack",
                table: "ContractMethods");

            migrationBuilder.DropIndex(
                name: "IX_ContractMethods_Type",
                schema: "raiseyourhack",
                table: "ContractMethods");

            migrationBuilder.DropIndex(
                name: "IX_ContractMethods_Type_Name",
                schema: "raiseyourhack",
                table: "ContractMethods");

            migrationBuilder.DropIndex(
                name: "IX_ContractMethods_ValidationsJson",
                schema: "raiseyourhack",
                table: "ContractMethods");

            migrationBuilder.DropColumn(
                name: "InputFieldsJson",
                schema: "raiseyourhack",
                table: "ContractMethods");

            migrationBuilder.DropColumn(
                name: "OutputFieldsJson",
                schema: "raiseyourhack",
                table: "ContractMethods");

            migrationBuilder.DropColumn(
                name: "PackageSize",
                schema: "raiseyourhack",
                table: "ContractMethods");

            migrationBuilder.AlterColumn<string>(
                name: "ValidationsJson",
                schema: "raiseyourhack",
                table: "ContractMethods",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "[]");

            migrationBuilder.AlterColumn<string>(
                name: "OutputStructJson",
                schema: "raiseyourhack",
                table: "ContractMethods",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "{}");

            migrationBuilder.AlterColumn<bool>(
                name: "IsOrderBookRelated",
                schema: "raiseyourhack",
                table: "ContractMethods",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "IsAssetRelated",
                schema: "raiseyourhack",
                table: "ContractMethods",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "InputStructJson",
                schema: "raiseyourhack",
                table: "ContractMethods",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "{}");

            migrationBuilder.AlterColumn<string>(
                name: "FeesJson",
                schema: "raiseyourhack",
                table: "ContractMethods",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "{}");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "raiseyourhack",
                table: "ContractMethods",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");
        }
    }
}
