using System;
using System.Collections.Generic;
using System.Text;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Infrastructure.Data.Procedures;

namespace Xdb.Api.Application.GeneratedObjects.GenTables;

/// <summary>Everything the gen-table SQL is built from, already loaded.</summary>
/// <param name="PropertyIds">The type's property set in column order (see <see cref="GenPropertySet"/>).</param>
/// <param name="Properties">Every id in <paramref name="PropertyIds"/>, as spr_Properties_GetByListOfPropertyIds returns it.</param>
/// <param name="Relations">The type's live relations for those properties, by property id.</param>
/// <param name="LoadAssetTypeIds">Asset types whose assets the initial SELECT INTO loads.</param>
/// <param name="DefinitionLabel">ReportingLabel(type, definitions): alias of the InstanceOf columns.</param>
/// <param name="ViewImageUrl">Baked into Image columns as a string literal.</param>
public sealed record GenTableInput(
    Guid AssetTypeId,
    bool IsInstance,
    IReadOnlyList<Guid> PropertyIds,
    IReadOnlyDictionary<Guid, PropertyMetaRow> Properties,
    IReadOnlyDictionary<Guid, PropertyRelationRow> Relations,
    IReadOnlyList<Guid> LoadAssetTypeIds,
    string DefinitionLabel,
    string ViewImageUrl);

/// <summary>The decoded and raw SELECT ... INTO statements for one gen table pair.</summary>
public sealed record GenTableSelects(string Decoded, string Raw);

/// <summary>
/// The gen-table SQL, reproduced character for character from the original generator (GetSqlForGenTable and
/// CreateGeneratedObjects), including its quirks: unescaped names, CRLF line endings, a bare LF after the DROP,
/// the never-joined [pv{n}] alias for single-valued Dependency properties. Only the branches the gen-table
/// generator reaches are ported: no sub-properties, no custom report fields, no explicit asset list, no ORDER BY.
/// </summary>
public static class GenTableSql
{
    /// <summary>The original appended lines with Environment.NewLine on Windows.</summary>
    public const string NewLine = "\r\n";

    /// <summary>Upper-case literals in the raw header, exactly as the original wrote them.</summary>
    private const string RawAssetNameAlias = "8D145EE0-54D2-47DB-8785-5AE086C20DDE";
    private const string RawAssetTypeAlias = "48014265-A355-45EC-9878-94C041ABD927";

