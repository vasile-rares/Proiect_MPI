using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Keyless.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    TestsStarted = table.Column<int>(type: "INTEGER", nullable: false),
                    TestsCompleted = table.Column<int>(type: "INTEGER", nullable: false),
                    Biography = table.Column<string>(type: "TEXT", nullable: true),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatisticsGames",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WordsPerMinute = table.Column<decimal>(type: "TEXT", nullable: false),
                    RawWordsPerMinute = table.Column<decimal>(type: "TEXT", nullable: false),
                    Accuracy = table.Column<decimal>(type: "TEXT", nullable: false),
                    Consistency = table.Column<decimal>(type: "TEXT", nullable: false),
                    CorrectCharacters = table.Column<int>(type: "INTEGER", nullable: false),
                    IncorrectCharacters = table.Column<int>(type: "INTEGER", nullable: false),
                    ExtraCharacters = table.Column<int>(type: "INTEGER", nullable: false),
                    MissedCharacters = table.Column<int>(type: "INTEGER", nullable: false),
                    DurationInSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    Mode = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatisticsGames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatisticsGames_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserStatsAggregates",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GamesCount = table.Column<int>(type: "INTEGER", nullable: false),
                    HighestWordsPerMinute = table.Column<decimal>(type: "TEXT", nullable: false),
                    AverageWordsPerMinute = table.Column<decimal>(type: "TEXT", nullable: false),
                    HighestRawWordsPerMinute = table.Column<decimal>(type: "TEXT", nullable: false),
                    AverageRawWordsPerMinute = table.Column<decimal>(type: "TEXT", nullable: false),
                    HighestAccuracy = table.Column<decimal>(type: "TEXT", nullable: false),
                    AverageAccuracy = table.Column<decimal>(type: "TEXT", nullable: false),
                    HighestConsistency = table.Column<decimal>(type: "TEXT", nullable: false),
                    AverageConsistency = table.Column<decimal>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStatsAggregates", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserStatsAggregates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StatisticsGames_CreatedAt_DurationInSeconds_UserId",
                table: "StatisticsGames",
                columns: new[] { "CreatedAt", "DurationInSeconds", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_StatisticsGames_UserId",
                table: "StatisticsGames",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStatsAggregates_UpdatedAt",
                table: "UserStatsAggregates",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StatisticsGames");

            migrationBuilder.DropTable(
                name: "UserStatsAggregates");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
