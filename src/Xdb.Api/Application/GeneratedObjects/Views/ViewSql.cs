using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Infrastructure.Data.Procedures;

namespace Xdb.Api.Application.GeneratedObjects.Views;

/// <summary>A view column (a report property, or the single AssetName column of a role view).</summary>
/// <param name="CustomReportFieldTypeId">Null or 0 for a plain property; custom fields emit nothing in a view.</param>
/// <param name="PropertyName">ISNULL(DisplayValue, Name) of the property, as spr_ReportProperties_Get returns it.</param>
public sealed record ViewColumn(Guid PropertyId, Guid? SubPropertyId, int? CustomReportFieldTypeId, string? PropertyName, string? SubPropertyName)
{
    public bool IsPlainProperty => CustomReportFieldTypeId is null or 0;
}

/// <summary>A filter as the original held it: the operator's id and its text from lkOperators.</summary>
public sealed record ViewFilter(Guid PropertyId, int OperatorId, string Operator, string Value);

/// <summary>Everything a view's SQL is built from, already loaded.</summary>
/// <param name="Label">ReportingLabel(type, IsInstance): alias of the name column.</param>
/// <param name="DefinitionLabel">ReportingLabel(type, definitions).</param>
/// <param name="Properties">spr_Properties_GetByListOfPropertyIds for every filter, column and sub-property id (deleted ones included).</param>
/// <param name="Relations">The type's live relations for those ids, by property id (spr_AssetTypePropertyRelationList_...).</param>
/// <param name="RelationLookup">
/// AssetTypePropertyRelation_Get results by (asset type id, property id): the first live relation, or absent. Needed for
/// every filter property on this type and every non-system sub-property on its parent property's type.
/// </param>
public sealed record ViewInput(
    Guid AssetTypeId,
    bool IsInstance,
    string Label,
    string DefinitionLabel,
    IReadOnlyList<ViewColumn> Columns,
    IReadOnlyList<ViewFilter> Filters,
    string? FilterLogic,
    IReadOnlyDictionary<Guid, PropertyMetaRow> Properties,
    IReadOnlyDictionary<Guid, PropertyRelationRow> Relations,
    IReadOnlyDictionary<(Guid AssetTypeId, Guid PropertyId), PropertyRelationRow> RelationLookup)
{
    /// <summary>The ids the original loaded properties and bulk relations for: filters first, then plain columns and their sub-properties.</summary>
    public static IReadOnlyList<Guid> ReferencedPropertyIds(IReadOnlyList<ViewColumn> columns, IReadOnlyList<ViewFilter> filters)
    {
        var ids = new List<Guid>();
        foreach (var filter in filters)
            if (!ids.Contains(filter.PropertyId))
                ids.Add(filter.PropertyId);
        foreach (var column in columns.Where(c => c.IsPlainProperty))
        {
            if (!ids.Contains(column.PropertyId))
                ids.Add(column.PropertyId);
            if (column.SubPropertyId is { } sub && !ids.Contains(sub))
                ids.Add(sub);
        }
        return ids;
    }
}

/// <summary>
/// The SQL of report and role views, reproduced character for character from the original's GetSqlString (with
/// includeOrderBy and forSingleProp false, the only way views were made) and ViewCreate/ViewDrop. Quirks kept:
/// the first column is always the name column whatever it is; filter values are interpolated unescaped; an empty
/// Guid or empty value means IS NOT NULL for any operator; custom fields emit nothing.
/// </summary>
public static class ViewSql
{
    public const string NewLine = "\r\n";

    public static string DropView(string viewName) =>
        $"IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[{viewName}]'))DROP VIEW [dbo].[{viewName}]";

    public static string CreateView(string viewName, string select) =>
        $"CREATE VIEW [dbo].[{viewName}]{NewLine}AS{NewLine}{select}{NewLine}";

