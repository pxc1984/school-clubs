using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolClubs.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAvatar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "avatar_id",
                table: "profile",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_profile_avatar_id",
                table: "profile",
                column: "avatar_id");

            migrationBuilder.AddForeignKey(
                name: "FK_profile_data_entry_avatar_id",
                table: "profile",
                column: "avatar_id",
                principalTable: "data_entry",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_profile_data_entry_avatar_id",
                table: "profile");

            migrationBuilder.DropIndex(
                name: "IX_profile_avatar_id",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "avatar_id",
                table: "profile");
        }
    }
}
