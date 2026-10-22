using System;

using FluentAssertions;

using Moq;

using Xdb.Api.Application.Metadata.Legacy;
using Xdb.Api.Application.Views;
using Xdb.Core.Common;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.Views;

public class PropertyEditsTests
{
    private static readonly DateTime StoredLastModified = new(2012, 3, 4, 5, 6, 0);
    private static readonly DateTime NowUtc = new(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

    private readonly PropertyEdits _edits;

    public PropertyEditsTests()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow()).Returns(NowUtc);
        _edits = new PropertyEdits(clock.Object);
    }

    private static PropertyContract Loaded() => new() { DisplayValue = "Name", IsReadOnly = true, LastModified = StoredLastModified };

    [Fact]
    public void UnchangedValues_LeaveThePropertyClean()
    {
        var property = Loaded();

        _edits.DisplayValue(property, "Name");
        _edits.IsReadOnly(property, true);
        _edits.IsInherited(property, false);

        property.IsDirty.Should().BeFalse();
        property.LastModified.Should().Be(StoredLastModified);
    }

    [Fact]
    public void FirstChange_MarksDirtyAndStampsLocalNow_LaterChangesKeepIt()
    {
        var property = Loaded();

        _edits.IsReadOnly(property, false);
        var stamped = property.LastModified;
        _edits.DisplayValue(property, "Other");

        property.IsDirty.Should().BeTrue();
        stamped.Should().Be(NowUtc.ToLocalTime());
        property.LastModified.Should().Be(stamped);
        property.DisplayValue.Should().Be("Other");
    }

    [Fact]
    public void AssetTypeId_FromNull_IsAChange()
    {
        var property = Loaded();
        var id = Guid.NewGuid();

        _edits.AssetTypeId(property, id);

        property.IsDirty.Should().BeTrue();
        property.AssetTypeId.Should().Be(id);
    }

    [Fact]
    public void ViewClientProperty_AlwaysHasATriggerList()
    {
        ViewLoader.PropertyClient(new PropertyContract { Triggers = null }).Triggers.Should().BeEmpty();
    }
}
