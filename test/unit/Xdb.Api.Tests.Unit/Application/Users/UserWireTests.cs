using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

using FluentAssertions;

using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Users.Legacy;
using Xdb.Core.LegacyWire;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.Users;

/// <summary>Request bodies and response bytes of the member operations, from the FX measurements (spec M7-U appendix B).</summary>
public class UserWireTests
{
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement;

    [Theory]
    [InlineData("""{"key":"11111111-1111-1111-1111-111111111111","value":true}""", "11111111-1111-1111-1111-111111111111", true)]   // P1
    [InlineData("""{"key":"11111111-1111-1111-1111-111111111111","value":"true"}""", "11111111-1111-1111-1111-111111111111", true)] // P2
    [InlineData("""{"key":"11111111-1111-1111-1111-111111111111","value":1}""", "11111111-1111-1111-1111-111111111111", true)]      // P3
    [InlineData("""{"key":null,"value":true}""", null, true)]                                                                        // P6
    [InlineData("""{"key":5,"value":false}""", "5", false)]                                                                          // P7
    [InlineData("""{"key":"zz","value":false,"x":1}""", "zz", false)]                                                                // P14
    public void Pin_Binds(string body, string? key, bool value) =>
        UserBodies.Pin(Json(body)).Should().Be((key, value));

    [Theory]
    [InlineData("""{"key":"a"}""")]                    // P4
    [InlineData("""{"value":true}""")]                 // P5
    [InlineData("""{"Key":"a","Value":true}""")]       // P8
    [InlineData("""{"key":"a","value":null}""")]       // P9
    [InlineData("{}")]                                  // P10
    [InlineData("[]")]                                  // P11
    [InlineData("""{"key":"a","value":"x"}""")]        // P13
    [InlineData("""{"key":"a","value":"True"}""")]     // P15
    public void Pin_Rejects(string body) =>
        FluentActions.Invoking(() => UserBodies.Pin(Json(body))).Should().Throw<LegacyBodyException>().Which.InnerMessage.Should().BeNull();

    [Fact]
    public void Pin_NullBody_IsTheDefaultPair() => UserBodies.Pin(null).Should().Be(((string?)null, false));

    [Theory]
    [InlineData("""[{"questionId":"a","responseValue":1,"responseText":null},{"questionId":"b","responseValue":0}]""", 2)] // R1, R2
    [InlineData("""[{"questionId":"a","responseValue":"0"}]""", 1)]   // R5
    [InlineData("[]", 0)]                                              // R9
    [InlineData("{}", 0)]                                              // R10
    [InlineData("""[null]""", 1)]                                      // R12
    [InlineData("""[{"questionId":null,"responseValue":1}]""", 1)]    // R13
    [InlineData("""[{"questionId":5,"responseValue":-1}]""", 1)]      // R14, R15
    [InlineData("""{"a":[1]}""", 0)]                                   // R19
    [InlineData("""[{"questionId":"a","responseValue":1,"responseText":5,"x":1},{"questionId":"a","responseValue":1}]""", 2)] // R17, R20, R21
    public void Responses_Bind(string body, int count) =>
        UserBodies.Responses(Json(body)).Should().HaveCount(count);

    [Fact]
    public void Responses_KeepTheirValues()
    {
        var responses = UserBodies.Responses(Json("""[{"questionId":5,"responseValue":"-1","responseText":7},null]"""))!;
        responses[0].Should().Be(new QuestionResponse("5", -1, "7"));
        responses[1].Should().BeNull();
    }

    [Theory]
    [InlineData("""[{"questionId":"a"}]""")]                                // R3
    [InlineData("""[{"responseValue":1}]""")]                               // R4
    [InlineData("""[{"questionId":"a","responseValue":70000}]""")]          // R6
    [InlineData("""[{"questionId":"a","responseValue":1.5}]""")]            // R7
    [InlineData("""[{"questionId":"a","responseValue":true}]""")]           // R8
    [InlineData("""[{"QuestionId":"a","ResponseValue":1}]""")]              // R16
    [InlineData("""[{"questionId":"a","responseValue":null}]""")]           // R18
    public void Responses_Reject(string body) =>
        FluentActions.Invoking(() => UserBodies.Responses(Json(body))).Should().Throw<LegacyBodyException>();

    [Fact]
    public void Responses_NullBody_IsNull() => UserBodies.Responses(null).Should().BeNull();

    /// <summary>DT1: an unset DateTime is the local (Eastern) offset of year 1 under Windows rules, not a local-mean-time offset.</summary>
    [Fact]
    [Trait("Requires", "EasternWindowsTimeZone")]
    public void UnsetDate_SerializesWithTheStandardEasternOffset()
    {
        var bytes = Encoding.UTF8.GetString(LegacyWire.TypedBytes(new QuestionGroupContract()));
        bytes.Should().Contain("\"Closed\":\"\\/Date(-62135578800000-0500)\\/\"");
    }

    [Fact]
    [Trait("Requires", "EasternWindowsTimeZone")]
    public void AlertClient_MatchesTheOriginalBytes()
    {
        var alert = new AlertClientContract
        {
            AssetId = "11111111-1111-1111-1111-111111111111", Id = "11111111-1111-1111-1111-111111111111", NewValue = null, OldValue = "", Pinned = true,
            PropertyId = "11111111-1111-1111-1111-111111111111", PropertyValueId = "33333333-3333-3333-3333-333333333333",
            SubmittalId = "22222222-2222-2222-2222-222222222222", TriggerId = "33333333-3333-3333-3333-333333333333",
            TriggerLogId = "22222222-2222-2222-2222-222222222222", TriggerText = "t"
        };

        Encoding.UTF8.GetString(LegacyWire.TypedBytes(alert)).Should().Be(
            """{"assetId":"11111111-1111-1111-1111-111111111111","created":"\/Date(-62135578800000-0500)\/","id":"11111111-1111-1111-1111-111111111111","newValue":null,"oldValue":"","pinned":true,"propertyId":"11111111-1111-1111-1111-111111111111","propertyValueId":"33333333-3333-3333-3333-333333333333","submittalId":"22222222-2222-2222-2222-222222222222","triggerId":"33333333-3333-3333-3333-333333333333","triggerLogId":"22222222-2222-2222-2222-222222222222","triggerText":"t","viewed":"\/Date(-62135578800000-0500)\/"}""");
    }

    [Fact]
    public void ErrorDefault_MatchesTheOriginalBytes() =>
        Encoding.UTF8.GetString(LegacyWire.TypedBytes(new ErrorContract())).Should().Be("""{"Exception":null,"Id":null,"Message":null}""");

    [Fact]
    public void UserClient_MembersInTheMeasuredOrder()
    {
        var client = new UserClientContract { Shortcuts = new Dictionary<string, string> { ["a"] = "b" } };

        var json = Encoding.UTF8.GetString(LegacyWire.TypedBytes(client));

        var names = new List<string>();
        foreach (var property in JsonDocument.Parse(json).RootElement.EnumerateObject())
            names.Add(property.Name);
        names.Should().Equal("AllowBulkUploads", "DisplayValue", "FirstName", "HiddenUIElements", "Id", "IsAdmin", "LastModified", "LastName", "Processes", "UserId",
            "Views", "allowBeta", "dashboards", "recent", "reports", "shortcuts", "showReleaseNotice");
        json.Should().Contain("\"shortcuts\":[{\"Key\":\"a\",\"Value\":\"b\"}]");
    }
}
