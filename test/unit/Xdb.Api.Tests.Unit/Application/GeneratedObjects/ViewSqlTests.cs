using System;
using System.Collections.Generic;

using FluentAssertions;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects.Views;
using Xdb.Api.Infrastructure.Data.Procedures;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.GeneratedObjects;

public class ViewSqlTests
{
    private static readonly Guid TypeId = new("11111111-2222-3333-4444-555555555555");
    private const string T = "11111111222233334444555555555555";
    private static readonly string InstanceOf = SystemPropertyIds.InstanceOf.ToString("D");

    private readonly Dictionary<Guid, PropertyMetaRow> _properties = [];
    private readonly Dictionary<Guid, PropertyRelationRow> _relations = [];
    private readonly Dictionary<(Guid, Guid), PropertyRelationRow> _lookup = [];

    public ViewSqlTests()
    {
        _properties[SystemPropertyIds.AssetName] = new PropertyMetaRow
        {
            Id = SystemPropertyIds.AssetName, DataTypeId = (byte)DataType.String, Name = "AssetName", IsSystem = true, SystemTypeId = (int)SystemType.AssetName
        };
    }

    private Guid Property(byte dataType, string name, bool? relationIsInstance)
    {
        var id = Guid.NewGuid();
        _properties[id] = new PropertyMetaRow { Id = id, DataTypeId = dataType, Name = name };
        if (relationIsInstance is { } isInstance)
        {
            var relation = new PropertyRelationRow { Id = Guid.NewGuid(), AssetTypeId = TypeId, PropertyId = id, IsInstance = isInstance };
            _relations[id] = relation;
            _lookup[(TypeId, id)] = relation;
        }
        return id;
    }

    private string Build(bool instance, IReadOnlyList<ViewColumn> columns, IReadOnlyList<ViewFilter> filters, string? logic = null) =>
        ViewSql.BuildSelect(new ViewInput(TypeId, instance, instance ? "Instance" : "Server", "Server", columns, filters, logic,
            _properties, _relations, _lookup));

    private static readonly ViewColumn NameColumn = new(SystemPropertyIds.AssetName, null, null, "Asset Name", null);

    [Fact]
    public void RoleView_MatchesTheOriginalLayout()
    {
        var colour = Property((byte)DataType.PickList, "Colour", relationIsInstance: false);
        var value = Guid.NewGuid();

        var sql = Build(false, [NameColumn], [new ViewFilter(colour, 0, "=", value.ToString())], "({0})");

        ViewSql.CreateView("vwRole_x", sql).Should().Be(
            "CREATE VIEW [dbo].[vwRole_x]\r\nAS\r\n" +
            "SELECT\r\n" +
            "\t[A].[AssetId] AS [AssetId]\r\n" +
            ",\t[A].[Name] AS [Server]\r\n" +
            $"FROM [dbo].[gen{T}_D] AS [A]\r\n" +
            $"INNER JOIN [dbo].[gen{T}_D_Raw] AS [B] ON [B].[AssetId] = [A].[AssetId]\r\n" +
            "WHERE\r\n" +
            $"([B].[{colour:D}] = '{value:D}')\r\n" +
            "\r\n");
    }

    [Fact]
    public void DropView_HasNoSeparatorBeforeDrop()
    {
        ViewSql.DropView("vwRpt_x").Should().Be(
            "IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[vwRpt_x]'))DROP VIEW [dbo].[vwRpt_x]");
    }

    [Fact]
    public void InstanceReport_ReadsDefinitionColumnsThroughTheDefinitionTables_JoinedOnce()
    {
        var cores = Property((byte)DataType.Int, "Cores", relationIsInstance: false);
        var ram = Property((byte)DataType.Int, "Ram", relationIsInstance: false);
        var slot = Property((byte)DataType.String, "Slot", relationIsInstance: true);

        var sql = Build(true, [NameColumn, new(cores, null, 0, "Cores", null), new(ram, null, null, "Ram", null), new(slot, null, null, "Slot", null)], []);

        sql.Should().Be(
            "SELECT\r\n" +
            "\t[A].[AssetId] AS [AssetId]\r\n" +
            ",\t[A].[Name] AS [Instance]\r\n" +
            $"\t,[gen{T}_D].[Cores] AS [Cores]\r\n" +
            $"\t,[gen{T}_D].[Ram] AS [Ram]\r\n" +
            "\t,[A].[Slot] AS [Slot]\r\n" +
            $"FROM [dbo].[gen{T}_I] AS [A]\r\n" +
            $"INNER JOIN [dbo].[gen{T}_I_Raw] AS [B] ON [B].[AssetId] = [A].[AssetId]\r\n" +
            $"INNER JOIN [dbo].[gen{T}_D_Raw] ON [gen{T}_D_Raw].[AssetId] = [B].[{InstanceOf}]\r\n" +
            $"INNER JOIN [dbo].[gen{T}_D] ON [gen{T}_D].[AssetId] = [gen{T}_D_Raw].[AssetId]\r\n");
    }

    [Fact]
    public void Filters_KeepTheOriginalsQuirks()
    {
        var text = Property((byte)DataType.String, "Text", relationIsInstance: false);
        var asset = Property((byte)DataType.Asset, "Parent", relationIsInstance: false);

        var sql = Build(false, [NameColumn],
        [
            new ViewFilter(SystemPropertyIds.AssetName, 6, "LIKE", "web"),
            new ViewFilter(text, 5, "<>", "00000000-0000-0000-0000-000000000000"),
            new ViewFilter(text, 0, "=", "it's"),
            new ViewFilter(asset, 0, "=", "x")
        ], "{0} AND {1} AND {2} AND {3}");

        sql.Should().EndWith(
            "WHERE\r\n" +
            $"[A].[Name] LIKE '%web%' AND [B].[{text:D}] IS NOT NULL AND [B].[{text:D}] = 'it's' AND [B].[{asset:D}] = 'x'\r\n");
    }

    [Fact]
    public void FiltersThatProduceNoItems_LeaveABareWhere()
    {
        var asset = Property((byte)DataType.Asset, "Parent", relationIsInstance: true);

        Build(true, [NameColumn], [new ViewFilter(asset, 0, "=", "not-a-guid")], "{0}").Should().EndWith("WHERE\r\n");
    }

    [Fact]
    public void CustomFieldNotOtherwiseReferenced_Fails()
    {
        var custom = new ViewColumn(Guid.NewGuid(), null, 1, "Calc", null);

        FluentActions.Invoking(() => Build(false, [NameColumn, custom], [])).Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void ReferencedIds_AreFiltersThenPlainColumnsAndSubProperties()
    {
        var f = Guid.NewGuid();
        var c = Guid.NewGuid();
        var s = Guid.NewGuid();
        var custom = Guid.NewGuid();

        ViewInput.ReferencedPropertyIds(
            [new ViewColumn(c, s, null, "C", "S"), new ViewColumn(custom, null, 1, "X", null), new ViewColumn(f, null, 0, "F", null)],
            [new ViewFilter(f, 0, "=", "1")]).Should().Equal(f, c, s);
    }
}
