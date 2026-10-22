using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects;
using Xdb.Api.Application.GeneratedObjects.GenTables;
using Xdb.Api.Infrastructure.Data.Procedures;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.GeneratedObjects;

public class GenTableSqlTests
{
    private static readonly Guid TypeId = new("11111111-2222-3333-4444-555555555555");
    private const string T = "11111111222233334444555555555555";

    private readonly Dictionary<Guid, PropertyMetaRow> _properties = [];
    private readonly Dictionary<Guid, PropertyRelationRow> _relations = [];
    private readonly List<Guid> _order = [];

    private Guid Add(byte dataType, string name, bool system = false, SystemType? systemType = null, bool multi = false,
        bool relationIsInstance = false, bool inheritedValue = false, bool related = true)
    {
        var id = Guid.NewGuid();
        _properties[id] = new PropertyMetaRow
        {
            Id = id, DataTypeId = dataType, Name = name, IsSystem = system, SystemTypeId = (int?)systemType, AllowMultiValue = multi
        };
        if (related && !system)
            _relations[id] = new PropertyRelationRow { Id = Guid.NewGuid(), AssetTypeId = TypeId, PropertyId = id, IsInstance = relationIsInstance, IsInheritedValue = inheritedValue };
        _order.Add(id);
        return id;
    }

    private GenTableSelects Build(bool instance = false, IReadOnlyList<Guid>? load = null) =>
        GenTableSql.BuildSelects(new GenTableInput(TypeId, instance, _order, _properties, _relations, load ?? [TypeId], "Server", "https://img/"));

    [Fact]
    public void EmptyPropertySet_GivesTheFixedHeaderAndTail()
    {
        var selects = Build();

        selects.Decoded.Should().Be(
            "SELECT DISTINCT\r\n" +
            "\t[A].[Id] AS [AssetId]\r\n" +
            "\t,[A].[Name] AS [Name]\r\n" +
            "\t,[A].[DisplayValue] AS [DisplayValue]\r\n" +
            "\t,dbo.GetAssetType(A.[Id]) AS [Asset Type]\r\n" +
            $"INTO [gen{T}_D]\r\n" +
            "FROM [Assets] A WITH (NoLock)\r\n" +
            "WHERE\r\n" +
            $" ([A].[AssetTypeId] = '{TypeId:D}') AND ([A].[IsInstance] = 0)\r\n" +
            "AND (A.[Deleted] IS NULL)\r\n");
        selects.Raw.Should().Be(
            "SELECT DISTINCT\r\n" +
            "\t[A].[Id] AS [AssetId]\r\n" +
            "\t,[A].[Name] AS [8D145EE0-54D2-47DB-8785-5AE086C20DDE]\r\n" +
            "\t,[A].[DisplayValue] AS [DisplayValue]\r\n" +
            "\t,[A].[AssetTypeId] AS [48014265-A355-45EC-9878-94C041ABD927]\r\n" +
            $"INTO [gen{T}_D_Raw]\r\n" +
            "FROM [Assets] A WITH (NoLock)\r\n" +
            "WHERE\r\n" +
            $" ([A].[AssetTypeId] = '{TypeId:D}') AND ([A].[IsInstance] = 0)\r\n" +
            "AND (A.[Deleted] IS NULL)\r\n");
    }

