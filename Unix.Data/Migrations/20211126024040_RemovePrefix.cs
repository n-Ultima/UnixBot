using Microsoft.EntityFrameworkCore.Migrations;

namespace Unix.Data.Migrations
{
    public partial class RemovePrefix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Prefix",
                table: "GuildConfigurations");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Prefix",
                table: "GuildConfigurations",
                type: "text",
                nullable: true);
        }
    }
}
