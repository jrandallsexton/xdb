using System;
using System.Text;

using FluentAssertions;

using Xdb.Compare.Har;

using Xunit;

namespace Xdb.Compare.Tests.Unit.Har;

public class HarReaderTests
{
    private static readonly string Har = $$"""
        {
          "log": {
            "entries": [
              {
                "request": { "method": "GET", "url": "http://localhost/app/page.html", "headers": [] },
                "response": { "status": 200, "headers": [], "content": { "text": "<html/>" } }
              },
              {
                "request": {
                  "method": "GET",
                  "url": "http://localhost/svc/Service.svc/Report_GetData?reportId=x&_=123",
                  "headers": []
                },
                "response": {
                  "status": 200,
                  "headers": [ { "name": "Content-Type", "value": "text/html; charset=utf-8" } ],
                  "content": { "text": "{\"page\":1}" }
                }
              },
              {
                "request": {
                  "method": "POST",
                  "url": "http://localhost/svc/Service.svc/Role_Save",
                  "headers": [ { "name": "content-type", "value": "application/json; charset=utf-8" } ],
                  "postData": { "text": "{\"Id\":1}" }
                },
                "response": {
                  "status": 500,
                  "headers": [],
                  "content": { "text": "{{Convert.ToBase64String(Encoding.UTF8.GetBytes("false"))}}", "encoding": "base64" }
                }
              }
            ]
          }
        }
        """;

    [Fact]
    public void ReadsOnlyServiceCalls_WithOperationAndRelativeUrl()
    {
        var calls = HarReader.Parse(Har);

        calls.Should().HaveCount(2);
        calls[0].Operation.Should().Be("Report_GetData");
        calls[0].RelativeUrl.Should().Be("Report_GetData?reportId=x&_=123");
        calls[0].RecordedContentType.Should().Be("text/html; charset=utf-8");
        calls[0].RecordedBody.Should().Be("""{"page":1}""");
    }

    [Fact]
    public void ReadsPostBodies_AndDecodesBase64Responses()
    {
        var call = HarReader.Parse(Har)[1];

        call.Method.Should().Be("POST");
        call.RequestBody.Should().Be("""{"Id":1}""");
        call.RequestContentType.Should().Be("application/json; charset=utf-8");
        call.RecordedStatus.Should().Be(500);
        call.RecordedBody.Should().Be("false");
    }
}
