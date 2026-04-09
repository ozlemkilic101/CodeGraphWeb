using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeGraphWeb.Migrations
{
    /// <inheritdoc />
    public partial class RestoreProjectCompanyAndAddUploadMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Projects",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Projects",
                type: "TEXT",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "Projects",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UploadDate",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql("""
                UPDATE Projects
                SET CompanyId = (SELECT Id FROM Companies ORDER BY Id LIMIT 1)
                WHERE CompanyId = 0;
                """);

            migrationBuilder.Sql("""
                UPDATE Projects
                SET UploadDate = CreatedAt
                WHERE UploadDate = '0001-01-01 00:00:00';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CompanyId",
                table: "Projects",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Companies_CompanyId",
                table: "Projects",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Companies_CompanyId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_CompanyId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "UploadDate",
                table: "Projects");
        }
    }
}
