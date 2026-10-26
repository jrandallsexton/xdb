using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xdb.Api.Application.Assets.Commands.CreateAsset;
using Xdb.Api.Application.Assets.Commands.CreateAssets;
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
/// CreateAssets against a fresh install: a team type whose definitions are franchises and whose instances are seasons,
/// with a pick list and an Asset reference, written in batches. (Single-asset behavior is covered through CreateAsset,
/// which runs on this handler.)
/// </summary>
public class CreateAssetsCommandHandlerTests(FreshInstallFixture fixture) : IClassFixture<FreshInstallFixture>
{
    private sealed record Team(Guid TypeId, Guid Nickname, Guid Year, Guid Conference, Guid Big, Guid Small, Guid VenueType, Guid Venue);

    [Fact]
    public async Task DefinitionsAndTheirInstances_InOneBatch_AreCreated_AndInTheGenTables()
    {
        var team = await BuildTeamAsync();
        var franchise = Guid.NewGuid();
        var seasons = new[] { Guid.NewGuid(), Guid.NewGuid() };

        // Instances listed first: the handler writes definitions first.
        var result = await ExecuteAsync(team,
            Season(team, seasons[0], franchise, 2024, team.Big),
            Season(team, seasons[1], franchise, 2025, team.Small),
            Franchise(team, franchise, "Eagles"));

        result.Status.Should().Be(ResultStatus.Created);
        result.Value.Rejected.Should().BeEmpty();
        result.Value.Failures.Should().BeEmpty();
        result.Value.Created.Should().Equal(franchise, seasons[0], seasons[1]);

        var table = GeneratedObjectNames.Table(team.TypeId, isInstance: true, isRaw: false);
        (await ScalarAsync<string>($"SELECT [Conference] FROM [dbo].[{table}] WHERE [AssetId] = '{seasons[1]:D}'")).Should().Be("Small");
        (await ScalarAsync<int>($"SELECT COUNT(*) FROM [dbo].[{GeneratedObjectNames.Table(team.TypeId, isInstance: false, isRaw: false)}]")).Should().Be(1);
    }

    [Fact]
    public async Task MoreThanThePerAssetLimit_RebuildsTheGenTables()
    {
        var team = await BuildTeamAsync();
        var franchise = Guid.NewGuid();
        var count = CreateAssetsCommandHandler.PerAssetRefreshLimit + 1;
        var items = Enumerable.Range(0, count).Select(i => Season(team, Guid.NewGuid(), franchise, 1900 + i, team.Big)).Prepend(Franchise(team, franchise, "Owls"));

        var result = await ExecuteAsync(team, [.. items]);

        result.Value.Created.Should().HaveCount(count + 1);
        result.Value.Failures.Should().BeEmpty();
        var table = GeneratedObjectNames.Table(team.TypeId, isInstance: true, isRaw: false);
        (await ScalarAsync<int>($"SELECT COUNT(*) FROM [dbo].[{table}] WHERE [Conference] = 'Big'")).Should().Be(count);
    }

