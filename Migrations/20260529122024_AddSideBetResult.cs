using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchBetting.Migrations
{
    /// <inheritdoc />
    public partial class AddSideBetResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SideBetResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TournamentId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Toppscorer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WinnerTeam = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MostCards = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SideBetResults", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SideBetResults");
        }
    }
}
