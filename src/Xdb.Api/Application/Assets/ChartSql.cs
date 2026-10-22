using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects;
using Xdb.Api.Application.GeneratedObjects.Views;
using Xdb.Api.Infrastructure.Data.Procedures;

namespace Xdb.Api.Application.Assets;

/// <summary>
/// The SQL behind AssetFilterClient_Get, transcribed from the original's SqlDatabaseLayer: the 11-argument
/// GetSqlString with no report properties and an INTO table (SELECT A.* INTO [#..]), and the per-property
/// enabled/disabled value query over those temp tables. Unlike the view builder, an Asset Name filter compares the
/// column named after the asset type ([A].[&lt;type name&gt;]) and InstanceOf filters emit nothing.
/// </summary>
public static class ChartSql
{
    public const string NewLine = "\r\n";

    private const int EqualToOperatorId = 0;
    private const int NotEqualOperatorId = 5;
    private const int LikeOperatorId = 6;
    private const string EmptyGuid = "00000000-0000-0000-0000-000000000000";

    /// <summary>EnumerationOps.EFilterOperatorToSql.</summary>
    public static string OperatorSql(int operatorId) => operatorId switch
    {
        0 => "=",
        1 => "<",
        2 => ">",
        3 => "<=",
        4 => ">=",
        5 => "!=",
        6 => "LIKE",
        _ => throw new InvalidOperationException("Invalid value for EFilterOperator")
    };

    /// <param name="input">Loaded for the filters only (no columns); its Label is unused.</param>
    /// <param name="assetTypeName">AssetTypeLayer.Name(type): the column an Asset Name filter compares.</param>
    public static string BuildInto(ViewInput input, string assetTypeName, string intoTableName)
    {
        var instance = input.IsInstance;
        var table = GeneratedObjectNames.Table(input.AssetTypeId, instance, isRaw: false);
        var rawTable = GeneratedObjectNames.Table(input.AssetTypeId, instance, isRaw: true);
        var otherRawTable = GeneratedObjectNames.Table(input.AssetTypeId, !instance, isRaw: true);
        var instanceOf = SystemPropertyIds.InstanceOf.ToString("D");

        var select = new StringBuilder();
        var from = new StringBuilder();
        var where = new StringBuilder();
        var joined = new List<string>();

        Line(select, "SELECT");
        Line(from, $"FROM [dbo].[{table}] AS [A]");
        Line(from, $"INNER JOIN [dbo].[{rawTable}] AS [B] ON [B].[AssetId] = [A].[AssetId]");
        Line(select, $"A.* INTO [#{intoTableName}]");

        void JoinOtherRaw()
        {
            if (joined.Contains(otherRawTable))
                return;
            joined.Add(otherRawTable);
            Line(from, $"INNER JOIN [dbo].[{otherRawTable}] ON [{otherRawTable}].[AssetId] = [B].[{instanceOf}]");
        }

        if (input.Filters.Count > 0)
        {
            var items = new List<object>();
            Line(where, "WHERE");
            foreach (var filter in input.Filters)
            {
                var property = input.Properties[filter.PropertyId];
                var relation = input.RelationLookup.GetValueOrDefault((input.AssetTypeId, filter.PropertyId));
                var id = filter.PropertyId.ToString("D");

                if (property.IsSystem)
                {
                    switch (ToSystemType(property.SystemTypeId))
                    {
                        case SystemType.AssetName:
                            items.Add(filter.OperatorId == LikeOperatorId
                                ? $"[A].[{assetTypeName}] LIKE '%{filter.Value}%'"
                                : $"[A].[{assetTypeName}] {filter.Operator} '{filter.Value}'");
                            break;
                        case SystemType.AssetType:
                            items.Add($"[B].[{id}] {filter.Operator} '{filter.Value}'");
                            break;
                    }
                    continue;
                }

                var dataType = property.DataTypeId;
                if (dataType == (byte)DataType.Asset)
                {
                    if (instance && RequireRelation(relation).IsInstance)
                    {
                        if (!Guid.TryParse(filter.Value, out var value))
                            continue;
                        if (value == Guid.Empty)
                        {
                            if (filter.OperatorId == EqualToOperatorId)
                                items.Add($"([B].[{id}] = '') OR ([B].[{id}] IS NULL) ");
                            else if (filter.OperatorId == NotEqualOperatorId)
                                items.Add($"([B].[{id}] != '') AND ([B].[{id}] IS NOT NULL) ");
                        }
                        else
                        {
                            items.Add($"[B].[{id}] {filter.Operator} '{filter.Value}'");
                        }
                    }
                    else if (instance && !RequireRelation(relation).IsInstance)
                    {
                        JoinOtherRaw();
                        items.Add($"[{otherRawTable}].[{id}] {filter.Operator} '{filter.Value}'");
                    }
                    else
                    {
                        items.Add($"[B].[{id}] {filter.Operator} '{filter.Value}'");
                    }
                    continue;
                }

                if (dataType == (byte)DataType.PickList)
                {
                    if (instance && RequireRelation(relation).IsInstance)
                    {
                        items.Add($"[B].[{id}] {filter.Operator} '{filter.Value}'");
                    }
                    else if (instance && !RequireRelation(relation).IsInstance)
                    {
                        JoinOtherRaw();
                        items.Add($"[{otherRawTable}].[{id}] {filter.Operator} '{filter.Value}'");
                    }
                    else
                    {
                        items.Add($"[B].[{id}] {filter.Operator} '{filter.Value}'");
                    }
                    continue;
                }

                var isEmpty = filter.Value == EmptyGuid || string.IsNullOrEmpty(filter.Value);
                if (instance && RequireRelation(relation).IsInstance)
                    items.Add(isEmpty ? $"[B].[{id}] IS NOT NULL" : $"[B].[{id}] {filter.Operator} '{filter.Value}'");
                else if (instance && !RequireRelation(relation).IsInstance)
                    items.Add(isEmpty ? $"[{otherRawTable}].[{id}] IS NOT NULL" : $"[{otherRawTable}].[{id}] {filter.Operator} '{filter.Value}'");
                else
                    items.Add(isEmpty ? $"[B].[{id}] IS NOT NULL" : $"[B].[{id}] {filter.Operator} '{filter.Value}'");
            }

            if (items.Count > 0)
                Line(where, string.Format(CultureInfo.InvariantCulture, input.FilterLogic!, [.. items]));
        }

        var sql = new StringBuilder();
        sql.Append(select).Append(from);
        if (where.Length > 0)
            sql.Append(where);
        return sql.ToString();
    }

