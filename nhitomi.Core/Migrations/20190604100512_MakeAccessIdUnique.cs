using Microsoft.EntityFrameworkCore.Migrations;

namespace nhitomi.Core.Migrations
{
    public partial class MakeAccessIdUnique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                "IX_Tags_AccessId",
                "Tags");

            migrationBuilder.DropIndex(
                "IX_Doujins_AccessId",
                "Doujins");

            migrationBuilder.CreateIndex(
                "IX_Tags_AccessId",
                "Tags",
                "AccessId",
                unique: true);

            migrationBuilder.CreateIndex(
                "IX_Doujins_AccessId",
                "Doujins",
                "AccessId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                "IX_Tags_AccessId",
                "Tags");

            migrationBuilder.DropIndex(
                "IX_Doujins_AccessId",
                "Doujins");

            migrationBuilder.CreateIndex(
                "IX_Tags_AccessId",
                "Tags",
                "AccessId");

            migrationBuilder.CreateIndex(
                "IX_Doujins_AccessId",
                "Doujins",
                "AccessId");
        }
    }
}