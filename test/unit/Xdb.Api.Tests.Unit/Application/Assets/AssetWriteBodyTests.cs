using System;
using System.Text.Json;

using FluentAssertions;

using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Common.Legacy;
using Xdb.Core.LegacyWire;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.Assets;

/// <summary>Body binding measured against the original serializer (spec M6 0.3, FX S/C/K labels; spec M6-M 5.0).</summary>
public class AssetWriteBodyTests
{
    private const string PropertyId = "11111111-1111-1111-1111-111111111111";
    private const string ValueId = "22222222-2222-2222-2222-222222222222";
    private const string AssetId = "33333333-3333-3333-3333-333333333333";

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement;

    private static string Submittal(string newValues, string valuesToDelete = "[]") =>
        $"{{\"UserId\":\"{AssetId}\",\"ViewId\":\"{AssetId}\",\"AssetId\":\"{AssetId}\",\"NewValues\":{newValues},\"ValuesToDelete\":{valuesToDelete}}}";

    [Fact]
    public void Submittal_KeepsPostedOrder_NumbersAsText_NullValues_S1_S8_S15()
    {
        var body = AssetBodies.Submittal(Json(Submittal(
            $"[{{\"Key\":\"{PropertyId}\",\"Value\":5}},{{\"Key\":\"{ValueId}\",\"Value\":null}}]", $"[{{\"Key\":\"0\",\"Value\":\"{ValueId}\"}}]")))!;

        body.NewValues.Should().HaveCount(2);
        body.NewValues![0].Value.Should().Be("5");
        body.NewValues[1].Value.Should().BeNull();
        body.ValuesToDelete!.Should().ContainSingle().Which.Key.Should().Be(0);
    }

    [Fact]
    public void Submittal_ObjectForm_IsEmpty_NullStaysNull_S4_S6()
    {
        AssetBodies.Submittal(Json(Submittal("{\"a\":\"b\"}")))!.NewValues.Should().BeEmpty();
        AssetBodies.Submittal(Json(Submittal("null")))!.NewValues.Should().BeNull();
    }

    [Theory]
    [InlineData("[{\"Key\":\"" + PropertyId + "\",\"Value\":\"a\"},{\"Key\":\"" + PropertyId + "\",\"Value\":\"b\"}]")] // duplicate key (S2)
    [InlineData("[{\"key\":\"" + PropertyId + "\",\"value\":\"a\"}]")]                                                   // lowercase members (S10)
    [InlineData("[null]")]                                                                                                // null entry (S13)
    [InlineData("[{\"Key\":\"zz\",\"Value\":\"a\"}]")]                                                                    // bad Guid key (S9)
    public void Submittal_BadDictionaries_AreTheRequestErrorPage(string newValues) =>
        FluentActions.Invoking(() => AssetBodies.Submittal(Json(Submittal(newValues)))).Should().Throw<LegacyBodyException>();

    [Fact]
    public void Submittal_MissingMember_Fails_S3() =>
        FluentActions.Invoking(() => AssetBodies.Submittal(Json($"{{\"UserId\":\"{AssetId}\",\"AssetId\":\"{AssetId}\",\"NewValues\":[],\"ValuesToDelete\":[]}}")))
            .Should().Throw<LegacyBodyException>();

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("{\"name\":\"lowercase is ignored\"}")]
    public void Creation_PlainClass_EmptyBodiesBindDefaults_C3_C9_C10(string json)
    {
        var body = AssetBodies.Creation(Json(json))!;

        body.Id.Should().BeEmpty();
        body.Name.Should().BeNull();
        body.ParentId.Should().BeNull();
    }

    [Fact]
    public void Creation_NullForAGuidFails_ButNotForTheNullableOnes_C5_C6()
    {
        AssetBodies.Creation(Json("{\"ParentId\":null,\"InstanceOfId\":null}"))!.ParentId.Should().BeNull();
        FluentActions.Invoking(() => AssetBodies.Creation(Json("{\"Id\":null}"))).Should().Throw<LegacyBodyException>();
    }

    [Fact]
    public void FilterValues_BadKey_ResetsTheConnection_DuplicateKey_IsTheErrorPage()
    {
        FluentActions.Invoking(() => AssetBodies.FilterValues(Json("{\"propsAndValues\":[{\"Key\":\"zz\",\"Value\":\"a\"}]}"))).Should().Throw<ConnectionResetException>();
        FluentActions.Invoking(() => AssetBodies.FilterValues(Json($"{{\"propsAndValues\":[{{\"Key\":\"{PropertyId}\",\"Value\":\"a\"}},{{\"Key\":\"{PropertyId}\",\"Value\":\"b\"}}]}}")))
            .Should().Throw<LegacyBodyException>();
        AssetBodies.FilterValues(Json("{}")).Should().BeNull();
    }

    [Theory]
    [InlineData("{\"key\":\"" + AssetId + "\"}")]
    [InlineData("{\"key\":null,\"value\":1}")]
    [InlineData("{\"key\":\"" + AssetId + "\",\"value\":null}")]
    [InlineData("{\"Key\":\"" + AssetId + "\",\"Value\":1}")]
    [InlineData("{\"key\":\"" + AssetId + "\",\"value\":\"DEV\"}")]
    public void MigrateBody_RequiresLowercaseNonNullKeyAndValue_K(string json) =>
        FluentActions.Invoking(() => MigrateBody.Read(Json(json))).Should().Throw<LegacyBodyException>();

    [Fact]
    public void MigrateBody_QuotedTarget_AndNullBodyDefault_K()
    {
        MigrateBody.Read(Json("{\"key\":\"" + AssetId + "\",\"value\":\"1\"}")).Should().Be((new Guid(AssetId), 1));
        MigrateBody.Read(null).Should().Be((Guid.Empty, 0));
    }

    [Fact]
    public void MethodNotAllowedPage_IsTheHostTemplate_WithTheHelpLink()
    {
        var page = LegacyWire.MethodNotAllowedPage("http://host/Service.svc/help");

        // Measured: the host's page is 1102 bytes plus its help URL.
        page.Should().HaveLength(1102 + "http://host/Service.svc/help".Length);
        page.Should().Contain("<B>Error Description:</B> 'HTTP Method not allowed'</P>").And.Contain("<A HREF=\"http://host/Service.svc/help\">service help page</A>");
    }
}
