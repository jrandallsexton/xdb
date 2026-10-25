using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xdb.Api.Application.AssetTypes.Commands.CreateAssetType;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Properties.Commands.CreateProperty;
using Xdb.Api.Application.PropertyGroups.Commands.CreatePropertyGroup;
using Xdb.Api.Infrastructure.Data;
using Xdb.Api.Tests.Integration.Application.Setup;
using Xdb.Core.Common;

using Xunit;

using DataType = Xdb.Api.Application.Common.Metadata.DataType;

namespace Xdb.Api.Tests.Integration.Application.PropertyGroups;

/// <summary>CreatePropertyGroup against a fresh install: real foreign keys, links and the seeded system properties.</summary>
public class CreatePropertyGroupCommandHandlerTests(FreshInstallFixture fixture) : IClassFixture<FreshInstallFixture>
{
    [Fact]
    public async Task CreatesTheGroup_WithItsPropertiesInOrder()
    {
        var (typeId, city, _) = await CreateVenueTypeAsync();
        var command = new CreatePropertyGroupCommand
        {
            ActorId = SystemMemberIds.Admin,
            Id = Guid.NewGuid(),
            AssetTypeId = typeId,
            IsInstance = true,
            Name = "Location",
            Properties =
            [
                new CreatePropertyGroupProperty(SystemPropertyIds.AssetName, IsRequired: true),
                new CreatePropertyGroupProperty(SystemPropertyIds.InstanceOf, IsReadOnly: true),
                new CreatePropertyGroupProperty(city)
            ]
        };

        var result = await ExecuteAsync(command);

        result.Should().BeOfType<Success<Guid>>();
        result.Status.Should().Be(ResultStatus.Created);
        result.Value.Should().Be(command.Id!.Value);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<XdbDataContext>();
        var group = await db.PropertyGroups.AsNoTracking().SingleAsync(g => g.Id == result.Value);
        group.AssetTypeId.Should().Be(typeId);
        group.IsInstance.Should().BeTrue();
        group.Name.Should().Be("Location");
        group.DisplayValue.Should().Be("Location", "the display value defaults to the name");
        group.CreatedBy.Should().Be(SystemMemberIds.Admin);
        group.Created.Should().BeCloseTo(fixture.Services.GetRequiredService<IDateTimeProvider>().UtcNow(), TimeSpan.FromMinutes(2));

        var members = await db.PropertyGroupsProperties.AsNoTracking().Where(m => m.PropertyGroupId == result.Value).OrderBy(m => m.Order).ToListAsync();
        members.Select(m => (m.PropertyId, m.Order, m.IsRequired, m.IsReadOnly)).Should().Equal(
            (SystemPropertyIds.AssetName, 0, true, (bool?)false),
            (SystemPropertyIds.InstanceOf, 1, false, (bool?)true),
            (city, 2, false, (bool?)false));
        members.Should().OnlyContain(m => m.DefaultValue == null && m.CreatedBy == SystemMemberIds.Admin);
    }

    [Fact]
    public async Task PropertyNotLinkedAtTheGroupsLevel_FailsValidation_AndWritesNothing()
    {
        var (typeId, city, code) = await CreateVenueTypeAsync();
        var unlinked = await CreatePropertyAsync("Nickname");
        var command = Command(typeId, isInstance: true, city, code, unlinked);

        var result = await ExecuteAsync(command);

        result.Status.Should().Be(ResultStatus.Validation);
        ErrorsOf(result).Select(e => e.PropertyName).Should().BeEquivalentTo(["Properties[1]", "Properties[2]"],
            "the code is linked to definitions only, and the nickname to nothing");
        (await GroupExistsAsync(command.Id!.Value)).Should().BeFalse();
    }

    [Fact]
    public async Task InstanceOfOnADefinitionGroup_FailsValidation()
    {
        var (typeId, _, code) = await CreateVenueTypeAsync();

        var result = await ExecuteAsync(Command(typeId, isInstance: false, code, SystemPropertyIds.InstanceOf));

        result.Status.Should().Be(ResultStatus.Validation);
        ErrorsOf(result).Should().ContainSingle(e => e.PropertyName == "Properties[1]");
    }

