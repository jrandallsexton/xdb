using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using FluentValidation.Results;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xdb.Api.Application.Assets.Commands.CreateAsset;
using Xdb.Api.Application.AssetTypes.Commands.CreateAssetType;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects;
using Xdb.Api.Application.PickLists.Commands.CreatePickList;
using Xdb.Api.Application.Properties.Commands.CreateProperty;
using Xdb.Api.Infrastructure.Data;
using Xdb.Api.Tests.Integration.Application.Setup;
using Xdb.Core.Common;

using Xunit;

using DataType = Xdb.Api.Application.Common.Metadata.DataType;

namespace Xdb.Api.Tests.Integration.Application.Assets;

/// <summary>
/// CreateAsset against a fresh install, over a Venue-shaped type built with the other slices: one definition, instances
/// carrying string, number, date and pick list values, merged into the type's gen tables.
/// </summary>
public class CreateAssetCommandHandlerTests(FreshInstallFixture fixture) : IClassFixture<FreshInstallFixture>
{
    private sealed record Venue(Guid TypeId, Guid Definition, Guid City, Guid Capacity, Guid Latitude, Guid Opened, Guid Surface, Guid Grass,
        Guid Artificial, Guid Code, Guid Tags, Guid OtherListValue);

    [Fact]
    public async Task CreatesAnInstance_WithItsValues_InOneApprovedSubmittal()
    {
        var venue = await BuildVenueAsync();
        var command = Instance(venue,
            new CreateAssetValue(venue.City, "Columbus"),
            new CreateAssetValue(venue.Capacity, "12500"),
            new CreateAssetValue(venue.Latitude, "32.4520219"),
            new CreateAssetValue(venue.Opened, "1916-10-01"),
            new CreateAssetValue(venue.Surface, venue.Grass.ToString()));

        var result = await ExecuteAsync(command);

        result.Should().BeOfType<Success<CreateAssetResult>>();
        result.Status.Should().Be(ResultStatus.Created);
        result.Value.Id.Should().Be(command.Id!.Value);
        result.Value.Failures.Should().BeEmpty();

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<XdbDataContext>();
        var asset = await db.Assets.AsNoTracking().SingleAsync(a => a.Id == result.Value.Id);
        asset.Name.Should().Be(command.Name);
        asset.DisplayValue.Should().BeNull("readers show ISNULL(DisplayValue, Name); an empty string would hide the name");
        asset.Description.Should().BeNull();
        asset.AssetTypeId.Should().Be(venue.TypeId);
        asset.IsInstance.Should().BeTrue();
        asset.InstanceOfId.Should().Be(venue.Definition);
        asset.CreatedBy.Should().Be(SystemMemberIds.Admin);
        asset.ApprovedBy.Should().Be(SystemMemberIds.Admin);
        asset.Created.Should().BeCloseTo(fixture.Services.GetRequiredService<IDateTimeProvider>().UtcNow(), TimeSpan.FromMinutes(2));

        var submittal = await db.PropertyValueSubmittals.AsNoTracking().SingleAsync(s => s.AssetId == result.Value.Id);
        submittal.ReviewedBy.Should().Be(SystemMemberIds.Admin);

        var values = await db.PropertyValues.AsNoTracking().Where(v => v.AssetId == result.Value.Id).ToListAsync();
        values.Should().OnlyContain(v => v.SubmittalGroupId == submittal.Id && v.Approved != null && v.ApprovedBy == SystemMemberIds.Admin);
        values.ToDictionary(v => v.PropertyId, v => v.Value).Should().BeEquivalentTo(new Dictionary<Guid, string?>
        {
            [SystemPropertyIds.AssetName] = command.Name,
            [venue.City] = "Columbus",
            [venue.Capacity] = "12500",
            [venue.Latitude] = "32.4520219",
            [venue.Opened] = "1916-10-01",
            [venue.Surface] = venue.Grass.ToString("D")
        });
    }

    [Fact]
    public async Task NewInstance_IsInTheGenTable_WithDecodedValues()
    {
        var venue = await BuildVenueAsync();

        var result = await ExecuteAsync(Instance(venue,
            new CreateAssetValue(venue.City, "Columbus"),
            new CreateAssetValue(venue.Surface, venue.Artificial.ToString())));

        var table = GeneratedObjectNames.Table(venue.TypeId, isInstance: true, isRaw: false);
        (await ScalarAsync<string>($"SELECT [City] FROM [dbo].[{table}] WHERE [AssetId] = '{result.Value.Id:D}'")).Should().Be("Columbus");
        (await ScalarAsync<string>($"SELECT [Surface] FROM [dbo].[{table}] WHERE [AssetId] = '{result.Value.Id:D}'")).Should().Be("Artificial turf");
    }

