using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unix.Data.Migrations
{
    public partial class Infraction_Rescind : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRescinded",
                table: "Infractions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRescinded",
                table: "Infractions");
        }
    }
}