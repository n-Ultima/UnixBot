using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unix.Data.Migrations
{
    public partial class MiscellanousLogChannelId_Add : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "MiscellaneousLogChannelId",
                table: "GuildConfigurations",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0ul);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MiscellaneousLogChannelId",
                table: "GuildConfigurations");
        }
    }
}
