using Microsoft.EntityFrameworkCore.Migrations;

using Xdb.Api.Infrastructure.Data.Scripts;

#nullable disable

namespace Xdb.Api.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Seeds a fresh install's first view ("Users", for the Admin role), so an administrator can enter the UI of an
    /// empty database. Skipped when any view exists: an existing database is unchanged.
    /// </summary>
    public partial class DefaultView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(SqlScripts.Read("DefaultView/0001_default_view.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Users may have changed or linked the view since; it is not removed.
        }
    }
}
