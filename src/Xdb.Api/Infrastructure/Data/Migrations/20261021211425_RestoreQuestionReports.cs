using Microsoft.EntityFrameworkCore.Migrations;

using Xdb.Api.Infrastructure.Data.Scripts;

#nullable disable

namespace Xdb.Api.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Restores the three question report procedures (and the three functions they call) that the pre-baseline
    /// cleanup removed: stored report definitions name them in Reports.StoredProc, a reference made through data
    /// rather than code. Idempotent, so it is safe on databases that still have them.
    /// </summary>
    public partial class RestoreQuestionReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(SqlScripts.Read("RestoreQuestionReports/0001_objects.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP PROCEDURE IF EXISTS [dbo].[rpt_Questions_Unanswered];
                DROP PROCEDURE IF EXISTS [dbo].[rpt_Questions_Answered];
                DROP PROCEDURE IF EXISTS [dbo].[rpt_Questions];
                DROP FUNCTION IF EXISTS [dbo].[GetUnknownValue];
                DROP FUNCTION IF EXISTS [dbo].[F_AGE_DD_HH_MM];
                DROP FUNCTION IF EXISTS [dbo].[IsAGuid];
                """);
        }
    }
}
