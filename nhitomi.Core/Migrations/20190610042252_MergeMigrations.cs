using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace nhitomi.Core.Migrations
{
    public partial class MergeMigrations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "Collections",
                table => new
                {
                    Id = table.Column<int>()
                              .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name           = table.Column<string>(maxLength: 32),
                    Sort           = table.Column<int>(),
                    SortDescending = table.Column<bool>(),
                    OwnerId        = table.Column<ulong>()
                },
                constraints: table => { table.PrimaryKey("PK_Collections", x => x.Id); });

            migrationBuilder.CreateTable(
                "Doujins",
                table => new
                {
                    Id = table.Column<int>()
                              .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AccessId         = table.Column<Guid>(),
                    PrettyName       = table.Column<string>(maxLength: 256),
                    OriginalName     = table.Column<string>(maxLength: 256),
                    UploadTime       = table.Column<DateTime>(),
                    ProcessTime      = table.Column<DateTime>(),
                    Source           = table.Column<string>(maxLength: 16),
                    SourceId         = table.Column<string>(maxLength: 16),
                    Data             = table.Column<string>(maxLength: 4096, nullable: true),
                    PageCount        = table.Column<int>(),
                    TagsDenormalized = table.Column<string>(nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_Doujins", x => x.Id); });

            migrationBuilder.CreateTable(
                "Guilds",
                table => new
                {
                    Id = table.Column<ulong>()
                              .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Language            = table.Column<string>(nullable: true),
                    SearchQualityFilter = table.Column<bool>(nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_Guilds", x => x.Id); });

            migrationBuilder.CreateTable(
                "Tags",
                table => new
                {
                    Id = table.Column<int>()
                              .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AccessId = table.Column<Guid>(),
                    Type     = table.Column<int>(),
                    Value    = table.Column<string>(maxLength: 128)
                },
                constraints: table => { table.PrimaryKey("PK_Tags", x => x.Id); });

            migrationBuilder.CreateTable(
                "CollectionRef",
                table => new
                {
                    CollectionId = table.Column<int>(),
                    DoujinId     = table.Column<int>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionRef", x => new { x.CollectionId, x.DoujinId });

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

            migrationBuilder.CreateTable(
                "FeedChannels",
                table => new
                {
                    Id = table.Column<ulong>()
                              .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GuildId       = table.Column<ulong>(),
                    LastDoujinId  = table.Column<int>(),
                    WhitelistType = table.Column<int>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedChannels", x => x.Id);

                    table.ForeignKey(
                        "FK_FeedChannels_Guilds_GuildId",
                        x => x.GuildId,
                        "Guilds",
                        "Id",
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        "FK_FeedChannels_Doujins_LastDoujinId",
                        x => x.LastDoujinId,
                        "Doujins",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "TagRef",
                table => new
                {
                    DoujinId = table.Column<int>(),
                    TagId    = table.Column<int>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagRef", x => new { x.DoujinId, x.TagId });

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
                "FeedChannelTag",
                table => new
                {
                    FeedChannelId = table.Column<ulong>(),
                    TagId         = table.Column<int>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedChannelTag", x => new { x.FeedChannelId, x.TagId });

                    table.ForeignKey(
                        "FK_FeedChannelTag_FeedChannels_FeedChannelId",
                        x => x.FeedChannelId,
                        "FeedChannels",
                        "Id",
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        "FK_FeedChannelTag_Tags_TagId",
                        x => x.TagId,
                        "Tags",
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
                "IX_Doujins_AccessId",
                "Doujins",
                "AccessId",
                unique: true);

            migrationBuilder.CreateIndex(
                "IX_Doujins_ProcessTime",
                "Doujins",
                "ProcessTime");

            migrationBuilder.CreateIndex(
                "IX_Doujins_TagsDenormalized",
                "Doujins",
                "TagsDenormalized");

            migrationBuilder.CreateIndex(
                "IX_Doujins_UploadTime",
                "Doujins",
                "UploadTime");

            migrationBuilder.CreateIndex(
                "IX_Doujins_Source_SourceId",
                "Doujins",
                new[] { "Source", "SourceId" });

            migrationBuilder.CreateIndex(
                "IX_Doujins_Source_UploadTime",
                "Doujins",
                new[] { "Source", "UploadTime" });

            migrationBuilder.CreateIndex(
                "IX_FeedChannels_GuildId",
                "FeedChannels",
                "GuildId");

            migrationBuilder.CreateIndex(
                "IX_FeedChannels_LastDoujinId",
                "FeedChannels",
                "LastDoujinId");

            migrationBuilder.CreateIndex(
                "IX_FeedChannelTag_TagId",
                "FeedChannelTag",
                "TagId");

            migrationBuilder.CreateIndex(
                "IX_TagRef_TagId",
                "TagRef",
                "TagId");

            migrationBuilder.CreateIndex(
                "IX_Tags_AccessId",
                "Tags",
                "AccessId",
                unique: true);

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
                "FeedChannelTag");

            migrationBuilder.DropTable(
                "TagRef");

            migrationBuilder.DropTable(
                "Collections");

            migrationBuilder.DropTable(
                "FeedChannels");

            migrationBuilder.DropTable(
                "Tags");

            migrationBuilder.DropTable(
                "Guilds");

            migrationBuilder.DropTable(
                "Doujins");
        }
    }
}