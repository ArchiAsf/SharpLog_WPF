using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharpLog.Migrations
{
    /// <inheritdoc />
    public partial class AddAppSettingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettingsTable",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    STATION_CALLSIGN = table.Column<string>(type: "TEXT", nullable: true),
                    OPERATOR = table.Column<string>(type: "TEXT", nullable: true),
                    MY_DXCC = table.Column<string>(type: "TEXT", nullable: true),
                    MY_CQ_ZONE = table.Column<string>(type: "TEXT", nullable: true),
                    MY_ITU_ZONE = table.Column<string>(type: "TEXT", nullable: true),
                    MY_GRIDSQUARE = table.Column<string>(type: "TEXT", nullable: true),
                    MY_COUNTRY = table.Column<string>(type: "TEXT", nullable: true),
                    MY_LON = table.Column<string>(type: "TEXT", nullable: true),
                    MY_LAT = table.Column<string>(type: "TEXT", nullable: true),
                    MY_POSTAL_CODE = table.Column<string>(type: "TEXT", nullable: true),
                    MY_STREET = table.Column<string>(type: "TEXT", nullable: true),
                    MY_MailAddress = table.Column<string>(type: "TEXT", nullable: true),
                    MY_QslAddress = table.Column<string>(type: "TEXT", nullable: true),
                    MY_Email = table.Column<string>(type: "TEXT", nullable: true),
                    MY_ANTENNA = table.Column<string>(type: "TEXT", nullable: true),
                    MY_RIG = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettingsTable", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettingsTable");
        }
    }
}
