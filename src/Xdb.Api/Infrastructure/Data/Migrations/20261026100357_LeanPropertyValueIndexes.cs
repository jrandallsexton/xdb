using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xdb.Api.Infrastructure.Data.Migrations
{
    /// <summary>
    /// PropertyValues carried seven indexes on a heap: two tuning-advisor indexes copied every column (indexes were 5.5x
    /// the data, and each value row was written eight times). Keeps the key, IX_AssetIds, one lookup index covering
    /// dbo.PropertyValue's live, approved filter (it replaces the (PropertyId, AssetId) indexes, which it leads with) and
    /// a narrow SubmittalGroupId index in place of the covering one. Down restores the originals as created.
    /// </summary>
    public partial class LeanPropertyValueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "_dta_index_PropertyValues_5_836914053__K2_K3_K9_5",
                table: "PropertyValues");

            migrationBuilder.DropIndex(
                name: "_dta_index_PropertyValues_7_1989582126__K1_2_3_4_5_6_7_8_9_10_11_12_13_14",
                table: "PropertyValues");

            migrationBuilder.DropIndex(
                name: "_dta_index_PropertyValues_7_1989582126__K9_K4_1_2_3_5_6_7_8_10_11_12_13_14",
                table: "PropertyValues");

            migrationBuilder.DropIndex(
                name: "IX_PropertyIds",
                table: "PropertyValues");

            migrationBuilder.DropIndex(
                name: "IX_PropVals",
                table: "PropertyValues");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyValues_Lookup",
                table: "PropertyValues",
                columns: new[] { "PropertyId", "AssetId", "Deleted" })
                .Annotation("SqlServer:Include", new[] { "Approved", "Value" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyValues_SubmittalGroupId",
                table: "PropertyValues",
                column: "SubmittalGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PropertyValues_Lookup",
                table: "PropertyValues");

            migrationBuilder.DropIndex(
                name: "IX_PropertyValues_SubmittalGroupId",
                table: "PropertyValues");

            migrationBuilder.CreateIndex(
                name: "_dta_index_PropertyValues_5_836914053__K2_K3_K9_5",
                table: "PropertyValues",
                columns: new[] { "PropertyId", "AssetId", "Deleted" })
                .Annotation("SqlServer:Include", new[] { "Value" });

            migrationBuilder.CreateIndex(
                name: "_dta_index_PropertyValues_7_1989582126__K1_2_3_4_5_6_7_8_9_10_11_12_13_14",
                table: "PropertyValues",
                column: "Id")
                .Annotation("SqlServer:Include", new[] { "Approved", "ApprovedBy", "AssetId", "Created", "CreatedBy", "Deleted", "DeletedBy", "Order",
                    "PropertyId", "Rejected", "RejectedBy", "SubmittalGroupId", "Value" });

            migrationBuilder.CreateIndex(
                name: "_dta_index_PropertyValues_7_1989582126__K9_K4_1_2_3_5_6_7_8_10_11_12_13_14",
                table: "PropertyValues",
                columns: new[] { "Deleted", "SubmittalGroupId" })
                .Annotation("SqlServer:Include", new[] { "Approved", "ApprovedBy", "AssetId", "Created", "CreatedBy", "DeletedBy", "Id", "Order",
                    "PropertyId", "Rejected", "RejectedBy", "Value" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyIds",
                table: "PropertyValues",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_PropVals",
                table: "PropertyValues",
                columns: new[] { "PropertyId", "AssetId" });
        }
    }
}