    [Fact]
    public void Columns_FollowTheDataTypeRules()
    {
        var pick = Add((byte)DataType.PickList, "Colour");
        var user = Add((byte)DataType.User, "Owner");
        var image = Add((byte)DataType.Image, "Photo");
        var inherited = Add((byte)DataType.String, " Site ", inheritedValue: true);
        Add((byte)DataType.String, "Unrelated", related: false);
        var created = Add((byte)DataType.DateTime, "Created", system: true, systemType: SystemType.Created);

        var selects = Build();

        selects.Decoded.Should().Contain(
            $"\t,dbo.PropertyValue_PickList([A].[Id], '{pick:D}') AS [Colour]\r\n" +
            $"\t,dbo.PropertyValue_Member([A].[Id], '{user:D}') AS [Owner]\r\n" +
            $"\t,'https://img/' + dbo.GetPropertyValueLookup([A].[Id], '{image:D}') AS [Photo]\r\n" +
            $"\t,[dbo].[GetPropertyValueLookup]([dbo].[GetAssetParentIdByChildId]([A].[Id]), '{inherited:D}') AS [Site]\r\n" +
            "\t,[A].[Created] AS [Created]\r\n");
        selects.Raw.Should().Contain(
            $"\t,dbo.PropertyValue([A].[Id], '{pick:D}') AS [{pick:D}]\r\n" +
            $"\t,dbo.PropertyValue([A].[Id], '{user:D}') AS [{user:D}]\r\n" +
            $"\t,[dbo].[GetPropertyValueLookup]([dbo].[GetAssetParentIdByChildId]([A].[Id]), '{inherited:D}') AS [{inherited:D}]\r\n" +
            $"\t,[A].[Created] AS [{created:D}]\r\n");
        selects.Decoded.Should().NotContain("Unrelated");
    }

    [Fact]
    public void InstancePass_UsesTheDefinitionLabelForInstanceOf_AndReadsDefinitionValuesThroughInstanceOfId()
    {
        Add((byte)DataType.Asset, "InstanceOf", system: true, systemType: SystemType.InstanceOf);
        Add((byte)DataType.String, "InstanceOfDesc", system: true, systemType: SystemType.InstanceOfDescription);
        var definitionLevel = Add((byte)DataType.Int, "Cores", relationIsInstance: false);

        var selects = Build(instance: true);

        selects.Decoded.Should().Contain("\t,dbo.GetAssetParentNameByInstanceId([A].[Id]) AS [Server]\r\n");
        selects.Decoded.Should().Contain("\t,dbo.GetAssetParentDescByInstanceId([A].[Id]) AS [Server Description]\r\n");
        selects.Decoded.Should().Contain($"\t,dbo.PropertyValue([A].[InstanceOfId], '{definitionLevel:D}') AS [Cores]\r\n");
        selects.Raw.Should().Contain($"\t,[A].[InstanceOfId] AS [{SystemPropertyIds.InstanceOf:D}]\r\n");
        selects.Decoded.Should().Contain("AND ([A].[IsInstance] = 1)");
    }

    [Fact]
    public void SingleValuedDependency_KeepsTheOriginalsUnjoinedAlias_CountingOnlyNonInheritedProperties()
    {
        Add((byte)DataType.String, "Inherited", inheritedValue: true);
        Add((byte)DataType.String, "First");
        Add((byte)DataType.Dependency, "Depends");

        Build().Decoded.Should().Contain("\t,dbo.GetAssetValueById([pv1].[Value]) AS [Depends]\r\n");
    }

    [Fact]
    public void SeveralLoadTypes_AreListedInOrder()
    {
        var child = new Guid("aaaaaaaa-0000-0000-0000-000000000001");

        Build(load: [child, TypeId]).Decoded.Should().Contain(
            $"WHERE\r\n ([A].[AssetTypeId] IN ( '{child:D}', '{TypeId:D}')) AND ([A].[IsInstance] = 0)\r\n");
    }

    [Fact]
    public void CreateTable_PrefixesTheDropWithABareLineFeed()
    {
        GenTableSql.CreateTable("genX_D", "SELECT 1").Should().Be("IF OBJECT_ID('dbo.[genX_D]', 'U') IS NOT NULL DROP TABLE dbo.[genX_D]\nSELECT 1");
        GenTableSql.AddPrimaryKey("genX_D").Should().Be("ALTER TABLE [dbo].[genX_D] ADD PRIMARY KEY (AssetId)");
        GenTableSql.DropProcedure("spr_genX_D_Update").Should().Be("IF OBJECT_ID('spr_genX_D_Update') IS NOT NULL DROP PROC [dbo].[spr_genX_D_Update]");
    }

