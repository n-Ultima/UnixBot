using Microsoft.EntityFrameworkCore.Migrations;

namespace Unix.Data.Migrations
{
    public partial class Phisherman : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhishermanApiKey",
                table: "GuildConfigurations",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhishermanApiKey",
                table: "GuildConfigurations");
        }
    }
}