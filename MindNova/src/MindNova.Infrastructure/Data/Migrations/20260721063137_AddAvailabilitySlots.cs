using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MindNova.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAvailabilitySlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AvailabilitySlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TherapistProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: true),
                    SpecificDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailabilitySlots", x => x.Id);
                    table.CheckConstraint("CK_AvailabilitySlot_DayOrDate", "(IsRecurring = 1 AND DayOfWeek IS NOT NULL AND SpecificDate IS NULL) OR (IsRecurring = 0 AND DayOfWeek IS NULL AND SpecificDate IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_AvailabilitySlots_TherapistProfiles_TherapistProfileId",
                        column: x => x.TherapistProfileId,
                        principalTable: "TherapistProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilitySlots_TherapistProfileId",
                table: "AvailabilitySlots",
                column: "TherapistProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvailabilitySlots");
        }
    }
}
