using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonkeyType.Infrastructure.Migrations
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
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    IsEmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmailConfirmationToken = table.Column<string>(type: "TEXT", nullable: false),
                    EmailConfirmationTokenExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Mode = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId1 = table.Column<Guid>(type: "TEXT", nullable: false)
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
                    table.ForeignKey(
                        name: "FK_StatisticsGames_Users_UserId1",
                        column: x => x.UserId1,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StatisticsGames_UserId",
                table: "StatisticsGames",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StatisticsGames_UserId1",
                table: "StatisticsGames",
                column: "UserId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StatisticsGames");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
