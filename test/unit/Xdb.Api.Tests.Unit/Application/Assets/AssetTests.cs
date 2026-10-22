using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

using FluentAssertions;

using Xdb.Api.Application.Assets;
using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects.Views;
using Xdb.Api.Application.Metadata.Legacy;
using Xdb.Api.Application.Metadata.Queries.Lookups;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.LegacyWire;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.Assets;

public class AssetTests
{
    private static readonly Guid TypeId = new("11111111-2222-3333-4444-555555555555");
    private const string T = "11111111222233334444555555555555";
    private static readonly Guid NameProperty = SystemPropertyIds.AssetName;
    private static readonly Guid TextProperty = new("aaaaaaaa-0000-0000-0000-000000000001");

    [Theory]
    [InlineData("http://host/", "app", "http://host/app")]
    [InlineData("http://host", "app", "http://host\\app")]
    [InlineData("http://host/", "/rooted", "/rooted")]
    [InlineData("http://host/", "", "http://host/")]
    public void CombinedWebServerUrl_FollowsPathCombine(string server, string virtualDir, string expected)
    {
        new LegacyEnvironmentOptions { WebServerUrl = server, VirtualDir = virtualDir }.CombinedWebServerUrl().Should().Be(expected);
    }

    [Theory]
    [InlineData("1234.5", "$1,234.50")]
    [InlineData("$-3", "($3.00)")]
    [InlineData("x", "")]
    [InlineData("abc", "abc")]
    [InlineData("12abc", "12abc")]
    public void FormatValue_RewritesCurrencyOnly(string value, string expected)
    {
        var currency = new PropertyContract { DataType = (int)LegacyDataType.Currency };
        PropertyValueLoader.FormatValue(currency, value).Should().Be(expected);
        PropertyValueLoader.FormatValue(new PropertyContract { DataType = (int)LegacyDataType.String }, value).Should().Be(value);
    }

    [Fact]
    public void SearchClient_AcceptsQuotedNumbers_AndRejectsMissingRequiredMembers()
    {
        var client = LegacyJsonBody.SearchClient(Parse(
            """{"id":"c","assetTypeId":"t","assetRequestType":"1","filterName":"","filterExpression":"({0})","filters":[{"propertyId":"p","operatorId":"6","value":"v","order":0}],"maxRecords":"5"}"""))!;
        client.Id.Should().Be("c");
        client.AssetRequestType.Should().Be(1);
        client.Filters.Should().ContainSingle().Which.OperatorId.Should().Be(6);

        FluentActions.Invoking(() => LegacyJsonBody.SearchClient(Parse("""{"assetTypeId":"t","assetRequestType":0,"filterName":"","filterExpression":""}""")))
            .Should().Throw<LegacyBodyException>();
        FluentActions.Invoking(() => LegacyJsonBody.SearchClient(Parse(
                """{"assetTypeId":"t","assetRequestType":"x","filterName":"","filterExpression":"","filters":[]}""")))
            .Should().Throw<LegacyBodyException>();
        LegacyJsonBody.SearchClient(null).Should().BeNull();
    }

    [Fact]
    public async Task WrappedMemberErrors_AreTheHostsBadRequestPage()
    {
        var root = Parse("""{"assetIds":["not-a-guid"],"includeDesc":"maybe"}""");

        var list = FluentActions.Invoking(() => LegacyJsonBody.Member(root, "assetIds", LegacyJsonBody.GuidList, null)).Should().Throw<LegacyBodyException>().Which;
        var flag = FluentActions.Invoking(() => LegacyJsonBody.Member(root, "includeDesc", LegacyJsonBody.Bool, false)).Should().Throw<LegacyBodyException>().Which;

        (await Write(list.ToResult())).Should().Be((400, 1416));
        (await Write(flag.ToResult())).Should().Be((400, 1306));
    }

