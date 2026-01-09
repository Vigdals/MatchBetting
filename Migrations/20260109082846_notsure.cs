using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchBetting.Migrations
{
    /// <inheritdoc />
    public partial class notsure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompetitionGroupCompetitionId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompetitionGroups",
                columns: table => new
                {
                    CompetitionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    isactive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitionGroups", x => x.CompetitionId);
                });

            migrationBuilder.CreateTable(
                name: "FootballPlayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ExternalApiId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FootballPlayers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CompetitionGroupCompetitionId",
                table: "AspNetUsers",
                column: "CompetitionGroupCompetitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_CompetitionGroups_CompetitionGroupCompetitionId",
                table: "AspNetUsers",
                column: "CompetitionGroupCompetitionId",
                principalTable: "CompetitionGroups",
                principalColumn: "CompetitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_CompetitionGroups_CompetitionGroupCompetitionId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "CompetitionGroups");

            migrationBuilder.DropTable(
                name: "FootballPlayers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CompetitionGroupCompetitionId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CompetitionGroupCompetitionId",
                table: "AspNetUsers");
        }
    }
}
