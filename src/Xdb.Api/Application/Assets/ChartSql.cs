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
/// The chart SQL, transcribed from the original's 11-argument SqlDatabaseLayer.GetSqlString in its three uses:
/// SELECT A.* INTO a temp table (the chart filter panel), the grouping count (a chart's stored GroupSql) and the
/// detail query with @Val (its stored FullSql); plus the filter panel's enabled/disabled value query. Unlike the view
/// builder, an Asset Name filter compares the column named after the asset type ([A].[&lt;type name&gt;]) and
/// InstanceOf filters emit nothing.
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

    /// <summary>SELECT A.* INTO [#name] over the type's table, filtered.</summary>
    /// <param name="input">Loaded for the filters only (no columns); its Label is unused.</param>
    /// <param name="assetTypeName">AssetTypeLayer.Name(type): the column an Asset Name filter compares.</param>
    public static string BuildInto(ViewInput input, string assetTypeName, string intoTableName)
    {
        var parts = new Parts(input);
        Line(parts.Select, $"A.* INTO [#{intoTableName}]");
        parts.AppendFilters(assetTypeName);
        return parts.Assemble(null, null);
    }

    /// <summary>
    /// A chart's grouping count (its GroupSql): raw id, display key and count of the one chart property, grouped,
    /// filtered, no ORDER BY. Special and definition-scoped properties of instance charts emit a lone column with a
    /// leading comma, as the original did (the query then fails when run).
    /// </summary>
    /// <param name="input">Loaded with the chart property as its only column.</param>
    public static string BuildGroupCount(ViewInput input, string assetTypeName, Guid propertyId, string propertyName)
    {
        var parts = new Parts(input);
        var groupBy = new StringBuilder();
        var having = new StringBuilder();
        var instance = input.IsInstance;
        var id = propertyId.ToString("D");
        var instanceOfLabel = instance ? input.DefinitionLabel : input.Label;

        _ = input.Properties[propertyId];
        if (input.Relations.TryGetValue(propertyId, out var relation))
        {
            if (instance && !relation.IsInstance)
            {
                parts.Join(parts.OtherRawTable, $"INNER JOIN [dbo].[{parts.OtherRawTable}] ON [{parts.OtherRawTable}].[AssetId] = [B].[{SystemPropertyIds.InstanceOf:D}]");
                parts.Join(parts.OtherTable, $"INNER JOIN [dbo].[{parts.OtherTable}] ON [{parts.OtherTable}].[AssetId] = [{parts.OtherRawTable}].[AssetId]");
                Line(parts.Select, $"\t,[{parts.OtherTable}].[{propertyName}] AS [{propertyName}]");
            }
            else
            {
                Line(parts.Select, $"\t[B].[{id}] AS [Id]");
                Line(parts.Select, $"\t,[A].[{propertyName}] AS [Key]");
                Line(parts.Select, $"\t,COUNT([A].[{propertyName}]) AS [Value]");
                Line(groupBy, $"GROUP BY [B].[{id}], [A].[{propertyName}]");
                Line(having, $"HAVING (COUNT([A].[{propertyName}]) > 0)");
            }
        }
        else if (propertyId == SystemPropertyIds.InstanceOf)
        {
            Line(parts.Select, $"\t,[A].[{instanceOfLabel}] AS [{instanceOfLabel}]");
        }
        else if (propertyId == SystemPropertyIds.InstanceOfDescription)
        {
            var definitionTable = GeneratedObjectNames.Table(input.AssetTypeId, isInstance: false, isRaw: false);
            parts.Join(definitionTable, $"INNER JOIN [dbo].[{definitionTable}] ON [{definitionTable}].[AssetId] = [{definitionTable}].[AssetId]");
            Line(parts.Select, $"\t,[{definitionTable}].[Description] AS [{instanceOfLabel} Description]");
        }
        else if (propertyId == SystemPropertyIds.AssetType)
        {
            var definitionRawTable = GeneratedObjectNames.Table(input.AssetTypeId, isInstance: false, isRaw: true);
            var definitionTable = GeneratedObjectNames.Table(input.AssetTypeId, isInstance: false, isRaw: false);
            parts.Join(definitionRawTable, $"INNER JOIN [dbo].[{definitionRawTable}] ON [{definitionRawTable}].[AssetId] = [B].[{SystemPropertyIds.InstanceOfDescription:D}]");
            parts.Join(definitionTable, $"INNER JOIN [dbo].[{definitionTable}] ON [{definitionTable}].[AssetId] = [{definitionRawTable}].[AssetId]");
            Line(parts.Select, $"\t,[{definitionTable}].[Asset Type] AS [Asset Type]");
        }
        else if (propertyId == SystemPropertyIds.AssetName)
        {
            Line(parts.Select, $"\t,[A].[Name] AS [{propertyName}]");
        }
        else
        {
            Line(parts.Select, $"\t,[A].[{propertyName}] AS [{propertyName}]");
        }

        parts.AppendFilters(assetTypeName);
        return parts.Assemble(groupBy, having);
    }

    /// <summary>
    /// A chart's detail query (its FullSql): asset id and name of the filtered assets whose value of the chart property
    /// equals @Val.
    /// </summary>
    /// <param name="input">Loaded for the filters only (no columns).</param>
    public static string BuildFull(ViewInput input, string assetTypeName, Guid dynamicPropertyId)
    {
        var parts = new Parts(input);
        Line(parts.Select, "\t[A].[AssetId] AS [AssetId]");
        Line(parts.Select, string.Empty);
        Line(parts.Select, ",\t[A].[Name]");
        parts.AppendFilters(assetTypeName);
        Line(parts.Where, parts.Where.Length > 0 ? $"AND ([B].[{dynamicPropertyId:D}] = @Val)" : $"WHERE ([B].[{dynamicPropertyId:D}] = @Val)");
        return parts.Assemble(null, null);
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

    /// <summary>The overload's builders: SELECT, FROM (with the joins added once each), WHERE.</summary>
    private sealed class Parts
    {
        private readonly ViewInput _input;
        private readonly List<string> _joined = [];

        public Parts(ViewInput input)
        {
            _input = input;
            OtherRawTable = GeneratedObjectNames.Table(input.AssetTypeId, !input.IsInstance, isRaw: true);
            OtherTable = GeneratedObjectNames.Table(input.AssetTypeId, !input.IsInstance, isRaw: false);
            Line(Select, "SELECT");
            Line(From, $"FROM [dbo].[{GeneratedObjectNames.Table(input.AssetTypeId, input.IsInstance, isRaw: false)}] AS [A]");
            Line(From, $"INNER JOIN [dbo].[{GeneratedObjectNames.Table(input.AssetTypeId, input.IsInstance, isRaw: true)}] AS [B] ON [B].[AssetId] = [A].[AssetId]");
        }

        public StringBuilder Select { get; } = new();
        public StringBuilder From { get; } = new();
        public StringBuilder Where { get; } = new();
        public string OtherRawTable { get; }
        public string OtherTable { get; }

        public void Join(string table, string sql)
        {
            if (_joined.Contains(table))
                return;
            _joined.Add(table);
            Line(From, sql);
        }

        /// <summary>The WHERE block (emitted whenever there are filters), formatted with the filter logic over the predicates.</summary>
        public void AppendFilters(string assetTypeName)
        {
            if (_input.Filters.Count == 0)
                return;
            var instance = _input.IsInstance;
            var instanceOf = SystemPropertyIds.InstanceOf.ToString("D");
            var items = new List<object>();
            Line(Where, "WHERE");
            void JoinOtherRaw() => Join(OtherRawTable, $"INNER JOIN [dbo].[{OtherRawTable}] ON [{OtherRawTable}].[AssetId] = [B].[{instanceOf}]");

            foreach (var filter in _input.Filters)
            {
                var property = _input.Properties[filter.PropertyId];
                var relation = _input.RelationLookup.GetValueOrDefault((_input.AssetTypeId, filter.PropertyId));
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
                        items.Add($"[{OtherRawTable}].[{id}] {filter.Operator} '{filter.Value}'");
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
                        items.Add($"[{OtherRawTable}].[{id}] {filter.Operator} '{filter.Value}'");
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
                    items.Add(isEmpty ? $"[{OtherRawTable}].[{id}] IS NOT NULL" : $"[{OtherRawTable}].[{id}] {filter.Operator} '{filter.Value}'");
                else
                    items.Add(isEmpty ? $"[B].[{id}] IS NOT NULL" : $"[B].[{id}] {filter.Operator} '{filter.Value}'");
            }

            if (items.Count > 0)
                Line(Where, string.Format(CultureInfo.InvariantCulture, _input.FilterLogic!, [.. items]));
        }

        public string Assemble(StringBuilder? groupBy, StringBuilder? having)
        {
            var sql = new StringBuilder();
            sql.Append(Select).Append(From);
            if (Where.Length > 0)
                sql.Append(Where);
            if (groupBy is { Length: > 0 })
                sql.Append(groupBy);
            if (having is { Length: > 0 })
                sql.Append(having);
            return sql.ToString();
        }
    }

    private static PropertyRelationRow RequireRelation(PropertyRelationRow? relation) =>
        relation ?? throw new NullReferenceException("Filter property has no relation on the chart's asset type.");

    private static void Line(StringBuilder builder, string text) => builder.Append(text).Append(NewLine);

    private static SystemType ToSystemType(int? id) =>
        id is { } value && Enum.IsDefined(typeof(SystemType), value) ? (SystemType)value : SystemType.NotApplicable;
}