    [Theory]
    [InlineData("x", 400, 0)]
    [InlineData("  {bad", 400, 0)]
    [InlineData("{,", 400, 0)]
    [InlineData("not json", 500, 1047)]
    [InlineData("{\"a\":}", 500, 1047)]
    [InlineData("[1", 500, 1047)]
    [InlineData("{\"assetTypeId\":\"x\"}junk", 500, 1047)]
    public async Task MalformedBodies_AnswerAsTheHostDid(string body, int status, long length)
    {
        (await Write(LegacyJsonBody.Malformed(body)!)).Should().Be((status, length));
        LegacyJsonBody.Malformed("{}").Should().BeNull();
    }

    [Fact]
    public void GuidDictionary_RejectsRepeatedKeys()
    {
        var key = Guid.NewGuid().ToString("D");
        FluentActions.Invoking(() => LegacyJsonBody.GuidDictionary(Parse($$"""[{"Key":"{{key}}","Value":"a"},{"Key":"{{key}}","Value":"b"}]""")))
            .Should().Throw<LegacyBodyException>();
    }

    [Fact]
    public void ChartBaseQuery_SelectsIntoTheTempTable_AndFiltersNameOnTheTypeNameColumn()
    {
        var input = Input(isInstance: false, [new ViewFilter(NameProperty, 6, "LIKE", "ab")], "({0})");

        ChartSql.BuildInto(input, "Widget", "c1_D").Should().Be(
            "SELECT\r\n"
            + "A.* INTO [#c1_D]\r\n"
            + $"FROM [dbo].[gen{T}_D] AS [A]\r\n"
            + $"INNER JOIN [dbo].[gen{T}_D_Raw] AS [B] ON [B].[AssetId] = [A].[AssetId]\r\n"
            + "WHERE\r\n"
            + "([A].[Widget] LIKE '%ab%')\r\n");
    }

    [Fact]
    public void ChartValueQuery_UsesOnlyDirectRelations_AndDropsTheTempTables()
    {
        var other = Guid.NewGuid();
        var relations = new Dictionary<Guid, PropertyRelationRow> { [TextProperty] = new() { PropertyId = TextProperty, IsInstance = true } };

        var sql = ChartSql.BuildValues("BASE\r\n", TypeId, (int)AssetRequestType.Instance, "c1",
            [new(other, "Inherited"), new(TextProperty, "Colour")], relations);

        sql.Should().StartWith("BASE\r\n\r\nSELECT COMPLETE.[Key], COMPLETE.[Colour] AS [Value], COMPLETE.[Enabled] FROM (\r\n");
        sql.Should().Contain($"\t\tFROM [gen{T}_I] AS A\r\n").And.Contain("\t\tSELECT DISTINCT [Colour] FROM [#c1_I]\r\n");
        sql.Should().NotContain("Inherited").And.EndWith("ORDER BY [Value]\r\n\r\nDROP TABLE [#c1_I]\r\n");
    }

    private static ViewInput Input(bool isInstance, IReadOnlyList<ViewFilter> filters, string logic) => new(
        TypeId, isInstance, "Label", "Label", [], filters, logic,
        new Dictionary<Guid, PropertyMetaRow>
        {
            [NameProperty] = new() { Id = NameProperty, IsSystem = true, SystemTypeId = (int)SystemType.AssetName },
            [TextProperty] = new() { Id = TextProperty, IsSystem = false, DataTypeId = 0 }
        },
        new Dictionary<Guid, PropertyRelationRow>(),
        new Dictionary<(Guid, Guid), PropertyRelationRow>());

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    /// <summary>Executes a result and returns its status and body length (the sizes the original's pages measured).</summary>
    private static async Task<(int Status, long Length)> Write(Microsoft.AspNetCore.Mvc.IActionResult result)
    {
        var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        context.Response.Body = new System.IO.MemoryStream();
        await result.ExecuteResultAsync(new Microsoft.AspNetCore.Mvc.ActionContext(context, new Microsoft.AspNetCore.Routing.RouteData(),
            new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()));
        return (context.Response.StatusCode, context.Response.Body.Length);
    }
}
