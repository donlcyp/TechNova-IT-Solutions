using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TechNova_IT_Solutions.Data;

#nullable disable

namespace TechNova_IT_Solutions.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260222110000_AddExchangeRateAndGeocoding")]
    public partial class AddExchangeRateAndGeocoding : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally no-op.
            // These columns were introduced/managed by prior migrations in this branch.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally no-op.
        }
    }
}
