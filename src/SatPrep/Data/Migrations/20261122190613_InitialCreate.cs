using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatPrep.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Domains",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Section = table.Column<int>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Domains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudyPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    TestDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TargetScore = table.Column<int>(type: "INTEGER", nullable: false),
                    MinutesPerDay = table.Column<int>(type: "INTEGER", nullable: false),
                    PracticeTestDay = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentModule = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModuleStartedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RwRoute = table.Column<int>(type: "INTEGER", nullable: false),
                    MathRoute = table.Column<int>(type: "INTEGER", nullable: false),
                    RwScore = table.Column<int>(type: "INTEGER", nullable: true),
                    MathScore = table.Column<int>(type: "INTEGER", nullable: true),
                    TotalScore = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserName = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSeenUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VocabWords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Word = table.Column<string>(type: "TEXT", nullable: false),
                    PartOfSpeech = table.Column<string>(type: "TEXT", nullable: false),
                    Definition = table.Column<string>(type: "TEXT", nullable: false),
                    Example = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabWords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DomainId = table.Column<int>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Skills_Domains_DomainId",
                        column: x => x.DomainId,
                        principalTable: "Domains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VocabProgress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    VocabWordId = table.Column<int>(type: "INTEGER", nullable: false),
                    Box = table.Column<int>(type: "INTEGER", nullable: false),
                    NextDueUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TimesSeen = table.Column<int>(type: "INTEGER", nullable: false),
                    TimesCorrect = table.Column<int>(type: "INTEGER", nullable: false),
                    LastReviewedUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VocabProgress_VocabWords_VocabWordId",
                        column: x => x.VocabWordId,
                        principalTable: "VocabWords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Masteries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    SkillId = table.Column<int>(type: "INTEGER", nullable: false),
                    Rating = table.Column<double>(type: "REAL", nullable: false),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CorrectCount = table.Column<int>(type: "INTEGER", nullable: false),
                    HardCorrectCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastPracticedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Masteries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Masteries_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExternalId = table.Column<string>(type: "TEXT", nullable: false),
                    SkillId = table.Column<int>(type: "INTEGER", nullable: false),
                    Difficulty = table.Column<int>(type: "INTEGER", nullable: false),
                    Format = table.Column<int>(type: "INTEGER", nullable: false),
                    Passage = table.Column<string>(type: "TEXT", nullable: true),
                    Stem = table.Column<string>(type: "TEXT", nullable: false),
                    ChoicesJson = table.Column<string>(type: "TEXT", nullable: false),
                    CorrectAnswer = table.Column<string>(type: "TEXT", nullable: false),
                    Explanation = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsManual = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questions_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Attempts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    QuestionId = table.Column<int>(type: "INTEGER", nullable: false),
                    GivenAnswer = table.Column<string>(type: "TEXT", nullable: false),
                    IsCorrect = table.Column<bool>(type: "INTEGER", nullable: false),
                    SecondsSpent = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    TestAttemptId = table.Column<int>(type: "INTEGER", nullable: true),
                    AnsweredUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attempts_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Marks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    QuestionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Marks_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    QuestionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Stage = table.Column<int>(type: "INTEGER", nullable: false),
                    DueUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsRetired = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TestAttemptId = table.Column<int>(type: "INTEGER", nullable: false),
                    ModuleIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    QuestionId = table.Column<int>(type: "INTEGER", nullable: false),
                    GivenAnswer = table.Column<string>(type: "TEXT", nullable: false),
                    IsCorrect = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsMarked = table.Column<bool>(type: "INTEGER", nullable: false),
                    SecondsSpent = table.Column<int>(type: "INTEGER", nullable: false),
                    EliminatedJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestItems_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestItems_Tests_TestAttemptId",
                        column: x => x.TestAttemptId,
                        principalTable: "Tests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_QuestionId",
                table: "Attempts",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_UserId_AnsweredUtc",
                table: "Attempts",
                columns: new[] { "UserId", "AnsweredUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_UserId_QuestionId",
                table: "Attempts",
                columns: new[] { "UserId", "QuestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Domains_Code",
                table: "Domains",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Marks_QuestionId",
                table: "Marks",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Marks_UserId_QuestionId",
                table: "Marks",
                columns: new[] { "UserId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Masteries_SkillId",
                table: "Masteries",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_Masteries_UserId_SkillId",
                table: "Masteries",
                columns: new[] { "UserId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_ExternalId",
                table: "Questions",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_SkillId_Difficulty",
                table: "Questions",
                columns: new[] { "SkillId", "Difficulty" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_QuestionId",
                table: "Reviews",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserId_DueUtc",
                table: "Reviews",
                columns: new[] { "UserId", "DueUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserId_QuestionId",
                table: "Reviews",
                columns: new[] { "UserId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Skills_Code",
                table: "Skills",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Skills_DomainId",
                table: "Skills",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlans_UserId",
                table: "StudyPlans",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestItems_QuestionId",
                table: "TestItems",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_TestItems_TestAttemptId_ModuleIndex_Order",
                table: "TestItems",
                columns: new[] { "TestAttemptId", "ModuleIndex", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tests_UserId_StartedUtc",
                table: "Tests",
                columns: new[] { "UserId", "StartedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName",
                table: "Users",
                column: "UserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VocabProgress_UserId_VocabWordId",
                table: "VocabProgress",
                columns: new[] { "UserId", "VocabWordId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VocabProgress_VocabWordId",
                table: "VocabProgress",
                column: "VocabWordId");

            migrationBuilder.CreateIndex(
                name: "IX_VocabWords_Word",
                table: "VocabWords",
                column: "Word",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attempts");

            migrationBuilder.DropTable(
                name: "Marks");

            migrationBuilder.DropTable(
                name: "Masteries");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "StudyPlans");

            migrationBuilder.DropTable(
                name: "TestItems");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "VocabProgress");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "Tests");

            migrationBuilder.DropTable(
                name: "VocabWords");

            migrationBuilder.DropTable(
                name: "Skills");

            migrationBuilder.DropTable(
                name: "Domains");
        }
    }
}
