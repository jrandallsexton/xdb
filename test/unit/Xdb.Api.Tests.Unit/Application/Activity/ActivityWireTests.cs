using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

using FluentAssertions;

using Newtonsoft.Json;

using Xdb.Api.Application.Activity;
using Xdb.Api.Application.Activity.Legacy;
using Xdb.Api.Application.Assets.Legacy;
using Xdb.Core.LegacyWire;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.Activity;

/// <summary>Search output, trigger bodies and contract bytes from the FX measurements (spec M7-S appendix B).</summary>
public class ActivityWireTests
{
    private static readonly Guid A = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid B = new("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void SearchResults_J1()
    {
        var results = new List<SearchResultContract>
        {
            new() { Title = "T \"q\" <b> & / \\ é \u0001", Content = "c\r\n\t", Category = "Asset", Index = 0, Url = "", AssetId = A, AssetTypeId = B.ToString() },
            new() { Title = "t", Content = "x<b>y</b>", Category = "Asset", Index = 3, Url = "root/view.html?viewId=22222222-2222-2222-2222-222222222222&assetId=11111111-1111-1111-1111-111111111111",
                AssetId = A, AssetTypeId = B.ToString() }
        };

        JsonConvert.SerializeObject(results).Should().Be(
            """[{"Title":"T \"q\" <b> & / \\ é \u0001","Content":"c\r\n\t","Category":"Asset","Index":0,"Url":"","VisitCount":0,"LastVisit":null,"AssetId":"11111111-1111-1111-1111-111111111111","AssetTypeId":"22222222-2222-2222-2222-222222222222"},{"Title":"t","Content":"x<b>y</b>","Category":"Asset","Index":3,"Url":"root/view.html?viewId=22222222-2222-2222-2222-222222222222&assetId=11111111-1111-1111-1111-111111111111","VisitCount":0,"LastVisit":null,"AssetId":"11111111-1111-1111-1111-111111111111","AssetTypeId":"22222222-2222-2222-2222-222222222222"}]""");
    }

    [Fact]
    public void Tags_J4() =>
        JsonConvert.SerializeObject(new List<TagContract> { new() { Id = 1, Name = "n'1 \"x\"", Img = "http://example.com/img1.jpg", Type = "contact" } })
            .Should().Be("""[{"id":1,"name":"n'1 \"x\"","img":"http://example.com/img1.jpg","type":"contact"}]""");

    [Fact]
    public void ControlCharacters_J5() =>
        JsonConvert.SerializeObject("\u0000\b\u000b\u001f\u0085\u2028<>'&").Should().Be("\"\\u0000\\b\\u000b\\u001f\\u0085\\u2028<>'&\"");

    [Fact]
    public void EmptyList_J3() => JsonConvert.SerializeObject(new List<SearchResultContract>()).Should().Be("[]");

    [Theory]
    [InlineData("abc", "b", "a<b>b</b>c")]
    [InlineData("ABC", "bc", "A<b>BC</b>")]
    [InlineData("co-op", "coop", "co-op")]                // IX: a hyphen is significant
    [InlineData("abc", "", "<b></b>abc")]                  // IX: the empty phrase matches at 0
    [InlineData("a\r\nb", "a\nb", "a\r\nb")]               // IX
    public void Highlight(string text, string phrase, string expected) => SearchQueryHandler.Highlight(text, phrase).Should().Be(expected);

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement;

    private const string Members = "\"id\":\"11111111-1111-1111-1111-111111111111\",\"name\":\"n\",\"description\":\"d\"";

    [Theory]
    [InlineData("{" + Members + ""","type":0,"assetTypeIds":["22222222-2222-2222-2222-222222222222"],"specific":false,"properties":null,"filters":[],"filterLogic":"x","requestType":0}""")] // T1
    [InlineData("{" + Members + ""","type":99,"assetTypeIds":[],"specific":false,"properties":null}""")]          // T7
    [InlineData("{" + Members + ""","type":"3","assetTypeIds":[],"specific":false,"properties":null}""")]         // T8
    [InlineData("{" + Members + ""","type":0,"assetTypeIds":null,"specific":false,"properties":null}""")]         // T10
    [InlineData("""{"id":null,"name":null,"description":null,"type":0,"assetTypeIds":[],"specific":false,"properties":null}""")] // T11
    [InlineData("""{"id":5,"name":5,"description":"d","type":0,"assetTypeIds":[5],"specific":false,"properties":null}""")]      // T12
    [InlineData("{" + Members + ""","type":0,"assetTypeIds":[],"specific":"true","properties":null}""")]          // T19
    [InlineData("{" + Members + ""","type":0,"assetTypeIds":[null],"specific":false,"properties":null}""")]       // T20
    [InlineData("{" + Members + ""","type":0,"assetTypeIds":{"a":1},"specific":false,"properties":null}""")]      // T22
    [InlineData("{" + Members + ""","type":0,"assetTypeIds":[],"specific":false,"properties":[null]}""")]         // T23
    [InlineData("{" + Members + ""","type":-1,"assetTypeIds":[],"specific":false,"properties":[{"propertyId":"a","operatorId":"b","value":"v"}]}""")] // T13, T24
    public void Trigger_Binds(string body) => ActivityBodies.Trigger(Json(body)).Should().NotBeNull();

    [Theory]
    [InlineData("""{"id":"x","name":"n","type":0,"assetTypeIds":[],"specific":false,"properties":null}""")]                       // T3
    [InlineData("""{"id":"x","name":"n","description":"d","type":0,"assetTypeIds":[],"specific":false}""")]                        // T4
    [InlineData("{" + Members + ""","type":0,"assetTypeIds":[],"specific":null,"properties":null}""")]                             // T5
    [InlineData("{" + Members + ""","type":null,"assetTypeIds":[],"specific":false,"properties":null}""")]                         // T6
    [InlineData("{" + Members + ""","type":"Modified","assetTypeIds":[],"specific":false,"properties":null}""")]                   // T9
    [InlineData("{" + Members + ""","type":0,"assetTypeIds":[],"specific":false,"properties":[{"propertyId":"a","operatorId":"b"}]}""")] // T14
    [InlineData("""{"Id":"x","Name":"n","Description":"d","Type":0,"AssetTypeIds":[],"Specific":false,"Properties":null}""")]      // T15
    [InlineData("{}")]                                                                                                              // T16
    [InlineData("[]")]                                                                                                              // T17
    public void Trigger_Rejects(string body) =>
        FluentActions.Invoking(() => ActivityBodies.Trigger(Json(body))).Should().Throw<LegacyBodyException>();

    [Fact]
    public void Trigger_Values()
    {
        var body = ActivityBodies.Trigger(Json("""{"id":5,"name":"n","description":"d","type":"3","assetTypeIds":[5,null],"specific":"true","properties":null}"""))!;
        body.Should().BeEquivalentTo(new TriggerBody("5", "n", "d", 3, ["5", null], true));
        ActivityBodies.Trigger(null).Should().BeNull();
    }

    [Fact]
    public void Summary_ValuesAreADictionaryOfPairs()
    {
        var summary = new SubmittalSummaryContract
        {
            AssetId = "a", AssetName = "n", AssetTypeId = "t", Created = "10/3/2013 9:41:00 AM", ViewId = "v",
            Values = new Dictionary<string, KeyValuePair<string, string>> { ["p"] = new("old", "new") }
        };

        Encoding.UTF8.GetString(LegacyWire.TypedBytes(summary)).Should().Be(
            """{"assetId":"a","assetName":"n","assetTypeId":"t","created":"10\/3\/2013 9:41:00 AM","values":[{"Key":"p","Value":{"key":"old","value":"new"}}],"viewId":"v"}""");
    }

    [Fact]
    public void Trigger_AssetTypesSerializeAsEmptyObjects()
    {
        var json = Encoding.UTF8.GetString(LegacyWire.TypedBytes(new TriggerContract { AssetTypes = [new EmptyContract()] }));

        json.Should().StartWith("""{"AssetTypes":[{}],"Assets":[],"Created":""");
        json.Should().Contain("\"FilterLogic\":null");
    }
}
