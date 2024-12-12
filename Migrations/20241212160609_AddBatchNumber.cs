using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PipeWorkshopApp.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BatchNumber",
                table: "Pipes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatchNumber",
                table: "Pipes");
        }
    }
}
