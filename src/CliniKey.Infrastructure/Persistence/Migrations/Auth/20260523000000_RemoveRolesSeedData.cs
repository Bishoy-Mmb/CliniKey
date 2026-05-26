using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CliniKey.Infrastructure.Persistence.Migrations.Auth
{
    /// <inheritdoc />
    public partial class RemoveRolesSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally left blank.
            // Role rows are application data and are now managed by RoleManager at startup,
            // not by EF Core model seed data. Deleting them here could remove user role assignments.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally left blank. Startup seeding remains responsible for role rows.
        }
    }
}
