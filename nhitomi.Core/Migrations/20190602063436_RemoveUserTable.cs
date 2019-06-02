using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace nhitomi.Core.Migrations
{
    public partial class RemoveUserTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_Collections_Users_OwnerId",
                "Collections");

            migrationBuilder.DropTable(
                "Users");

            migrationBuilder.DropIndex(
                "IX_Collections_OwnerId",
                "Collections");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "Users",
                table => new
                {
                    Id = table.Column<ulong>()
                        .Annotation("Sqlite:Autoincrement", true)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                },
                constraints: table => { table.PrimaryKey("PK_Users", x => x.Id); });

            migrationBuilder.CreateIndex(
                "IX_Collections_OwnerId",
                "Collections",
                "OwnerId");

            migrationBuilder.AddForeignKey(
                "FK_Collections_Users_OwnerId",
                "Collections",
                "OwnerId",
                "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}