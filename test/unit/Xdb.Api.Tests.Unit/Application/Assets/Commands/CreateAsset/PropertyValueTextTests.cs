using System;

using FluentAssertions;

using Xdb.Api.Application.Assets.Commands.CreateAsset;
using Xdb.Api.Application.Common.Metadata;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.Assets.Commands.CreateAsset;

public class PropertyValueTextTests
{
    [Theory]
    [InlineData(DataType.String, " Columbus ", " Columbus ")]
    [InlineData(DataType.Memo, "two\nlines", "two\nlines")]
    [InlineData(DataType.Int, "31901", "31901")]
    [InlineData(DataType.Int, "-7", "-7")]
    [InlineData(DataType.Float, "32.4520219", "32.4520219")]
    [InlineData(DataType.Float, "-84.9849118", "-84.9849118")]
    [InlineData(DataType.Float, "1e3", "1000")]
    [InlineData(DataType.Date, "2025-08-25", "2025-08-25")]
    [InlineData(DataType.DateTime, "2025-08-25T06:34:07", "2025-08-25T06:34:07Z")]
    [InlineData(DataType.DateTime, "2025-08-25T06:34:07-04:00", "2025-08-25T10:34:07Z")]
    public void ValidValues_AreStoredInCanonicalForm(DataType type, string value, string stored)
    {
        PropertyValueText.Normalize(type, value, out var error).Should().Be(stored);
        error.Should().BeNull();
    }

    [Theory]
    [InlineData(DataType.PickList)]
    [InlineData(DataType.Asset)]
    [InlineData(DataType.User)]
    public void ReferenceValues_AreStoredAsDashedIds(DataType type)
    {
        var id = Guid.NewGuid();

        PropertyValueText.Normalize(type, id.ToString("N").ToUpperInvariant(), out _).Should().Be(id.ToString("D"));
    }

    [Theory]
    [InlineData(DataType.Int, "12.5")]
    [InlineData(DataType.Int, "1,000")]
    [InlineData(DataType.Float, "32,45")]
    [InlineData(DataType.Float, "NaN")]
    [InlineData(DataType.Date, "8/25/2025")]
    [InlineData(DataType.Date, "2025-02-30")]
    [InlineData(DataType.DateTime, "yesterday")]
    [InlineData(DataType.PickList, "Grass")]
    public void InvalidValues_AreRejected(DataType type, string value)
    {
        PropertyValueText.Normalize(type, value, out var error).Should().BeNull();
        error.Should().Contain(value);
    }

    [Theory]
    [InlineData(DataType.Currency)]
    [InlineData(DataType.Url)]
    [InlineData(DataType.Document)]
    [InlineData(DataType.Dependency)]
    public void UnsupportedTypes_AreRejected(DataType type)
    {
        PropertyValueText.IsSupported(type).Should().BeFalse();
        PropertyValueText.Normalize(type, "x", out var error).Should().BeNull();
        error.Should().Contain("not supported");
    }
}
