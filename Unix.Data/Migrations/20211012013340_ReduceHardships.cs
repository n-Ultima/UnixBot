using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Unix.Data.Migrations
{
    public partial class ReduceHardships : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildPunishmentConfigurations");

            migrationBuilder.DropTable(
                name: "RoleClaimMaps");

            migrationBuilder.DropTable(
                name: "UserClaimMaps");

            migrationBuilder.DropColumn(
                name: "PlainTextLogs",
                table: "GuildConfigurations");

            migrationBuilder.AddColumn<ulong>(
                name: "AdministratorRoleId",
                table: "GuildConfigurations",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddColumn<ulong>(
                name: "ModeratorRoleId",
                table: "GuildConfigurations",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0ul);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdministratorRoleId",
                table: "GuildConfigurations");

            migrationBuilder.DropColumn(
                name: "ModeratorRoleId",
                table: "GuildConfigurations");

            migrationBuilder.AddColumn<bool>(
                name: "PlainTextLogs",
                table: "GuildConfigurations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "GuildPunishmentConfigurations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    NumberOfInfractions = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildPunishmentConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaimMaps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    RoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaimMaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserClaimMaps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaimMaps", x => x.Id);
                });
        }
    }
}