using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MindNova.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAvailabilityBlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AvailabilityBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TherapistUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: true),
                    StartTime = table.Column<TimeSpan>(type: "time(7)", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time(7)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    SpecificDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailabilityBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvailabilityBlocks_AspNetUsers_TherapistUserId",
                        column: x => x.TherapistUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityBlocks_DayOfWeek",
                table: "AvailabilityBlocks",
                column: "DayOfWeek");

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityBlocks_TherapistUserId",
                table: "AvailabilityBlocks",
                column: "TherapistUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvailabilityBlocks");
        }
    }
}
