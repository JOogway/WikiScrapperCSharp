using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WikiScrapper.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPolishWikiFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescriptionPl",
                table: "Voivodeships",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FetchedAtPl",
                table: "Voivodeships",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WikiTitlePl",
                table: "Voivodeships",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WikiUrlPl",
                table: "Voivodeships",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionPl",
                table: "Countries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FetchedAtPl",
                table: "Countries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WikiTitlePl",
                table: "Countries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WikiUrlPl",
                table: "Countries",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescriptionPl",
                table: "Voivodeships");

            migrationBuilder.DropColumn(
                name: "FetchedAtPl",
                table: "Voivodeships");

            migrationBuilder.DropColumn(
                name: "WikiTitlePl",
                table: "Voivodeships");

            migrationBuilder.DropColumn(
                name: "WikiUrlPl",
                table: "Voivodeships");

            migrationBuilder.DropColumn(
                name: "DescriptionPl",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "FetchedAtPl",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "WikiTitlePl",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "WikiUrlPl",
                table: "Countries");
        }
    }
}
