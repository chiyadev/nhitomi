using Microsoft.EntityFrameworkCore.Migrations;

namespace nhitomi.Core.Migrations
{
    public partial class AddDoujinDenormalized : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                "IX_Doujins_OriginalName",
                "Doujins");

            migrationBuilder.DropIndex(
                "IX_Doujins_PrettyName",
                "Doujins");

            migrationBuilder.DropIndex(
                "IX_Doujins_Source",
                "Doujins");

            migrationBuilder.DropIndex(
                "IX_Doujins_SourceId",
                "Doujins");

            migrationBuilder.CreateTable(
                "DoujinTexts",
                table => new
                {
                    Id = table.Column<int>(),
                    Value = table.Column<string>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoujinTexts", x => x.Id);
                    table.ForeignKey(
                        "FK_DoujinTexts_Doujins_Id",
                        x => x.Id,
                        "Doujins",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_Doujins_ProcessTime",
                "Doujins",
                "ProcessTime");

            migrationBuilder.CreateIndex(
                "IX_Doujins_UploadTime",
                "Doujins",
                "UploadTime");

            migrationBuilder.CreateIndex(
                "IX_Doujins_Source_SourceId",
                "Doujins",
                new[] {"Source", "SourceId"});

            migrationBuilder.CreateIndex(
                "IX_DoujinTexts_Value",
                "DoujinTexts",
                "Value");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "DoujinTexts");

            migrationBuilder.DropIndex(
                "IX_Doujins_ProcessTime",
                "Doujins");

            migrationBuilder.DropIndex(
                "IX_Doujins_UploadTime",
                "Doujins");

            migrationBuilder.DropIndex(
                "IX_Doujins_Source_SourceId",
                "Doujins");

            migrationBuilder.CreateIndex(
                "IX_Doujins_OriginalName",
                "Doujins",
                "OriginalName");

            migrationBuilder.CreateIndex(
                "IX_Doujins_PrettyName",
                "Doujins",
                "PrettyName");

            migrationBuilder.CreateIndex(
                "IX_Doujins_Source",
                "Doujins",
                "Source");

            migrationBuilder.CreateIndex(
                "IX_Doujins_SourceId",
                "Doujins",
                "SourceId");
        }
    }
}
