using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Assets.Writes;
using Xdb.Api.Infrastructure.Background;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.Assets;

/// <summary>The statement order of the two-pass submittal save (spec M6 2.3, corrected from the DLL for skipped values).</summary>
public class SubmittalEngineTests
{
    private static readonly Guid UserId = new("cccccccc-0000-0000-0000-000000000001");
    private static readonly Guid AssetId = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid PropertyId = new("bbbbbbbb-0000-0000-0000-000000000001");
    private const int StringDataType = 6;

    private readonly List<string> _statements = [];
    private readonly Mock<IAssetWriteProcedures> _writes = new();
    private readonly Mock<IMetadataProcedures> _metadata = new();

    private SubmittalEngine Engine(bool identicalValueStored)
    {
        _metadata.Setup(m => m.GetPropertiesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PropertyDetailRow { Id = PropertyId, Name = "P", DataTypeId = StringDataType, AllowMultiValue = false, IsSystem = false }]);
        _metadata.Setup(m => m.IsLiveMemberIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _writes.Setup(w => w.SaveSubmittalAsync(It.IsAny<Guid>(), AssetId, It.IsAny<DateTime>(), It.IsAny<DateTime?>(), UserId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .Callback(() => _statements.Add("submittal")).Returns(Task.CompletedTask);
        _writes.Setup(w => w.ValueExistsAsync(PropertyId, AssetId, It.IsAny<string?>(), false, It.IsAny<CancellationToken>()))
            .Callback(() => _statements.Add("exists")).ReturnsAsync(identicalValueStored);
        _writes.Setup(w => w.DeleteValuesForAssetAsync(PropertyId, UserId, AssetId, It.IsAny<CancellationToken>()))
            .Callback(() => _statements.Add("delete-for-asset")).Returns(Task.CompletedTask);
        _writes.Setup(w => w.SaveValueAsync(It.IsAny<SubmittalValue>(), It.IsAny<CancellationToken>()))
            .Callback(() => _statements.Add("value")).Returns(Task.CompletedTask);
        _writes.Setup(w => w.MarkAsUpdatedAsync(AssetId, UserId, It.IsAny<CancellationToken>()))
            .Callback(() => _statements.Add("mark-updated")).Returns(Task.CompletedTask);

        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow()).Returns(new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc));
        var background = new Mock<IBackgroundWorkQueue>();
        var assets = new AssetWriter(_writes.Object, new Mock<IAssetProcedures>().Object, _metadata.Object, background.Object);
        return new SubmittalEngine(_writes.Object, new Mock<IAssetProcedures>().Object, _metadata.Object, assets, background.Object, clock.Object);
    }

    private static Submittal OneValue()
    {
        var submittal = new Submittal(Guid.NewGuid(), AssetId, DateTime.Now, UserId);
        submittal.Values.Add(new SubmittalValue { Id = Guid.NewGuid(), PropertyId = PropertyId, AssetId = AssetId, SubmittalGroupId = submittal.Id, Value = "v", CreatedBy = UserId });
        return submittal;
    }

    [Fact]
    public async Task NewValue_IsWrittenInBothPasses_ThenTheAssetIsMarkedUpdated()
    {
        (await Engine(identicalValueStored: false).SaveAsync(OneValue(), isAutoApprove: true, UserId, CancellationToken.None)).Should().BeTrue();

        _statements.Should().Equal("submittal", "exists", "delete-for-asset", "value", "submittal", "delete-for-asset", "value", "mark-updated");
    }

    [Fact]
    public async Task IdenticalStoredValue_IsSkippedInTheFirstPassOnly()
    {
        (await Engine(identicalValueStored: true).SaveAsync(OneValue(), isAutoApprove: true, UserId, CancellationToken.None)).Should().BeTrue();

        // The list marks the skipped value saved, so the approval pass deletes the identical row and inserts this one.
        _statements.Should().Equal("submittal", "exists", "submittal", "delete-for-asset", "value", "mark-updated");
    }
}
