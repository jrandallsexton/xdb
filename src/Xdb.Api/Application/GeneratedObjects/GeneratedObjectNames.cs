using System;

namespace Xdb.Api.Application.GeneratedObjects;

/// <summary>
/// Names of the generated objects, formatted exactly as the original formatted them. Table and procedure names
/// use the asset type id without dashes; view names keep the dashes. Guids are lowercase ("D" format).
/// </summary>
public static class GeneratedObjectNames
{
    public static string Table(Guid assetTypeId, bool isInstance, bool isRaw) =>
        "gen" + assetTypeId.ToString("N") + (isInstance ? "_I" : "_D") + (isRaw ? "_Raw" : string.Empty);

    /// <summary>The MERGE procedure that upserts one asset into a gen table.</summary>
    public static string UpdateProcedure(string tableName) => "spr_" + tableName + "_Update";

    public static string ReportView(Guid reportId) => "vwRpt_" + reportId.ToString("D");

    /// <summary>Omits definition vs instance: when a role has both for a type, the last one generated wins (as in the original).</summary>
    public static string RoleView(Guid roleId, Guid assetTypeId, int permissionId) =>
        $"vwRole_{roleId:D}_{assetTypeId:D}_{permissionId}";
}
