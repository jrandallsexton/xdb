using Microsoft.EntityFrameworkCore.Migrations;

using Xdb.Api.Infrastructure.Data.Scripts;

#nullable disable

namespace Xdb.Api.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Seeds the system rows a fresh install needs and the application hard-codes: the User asset type, the system
    /// properties, the Admin and DB Admin roles, and the built-in system administrator (member and Admin
    /// membership; no User asset: a fresh install holds no assets). Idempotent: an existing database already has every
    /// row and is left unchanged.
    /// </summary>
    public partial class SystemSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(SqlScripts.Read("SystemSeed/0001_system_rows.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The rows may predate this migration (every existing database has them), so they are not removed.
        }
    }
}