    [Fact]
    public async Task InstanceGroupOnATypeWithoutInstances_FailsValidation()
    {
        var typeId = await CreateTypeAsync(instanceLabel: null);

        var result = await ExecuteAsync(Command(typeId, isInstance: true));

        result.Status.Should().Be(ResultStatus.Validation);
        ErrorsOf(result).Should().ContainSingle(e => e.PropertyName == nameof(CreatePropertyGroupCommand.IsInstance));
    }

    [Fact]
    public async Task NameTakenAtTheSameLevel_FailsValidation_ButNotAtTheOtherLevel()
    {
        var (typeId, _, _) = await CreateVenueTypeAsync();
        (await ExecuteAsync(Command(typeId, isInstance: true) with { Name = "General" })).Status.Should().Be(ResultStatus.Created);

        var sameLevel = await ExecuteAsync(Command(typeId, isInstance: true) with { Name = "General" });
        var otherLevel = await ExecuteAsync(Command(typeId, isInstance: false) with { Name = "General" });

        sameLevel.Status.Should().Be(ResultStatus.Validation);
        ErrorsOf(sameLevel).Should().ContainSingle(e => e.PropertyName == nameof(CreatePropertyGroupCommand.Name));
        otherLevel.Status.Should().Be(ResultStatus.Created);
    }

    [Fact]
    public async Task MissingTypeOrProperty_FailsValidation()
    {
        var result = await ExecuteAsync(Command(Guid.NewGuid(), isInstance: false, Guid.NewGuid()));

        result.Status.Should().Be(ResultStatus.Validation);
        ErrorsOf(result).Select(e => e.PropertyName).Should().Contain([nameof(CreatePropertyGroupCommand.AssetTypeId), "Properties[0]"]);
    }

    [Fact]
    public async Task TakenId_IsAConflict()
    {
        var (typeId, _, _) = await CreateVenueTypeAsync();
        var first = await ExecuteAsync(Command(typeId, isInstance: true));

        var second = await ExecuteAsync(Command(typeId, isInstance: true) with { Id = first.Value });

        second.Status.Should().Be(ResultStatus.Conflict);
        ErrorsOf(second).Should().ContainSingle(e => e.PropertyName == nameof(CreatePropertyGroupCommand.Id));
    }

    private static CreatePropertyGroupCommand Command(Guid typeId, bool isInstance, params Guid[] properties) => new()
    {
        ActorId = SystemMemberIds.Admin,
        Id = Guid.NewGuid(),
        AssetTypeId = typeId,
        IsInstance = isInstance,
        Name = $"Group {Guid.NewGuid():N}",
        Properties = properties.Select(p => new CreatePropertyGroupProperty(p)).ToList()
    };

    private static List<FluentValidation.Results.ValidationFailure> ErrorsOf(Result<Guid> result) =>
        result.Should().BeOfType<Failure<Guid>>().Subject.Errors;

    private async Task<Result<Guid>> ExecuteAsync(CreatePropertyGroupCommand command)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ICreatePropertyGroupCommandHandler>().ExecuteAsync(command);
    }

    /// <summary>A type with City on instances and Code on definitions.</summary>
    private async Task<(Guid TypeId, Guid City, Guid Code)> CreateVenueTypeAsync()
    {
        var city = await CreatePropertyAsync("City");
        var code = await CreatePropertyAsync("Code");
        var typeId = await CreateTypeAsync("Venue", new CreateAssetTypeProperty(city, IsInstance: true), new CreateAssetTypeProperty(code, IsInstance: false));
        return (typeId, city, code);
    }

    private async Task<Guid> CreateTypeAsync(string? instanceLabel, params CreateAssetTypeProperty[] properties)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<ICreateAssetTypeCommandHandler>().ExecuteAsync(new CreateAssetTypeCommand
        {
            ActorId = SystemMemberIds.Admin,
            Name = $"Venue {Guid.NewGuid():N}",
            Pluralization = "Venues",
            InstanceLabel = instanceLabel,
            Properties = properties
        });
        result.Status.Should().Be(ResultStatus.Created);
        return result.Value.Id;
    }

    private async Task<Guid> CreatePropertyAsync(string name)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<ICreatePropertyCommandHandler>()
            .ExecuteAsync(new CreatePropertyCommand { ActorId = SystemMemberIds.Admin, Name = name, DataType = DataType.String });
        result.Status.Should().Be(ResultStatus.Created);
        return result.Value;
    }

    private async Task<bool> GroupExistsAsync(Guid id)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<XdbDataContext>().PropertyGroups.AsNoTracking().AnyAsync(g => g.Id == id);
    }
}
