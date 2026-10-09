using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolClubs.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lesson_attendance",
                columns: table => new
                {
                    lesson_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lesson_attendance", x => new { x.lesson_id, x.student_id });
                    table.ForeignKey(
                        name: "FK_lesson_attendance_lesson_gradebook_lesson_id",
                        column: x => x.lesson_id,
                        principalTable: "lesson_gradebook",
                        principalColumn: "lesson_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lesson_attendance_profile_student_id",
                        column: x => x.student_id,
                        principalTable: "profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lesson_attendance_student_id",
                table: "lesson_attendance",
                column: "student_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lesson_attendance");
        }
    }
}
