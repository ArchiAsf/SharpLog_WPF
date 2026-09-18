using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharpLog.Migrations
{
    /// <inheritdoc />
    public partial class AddContactedOpField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CONTACTED_OP",
                table: "LogTable",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CONTACTED_OP",
                table: "LogTable");
        }
    }
}