    [Fact]
    public async Task BadAssets_AreRejected_AndTheRestCreated()
    {
        var team = await BuildTeamAsync();
        var existing = Guid.NewGuid();
        await ExecuteAsync(team, Franchise(team, existing, "Hawks"));
        var good = Guid.NewGuid();
        var badDefinition = Guid.NewGuid();
        var orphan = Guid.NewGuid();
        var badReference = Guid.NewGuid();

        var result = await ExecuteAsync(team,
            Franchise(team, good, "Ravens"),
            Franchise(team, existing, "Hawks again"),
            Franchise(team, badDefinition, "Wrens") with { Values = [new CreateAssetValue(team.Year, "1990")] },
            Season(team, orphan, badDefinition, 2025, team.Big),
            Season(team, badReference, good, 2025, team.Big) with { Values = [new CreateAssetValue(team.Venue, Guid.NewGuid().ToString())] });

        result.Value.Created.Should().Equal(good);
        result.Value.Rejected.Select(r => (r.Id, r.Status)).Should().Equal(
            (existing, ResultStatus.Conflict),
            (badDefinition, ResultStatus.Validation),
            (orphan, ResultStatus.Validation),
            (badReference, ResultStatus.Validation));
        result.Value.Rejected[1].Errors.Should().ContainSingle(e => e.PropertyName == "Values[0]", "Year is an instance property");
        result.Value.Rejected[2].Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateAssetsItem.InstanceOfId));
        result.Value.Rejected[3].Errors.Should().ContainSingle(e => e.PropertyName == "Values[0]");
    }

    [Fact]
    public async Task ForeignKeys_KeepTheirTrust()
    {
        // The baseline creates some of these keys WITH NOCHECK (legacy); the write must not add to them.
        const string untrusted = """
            SELECT COUNT(*) FROM sys.foreign_keys
            WHERE is_not_trusted = 1 AND parent_object_id IN (OBJECT_ID('dbo.Assets'), OBJECT_ID('dbo.PropertyValueSubmittals'), OBJECT_ID('dbo.PropertyValues'))
            """;
        var team = await BuildTeamAsync();
        var franchise = Guid.NewGuid();
        var before = await ScalarAsync<int>(untrusted);

        await ExecuteAsync(team, Franchise(team, franchise, "Larks"), Season(team, Guid.NewGuid(), franchise, 2025, team.Big));

        (await ScalarAsync<int>(untrusted)).Should().Be(before, "a bulk copy that skips constraint checks marks the foreign keys untrusted");
    }

    [Fact]
    public async Task ValuesAndSubmittal_AreWritten_AsCreateAssetWritesThem()
    {
        var team = await BuildTeamAsync();
        var franchise = Guid.NewGuid();
        var season = Guid.NewGuid();

        await ExecuteAsync(team, Franchise(team, franchise, "Finches"), Season(team, season, franchise, 2025, team.Small));

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<XdbDataContext>();
        var asset = await db.Assets.AsNoTracking().SingleAsync(a => a.Id == season);
        asset.DisplayValue.Should().BeNull();
        asset.InstanceOfId.Should().Be(franchise);
        asset.ApprovedBy.Should().Be(SystemMemberIds.Admin);
        var submittal = await db.PropertyValueSubmittals.AsNoTracking().SingleAsync(s => s.AssetId == season);
        submittal.ReviewedBy.Should().Be(SystemMemberIds.Admin);
        (await db.PropertyValues.AsNoTracking().Where(v => v.AssetId == season).ToListAsync())
            .Should().OnlyContain(v => v.SubmittalGroupId == submittal.Id && v.ApprovedBy == SystemMemberIds.Admin)
            .And.Subject.ToDictionary(v => v.PropertyId, v => v.Value).Should().BeEquivalentTo(new Dictionary<Guid, string?>
            {
                [SystemPropertyIds.AssetName] = "2025 Finches",
                [team.Year] = "2025",
                [team.Conference] = team.Small.ToString("D")
            });
    }

    [Fact]
    public async Task RepeatedIds_FailTheCommand()
    {
        var team = await BuildTeamAsync();
        var id = Guid.NewGuid();

        var result = await ExecuteAsync(team, Franchise(team, id, "Kites"), Franchise(team, id, "Kites"));

        result.Status.Should().Be(ResultStatus.Validation);
    }

    private static CreateAssetsItem Franchise(Team team, Guid id, string name) => new()
    {
        Id = id, Name = name, Values = [new CreateAssetValue(team.Nickname, name)]
    };

    private static CreateAssetsItem Season(Team team, Guid id, Guid franchise, int year, Guid conference) => new()
    {
        Id = id,
        Name = $"{year} Finches",
        InstanceOfId = franchise,
        Values = [new CreateAssetValue(team.Year, year.ToString()), new CreateAssetValue(team.Conference, conference.ToString())]
    };

    /// <summary>Conference pick list, a venue type to refer to, and the team type: Nickname on franchises; Year, Conference, Venue on seasons.</summary>
    private async Task<Team> BuildTeamAsync()
    {
        var big = Guid.NewGuid();
        var small = Guid.NewGuid();
        var conferences = await RunAsync<ICreatePickListCommandHandler, CreatePickListCommand, Guid>(new CreatePickListCommand
        {
            ActorId = SystemMemberIds.Admin,
            Name = $"Conference {Guid.NewGuid():N}",
            Description = "Conference",
            Values = [new CreatePickListValue { Id = big, Value = "Big" }, new CreatePickListValue { Id = small, Value = "Small" }]
        }, (h, c) => h.ExecuteAsync(c));

        var venueTypeId = await CreateTypeAsync(instanceLabel: null, []);
        var nickname = await CreatePropertyAsync(new CreatePropertyCommand { ActorId = SystemMemberIds.Admin, Name = "Nickname", DataType = DataType.String });
        var year = await CreatePropertyAsync(new CreatePropertyCommand { ActorId = SystemMemberIds.Admin, Name = "Year", DataType = DataType.Int });
        var conference = await CreatePropertyAsync(new CreatePropertyCommand
        {
            ActorId = SystemMemberIds.Admin, Name = "Conference", DataType = DataType.PickList, PickListId = conferences
        });
        var venue = await CreatePropertyAsync(new CreatePropertyCommand
        {
            ActorId = SystemMemberIds.Admin, Name = "Venue", DataType = DataType.Asset, AssetTypeId = venueTypeId, AssetTypeIsInstance = false
        });

        var typeId = await CreateTypeAsync("Season",
        [
            new CreateAssetTypeProperty(nickname, IsInstance: false),
            new CreateAssetTypeProperty(year, IsInstance: true),
            new CreateAssetTypeProperty(conference, IsInstance: true),
            new CreateAssetTypeProperty(venue, IsInstance: true)
        ]);
        return new Team(typeId, nickname, year, conference, big, small, venueTypeId, venue);
    }

    private async Task<Guid> CreateTypeAsync(string? instanceLabel, IReadOnlyList<CreateAssetTypeProperty> properties)
    {
        var result = await RunAsync<ICreateAssetTypeCommandHandler, CreateAssetTypeCommand, CreateAssetTypeResult>(new CreateAssetTypeCommand
        {
            ActorId = SystemMemberIds.Admin,
            Name = $"Team {Guid.NewGuid():N}",
            Pluralization = "Teams",
            DefinitionLabel = "Franchise",
            InstanceLabel = instanceLabel,
            Properties = properties
        }, (h, c) => h.ExecuteAsync(c));
        return result.Id;
    }

    private Task<Guid> CreatePropertyAsync(CreatePropertyCommand command) =>
        RunAsync<ICreatePropertyCommandHandler, CreatePropertyCommand, Guid>(command, (h, c) => h.ExecuteAsync(c));

    private async Task<TResult> RunAsync<THandler, TCommand, TResult>(TCommand command, Func<THandler, TCommand, Task<Result<TResult>>> execute)
        where THandler : notnull
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var result = await execute(scope.ServiceProvider.GetRequiredService<THandler>(), command);
        result.Status.Should().Be(ResultStatus.Created);
        return result.Value;
    }

    private async Task<Result<CreateAssetsResult>> ExecuteAsync(Team team, params CreateAssetsItem[] items)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ICreateAssetsCommandHandler>().ExecuteAsync(new CreateAssetsCommand
        {
            ActorId = SystemMemberIds.Admin, AssetTypeId = team.TypeId, Assets = items
        });
    }

    private async Task<T> ScalarAsync<T>(string sql)
    {
        await using var connection = new SqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }
}