    public static GenTableSelects BuildSelects(GenTableInput input)
    {
        var decoded = new StringBuilder();
        var raw = new StringBuilder();
        var decodedColumns = new StringBuilder();
        var rawColumns = new StringBuilder();
        var where = new StringBuilder();

        decoded.Append("SELECT DISTINCT").Append(NewLine);
        raw.Append("SELECT DISTINCT").Append(NewLine);
        decoded.Append("\t[A].[Id] AS [AssetId]").Append(NewLine);
        decoded.Append("\t,[A].[Name] AS [Name]").Append(NewLine);
        decoded.Append("\t,[A].[DisplayValue] AS [DisplayValue]").Append(NewLine);
        decoded.Append("\t,dbo.GetAssetType(A.[Id]) AS [Asset Type]").Append(NewLine);
        raw.Append("\t[A].[Id] AS [AssetId]").Append(NewLine);
        raw.Append("\t,[A].[Name] AS [").Append(RawAssetNameAlias).Append(']').Append(NewLine);
        raw.Append("\t,[A].[DisplayValue] AS [DisplayValue]").Append(NewLine);
        raw.Append("\t,[A].[AssetTypeId] AS [").Append(RawAssetTypeAlias).Append(']').Append(NewLine);

        var index = 0;
        foreach (var propertyId in input.PropertyIds)
        {
            var property = input.Properties[propertyId];
            var assetIdColumn = "Id";
            input.Relations.TryGetValue(propertyId, out var relation);

            if (!property.IsSystem)
            {
                if (relation is null)
                    continue;
                if (input.IsInstance && !relation.IsInstance)
                    assetIdColumn = "InstanceOfId";
            }

            var name = string.IsNullOrEmpty(property.DisplayValue) ? property.Name : property.DisplayValue;
            var id = propertyId.ToString("D");

            if (!property.IsSystem && relation!.IsInheritedValue)
            {
                Line(decodedColumns, $"\t,[dbo].[GetPropertyValueLookup]([dbo].[GetAssetParentIdByChildId]([A].[Id]), '{id}') AS [{name!.Trim()}]");
                Line(rawColumns, $"\t,[dbo].[GetPropertyValueLookup]([dbo].[GetAssetParentIdByChildId]([A].[Id]), '{id}') AS [{id}]");
                continue; // as in the original, an inherited value does not advance the [pv{n}] index
            }

            var systemType = ToSystemType(property.SystemTypeId);
            switch (ToDataType(property.DataTypeId))
            {
                case DataType.Asset:
                    if (property.IsSystem)
                    {
                        if (systemType == SystemType.InstanceOf)
                        {
                            Line(decodedColumns, $"\t,dbo.GetAssetParentNameByInstanceId([A].[Id]) AS [{input.DefinitionLabel}]");
                            Line(rawColumns, $"\t,[A].[InstanceOfId] AS [{SystemPropertyIds.InstanceOf:D}]");
                        }
                        else
                        {
                            Line(decodedColumns, $"\t,dbo.GetPropertyValueLookup([A].[{assetIdColumn}], '{id}') AS [{name}]");
                            Line(rawColumns, $"\t,dbo.PropertyValue([A].[{assetIdColumn}], '{id}') AS [{id}]");
                        }
                    }
                    else if (property.AllowMultiValue)
                    {
                        Line(decodedColumns, $"\t,dbo.PropertyValueMulti_Asset([A].[{assetIdColumn}], '{id}') AS [{name}]");
                        Line(rawColumns, $"\t,dbo.PropertyValue([A].[{assetIdColumn}], '{id}') AS [{id}]");
                    }
                    else
                    {
                        Line(decodedColumns, $"\t,dbo.PropertyValue_Asset([A].[{assetIdColumn}], '{id}') AS [{name}]");
                        Line(rawColumns, $"\t,dbo.PropertyValue([A].[{assetIdColumn}], '{id}') AS [{id}]");
                    }
                    break;

                case DataType.Currency:
                    Line(decodedColumns, $"\t,dbo.PropertyValue_Currency([A].[{assetIdColumn}], '{id}') AS [{name}]");
                    Line(rawColumns, $"\t,dbo.PropertyValue([A].[{assetIdColumn}], '{id}') AS [{id}]");
                    break;

                case DataType.Date:
                case DataType.DateTime:
                    if (property.IsSystem)
                    {
                        switch (systemType)
                        {
                            case SystemType.NotApplicable:
                                Line(decodedColumns, $"\t,dbo.PropertyValue([A].[{assetIdColumn}], '{id}') AS [{name}]");
                                Line(rawColumns, $"\t,dbo.PropertyValue([A].[{assetIdColumn}], '{id}') AS [{id}]");
                                break;
                            case SystemType.Created:
                                Line(decodedColumns, "\t,[A].[Created] AS [Created]");
                                Line(rawColumns, $"\t,[A].[Created] AS [{id}]");
                                break;
                            case SystemType.Deleted:
                                Line(decodedColumns, "\t,[A].[Deleted] AS [Deleted]");
                                Line(rawColumns, $"\t,[A].[Deleted] AS [{id}]");
                                break;
                            case SystemType.LastModified:
                                Line(decodedColumns, "\t,[A].[LastModified] AS [Last Modified]");
                                Line(rawColumns, $"\t,[A].[LastModified] AS [{id}]");
                                break;
                        }
                    }
                    else
                    {
                        Line(decodedColumns, $"\t,dbo.PropertyValue([A].[{assetIdColumn}], '{id}') AS [{name}]");
                        Line(rawColumns, $"\t,dbo.PropertyValue([A].[{assetIdColumn}], '{id}') AS [{id}]");
                    }
                    break;

                case DataType.Dependency:
                    if (property.AllowMultiValue)
                    {
                        Line(decodedColumns, $"\t,dbo.PropertyValueMulti_Asset([A].[{assetIdColumn}], '{id}') AS [{name}]");
                        Line(rawColumns, $"\t,dbo.PropertyValue([A].[{assetIdColumn}], '{id}') AS [{id}]");
                    }
                    else
                    {
                        // The original's bug, kept: [pv{n}] is never joined, so creating the table fails.
                        Line(decodedColumns, $"\t,dbo.GetAssetValueById([pv{index}].[Value]) AS [{name}]");
                        Line(rawColumns, $"\t,dbo.PropertyValue([A].[{assetIdColumn}], '{id}') AS [{id}]");
                    }
                    break;

                case DataType.Document:
                    Line(decodedColumns, $"\t,dbo.GetPropertyValueLookup([A].[{assetIdColumn}], '{id}') AS [{name}]");
                    break;

                case DataType.Image:
                    Line(decodedColumns, $"\t,'{input.ViewImageUrl}' + dbo.GetPropertyValueLookup([A].[Id], '{id}') AS [{name}]");
                    break;

                case DataType.PickList:
                    Line(decodedColumns, $"\t,dbo.PropertyValue_PickList([A].[{assetIdColumn}], '{id}') AS [{name}]");
                    Line(rawColumns, $"\t,dbo.PropertyValue([A].[{assetIdColumn}], '{id}') AS [{id}]");
                    break;

                case DataType.RelationChildParent:
                case DataType.RelationParentChild:
                    Line(decodedColumns, $"\t,dbo.GetAssetParentNameByChildId([A].[{assetIdColumn}]) AS [{name}]");
                    Line(rawColumns, $"\t,dbo.PropertyValue([A].[{assetIdColumn}], '{id}') AS [{id}]");
                    break;

                case DataType.String:
                    if (property.IsSystem)
                    {
                        switch (systemType)
                        {
                            case SystemType.Description:
                                Line(decodedColumns, "\t,A.[Description] AS [Description]");
                                Line(rawColumns, $"\t,(A.[Description]) AS [{id}]");
                                break;
                            case SystemType.InstanceOfDescription:
                                Line(decodedColumns, $"\t,dbo.GetAssetParentDescByInstanceId([A].[Id]) AS [{input.DefinitionLabel} Description]");
                                break;
                            case SystemType.AssetName:
                            case SystemType.AssetType:
                                break; // covered by the fixed header
                            default:
                                Line(decodedColumns, $"\t,dbo.PropertyValue([A].[{assetIdColumn}], '{id}') AS [{name}]");
                                Line(rawColumns, $"\t,dbo.PropertyValue([A].[{assetIdColumn}], '{id}') AS [{id}]");
                                break;
                        }
                    }
                    else
                    {
                        Line(decodedColumns, $"\t,dbo.PropertyValue([A].[{assetIdColumn}], '{id}') AS [{name}]");
                        Line(rawColumns, $"\t,dbo.PropertyValue([A].[{assetIdColumn}], '{id}') AS [{id}]");
                    }
                    break;

                case DataType.Url:
                    Line(decodedColumns, $"\t,dbo.GetPropertyValueLookup([A].[Id], '{id}') AS [{name}]");
                    break;

                case DataType.User:
                    if (property.IsSystem)
                    {
                        switch (systemType)
                        {
                            case SystemType.CreatedBy:
                                Line(decodedColumns, "\t,dbo.GetMemberValueById([A].[CreatedBy]) AS [Created By]");
                                Line(rawColumns, $"\t,[A].[CreatedBy] AS [{id}]");
                                break;
                            case SystemType.LastModifiedBy:
                                Line(decodedColumns, "\t,dbo.GetMemberValueById([A].[LastModifiedBy]) AS [Last Modified By]");
                                Line(rawColumns, $"\t,[A].[LastModifiedBy] AS [{id}]");
                                break;
                            case SystemType.DeletedBy:
                                Line(decodedColumns, $"\t,dbo.GetMemberValueById(dbo.GetSystemValueLookup([A].[Id], '{id}')) AS [{name}]");
                                break;
                        }
                    }
                    else
                    {
                        Line(decodedColumns, $"\t,dbo.PropertyValue_Member([A].[Id], '{id}') AS [{name}]");
                        Line(rawColumns, $"\t,dbo.PropertyValue([A].[Id], '{id}') AS [{id}]");
                    }
                    break;

                default:
                    // Float, Int, Memo, RelationOther, IPv4, IPv6, Html, the System_* types and unknown values.
                    Line(decodedColumns, $"\t,dbo.PropertyValue([A].[{assetIdColumn}], '{id}') AS [{name}]");
                    Line(rawColumns, $"\t,dbo.PropertyValue([A].[{assetIdColumn}], '{id}') AS [{id}]");
                    break;
            }

            index++;
        }

        if (input.LoadAssetTypeIds.Count == 1)
        {
            where.Append($" ([A].[AssetTypeId] = '{input.LoadAssetTypeIds[0]:D}')");
        }
        else
        {
            for (var i = 0; i < input.LoadAssetTypeIds.Count; i++)
            {
                if (i == 0)
                    where.Append(" ([A].[AssetTypeId] IN (");
                where.Append(i == input.LoadAssetTypeIds.Count - 1 ? $" '{input.LoadAssetTypeIds[i]:D}'))" : $" '{input.LoadAssetTypeIds[i]:D}',");
            }
        }

        Line(where, input.IsInstance ? " AND ([A].[IsInstance] = 1)" : " AND ([A].[IsInstance] = 0)");
        Line(where, "AND (A.[Deleted] IS NULL)");

        decoded.Append(decodedColumns);
        raw.Append(rawColumns);
        Line(decoded, $"INTO [{GeneratedObjectNames.Table(input.AssetTypeId, input.IsInstance, isRaw: false)}]");
        Line(raw, $"INTO [{GeneratedObjectNames.Table(input.AssetTypeId, input.IsInstance, isRaw: true)}]");
        foreach (var builder in new[] { decoded, raw })
        {
            Line(builder, "FROM [Assets] A WITH (NoLock)");
            Line(builder, "WHERE");
            builder.Append(where);
        }

        return new GenTableSelects(decoded.ToString(), raw.ToString());
    }

