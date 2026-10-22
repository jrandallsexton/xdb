using System;
using System.Globalization;
using System.Text;

using Xdb.Api.Application.Roles.Legacy;

namespace Xdb.Api.Application.Roles;

/// <summary>
/// The inline batch RoleDal.Save built after spr_Role_Save, transcribed with its statement text, order and conditions,
/// including its bugs, which are behaviour:
/// <list type="bullet">
/// <item>an existing dirty asset type formats a two-item string with one argument (FormatException, nothing runs);</item>
/// <item>removing a report writes "SET [Deleted] = GetDate" (a compile error that rejects the whole batch);</item>
/// <item>an inherited asset type's permissions produce a DELETE missing its closing parenthesis (same);</item>
/// <item>a new inherited asset type without InheritFrom, and null lists, throw while building;</item>
/// <item>values are not escaped; members are processed even when not dirty.</item>
/// </list>
/// Guids are lowercase "D", booleans 1/0, null strings empty, report dates the original's en-US text.
/// </summary>
public static class RoleSaveBatch
{
    /// <summary>DateTime.ToString() under the original's en-US culture (no ICU narrow space before AM/PM).</summary>
    private const string EnUsGeneralPattern = "M/d/yyyy h:mm:ss tt";

    public static string Build(RoleContract role, Guid callerId)
    {
        var sb = new StringBuilder();
        var format = "INSERT INTO [dbo].[RoleMembers] ([Id], [RoleId], [MemberId], [Created], [CreatedBy]) VALUES ('{0}', '{1}', '{2}', GetDate(), '{3}')";
        foreach (var member in role.Members ?? throw new NullReferenceException("Members is null."))
        {
            if (member.IsDeleted)
                AppendLine(sb, "UPDATE [RoleMembers] SET [Deleted] = GetDate() WHERE [Id] = '{0}'", member.Id);
            else if (member.IsNew)
                AppendLine(sb, format, member.Id, role.Id, member.MemberId, callerId);
        }

        foreach (var report in role.Reports ?? throw new NullReferenceException("Reports is null."))
        {
            if (!report.IsDirty)
                continue;
            if (report.IsDeleted)
                AppendLine(sb, "UPDATE [RoleReports] SET [Deleted] = GetDate, [DeletedBy] = '{0}' WHERE [Id] = '{1}'", callerId, report.Id);
            else if (report.IsNew)
                AppendLine(sb, "INSERT INTO [RoleReports] ([Id], [RoleId], [ReportId], [Created], [CreatedBy]) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')",
                    report.Id, role.Id, report.ReportId, report.Created.ToString(EnUsGeneralPattern, CultureInfo.InvariantCulture), report.CreatedBy);
        }

        const string format2 = "INSERT INTO [dbo].[RoleAssetTypes] ([Id], [RoleId], [AssetTypeId], [IsInstance], [IsInherited]) VALUES ('{0}', '{1}', '{2}', {3}, {4})";
        const string format3 = "INSERT INTO [dbo].[RoleAssetTypes] ([Id], [RoleId], [AssetTypeId], [IsInstance], [IsInherited], [InheritFrom]) VALUES ('{0}', '{1}', '{2}', {3}, {4}, '{5}')";
        const string format4 = "UPDATE [dbo].[RoleAssetTypes] SET [IsInherited] = {0} WHERE [Id] = {1}";
        const string format5 = "INSERT INTO [dbo].[RolePermissions] ([Id], [RoleAssetTypesId], [PermissionId], [IsIncludesFilter], [FilterLogic]) VALUES ('{0}', '{1}', '{2}', {3}, '{4}')";
        const string format6 = "UPDATE [dbo].[RolePermissions] SET [IsIncludesFilter] = {0}, [FilterLogic] = '{1}' WHERE [Id] = '{2}'";
        const string format7 = "INSERT INTO [RoleFilters] ([Id], [RolePermissionsId], [Order], [PropertyId], [OperatorId], [Value]) VALUES ('{0}', '{1}', {2}, '{3}', {4}, '{5}')";
        const string format8 = "INSERT INTO [RoleFilters] ([Id], [RolePermissionsId], [Order], [PropertyId], [OperatorId], [Value]) VALUES ('{0}', '{1}', {2}, '{3}', {4}, NULL)";

        foreach (var assetType in role.AssetTypes ?? throw new NullReferenceException("AssetTypes is null."))
        {
            if (!assetType.IsDirty)
                continue;
            if (assetType.IsDeleted)
            {
                AppendLine(sb, "DELETE FROM [RoleAssetTypes] WHERE [Id] = '{0}'", assetType.Id);
            }
            else if (assetType.IsNew)
            {
                if (assetType.IsInherited)
                    AppendLine(sb, format3, assetType.Id, role.Id, assetType.AssetTypeId, Convert.ToInt32(assetType.IsInstance), 1,
                        assetType.InheritFrom ?? throw new InvalidOperationException("Nullable object must have a value."));
                else
                    AppendLine(sb, format2, assetType.Id, role.Id, assetType.AssetTypeId, Convert.ToInt32(assetType.IsInstance), 0);
            }
            else
            {
                AppendLine(sb, format4, Convert.ToInt32(assetType.IsInherited));
            }

            var permissions = assetType.Permissions ?? throw new NullReferenceException("Permissions is null.");
            if (assetType.IsInherited)
            {
                foreach (var permission in permissions)
                {
                    AppendLine(sb, "DELETE FROM [RoleFilters] WHERE [Id] IN (SELECT [Id] FROM [RoleFilters] WHERE [RolePermissionsId] = '{0}'", permission.Id);
                    AppendLine(sb, "DELETE FROM [RolePermissions] WHERE [Id] = '{0}'", permission.Id);
                }
                continue;
            }

            foreach (var permission in permissions)
            {
                if (!permission.IsDirty)
                    continue;
                if (permission.IsDeleted)
                    AppendLine(sb, "DELETE FROM [RolePermissions] WHERE [Id] = '{0}'", permission.Id);
                else if (permission.IsNew)
                    AppendLine(sb, format5, permission.Id, assetType.Id, permission.PermissionId, Convert.ToInt32(permission.IsIncludesPermission), permission.FilterExpression);
                else
                    AppendLine(sb, format6, Convert.ToInt32(permission.IsIncludesPermission), permission.FilterExpression, permission.Id);
                AppendLine(sb, "DELETE FROM [RoleFilters] WHERE [RolePermissionsId] = '{0}'", permission.Id);
                foreach (var filter in permission.Filters ?? throw new NullReferenceException("Filters is null."))
                {
                    if (string.IsNullOrEmpty(filter.Value))
                        AppendLine(sb, format8, filter.Id, permission.Id, filter.Order, filter.PropertyId, filter.OperatorId);
                    else
                        AppendLine(sb, format7, filter.Id, permission.Id, filter.Order, filter.PropertyId, filter.OperatorId, filter.Value);
                }
            }
        }
        return sb.ToString();
    }

    /// <summary>StringBuilder.AppendFormat(...).AppendLine() as the original wrote it (current-culture formatting of ints).</summary>
    private static void AppendLine(StringBuilder sb, string format, params object?[] args) =>
        sb.AppendFormat(CultureInfo.InvariantCulture, format, args).Append("\r\n");
}
