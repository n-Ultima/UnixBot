using Microsoft.EntityFrameworkCore.Migrations;

namespace Unix.Data.Migrations
{
    public partial class RoleReq : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "RequiredRoleToUse",
                table: "GuildConfigurations",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0ul);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiredRoleToUse",
                table: "GuildConfigurations");
        }
    }
}