    /// <summary>The table's creation batch: drop if present (followed by a bare LF, as in the original), then SELECT ... INTO.</summary>
    public static string CreateTable(string tableName, string select) =>
        $"IF OBJECT_ID('dbo.[{tableName}]', 'U') IS NOT NULL DROP TABLE dbo.[{tableName}]\n" + select;

    public static string AddPrimaryKey(string tableName) => $"ALTER TABLE [dbo].[{tableName}] ADD PRIMARY KEY (AssetId)";

    public static string DropProcedure(string procedureName) => $"IF OBJECT_ID('{procedureName}') IS NOT NULL DROP PROC [dbo].[{procedureName}]";

    /// <summary>
    /// The MERGE procedure that upserts one asset. Its source query is the table's SELECT without the INTO line,
    /// cut at WHERE; its column lists come from the created table, in column order.
    /// </summary>
    public static string CreateMergeProcedure(string tableName, string select, IReadOnlyList<string> columns)
    {
        var procedureName = GeneratedObjectNames.UpdateProcedure(tableName);
        var sp = new StringBuilder();
        Line(sp, $"CREATE PROCEDURE [dbo].[{procedureName}]");
        Line(sp, "\t@AssetId\tuniqueidentifier");
        Line(sp, "AS");
        Line(sp, string.Empty);
        Line(sp, $"MERGE INTO dbo.[{tableName}]");
        Line(sp, "\tUSING (");

        foreach (var line in select.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith("IF OBJECT_ID", StringComparison.Ordinal) || line.StartsWith("INTO", StringComparison.Ordinal))
                continue;
            if (line.StartsWith("WHERE", StringComparison.Ordinal))
                break;
            Line(sp, "\t\t" + line);
        }

