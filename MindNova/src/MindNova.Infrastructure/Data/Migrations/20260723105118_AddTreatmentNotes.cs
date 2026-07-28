using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MindNova.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTreatmentNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TreatmentNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TherapistUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PresentingIssue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Interventions = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Homework = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProgressRating = table.Column<int>(type: "int", nullable: false),
                    FreeText = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreatmentNotes_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentNotes_SessionId",
                table: "TreatmentNotes",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentNotes_TherapistUserId",
                table: "TreatmentNotes",
                column: "TherapistUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TreatmentNotes");
        }
    }
}
