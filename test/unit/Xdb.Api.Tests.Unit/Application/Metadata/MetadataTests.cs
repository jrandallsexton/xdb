using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;
using Moq.AutoMock;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects.Views;
using Xdb.Api.Application.Metadata.Legacy;
using Xdb.Api.Application.Metadata.Queries.AssetTypes;
using Xdb.Api.Infrastructure.Data.Procedures;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.Metadata;

public class MetadataTests
{
    private static readonly Guid TypeId = new("11111111-2222-3333-4444-555555555555");
    private const string T = "11111111222233334444555555555555";

    [Fact]
    public void TreeJson_MatchesTheOriginalText()
    {
        var root = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
        var child = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

        AssetTypeTreeJson.Build([new AssetTypeTreeNode(root, "Root", [new AssetTypeTreeNode(child, "Leaf", [])])]).Should().Be(
            "[\r\n{\r\n\"data\" : \"Root\",\r\n\"id\" : \"aaaaaaaa-0000-0000-0000-000000000001\",\r\n\"children\" : [\r\n" +
            "{\r\n\"data\" : \"Leaf\",\r\n\"children\" : [\r\n],\r\n\"metadata\" : { \"id\" :  \"bbbbbbbb-0000-0000-0000-000000000002\" }\r\n}\r\n" +
            "],\r\n\"metadata\" : { \"id\" :  \"aaaaaaaa-0000-0000-0000-000000000001\" }\r\n}\r\n]\r\n");
    }

    [Fact]
    public void SortByName_IsCultureOrderedAndFirstWins()
    {
        var first = Guid.NewGuid();
        var sorted = MetadataRules.SortByName([
            new KeyValuePair<Guid, string>(first, "beta"), new KeyValuePair<Guid, string>(Guid.NewGuid(), "Alpha"),
            new KeyValuePair<Guid, string>(Guid.NewGuid(), "beta")]);

        sorted.Should().HaveCount(2);
        sorted[0].Value.Should().Be("Alpha");
        sorted[1].Key.Should().Be(first);
    }

    [Fact]
    public async Task PropertyDictionary_RelabelsSystemProperties_ThenSorts()
    {
        var mocker = new AutoMocker();
        var procedures = mocker.GetMock<IMetadataProcedures>();
        var assetName = Guid.NewGuid();
        var colour = Guid.NewGuid();
        procedures.Setup(p => p.GetDescendantAssetTypeIdsAsync(TypeId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        procedures.Setup(p => p.GetAssetTypePropertyNamesAsync(TypeId, true, true, true, false, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new KeyValuePair<Guid, string>(assetName, "Asset Name"), new KeyValuePair<Guid, string>(colour, "Colour")]);
        procedures.Setup(p => p.GetPropertiesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PropertyDetailRow { Id = assetName, IsSystem = true, SystemTypeId = 0 }, new PropertyDetailRow { Id = colour }]);
        procedures.Setup(p => p.GetReportingLabelAsync(TypeId, false, It.IsAny<CancellationToken>())).ReturnsAsync("Server");

        var result = await mocker.CreateInstance<MetadataRules>().PropertyDictionaryAsync(TypeId, AssetRequestType.Definition, true, true, true, false, false, default);

        result.Should().Equal(new KeyValuePair<Guid, string>(colour, "Colour"), new KeyValuePair<Guid, string>(assetName, "Server"));
    }

    [Fact]
    public void ViewSql_SinglePropertyForm_MatchesTheOriginal()
    {
        var pid = Guid.NewGuid();
        var input = new ViewInput(TypeId, true, "Instance", "Server", [new ViewColumn(pid, null, 0, "Cores", null)], [], string.Empty,
            new Dictionary<Guid, PropertyMetaRow> { [pid] = new() { Id = pid, Name = "Cores" } },
            new Dictionary<Guid, PropertyRelationRow> { [pid] = new() { PropertyId = pid, AssetTypeId = TypeId, IsInstance = false } },
            new Dictionary<(Guid, Guid), PropertyRelationRow>(), ForSingleProp: true, IncludeOrderBy: true);

        ViewSql.BuildSelect(input).Should().Be(
            "SELECT DISTINCT\r\n" +
            $"\t[gen{T}_D_Raw].[{pid:D}] AS [Key]\r\n" +
            $"\t,[gen{T}_D].[Cores] AS [Value]\r\n" +
            $"FROM [dbo].[gen{T}_I] AS [A]\r\n" +
            $"INNER JOIN [dbo].[gen{T}_I_Raw] AS [B] ON [B].[AssetId] = [A].[AssetId]\r\n" +
            $"INNER JOIN [dbo].[gen{T}_D_Raw] ON [gen{T}_D_Raw].[AssetId] = [B].[{SystemPropertyIds.InstanceOf:D}]\r\n" +
            $"INNER JOIN [dbo].[gen{T}_D] ON [gen{T}_D].[AssetId] = [gen{T}_D_Raw].[AssetId]\r\n" +
            "ORDER BY [Value]\r\n");
    }

    [Fact]
    public void ViewSql_SinglePropertyWithoutRelation_LeavesTheOriginalsLeadingComma()
    {
        var pid = Guid.NewGuid();
        var input = new ViewInput(TypeId, false, "Server", "Server", [new ViewColumn(pid, null, 0, "Cores", null)], [], string.Empty,
            new Dictionary<Guid, PropertyMetaRow> { [pid] = new() { Id = pid, Name = "Cores" } },
            new Dictionary<Guid, PropertyRelationRow>(), new Dictionary<(Guid, Guid), PropertyRelationRow>(), ForSingleProp: true);

        ViewSql.BuildSelect(input).Should().StartWith("SELECT DISTINCT\r\n\t,[A].[Cores] AS [Cores]\r\n");
    }

    [Fact]
    public void Property_WithPrecision_FailsAsTheOriginal()
    {
        FluentActions.Invoking(() => Xdb.Api.Application.Metadata.MetadataLoader.Property(new PropertyDetailRow { Precision = 2 }))
            .Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void Property_MapsDefaults()
    {
        var contract = Xdb.Api.Application.Metadata.MetadataLoader.Property(new PropertyDetailRow { DataTypeId = 18, SystemTypeId = null, RoleId = Guid.Empty });

        contract.DataType.Should().Be(-1);
        contract.SystemType.Should().Be(11);
        contract.IsReadOnly.Should().BeTrue();
        contract.UsesRole.Should().BeFalse();
        contract.Name.Should().BeEmpty();
    }
}
