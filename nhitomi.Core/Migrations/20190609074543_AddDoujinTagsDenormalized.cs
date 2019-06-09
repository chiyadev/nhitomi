using Microsoft.EntityFrameworkCore.Migrations;

namespace nhitomi.Core.Migrations
{
    public partial class AddDoujinTagsDenormalized : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                "TagsDenormalized",
                "Doujins",
                maxLength: 2048);

            migrationBuilder.CreateIndex(
                "IX_Doujins_TagsDenormalized",
                "Doujins",
                "TagsDenormalized");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                "IX_Doujins_TagsDenormalized",
                "Doujins");

            migrationBuilder.DropColumn(
                "TagsDenormalized",
                "Doujins");
        }
    }
}