    [Fact]
    public void MergeProcedure_MatchesTheOriginalTemplate()
    {
        var pick = Add((byte)DataType.PickList, "Colour");
        var select = Build().Decoded;

        var procedure = GenTableSql.CreateMergeProcedure($"gen{T}_D", select, ["AssetId", "Name", "DisplayValue", "Asset Type", "Colour"]);

        procedure.Should().Be(
            $"CREATE PROCEDURE [dbo].[spr_gen{T}_D_Update]\r\n" +
            "\t@AssetId\tuniqueidentifier\r\n" +
            "AS\r\n" +
            "\r\n" +
            $"MERGE INTO dbo.[gen{T}_D]\r\n" +
            "\tUSING (\r\n" +
            "\t\tSELECT DISTINCT\r\n" +
            "\t\t\t[A].[Id] AS [AssetId]\r\n" +
            "\t\t\t,[A].[Name] AS [Name]\r\n" +
            "\t\t\t,[A].[DisplayValue] AS [DisplayValue]\r\n" +
            "\t\t\t,dbo.GetAssetType(A.[Id]) AS [Asset Type]\r\n" +
            $"\t\t\t,dbo.PropertyValue_PickList([A].[Id], '{pick:D}') AS [Colour]\r\n" +
            "\t\tFROM [Assets] A WITH (NoLock)\r\n" +
            "WHERE ([A].[Id] = @AssetId)\r\n" +
            "\t) AS [Source]\r\n" +
            $"\tON dbo.[gen{T}_D].[AssetId] = [Source].[AssetId]\r\n" +
            "WHEN MATCHED THEN\r\n" +
            "\tUPDATE\r\n" +
            "\t\tSET\r\n" +
            $"\t\t\tdbo.[gen{T}_D].[Name] = [Source].[Name],\r\n" +
            $"\t\t\tdbo.[gen{T}_D].[DisplayValue] = [Source].[DisplayValue],\r\n" +
            $"\t\t\tdbo.[gen{T}_D].[Asset Type] = [Source].[Asset Type],\r\n" +
            $"\t\t\tdbo.[gen{T}_D].[Colour] = [Source].[Colour]\r\n" +
            "WHEN NOT MATCHED THEN\r\n" +
            "\tINSERT (\r\n" +
            "\t\t\t[AssetId],\r\n" +
            "\t\t\t[Name],\r\n" +
            "\t\t\t[DisplayValue],\r\n" +
            "\t\t\t[Asset Type],\r\n" +
            "\t\t\t[Colour]\r\n" +
            ")\r\n" +
            "VALUES (\r\n" +
            "\t\t\t[Source].[AssetId],\r\n" +
            "\t\t\t[Source].[Name],\r\n" +
            "\t\t\t[Source].[DisplayValue],\r\n" +
            "\t\t\t[Source].[Asset Type],\r\n" +
            "\t\t\t[Source].[Colour]\r\n" +
            ");\r\n");
    }

    [Fact]
    public void Names_FormatAsTheOriginal()
    {
        var id = new Guid("ABCDEF00-1111-2222-3333-444455556666");

        GeneratedObjectNames.Table(id, isInstance: true, isRaw: true).Should().Be("genabcdef00111122223333444455556666_I_Raw");
        GeneratedObjectNames.UpdateProcedure("genX_D").Should().Be("spr_genX_D_Update");
        GeneratedObjectNames.ReportView(id).Should().Be("vwRpt_abcdef00-1111-2222-3333-444455556666");
        GeneratedObjectNames.RoleView(id, id, 1).Should().Be("vwRole_abcdef00-1111-2222-3333-444455556666_abcdef00-1111-2222-3333-444455556666_1");
    }

    [Fact]
    public void PropertyIds_NotInTheMetadata_Fail()
    {
        _order.Add(Guid.NewGuid());

        FluentActions.Invoking(() => Build()).Should().Throw<KeyNotFoundException>();
    }
}
