using Microsoft.EntityFrameworkCore.Migrations;

namespace nhitomi.Core.Migrations
{
    public partial class AddFeedChannelWhitelistType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_FeedChannel_Guilds_GuildId",
                "FeedChannel");

            migrationBuilder.DropForeignKey(
                "FK_FeedChannel_Doujins_LastDoujinId",
                "FeedChannel");

            migrationBuilder.DropForeignKey(
                "FK_FeedChannelTag_FeedChannel_FeedChannelId",
                "FeedChannelTag");

            migrationBuilder.DropPrimaryKey(
                "PK_FeedChannel",
                "FeedChannel");

            migrationBuilder.RenameTable(
                "FeedChannel",
                newName: "FeedChannels");

            migrationBuilder.RenameIndex(
                "IX_FeedChannel_LastDoujinId",
                table: "FeedChannels",
                newName: "IX_FeedChannels_LastDoujinId");

            migrationBuilder.RenameIndex(
                "IX_FeedChannel_GuildId",
                table: "FeedChannels",
                newName: "IX_FeedChannels_GuildId");

            migrationBuilder.AddColumn<int>(
                "WhitelistType",
                "FeedChannels",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                "PK_FeedChannels",
                "FeedChannels",
                "Id");

            migrationBuilder.AddForeignKey(
                "FK_FeedChannels_Guilds_GuildId",
                "FeedChannels",
                "GuildId",
                "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_FeedChannels_Doujins_LastDoujinId",
                "FeedChannels",
                "LastDoujinId",
                "Doujins",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_FeedChannelTag_FeedChannels_FeedChannelId",
                "FeedChannelTag",
                "FeedChannelId",
                "FeedChannels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_FeedChannels_Guilds_GuildId",
                "FeedChannels");

            migrationBuilder.DropForeignKey(
                "FK_FeedChannels_Doujins_LastDoujinId",
                "FeedChannels");

            migrationBuilder.DropForeignKey(
                "FK_FeedChannelTag_FeedChannels_FeedChannelId",
                "FeedChannelTag");

            migrationBuilder.DropPrimaryKey(
                "PK_FeedChannels",
                "FeedChannels");

            migrationBuilder.DropColumn(
                "WhitelistType",
                "FeedChannels");

            migrationBuilder.RenameTable(
                "FeedChannels",
                newName: "FeedChannel");

            migrationBuilder.RenameIndex(
                "IX_FeedChannels_LastDoujinId",
                table: "FeedChannel",
                newName: "IX_FeedChannel_LastDoujinId");

            migrationBuilder.RenameIndex(
                "IX_FeedChannels_GuildId",
                table: "FeedChannel",
                newName: "IX_FeedChannel_GuildId");

            migrationBuilder.AddPrimaryKey(
                "PK_FeedChannel",
                "FeedChannel",
                "Id");

            migrationBuilder.AddForeignKey(
                "FK_FeedChannel_Guilds_GuildId",
                "FeedChannel",
                "GuildId",
                "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_FeedChannel_Doujins_LastDoujinId",
                "FeedChannel",
                "LastDoujinId",
                "Doujins",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_FeedChannelTag_FeedChannel_FeedChannelId",
                "FeedChannelTag",
                "FeedChannelId",
                "FeedChannel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}