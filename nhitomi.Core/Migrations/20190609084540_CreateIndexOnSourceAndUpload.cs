using Microsoft.EntityFrameworkCore.Migrations;

namespace nhitomi.Core.Migrations
{
    public partial class CreateIndexOnSourceAndUpload : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                "IX_Doujins_Source_UploadTime",
                "Doujins",
                new[] {"Source", "UploadTime"});
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                "IX_Doujins_Source_UploadTime",
                "Doujins");
        }
    }
}