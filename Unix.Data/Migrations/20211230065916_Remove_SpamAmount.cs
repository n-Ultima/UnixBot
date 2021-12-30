using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unix.Data.Migrations
{
    public partial class Remove_SpamAmount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountOfMessagesConsideredSpam",
                table: "GuildConfigurations");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AmountOfMessagesConsideredSpam",
                table: "GuildConfigurations",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}