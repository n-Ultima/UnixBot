using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Unix.Data.Migrations
{
    public partial class TagInit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<List<decimal>>(
                name: "WhitelistedInvites",
                table: "GuildConfigurations",
                type: "numeric[]",
                nullable: true,
                oldClrType: typeof(decimal[]),
                oldType: "numeric(20,0)[]",
                oldNullable: true);

            migrationBuilder.AlterColumn<List<decimal>>(
                name: "SelfAssignableRoles",
                table: "GuildConfigurations",
                type: "numeric[]",
                nullable: true,
                oldClrType: typeof(decimal[]),
                oldType: "numeric(20,0)[]",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuildId = table.Column<ulong>(type: "numeric(20,0)", nullable: false),
                    OwnerId = table.Column<ulong>(type: "numeric(20,0)", nullable: false),
                    Name = table.Column<string>(type: "citext", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.AlterColumn<decimal[]>(
                name: "WhitelistedInvites",
                table: "GuildConfigurations",
                type: "numeric(20,0)[]",
                nullable: true,
                oldClrType: typeof(List<decimal>),
                oldType: "numeric[]",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal[]>(
                name: "SelfAssignableRoles",
                table: "GuildConfigurations",
                type: "numeric(20,0)[]",
                nullable: true,
                oldClrType: typeof(List<decimal>),
                oldType: "numeric[]",
                oldNullable: true);
        }
    }
}
