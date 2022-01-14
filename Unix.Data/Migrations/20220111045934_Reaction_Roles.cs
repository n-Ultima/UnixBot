using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Unix.Data.Migrations
{
    public partial class Reaction_Roles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReactionRoles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuildId = table.Column<ulong>(type: "numeric(20,0)", nullable: false),
                    MessageId = table.Column<ulong>(type: "numeric(20,0)", nullable: false),
                    RoleId = table.Column<ulong>(type: "numeric(20,0)", nullable: false),
                    EmojiId = table.Column<ulong>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReactionRoles", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReactionRoles");
        }
    }
}