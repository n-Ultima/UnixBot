using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Unix.Data.Migrations
{
    public partial class SelfAssignableRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<ulong>>(
                name: "SelfAssignableRoles",
                table: "GuildConfigurations",
                type: "numeric(20,0)[]",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelfAssignableRoles",
                table: "GuildConfigurations");
        }
    }
}
