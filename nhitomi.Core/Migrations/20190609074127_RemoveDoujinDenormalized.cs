using Microsoft.EntityFrameworkCore.Migrations;

namespace nhitomi.Core.Migrations
{
    public partial class RemoveDoujinDenormalized : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "DoujinTexts");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "DoujinTexts",
                table => new
                {
                    Id = table.Column<int>(),
                    Value = table.Column<string>(maxLength: 4096)
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
                "IX_DoujinTexts_Value",
                "DoujinTexts",
                "Value");
        }
    }
}