        Line(sp, "WHERE ([A].[Id] = @AssetId)");
        Line(sp, "\t) AS [Source]");
        Line(sp, $"\tON dbo.[{tableName}].[AssetId] = [Source].[AssetId]");
        Line(sp, "WHEN MATCHED THEN");
        Line(sp, "\tUPDATE");
        Line(sp, "\t\tSET");

        // The original's comma logic: it assumes AssetId appears exactly once.
        var i = 0;
        var last = columns.Count - 2;
        foreach (var column in columns)
        {
            if (column == "AssetId")
                continue;
            Line(sp, $"\t\t\tdbo.[{tableName}].[{column}] = [Source].[{column}]" + (i == last ? string.Empty : ","));
            i++;
        }

        Line(sp, "WHEN NOT MATCHED THEN");
        Line(sp, "\tINSERT (");
        last = columns.Count - 1;
        for (i = 0; i < columns.Count; i++)
            Line(sp, $"\t\t\t[{columns[i]}]" + (i == last ? string.Empty : ","));
        Line(sp, ")");
        Line(sp, "VALUES (");
        for (i = 0; i < columns.Count; i++)
            Line(sp, $"\t\t\t[Source].[{columns[i]}]" + (i == last ? string.Empty : ","));
        Line(sp, ");");

        return sp.ToString();
    }

    private static void Line(StringBuilder builder, string text) => builder.Append(text).Append(NewLine);

    /// <summary>The original's Geo type (25) is not in the DataTypes lookup but shared the String branch.</summary>
    private const byte GeoDataTypeId = 25;

    /// <summary>Unknown data type ids take the default branch, as the original's Undefined did.</summary>
    private static DataType? ToDataType(byte? id) => id switch
    {
        GeoDataTypeId => DataType.String,
        { } value when Enum.IsDefined(typeof(DataType), value) => (DataType)value,
        _ => null
    };

    /// <summary>A NULL system type is NotApplicable.</summary>
    private static SystemType ToSystemType(int? id) =>
        id is { } value && Enum.IsDefined(typeof(SystemType), value) ? (SystemType)value : SystemType.NotApplicable;
}
