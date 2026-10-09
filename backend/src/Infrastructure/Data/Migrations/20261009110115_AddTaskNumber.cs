using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolClubs.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "number",
                table: "task",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            // Прежде номером была позиция в занятии.
            migrationBuilder.Sql("UPDATE task SET number = \"order\"::text;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "number",
                table: "task");
        }
    }
}
