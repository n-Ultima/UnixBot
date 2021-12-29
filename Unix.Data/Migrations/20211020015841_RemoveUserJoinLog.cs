using Microsoft.EntityFrameworkCore.Migrations;

namespace Unix.Data.Migrations
{
    public partial class RemoveUserJoinLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserJoinedLogChannelId",
                table: "GuildConfigurations");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "UserJoinedLogChannelId",
                table: "GuildConfigurations",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}