using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace nhitomi.Core.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "Doujins",
                table => new
                {
                    Id = table.Column<int>()
                        .Annotation("Sqlite:Autoincrement", true)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GalleryUrl = table.Column<string>(),
                    PrettyName = table.Column<string>(),
                    OriginalName = table.Column<string>(),
                    UploadTime = table.Column<DateTime>(),
                    ProcessTime = table.Column<DateTime>(),
                    Source = table.Column<string>(),
                    SourceId = table.Column<string>(),
                    PageCount = table.Column<int>()
                },
                constraints: table => { table.PrimaryKey("PK_Doujins", x => x.Id); });

            migrationBuilder.CreateTable(
                "Guilds",
                table => new
                {
                    Id = table.Column<ulong>()
                        .Annotation("Sqlite:Autoincrement", true)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Language = table.Column<string>(nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_Guilds", x => x.Id); });

            migrationBuilder.CreateTable(
                "Tags",
                table => new
                {
                    Id = table.Column<int>()
                        .Annotation("Sqlite:Autoincrement", true)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Type = table.Column<int>(),
                    Value = table.Column<string>(maxLength: 32)
                },
                constraints: table => { table.PrimaryKey("PK_Tags", x => x.Id); });

            migrationBuilder.CreateTable(
                "Users",
                table => new
                {
                    Id = table.Column<ulong>()
                        .Annotation("Sqlite:Autoincrement", true)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                },
                constraints: table => { table.PrimaryKey("PK_Users", x => x.Id); });

            migrationBuilder.CreateTable(
                "TagRef",
                table => new
                {
                    DoujinId = table.Column<int>(),
                    TagId = table.Column<int>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagRef", x => new {x.DoujinId, x.TagId});
                    table.ForeignKey(
                        "FK_TagRef_Doujins_DoujinId",
                        x => x.DoujinId,
                        "Doujins",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_TagRef_Tags_TagId",
                        x => x.TagId,
                        "Tags",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "Collections",
                table => new
                {
                    Id = table.Column<int>()
                        .Annotation("Sqlite:Autoincrement", true)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 32),
                    Sort = table.Column<int>(),
                    SortDescending = table.Column<bool>(),
                    OwnerId = table.Column<ulong>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collections", x => x.Id);
                    table.ForeignKey(
                        "FK_Collections_Users_OwnerId",
                        x => x.OwnerId,
                        "Users",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "CollectionRef",
                table => new
                {
                    CollectionId = table.Column<int>(),
                    DoujinId = table.Column<int>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionRef", x => new {x.CollectionId, x.DoujinId});
                    table.ForeignKey(
                        "FK_CollectionRef_Collections_CollectionId",
                        x => x.CollectionId,
                        "Collections",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_CollectionRef_Doujins_DoujinId",
                        x => x.DoujinId,
                        "Doujins",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_CollectionRef_DoujinId",
                "CollectionRef",
                "DoujinId");

            migrationBuilder.CreateIndex(
                "IX_Collections_Name",
                "Collections",
                "Name");

            migrationBuilder.CreateIndex(
                "IX_Collections_OwnerId",
                "Collections",
                "OwnerId");

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

            migrationBuilder.CreateIndex(
                "IX_TagRef_TagId",
                "TagRef",
                "TagId");

            migrationBuilder.CreateIndex(
                "IX_Tags_Value",
                "Tags",
                "Value");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "CollectionRef");

            migrationBuilder.DropTable(
                "Guilds");

            migrationBuilder.DropTable(
                "TagRef");

            migrationBuilder.DropTable(
                "Collections");

            migrationBuilder.DropTable(
                "Doujins");

            migrationBuilder.DropTable(
                "Tags");

            migrationBuilder.DropTable(
                "Users");
        }
    }
}