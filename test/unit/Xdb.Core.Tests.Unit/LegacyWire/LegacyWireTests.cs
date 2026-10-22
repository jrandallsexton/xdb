using System;
using System.Security.Cryptography;
using System.Text;

using FluentAssertions;

using Xunit;

namespace Xdb.Core.Tests.Unit.LegacyWire;

public class LegacyWireTests
{
    [Fact]
    public void Typed_EscapesControlCharactersAsTheOriginal()
    {
        var result = (Core.LegacyWire.LegacyBytesResult)Core.LegacyWire.LegacyWire.Typed("a\r\n\t\u0001\b\f/\"\\n\u00e9");

        Encoding.UTF8.GetString(result.Body).Should().Be("\"a\\u000d\\u000a\\u0009\\u0001\\u0008\\u000c\\/\\\"\\\\n\u00e9\"");
    }

    [Fact]
    public void RequestErrorPage_IsTheHostsExactPage()
    {
        var bytes = Encoding.ASCII.GetBytes(Core.LegacyWire.LegacyWire.RequestErrorPage);

        bytes.Should().HaveCount(1047);
        Convert.ToHexStringLower(MD5.HashData(bytes)).Should().Be("96f84a4c2c926fc57bda38cf5bdfaed1");
    }
}
