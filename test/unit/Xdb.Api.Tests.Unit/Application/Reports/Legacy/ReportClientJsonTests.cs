using System;

using FluentAssertions;

using Xdb.Api.Application.Reports.Legacy;
using Xdb.Api.Application.Reports.Queries.GetReportClient.Dtos;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.Reports.Legacy;

public class ReportClientJsonTests
{
    [Fact]
    public void MemberOrderNullsAndRawId_MatchTheOriginal()
    {
        var dto = new ReportClientDto("Assets", ["Asset Code", null], null, null, "((Status = Active))");

        ReportClientJson.Serialize("ABC-raw", dto).Should().Be(
            "{\"Id\":\"ABC-raw\",\"Name\":\"Assets\",\"PropertyNames\":[\"Asset Code\",null],\"SubGridId\":null,\"SubGridJoinCol\":null," +
            "\"GroupColumns\":null,\"GroupColName\":null,\"FilterString\":\"((Status = Active))\"}");
    }

    [Fact]
    public void GroupColumns_AreLowercaseGuidStrings()
    {
        var dto = new ReportClientDto("R", [], [Guid.Parse("13C637C6-4924-42FC-9B88-F55B622324C3")], "Owner", "");

        ReportClientJson.Serialize("x", dto).Should().Contain("\"GroupColumns\":[\"13c637c6-4924-42fc-9b88-f55b622324c3\"],\"GroupColName\":\"Owner\"");
    }

    [Fact]
    public void Escaping_FollowsJsonNetDefaults()
    {
        // Quote and backslash escaped, control characters as \uXXXX (lowercase hex) or short forms,
        // slash and HTML characters written raw.
        var dto = new ReportClientDto("a\"b\\c/<&>\u0001\t", [], null, null, "");

        ReportClientJson.Serialize("x", dto).Should().Contain("\"Name\":\"a\\\"b\\\\c/<&>\\u0001\\t\"");
    }
}