    [Fact]
    public async Task Definition_IsCreatedWithoutValues()
    {
        var venue = await BuildVenueAsync();

        var result = await ExecuteAsync(new CreateAssetCommand
        {
            ActorId = SystemMemberIds.Admin, AssetTypeId = venue.TypeId, Name = "Arena", Values = [new CreateAssetValue(venue.Code, "ARN")]
        });

        result.Status.Should().Be(ResultStatus.Created);
        await using var scope = fixture.Services.CreateAsyncScope();
        var asset = await scope.ServiceProvider.GetRequiredService<XdbDataContext>().Assets.AsNoTracking().SingleAsync(a => a.Id == result.Value.Id);
        asset.IsInstance.Should().BeFalse();
        asset.InstanceOfId.Should().BeNull();
    }

    [Fact]
    public async Task MultiValueProperty_KeepsItsOrder()
    {
        var venue = await BuildVenueAsync();

        var result = await ExecuteAsync(Instance(venue, new CreateAssetValue(venue.Tags, "historic"), new CreateAssetValue(venue.Tags, "renovated")));

        await using var scope = fixture.Services.CreateAsyncScope();
        (await scope.ServiceProvider.GetRequiredService<XdbDataContext>().PropertyValues.AsNoTracking()
                .Where(v => v.AssetId == result.Value.Id && v.PropertyId == venue.Tags).OrderBy(v => v.Order).Select(v => v.Value).ToListAsync())
            .Should().Equal("historic", "renovated");
    }