    public static string BuildSelect(ViewInput input)
    {
        var instance = input.IsInstance;
        var label = input.Label;
        var instanceOfLabel = instance ? input.DefinitionLabel : label;
        var table = GeneratedObjectNames.Table(input.AssetTypeId, instance, isRaw: false);
        var rawTable = GeneratedObjectNames.Table(input.AssetTypeId, instance, isRaw: true);
        var otherRawTable = GeneratedObjectNames.Table(input.AssetTypeId, !instance, isRaw: true);
        var otherTable = GeneratedObjectNames.Table(input.AssetTypeId, !instance, isRaw: false);
        var instanceOf = SystemPropertyIds.InstanceOf.ToString("D");

        var select = new StringBuilder();
        var from = new StringBuilder();
        var where = new StringBuilder();
        var joined = new List<string>();

        Line(select, "SELECT");
        Line(from, $"FROM [dbo].[{table}] AS [A]");
        Line(from, $"INNER JOIN [dbo].[{rawTable}] AS [B] ON [B].[AssetId] = [A].[AssetId]");
        Line(select, "\t[A].[AssetId] AS [AssetId]");

        void Join(string tableName, string sql)
        {
            if (joined.Contains(tableName))
                return;
            joined.Add(tableName);
            Line(from, sql);
        }

        var index = 0;
        foreach (var column in input.Columns)
        {
            if (index == 0)
            {
                Line(select, $",\t[A].[Name] AS [{label}]");
                index++;
                continue;
            }

            // Looked up before the column kind is known: a custom field whose id is not otherwise referenced fails here, as it did.
            var property = input.Properties[column.PropertyId];
            input.Relations.TryGetValue(column.PropertyId, out var relation);

            if (relation is not null)
            {
                if (column.SubPropertyId is { } subId && subId != Guid.Empty)
                {
                    var subProperty = input.Properties[subId];
                    if (subProperty.IsSystem)
                    {
                        var isInstance = property.AssetTypeIsInstance.HasValue && property.AssetTypeIsInstance.Value;
                        var subTable = GeneratedObjectNames.Table(property.AssetTypeId!.Value, isInstance, isRaw: false);
                        var alias = column.PropertyName!.Replace(" ", "_") + "_Data";
                        Line(from, $"LEFT JOIN [dbo].[{subTable}] AS [{alias}] ON [{alias}].[AssetId] = [B].[{column.PropertyId:D}]");
                        // Only AssetName emits a column; other system sub-properties leave just the join.
                        if (ToSystemType(subProperty.SystemTypeId) == SystemType.AssetName)
                        {
                            Line(select, property.AssetTypeId.Value == SystemAssetTypeIds.User
                                ? $"\t,[{alias}].[Name] AS [{column.PropertyName}.BUN]"
                                : $"\t,[{alias}].[{column.SubPropertyName}] AS [{column.SubPropertyName}]");
                        }
                    }
                    else
                    {
                        var subRelation = input.RelationLookup.GetValueOrDefault((property.AssetTypeId!.Value, subId))
                            ?? throw new NullReferenceException($"No relation for sub-property {subId:D} on its asset type.");
                        var subTable = GeneratedObjectNames.Table(property.AssetTypeId.Value, subRelation.IsInstance, isRaw: false);
                        Join(subTable, $"LEFT JOIN [dbo].[{subTable}] ON [{subTable}].[AssetId] = [B].[{column.PropertyId:D}]");
                        Line(select, $"\t,[{subTable}].[{column.SubPropertyName}] AS [{column.SubPropertyName}]");
                    }
                }
                else if (column.IsPlainProperty)
                {
                    if (instance && !relation.IsInstance)
                    {
                        Join(otherRawTable, $"INNER JOIN [dbo].[{otherRawTable}] ON [{otherRawTable}].[AssetId] = [B].[{instanceOf}]");
                        Join(otherTable, $"INNER JOIN [dbo].[{otherTable}] ON [{otherTable}].[AssetId] = [{otherRawTable}].[AssetId]");
                        Line(select, $"\t,[{otherTable}].[{column.PropertyName}] AS [{column.PropertyName}]");
                    }
                    else
                    {
                        Line(select, $"\t,[A].[{column.PropertyName}] AS [{column.PropertyName}]");
                    }
                }
            }
            else if (column.IsPlainProperty)
            {
                if (column.PropertyId == SystemPropertyIds.InstanceOf)
                {
                    Line(select, $"\t,[A].[{instanceOfLabel}] AS [{instanceOfLabel}]");
                }
                else if (column.PropertyId == SystemPropertyIds.InstanceOfDescription)
                {
                    var definitionTable = GeneratedObjectNames.Table(input.AssetTypeId, isInstance: false, isRaw: false);
                    Join(definitionTable, $"INNER JOIN [dbo].[{definitionTable}] ON [{definitionTable}].[AssetId] = [B].[{instanceOf}]");
                    Line(select, $"\t,[{definitionTable}].[Description] AS [{instanceOfLabel} Description]");
                }
                else if (column.PropertyId == SystemPropertyIds.AssetType)
                {
                    var definitionRawTable = GeneratedObjectNames.Table(input.AssetTypeId, isInstance: false, isRaw: true);
                    var definitionTable = GeneratedObjectNames.Table(input.AssetTypeId, isInstance: false, isRaw: false);
                    // Joined on the InstanceOfDescription column (sic), as in the original.
                    Join(definitionRawTable, $"INNER JOIN [dbo].[{definitionRawTable}] ON [{definitionRawTable}].[AssetId] = [B].[{SystemPropertyIds.InstanceOfDescription:D}]");
                    Join(definitionTable, $"INNER JOIN [dbo].[{definitionTable}] ON [{definitionTable}].[AssetId] = [{definitionRawTable}].[AssetId]");
                    Line(select, $"\t,[{definitionTable}].[Asset Type] AS [Asset Type]");
                }
                else if (column.PropertyId == SystemPropertyIds.AssetName)
                {
                    Line(select, $"\t,[A].[Name] AS [{column.PropertyName}]");
                }
                else
                {
                    // Description and any other property without a relation on this type.
                    Line(select, $"\t,[A].[{column.PropertyName}] AS [{column.PropertyName}]");
                }
            }

            index++;
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
                                ? $"[A].[Name] LIKE '%{filter.Value}%'"
                                : $"[A].[Name] {filter.Operator} '{filter.Value}'");
                            break;
                        case SystemType.AssetType:
                            items.Add($"[B].[{id}] {filter.Operator} '{filter.Value}'");
                            break;
                        case SystemType.InstanceOf:
                            items.Add($"[B].[{instanceOf}] {filter.Operator} '{filter.Value}'");
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
                        Join(otherRawTable, $"INNER JOIN [dbo].[{otherRawTable}] ON [{otherRawTable}].[AssetId] = [B].[{instanceOf}]");
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
                        Join(otherRawTable, $"INNER JOIN [dbo].[{otherRawTable}] ON [{otherRawTable}].[AssetId] = [B].[{instanceOf}]");
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

            // The filter logic is a composite format string over the items, e.g. "(({0}) AND ({1}))".
            if (items.Count > 0)
                Line(where, string.Format(CultureInfo.InvariantCulture, input.FilterLogic!, [.. items]));
        }

        var sql = new StringBuilder();
        sql.Append(select).Append(from);
        if (input.Filters.Count > 0)
            sql.Append(where);
        return sql.ToString();
    }

    private const int EqualToOperatorId = 0;
    private const int NotEqualOperatorId = 5;
    private const int LikeOperatorId = 6;
    private const string EmptyGuid = "00000000-0000-0000-0000-000000000000";

    /// <summary>The original dereferenced a missing relation here (instance views only) and failed.</summary>
    private static PropertyRelationRow RequireRelation(PropertyRelationRow? relation) =>
        relation ?? throw new NullReferenceException("Filter property has no relation on the view's asset type.");

    private static void Line(StringBuilder builder, string text) => builder.Append(text).Append(NewLine);

    private static SystemType ToSystemType(int? id) =>
        id is { } value && Enum.IsDefined(typeof(SystemType), value) ? (SystemType)value : SystemType.NotApplicable;
}
