using Microsoft.EntityFrameworkCore.Migrations;

namespace nhitomi.Core.Migrations
{
    public partial class RemoveGalleryUrlFromDoujin : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "GalleryUrl",
                "Doujins");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                "GalleryUrl",
                "Doujins",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }
    }
}