using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManager.API.Migrations
{
    /// <inheritdoc />
    public partial class MakePriorityDynamic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriorityScore",
                table: "Tasks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "PriorityScore",
                table: "Tasks",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