    /// <summary>
    /// GetSqlString(baseQuery, type, requestType, tempTableName, properties): for each property (in the given order)
    /// related directly to the type, its values not in the temp table (disabled) union those in it (enabled), then
    /// the temp tables dropped; appended to the base query after a blank line.
    /// </summary>
    public static string BuildValues(string baseQuery, Guid assetTypeId, int requestType, string tempTableName,
        IReadOnlyList<KeyValuePair<Guid, string>> properties, IReadOnlyDictionary<Guid, PropertyRelationRow> relations)
    {
        var definitionTable = GeneratedObjectNames.Table(assetTypeId, isInstance: false, isRaw: false);
        var definitionRawTable = GeneratedObjectNames.Table(assetTypeId, isInstance: false, isRaw: true);
        var instanceTable = GeneratedObjectNames.Table(assetTypeId, isInstance: true, isRaw: false);
        var instanceRawTable = GeneratedObjectNames.Table(assetTypeId, isInstance: true, isRaw: true);

        var sb = new StringBuilder();
        bool usesDefinitions = false, usesInstances = false;
        foreach (var (propertyId, name) in properties)
        {
            if (!relations.TryGetValue(propertyId, out var relation))
                continue;
            string temp, table, raw;
            if (requestType == (int)AssetRequestType.Definition || !relation.IsInstance)
            {
                temp = $"{tempTableName}_D";
                table = definitionTable;
                raw = definitionRawTable;
                usesDefinitions = true;
            }
            else
            {
                temp = $"{tempTableName}_I";
                table = instanceTable;
                raw = instanceRawTable;
                usesInstances = true;
            }
            Line(sb, $"SELECT COMPLETE.[Key], COMPLETE.[{name}] AS [Value], COMPLETE.[Enabled] FROM (");
            Line(sb, $"\tSELECT DISTINCT B.[{propertyId:D}] AS [Key], A.[{name}], 0 AS [Enabled]");
            Line(sb, $"\t\tFROM [{table}] AS A");
            Line(sb, $"\tINNER JOIN [{raw}] AS B ON B.[AssetId] = A.[AssetId]");
            Line(sb, $"\tWHERE [{name}] NOT IN (");
            Line(sb, $"\t\tSELECT DISTINCT [{name}] FROM [#{temp}]");
            Line(sb, "\t)");
            Line(sb, "\t\tUNION");
            Line(sb, $"\tSELECT DISTINCT B.[{propertyId:D}] AS [Key], [{name}], 1 AS [Enabled]");
            Line(sb, $"\tFROM [#{temp}] AS C");
            Line(sb, $"\t\tINNER JOIN [{raw}] AS B ON B.[AssetId] = C.[AssetId]");
            Line(sb, "\t) COMPLETE");
            Line(sb, "ORDER BY [Value]");
            Line(sb, string.Empty);
        }
        if (usesDefinitions)
            Line(sb, $"DROP TABLE [#{tempTableName}_D]");
        if (usesInstances)
            Line(sb, $"DROP TABLE [#{tempTableName}_I]");
        return baseQuery + NewLine + sb;
    }

    private static PropertyRelationRow RequireRelation(PropertyRelationRow? relation) =>
        relation ?? throw new NullReferenceException("Filter property has no relation on the chart's asset type.");

    private static void Line(StringBuilder builder, string text) => builder.Append(text).Append(NewLine);

    private static SystemType ToSystemType(int? id) =>
        id is { } value && Enum.IsDefined(typeof(SystemType), value) ? (SystemType)value : SystemType.NotApplicable;
}
