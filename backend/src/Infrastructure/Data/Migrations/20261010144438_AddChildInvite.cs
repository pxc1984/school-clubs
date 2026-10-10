using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolClubs.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChildInvite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "child_invite",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(43)", maxLength: 43, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    accepted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_child_invite", x => x.id);
                    table.ForeignKey(
                        name: "FK_child_invite_profile_accepted_by_user_id",
                        column: x => x.accepted_by_user_id,
                        principalTable: "profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_child_invite_profile_student_id",
                        column: x => x.student_id,
                        principalTable: "profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_child_invite_accepted_by_user_id",
                table: "child_invite",
                column: "accepted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_child_invite_student_id",
                table: "child_invite",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_child_invite_token",
                table: "child_invite",
                column: "token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "child_invite");
        }
    }
}
