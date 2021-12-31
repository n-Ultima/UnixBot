using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unix.Data.Migrations
{
    public partial class AutoRole_Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal[]>(
                name: "AutoRoles",
                table: "GuildConfigurations",
                type: "numeric(20,0)[]",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoRoles",
                table: "GuildConfigurations");
        }
    }
}
