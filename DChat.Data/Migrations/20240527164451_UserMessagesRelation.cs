using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DChat.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserMessagesRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultRoom",
                schema: "Identity",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Sender",
                table: "Messages",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_Sender",
                table: "Messages",
                column: "Sender");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Users_Sender",
                table: "Messages",
                column: "Sender",
                principalSchema: "Identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_Sender",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_Sender",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "DefaultRoom",
                schema: "Identity",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "Sender",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
