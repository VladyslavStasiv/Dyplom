using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManager.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersAndCollaboration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Boards_Users_UserId",
                table: "Boards");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Boards");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Boards",
                newName: "OwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_Boards_UserId",
                table: "Boards",
                newName: "IX_Boards_OwnerId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Deadline",
                table: "Tasks",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "UserBoards",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    BoardId = table.Column<int>(type: "integer", nullable: false),
                    AccessLevel = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBoards", x => new { x.UserId, x.BoardId });
                    table.ForeignKey(
                        name: "FK_UserBoards_Boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "Boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserBoards_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserBoards_BoardId",
                table: "UserBoards",
                column: "BoardId");

            migrationBuilder.AddForeignKey(
                name: "FK_Boards_Users_OwnerId",
                table: "Boards",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Boards_Users_OwnerId",
                table: "Boards");

            migrationBuilder.DropTable(
                name: "UserBoards");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "Boards",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Boards_OwnerId",
                table: "Boards",
                newName: "IX_Boards_UserId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Deadline",
                table: "Tasks",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Boards",
                type: "text",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Boards_Users_UserId",
                table: "Boards",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
