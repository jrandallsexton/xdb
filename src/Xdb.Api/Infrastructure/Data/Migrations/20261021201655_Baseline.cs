using System;

using Microsoft.EntityFrameworkCore.Migrations;

using Xdb.Api.Infrastructure.Data.Scripts;

#nullable disable

namespace Xdb.Api.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Baseline: the original system's static schema, reproduced exactly from embedded SQL rather than from the
    /// EF model. A model-generated migration would lose each module's ANSI_NULLS / QUOTED_IDENTIFIER settings
    /// (most procedures depend on ANSI_NULLS OFF), invent keys for heaps and rename constraints.
    /// The model snapshot (Designer) is the scaffolded model, so later migrations diff normally.
    /// Generated objects (per-type read tables, their MERGE procedures, report and row-security views) are not
    /// part of any migration; the application rebuilds them from metadata.
    /// An existing database is adopted (recorded as applied without running this); see DatabaseRegistration.
    /// </summary>
    public partial class Baseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ALTER DATABASE cannot run inside a transaction.
            migrationBuilder.Sql(
                $"ALTER DATABASE CURRENT SET COMPATIBILITY_LEVEL = {DatabaseRegistration.CompatibilityLevel};",
                suppressTransaction: true);

            migrationBuilder.Sql(SqlScripts.Read("Baseline/0001_tables.sql"));
            migrationBuilder.Sql(SqlScripts.Read("Baseline/0002_foreign_keys.sql"));
            migrationBuilder.Sql(SqlScripts.Read("Baseline/0003_functions.sql"));
            migrationBuilder.Sql(SqlScripts.Read("Baseline/0004_procedures.sql"));
            migrationBuilder.Sql(SqlScripts.Read("Baseline/0005_seed.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("The baseline cannot be rolled back; drop the database instead.");
        }
    }
}
