using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechNova_IT_Solutions.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAdminPasswordHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "userID",
                keyValue: 1,
                column: "password",
                value: "$2a$11$kzRScf92mLmEjZRJTh3BRub/Li1F07G3TA5vBdZXYQ7tM1C6Lm65i");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "userID",
                keyValue: 1,
                column: "password",
                value: "$2a$11$8Z5UwVQf4ZPqLQJLqxJQgeZ8LpKhxGxH5yJqZqZx3wJ0eqJZLZMZK");
        }
    }
}
