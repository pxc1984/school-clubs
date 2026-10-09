using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolClubs.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUsername : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "username",
                table: "profile",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_profile_username",
                table: "profile",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_profile_username",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "username",
                table: "profile");
        }
    }
}
