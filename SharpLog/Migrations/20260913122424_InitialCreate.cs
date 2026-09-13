using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharpLog.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogTable",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    QSO_DATE = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TIME_ON = table.Column<string>(type: "TEXT", nullable: true),
                    QSO_DATE_OFF = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TIME_OFF = table.Column<string>(type: "TEXT", nullable: true),
                    CALL = table.Column<string>(type: "TEXT", nullable: false),
                    FREQ = table.Column<double>(type: "REAL", nullable: false),
                    MODE = table.Column<string>(type: "TEXT", nullable: false),
                    RadioOperator = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TimeZone = table.Column<string>(type: "TEXT", nullable: true),
                    RST_RCVD = table.Column<string>(type: "TEXT", nullable: true),
                    RST_SENT = table.Column<string>(type: "TEXT", nullable: true),
                    RX_PWR = table.Column<string>(type: "TEXT", nullable: true),
                    TX_PWR = table.Column<string>(type: "TEXT", nullable: true),
                    QTH = table.Column<string>(type: "TEXT", nullable: true),
                    RIG = table.Column<string>(type: "TEXT", nullable: true),
                    ANT = table.Column<string>(type: "TEXT", nullable: true),
                    Height = table.Column<string>(type: "TEXT", nullable: true),
                    OP = table.Column<string>(type: "TEXT", nullable: true),
                    PROP_MODE = table.Column<string>(type: "TEXT", nullable: true),
                    GRIDSQUARE = table.Column<string>(type: "TEXT", nullable: true),
                    DigitalID = table.Column<string>(type: "TEXT", nullable: false),
                    COMMENT = table.Column<string>(type: "TEXT", nullable: true),
                    GroupID = table.Column<int>(type: "INTEGER", nullable: true),
                    StarID = table.Column<int>(type: "INTEGER", nullable: true),
                    QSL_RCVD = table.Column<bool>(type: "INTEGER", nullable: false),
                    QSLRDATE = table.Column<DateTime>(type: "TEXT", nullable: true),
                    QSLSDATE = table.Column<DateTime>(type: "TEXT", nullable: true),
                    QslAddress = table.Column<string>(type: "TEXT", nullable: true),
                    PostalCode = table.Column<string>(type: "TEXT", nullable: true),
                    ReceiverName = table.Column<string>(type: "TEXT", nullable: true),
                    ReceiverContact = table.Column<string>(type: "TEXT", nullable: true),
                    Weather = table.Column<string>(type: "TEXT", nullable: true),
                    Temperature = table.Column<string>(type: "TEXT", nullable: true),
                    IsConfirmed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogTable", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LogTable_CALL",
                table: "LogTable",
                column: "CALL");

            migrationBuilder.CreateIndex(
                name: "IX_LogTable_FREQ",
                table: "LogTable",
                column: "FREQ");

            migrationBuilder.CreateIndex(
                name: "IX_LogTable_GroupID",
                table: "LogTable",
                column: "GroupID");

            migrationBuilder.CreateIndex(
                name: "IX_LogTable_MODE",
                table: "LogTable",
                column: "MODE");

            migrationBuilder.CreateIndex(
                name: "IX_LogTable_QSO_DATE",
                table: "LogTable",
                column: "QSO_DATE");

            migrationBuilder.CreateIndex(
                name: "IX_LogTable_StarID",
                table: "LogTable",
                column: "StarID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogTable");
        }
    }
}
