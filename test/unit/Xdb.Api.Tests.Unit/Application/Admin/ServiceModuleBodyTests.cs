using System;
using System.Text.Json;

using FluentAssertions;

using Xdb.Api.Application.Admin.Legacy;
using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Common.Legacy;
using Xdb.Core.LegacyWire;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.Admin;

/// <summary>The ServiceModuleMetadata body as the original serializer bound it (spec M9 0.3, FX S cases) and the 404 page.</summary>
public class ServiceModuleBodyTests
{
    private static readonly DcjsReader Reader = new(() => new DateTime(2026, 10, 23, 9, 0, 0, DateTimeKind.Local));

    private static string Body(string assemblyData = "null", string name = "\"m9\"") =>
        "{\"AssemblyData\":" + assemblyData + ",\"AssemblyName\":\"a\",\"AssemblyNamespace\":\"n\",\"AssemblyVersion\":\"1\",\"ClassName\":\"c\","
        + "\"Created\":\"\\/Date(1382500000000-0400)\\/\",\"Deleted\":null,\"Description\":\"d\",\"Downloaded\":null,"
        + "\"Id\":\"9e9e0001-0000-4000-8000-000000000201\",\"IsActive\":true,\"IsDirty\":false,\"IsNew\":true,\"LastCompleted\":null,\"LastRan\":null,"
        + "\"Name\":" + name + "}";

    private static ServiceModuleMetadataContract Read(string json) => Reader.Read<ServiceModuleMetadataContract>(JsonDocument.Parse(json).RootElement);

    [Fact]
    public void ByteArray_FromNumbers_S4() => Read(Body("[1,2,255]")).AssemblyData.Should().Equal(1, 2, 255);

    [Fact]
    public void ByteArray_FromObject_IsEmpty_S7() => Read(Body("{}")).AssemblyData.Should().BeEmpty();

    [Theory]
    [InlineData("\"AQID\"")]   // S5 base64
    [InlineData("[256]")]      // S6
    [InlineData("[1.5]")]
    public void ByteArray_Rejects(string value) =>
        FluentActions.Invoking(() => Read(Body(value))).Should().Throw<LegacyBodyException>();

    [Theory]
    [InlineData(50, 50)]   // S16
    [InlineData(51, 49)]   // S17
    [InlineData(60, 49)]   // S9
    public void Name_IsCutAbove50(int length, int expected) =>
        Read(Body(name: "\"" + new string('n', length) + "\"")).Name.Should().HaveLength(expected);

    [Fact]
    public void MissingMember_Fails_S19() =>
        FluentActions.Invoking(() => Read(Body().Replace("\"Id\":\"9e9e0001-0000-4000-8000-000000000201\",", string.Empty)))
            .Should().Throw<LegacyBodyException>();

    [Fact]
    public void NotFoundPage_IsTheMethodNotAllowedPageWithAnotherDescription() =>
        LegacyWire.NotFoundPage("http://h/help").Should().Be(
            LegacyWire.MethodNotAllowedPage("http://h/help").Replace("'HTTP Method not allowed'", "'Resource does not exist'"));
}
