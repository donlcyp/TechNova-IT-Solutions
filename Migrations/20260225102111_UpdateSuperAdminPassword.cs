using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechNova_IT_Solutions.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSuperAdminPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Generate BCrypt hash for "Admin@123"
            var newPasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");

            migrationBuilder.Sql($@"
                UPDATE Users 
                SET password = '{newPasswordHash}'
                WHERE email = 'superadmin@technova.com';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to old password hash for "Password@123"
            var oldPasswordHash = BCrypt.Net.BCrypt.HashPassword("Password@123");

            migrationBuilder.Sql($@"
                UPDATE Users 
                SET password = '{oldPasswordHash}'
                WHERE email = 'superadmin@technova.com';
            ");
        }
    }
}
