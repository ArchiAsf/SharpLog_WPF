using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharpLog.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmodeField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SUBMODE",
                table: "LogTable",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SUBMODE",
                table: "LogTable");
        }
    }
}
