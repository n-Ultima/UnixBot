using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Unix.Data.Migrations
{
    public partial class GuildWhitelistsAndBannedTerms : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AmountOfMessagesConsideredSpam",
                table: "GuildConfigurations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<List<string>>(
                name: "BannedTerms",
                table: "GuildConfigurations",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<List<ulong>>(
                name: "WhitelistedInvites",
                table: "GuildConfigurations",
                type: "numeric(20,0)[]",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountOfMessagesConsideredSpam",
                table: "GuildConfigurations");

            migrationBuilder.DropColumn(
                name: "BannedTerms",
                table: "GuildConfigurations");

            migrationBuilder.DropColumn(
                name: "WhitelistedInvites",
                table: "GuildConfigurations");
        }
    }
}
