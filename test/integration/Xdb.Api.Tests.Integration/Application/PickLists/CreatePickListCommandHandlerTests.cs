using System;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.PickLists.Commands.CreatePickList;
using Xdb.Api.Infrastructure.Data;
using Xdb.Api.Infrastructure.Data.Entities;
using Xdb.Api.Tests.Integration.Application.Setup;
using Xdb.Core.Common;

using Xunit;

namespace Xdb.Api.Tests.Integration.Application.PickLists;

/// <summary>CreatePickList against a fresh install: real foreign keys, column types and collation.</summary>
public class CreatePickListCommandHandlerTests(FreshInstallFixture fixture) : IClassFixture<FreshInstallFixture>
{
    // Names must be unique among live lists, and every test shares one database.
    private static CreatePickListCommand Command(string? name = null) => new()
    {
        ActorId = SystemMemberIds.Admin,
        Id = Guid.NewGuid(),
        Name = name ?? $"List {Guid.NewGuid():N}",
        Description = "Playing surface",
        Values = [new CreatePickListValue { Value = "Grass" }, new CreatePickListValue { Value = "Artificial", DisplayValue = "Artificial turf" }]
    };

    [Fact]
    public async Task CreatesAnApprovedList_WithOrderedValues()
    {
        var command = Command() with { DisplayValue = "Surface", AllowNewValues = true };

        var result = await ExecuteAsync(command);

        result.Should().BeOfType<Success<Guid>>();
        result.Status.Should().Be(ResultStatus.Created);
        result.Value.Should().Be(command.Id!.Value);

        var list = await LoadAsync(result.Value);
        list.Name.Should().Be(command.Name);
        list.DisplayValue.Should().Be("Surface");
        list.Description.Should().Be("Playing surface");
        list.AllowNewValues.Should().BeTrue();
        list.IsMemberList.Should().BeFalse();
        list.ServiceUrl.Should().BeNull();
        list.CreatedBy.Should().Be(SystemMemberIds.Admin);
        list.ApprovedBy.Should().Be(SystemMemberIds.Admin);
        list.Approved.Should().Be(list.Created);
        // UTC, not the host's local time (smalldatetime rounds to the minute).
        list.Created.Should().BeCloseTo(fixture.Services.GetRequiredService<IDateTimeProvider>().UtcNow(), TimeSpan.FromMinutes(2));

        var values = list.PickListValues.OrderBy(v => v.Order).ToList();
        values.Select(v => v.Value).Should().Equal("Grass", "Artificial");
        values.Select(v => v.Order).Should().Equal(0, 1);
        values[1].DisplayValue.Should().Be("Artificial turf");
        values.Should().OnlyContain(v => v.CreatedBy == SystemMemberIds.Admin && v.ApprovedBy == SystemMemberIds.Admin && v.Approved == v.Created);
    }

    [Fact]
    public async Task FixedValueIds_AreKept()
    {
        var grass = Guid.NewGuid();
        var command = Command() with { Values = [new CreatePickListValue { Id = grass, Value = "Grass" }] };

        var result = await ExecuteAsync(command);

        (await LoadAsync(result.Value)).PickListValues.Should().ContainSingle(v => v.Id == grass);
    }

    [Fact]
    public async Task WithoutAnId_GeneratesOne()
    {
        var result = await ExecuteAsync(Command() with { Id = null });

        result.Value.Should().NotBe(Guid.Empty);
        (await LoadAsync(result.Value)).PickListValues.Should().HaveCount(2);
    }

    [Fact]
    public async Task TakenListId_IsAConflict()
    {
        var command = Command();
        await ExecuteAsync(command);

        var again = await ExecuteAsync(command with { Name = $"Other {Guid.NewGuid():N}" });

        again.Status.Should().Be(ResultStatus.Conflict);
    }

    [Fact]
    public async Task TakenValueId_IsAConflict_AndWritesNothing()
    {
        var grass = Guid.NewGuid();
        await ExecuteAsync(Command() with { Values = [new CreatePickListValue { Id = grass, Value = "Grass" }] });
        var second = Command() with { Values = [new CreatePickListValue { Id = grass, Value = "Grass" }] };

        var result = await ExecuteAsync(second);

        result.Status.Should().Be(ResultStatus.Conflict);
        (await ExistsAsync(second.Id!.Value)).Should().BeFalse();
    }

    [Fact]
    public async Task NameOfALiveList_InAnyCase_FailsValidation()
    {
        var name = $"Surface {Guid.NewGuid():N}";
        await ExecuteAsync(Command(name));

        var result = await ExecuteAsync(Command(name.ToUpperInvariant()));

        result.Status.Should().Be(ResultStatus.Validation);
        ((Failure<Guid>)result).Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreatePickListCommand.Name));
    }

    [Fact]
    public async Task ActorThatIsNotALiveMember_FailsValidation()
    {
        var command = Command() with { ActorId = Guid.NewGuid() };

        var result = await ExecuteAsync(command);

        result.Status.Should().Be(ResultStatus.Validation);
        ((Failure<Guid>)result).Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreatePickListCommand.ActorId));
        (await ExistsAsync(command.Id!.Value)).Should().BeFalse();
    }

    private async Task<Result<Guid>> ExecuteAsync(CreatePickListCommand command)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ICreatePickListCommandHandler>().ExecuteAsync(command);
    }

    private async Task<PickList> LoadAsync(Guid id)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<XdbDataContext>().PickLists.AsNoTracking()
            .Include(l => l.PickListValues)
            .SingleAsync(l => l.Id == id);
    }

    private async Task<bool> ExistsAsync(Guid id)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<XdbDataContext>().PickLists.AsNoTracking().AnyAsync(l => l.Id == id);
    }
}
