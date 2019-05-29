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
                "Artists",
                table => new
                {
                    Id = table.Column<int>()
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Value = table.Column<string>(maxLength: 32)
                },
                constraints: table => { table.PrimaryKey("PK_Artists", x => x.Id); });

            migrationBuilder.CreateTable(
                "Categories",
                table => new
                {
                    Id = table.Column<int>()
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Value = table.Column<string>(maxLength: 32)
                },
                constraints: table => { table.PrimaryKey("PK_Categories", x => x.Id); });

            migrationBuilder.CreateTable(
                "Characters",
                table => new
                {
                    Id = table.Column<int>()
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Value = table.Column<string>(maxLength: 32)
                },
                constraints: table => { table.PrimaryKey("PK_Characters", x => x.Id); });

            migrationBuilder.CreateTable(
                "Groups",
                table => new
                {
                    Id = table.Column<int>()
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Value = table.Column<string>(maxLength: 32)
                },
                constraints: table => { table.PrimaryKey("PK_Groups", x => x.Id); });

            migrationBuilder.CreateTable(
                "Guilds",
                table => new
                {
                    Id = table.Column<ulong>()
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Language = table.Column<string>(nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_Guilds", x => x.Id); });

            migrationBuilder.CreateTable(
                "Languages",
                table => new
                {
                    Id = table.Column<int>()
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Value = table.Column<string>(maxLength: 32)
                },
                constraints: table => { table.PrimaryKey("PK_Languages", x => x.Id); });

            migrationBuilder.CreateTable(
                "Parodies",
                table => new
                {
                    Id = table.Column<int>()
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Value = table.Column<string>(maxLength: 32)
                },
                constraints: table => { table.PrimaryKey("PK_Parodies", x => x.Id); });

            migrationBuilder.CreateTable(
                "Scanlators",
                table => new
                {
                    Id = table.Column<int>()
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Value = table.Column<string>(maxLength: 32)
                },
                constraints: table => { table.PrimaryKey("PK_Scanlators", x => x.Id); });

            migrationBuilder.CreateTable(
                "Tags",
                table => new
                {
                    Id = table.Column<int>()
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Value = table.Column<string>(maxLength: 32)
                },
                constraints: table => { table.PrimaryKey("PK_Tags", x => x.Id); });

            migrationBuilder.CreateTable(
                "Users",
                table => new
                {
                    Id = table.Column<ulong>()
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                },
                constraints: table => { table.PrimaryKey("PK_Users", x => x.Id); });

            migrationBuilder.CreateTable(
                "Doujins",
                table => new
                {
                    Id = table.Column<int>()
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GalleryUrl = table.Column<string>(),
                    PrettyName = table.Column<string>(),
                    OriginalName = table.Column<string>(),
                    UploadTime = table.Column<DateTime>(),
                    ProcessTime = table.Column<DateTime>()
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn),
                    Source = table.Column<string>(),
                    SourceId = table.Column<string>(),
                    ArtistId = table.Column<int>(nullable: true),
                    GroupId = table.Column<int>(nullable: true),
                    ScanlatorId = table.Column<int>(nullable: true),
                    LanguageId = table.Column<int>(nullable: true),
                    ParodyOfId = table.Column<int>(nullable: true),
                    PageCount = table.Column<int>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doujins", x => x.Id);
                    table.ForeignKey(
                        "FK_Doujins_Artists_ArtistId",
                        x => x.ArtistId,
                        "Artists",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        "FK_Doujins_Groups_GroupId",
                        x => x.GroupId,
                        "Groups",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        "FK_Doujins_Languages_LanguageId",
                        x => x.LanguageId,
                        "Languages",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        "FK_Doujins_Parodies_ParodyOfId",
                        x => x.ParodyOfId,
                        "Parodies",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        "FK_Doujins_Scanlators_ScanlatorId",
                        x => x.ScanlatorId,
                        "Scanlators",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                "Collections",
                table => new
                {
                    Id = table.Column<int>()
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
                "CategoryRefs",
                table => new
                {
                    DoujinId = table.Column<int>(),
                    TagId = table.Column<int>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryRefs", x => new {x.DoujinId, x.TagId});
                    table.ForeignKey(
                        "FK_CategoryRefs_Doujins_DoujinId",
                        x => x.DoujinId,
                        "Doujins",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_CategoryRefs_Categories_TagId",
                        x => x.TagId,
                        "Categories",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "CharacterRefs",
                table => new
                {
                    DoujinId = table.Column<int>(),
                    TagId = table.Column<int>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterRefs", x => new {x.DoujinId, x.TagId});
                    table.ForeignKey(
                        "FK_CharacterRefs_Doujins_DoujinId",
                        x => x.DoujinId,
                        "Doujins",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_CharacterRefs_Characters_TagId",
                        x => x.TagId,
                        "Characters",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "TagRefs",
                table => new
                {
                    DoujinId = table.Column<int>(),
                    TagId = table.Column<int>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagRefs", x => new {x.DoujinId, x.TagId});
                    table.ForeignKey(
                        "FK_TagRefs_Doujins_DoujinId",
                        x => x.DoujinId,
                        "Doujins",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_TagRefs_Tags_TagId",
                        x => x.TagId,
                        "Tags",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "CollectionRefs",
                table => new
                {
                    CollectionId = table.Column<int>(),
                    DoujinId = table.Column<int>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionRefs", x => new {x.CollectionId, x.DoujinId});
                    table.ForeignKey(
                        "FK_CollectionRefs_Collections_CollectionId",
                        x => x.CollectionId,
                        "Collections",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_CollectionRefs_Doujins_DoujinId",
                        x => x.DoujinId,
                        "Doujins",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_Artists_Value",
                "Artists",
                "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                "IX_Categories_Value",
                "Categories",
                "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                "IX_CategoryRefs_TagId",
                "CategoryRefs",
                "TagId");

            migrationBuilder.CreateIndex(
                "IX_CharacterRefs_TagId",
                "CharacterRefs",
                "TagId");

            migrationBuilder.CreateIndex(
                "IX_Characters_Value",
                "Characters",
                "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                "IX_CollectionRefs_DoujinId",
                "CollectionRefs",
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
                "IX_Doujins_ArtistId",
                "Doujins",
                "ArtistId");

            migrationBuilder.CreateIndex(
                "IX_Doujins_GroupId",
                "Doujins",
                "GroupId");

            migrationBuilder.CreateIndex(
                "IX_Doujins_LanguageId",
                "Doujins",
                "LanguageId");

            migrationBuilder.CreateIndex(
                "IX_Doujins_OriginalName",
                "Doujins",
                "OriginalName");

            migrationBuilder.CreateIndex(
                "IX_Doujins_ParodyOfId",
                "Doujins",
                "ParodyOfId");

            migrationBuilder.CreateIndex(
                "IX_Doujins_PrettyName",
                "Doujins",
                "PrettyName");

            migrationBuilder.CreateIndex(
                "IX_Doujins_ScanlatorId",
                "Doujins",
                "ScanlatorId");

            migrationBuilder.CreateIndex(
                "IX_Doujins_Source",
                "Doujins",
                "Source");

            migrationBuilder.CreateIndex(
                "IX_Doujins_SourceId",
                "Doujins",
                "SourceId");

            migrationBuilder.CreateIndex(
                "IX_Groups_Value",
                "Groups",
                "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                "IX_Languages_Value",
                "Languages",
                "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                "IX_Parodies_Value",
                "Parodies",
                "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                "IX_Scanlators_Value",
                "Scanlators",
                "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                "IX_TagRefs_TagId",
                "TagRefs",
                "TagId");

            migrationBuilder.CreateIndex(
                "IX_Tags_Value",
                "Tags",
                "Value",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "CategoryRefs");

            migrationBuilder.DropTable(
                "CharacterRefs");

            migrationBuilder.DropTable(
                "CollectionRefs");

            migrationBuilder.DropTable(
                "Guilds");

            migrationBuilder.DropTable(
                "TagRefs");

            migrationBuilder.DropTable(
                "Categories");

            migrationBuilder.DropTable(
                "Characters");

            migrationBuilder.DropTable(
                "Collections");

            migrationBuilder.DropTable(
                "Doujins");

            migrationBuilder.DropTable(
                "Tags");

            migrationBuilder.DropTable(
                "Users");

            migrationBuilder.DropTable(
                "Artists");

            migrationBuilder.DropTable(
                "Groups");

            migrationBuilder.DropTable(
                "Languages");

            migrationBuilder.DropTable(
                "Parodies");

            migrationBuilder.DropTable(
                "Scanlators");
        }
    }
}