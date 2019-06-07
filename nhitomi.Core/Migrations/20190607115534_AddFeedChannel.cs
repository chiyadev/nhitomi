using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace nhitomi.Core.Migrations
{
    public partial class AddFeedChannel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "FeedChannel",
                table => new
                {
                    Id = table.Column<ulong>()
                        .Annotation("Sqlite:Autoincrement", true)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GuildId = table.Column<ulong>(),
                    LastDoujinId = table.Column<int>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedChannel", x => x.Id);
                    table.ForeignKey(
                        "FK_FeedChannel_Guilds_GuildId",
                        x => x.GuildId,
                        "Guilds",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_FeedChannel_Doujins_LastDoujinId",
                        x => x.LastDoujinId,
                        "Doujins",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "FeedChannelTag",
                table => new
                {
                    FeedChannelId = table.Column<ulong>(),
                    TagId = table.Column<int>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedChannelTag", x => new {x.FeedChannelId, x.TagId});
                    table.ForeignKey(
                        "FK_FeedChannelTag_FeedChannel_FeedChannelId",
                        x => x.FeedChannelId,
                        "FeedChannel",
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
                "IX_FeedChannel_GuildId",
                "FeedChannel",
                "GuildId");

            migrationBuilder.CreateIndex(
                "IX_FeedChannel_LastDoujinId",
                "FeedChannel",
                "LastDoujinId");

            migrationBuilder.CreateIndex(
                "IX_FeedChannelTag_TagId",
                "FeedChannelTag",
                "TagId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "FeedChannelTag");

            migrationBuilder.DropTable(
                "FeedChannel");
        }
    }
}