    [Fact]
    public async Task BadValues_FailValidation_AndWriteNothing()
    {
        var venue = await BuildVenueAsync();
        var command = Instance(venue,
            new CreateAssetValue(venue.Capacity, "lots"),
            new CreateAssetValue(venue.Surface, venue.OtherListValue.ToString()),
            new CreateAssetValue(venue.Code, "ARN"),
            new CreateAssetValue(venue.City, "Columbus"),
            new CreateAssetValue(venue.City, "Phenix City"));

        var result = await ExecuteAsync(command);

        result.Status.Should().Be(ResultStatus.Validation);
        ErrorsOf(result).Select(e => e.PropertyName).Should().Equal("Values[0]", "Values[1]", "Values[2]", "Values[4]");
        await using var scope = fixture.Services.CreateAsyncScope();
        (await scope.ServiceProvider.GetRequiredService<XdbDataContext>().Assets.AsNoTracking().AnyAsync(a => a.Id == command.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task InstanceOfAMissingDefinition_FailsValidation()
    {
        var venue = await BuildVenueAsync();

        var result = await ExecuteAsync(Instance(venue) with { InstanceOfId = Guid.NewGuid() });

        ErrorsOf(result).Should().ContainSingle(e => e.PropertyName == nameof(CreateAssetCommand.InstanceOfId));
    }

    [Fact]
    public async Task InstanceOfATypeWithoutInstances_FailsValidation()
    {
        var typeId = await CreateTypeAsync(instanceLabel: null);
        var definition = (await ExecuteAsync(new CreateAssetCommand { ActorId = SystemMemberIds.Admin, AssetTypeId = typeId, Name = "Only" })).Value.Id;

        var result = await ExecuteAsync(new CreateAssetCommand
        {
            ActorId = SystemMemberIds.Admin, AssetTypeId = typeId, Name = "Nope", InstanceOfId = definition
        });

        ErrorsOf(result).Should().ContainSingle(e => e.PropertyName == nameof(CreateAssetCommand.InstanceOfId));
    }

    [Fact]
    public async Task GroupingType_HoldsNoAssets()
    {
        var typeId = await CreateTypeAsync(instanceLabel: "Item", allowAssets: false);

        var result = await ExecuteAsync(new CreateAssetCommand { ActorId = SystemMemberIds.Admin, AssetTypeId = typeId, Name = "Nope" });

        ErrorsOf(result).Should().ContainSingle(e => e.PropertyName == nameof(CreateAssetCommand.AssetTypeId));
    }

    [Fact]
    public async Task TakenId_IsAConflict()
    {
        var venue = await BuildVenueAsync();
        var command = Instance(venue);
        await ExecuteAsync(command);

        (await ExecuteAsync(command)).Status.Should().Be(ResultStatus.Conflict);
    }

    private static CreateAssetCommand Instance(Venue venue, params CreateAssetValue[] values) => new()
    {
        ActorId = SystemMemberIds.Admin,
        Id = Guid.NewGuid(),
        AssetTypeId = venue.TypeId,
        Name = "A.J. McClung Stadium",
        InstanceOfId = venue.Definition,
        Values = values
    };

    private static List<ValidationFailure> ErrorsOf(Result<CreateAssetResult> result) => ((Failure<CreateAssetResult>)result).Errors;

    /// <summary>Surface pick list, instance properties (one multi-value), a definition property, the type and its definition.</summary>
    private async Task<Venue> BuildVenueAsync()
    {
        var grass = Guid.NewGuid();
        var artificial = Guid.NewGuid();
        var surfaceList = await RunAsync<ICreatePickListCommandHandler, CreatePickListCommand, Guid>(new CreatePickListCommand
        {
            ActorId = SystemMemberIds.Admin,
            Name = $"Surface {Guid.NewGuid():N}",
            Description = "Playing surface",
            Values = [new CreatePickListValue { Id = grass, Value = "Grass" }, new CreatePickListValue { Id = artificial, Value = "Artificial", DisplayValue = "Artificial turf" }]
        }, (h, c) => h.ExecuteAsync(c));
        var otherValue = Guid.NewGuid();
        await RunAsync<ICreatePickListCommandHandler, CreatePickListCommand, Guid>(new CreatePickListCommand
        {
            ActorId = SystemMemberIds.Admin, Name = $"Other {Guid.NewGuid():N}", Description = "Unrelated", Values = [new CreatePickListValue { Id = otherValue, Value = "Grass" }]
        }, (h, c) => h.ExecuteAsync(c));

        var city = await CreatePropertyAsync("City", DataType.String);
        var capacity = await CreatePropertyAsync("Capacity", DataType.Int);
        var latitude = await CreatePropertyAsync("Latitude", DataType.Float);
        var opened = await CreatePropertyAsync("Opened", DataType.Date);
        var surface = await CreatePropertyAsync("Surface", DataType.PickList, surfaceList);
        var tags = await CreatePropertyAsync("Tags", DataType.String, allowMultiValue: true);
        var code = await CreatePropertyAsync("Code", DataType.String);

        var typeId = await CreateTypeAsync("Venue",
            [.. new[] { city, capacity, latitude, opened, surface, tags }.Select(p => new CreateAssetTypeProperty(p, IsInstance: true)),
             new CreateAssetTypeProperty(code, IsInstance: false)]);
        var definition = (await ExecuteAsync(new CreateAssetCommand { ActorId = SystemMemberIds.Admin, AssetTypeId = typeId, Name = "Venue" })).Value.Id;

        return new Venue(typeId, definition, city, capacity, latitude, opened, surface, grass, artificial, code, tags, otherValue);
    }

    private async Task<Guid> CreateTypeAsync(string? instanceLabel, IReadOnlyList<CreateAssetTypeProperty>? properties = null, bool allowAssets = true)
    {
        var result = await RunAsync<ICreateAssetTypeCommandHandler, CreateAssetTypeCommand, CreateAssetTypeResult>(new CreateAssetTypeCommand
        {
            ActorId = SystemMemberIds.Admin,
            Name = $"Venue {Guid.NewGuid():N}",
            Pluralization = "Venues",
            DefinitionLabel = "Venue Kind",
            InstanceLabel = instanceLabel,
            AllowAssets = allowAssets,
            Properties = properties ?? []
        }, (h, c) => h.ExecuteAsync(c));
        return result.Id;
    }

    private Task<Guid> CreatePropertyAsync(string name, DataType type, Guid? pickListId = null, bool allowMultiValue = false) =>
        RunAsync<ICreatePropertyCommandHandler, CreatePropertyCommand, Guid>(new CreatePropertyCommand
        {
            ActorId = SystemMemberIds.Admin, Name = name, DataType = type, PickListId = pickListId, AllowMultiValue = allowMultiValue
        }, (h, c) => h.ExecuteAsync(c));

    private async Task<TResult> RunAsync<THandler, TCommand, TResult>(TCommand command, Func<THandler, TCommand, Task<Result<TResult>>> execute)
        where THandler : notnull
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var result = await execute(scope.ServiceProvider.GetRequiredService<THandler>(), command);
        result.Status.Should().Be(ResultStatus.Created);
        return result.Value;
    }

    private async Task<Result<CreateAssetResult>> ExecuteAsync(CreateAssetCommand command)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ICreateAssetCommandHandler>().ExecuteAsync(command);
    }

    private async Task<T> ScalarAsync<T>(string sql)
    {
        await using var connection = new SqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }
}
