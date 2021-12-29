using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unix.Data.Migrations
{
    public partial class RemoveMuteRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MuteRoleId",
                table: "GuildConfigurations");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MuteRoleId",
                table: "GuildConfigurations",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}