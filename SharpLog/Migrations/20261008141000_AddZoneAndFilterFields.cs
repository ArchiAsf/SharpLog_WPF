using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharpLog.Migrations
{
    /// <inheritdoc />
    public partial class AddZoneAndFilterFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CQZ",
                table: "LogTable",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DXCC",
                table: "LogTable",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ITUZ",
                table: "LogTable",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FilterBand",
                table: "AppSettingsTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FilterCall",
                table: "AppSettingsTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FilterMode",
                table: "AppSettingsTable",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FilterOnlyConfirmed",
                table: "AppSettingsTable",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FilterOnlyToday",
                table: "AppSettingsTable",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CQZ",
                table: "LogTable");

            migrationBuilder.DropColumn(
                name: "DXCC",
                table: "LogTable");

            migrationBuilder.DropColumn(
                name: "ITUZ",
                table: "LogTable");

            migrationBuilder.DropColumn(
                name: "FilterBand",
                table: "AppSettingsTable");

            migrationBuilder.DropColumn(
                name: "FilterCall",
                table: "AppSettingsTable");

            migrationBuilder.DropColumn(
                name: "FilterMode",
                table: "AppSettingsTable");

            migrationBuilder.DropColumn(
                name: "FilterOnlyConfirmed",
                table: "AppSettingsTable");

            migrationBuilder.DropColumn(
                name: "FilterOnlyToday",
                table: "AppSettingsTable");
        }
    }
}
