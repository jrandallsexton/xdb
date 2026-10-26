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
using Xdb.Api.Application.Views;
using Xdb.Api.Application.Views.Commands.CreateView;
using Xdb.Api.Infrastructure.Data;
using Xdb.Api.Infrastructure.Data.Entities;
using Xdb.Api.Tests.Integration.Application.Setup;
using Xdb.Core.Common;

using Xunit;

using DataType = Xdb.Api.Application.Common.Metadata.DataType;

namespace Xdb.Api.Tests.Integration.Application.Views;

/// <summary>CreateView against a fresh install: real foreign keys, roles, default views, and the legacy view loader reading the result.</summary>
public class CreateViewCommandHandlerTests(FreshInstallFixture fixture) : IClassFixture<FreshInstallFixture>
{
    [Fact]
    public async Task CreatesTheView_ItsGroupsAndRoles_AndSetsTheDefault()
    {
        var venue = await CreateVenueAsync();
        var roleId = await CreateRoleAsync();
        var command = new CreateViewCommand
        {
            ActorId = SystemMemberIds.Admin,
            Id = Guid.NewGuid(),
            AssetTypeId = venue.TypeId,
            IsInstance = true,
            Name = "Venues",
            Description = "Every venue",
            DriverCaption = "Venue",
            NewItemCaption = "Create Venue",
            PropertyGroupIds = [venue.DetailsGroup, venue.LocationGroup],
            RoleIds = [roleId],
            IsDefault = true
        };

        var result = await ExecuteAsync(command);

        result.Should().BeOfType<Success<Guid>>();
        result.Status.Should().Be(ResultStatus.Created);
        result.Value.Should().Be(command.Id!.Value);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<XdbDataContext>();
        var view = await db.Views.AsNoTracking().SingleAsync(v => v.Id == result.Value);
        view.AssetTypeId.Should().Be(venue.TypeId);
        view.IsInstance.Should().BeTrue();
        view.DisplayValue.Should().Be("Venues", "the display value defaults to the name");
        view.Description.Should().Be("Every venue");
        view.IsStandAlone.Should().BeTrue();
        view.IsReadOnly.Should().BeFalse();
        view.IsCreation.Should().BeFalse();
        view.CreatedBy.Should().Be(SystemMemberIds.Admin);
        view.Created.Should().BeCloseTo(fixture.Services.GetRequiredService<IDateTimeProvider>().UtcNow(), TimeSpan.FromMinutes(2));

        (await db.ViewsPropertyGroups.AsNoTracking().Where(g => g.ViewId == result.Value).OrderBy(g => g.Order).Select(g => new { g.PropertyGroupId, g.Order }).ToListAsync())
            .Should().Equal(new { PropertyGroupId = venue.DetailsGroup, Order = 0 }, new { PropertyGroupId = venue.LocationGroup, Order = 1 });
        (await db.RolesViews.AsNoTracking().Where(r => r.ViewId == result.Value).Select(r => r.RoleId).ToListAsync())
            .Should().BeEquivalentTo([roleId, SystemRoleIds.Admin]);

        var type = await db.AssetTypes.AsNoTracking().SingleAsync(t => t.Id == venue.TypeId);
        type.DefaultInstanceViewId.Should().Be(result.Value);
        type.DefaultViewId.Should().BeNull();
        type.LastModifiedBy.Should().Be(SystemMemberIds.Admin);
    }

    [Fact]
    public async Task TheLegacyViewLoader_ReadsTheView()
    {
        var venue = await CreateVenueAsync();
        var result = await ExecuteAsync(Command(venue.TypeId, isInstance: true, venue.DetailsGroup, venue.LocationGroup) with { DriverCaption = "Venue" });

        await using var scope = fixture.Services.CreateAsyncScope();
        var client = await scope.ServiceProvider.GetRequiredService<ViewLoader>().ViewClientAsync(result.Value, SystemMemberIds.Admin, default);

        client.IsInstance.Should().BeTrue();
        client.PropertyGroups.Select(g => g.Name).Should().Equal("Details", "Location");
        client.PropertyGroups[0].Properties.Select(p => p.Name).Should().Equal(["Venue", "City"], "the driver caption labels the name field");
    }

    [Fact]
    public async Task MembersOfItsRoles_AreTouched_SoTheCachedMenuRefreshes()
    {
        var venue = await CreateVenueAsync();
        var roleId = await CreateRoleAsync();
        var longAgo = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<XdbDataContext>();
            db.RoleMembers.Add(new RoleMember
            {
                Id = Guid.NewGuid(), RoleId = roleId, MemberId = SystemMemberIds.Admin,
                Created = fixture.Services.GetRequiredService<IDateTimeProvider>().UtcNow(), CreatedBy = SystemMemberIds.Admin
            });
            (await db.Members.SingleAsync(m => m.Id == SystemMemberIds.Admin)).LastModified = longAgo;
            await db.SaveChangesAsync();
        }

        await ExecuteAsync(Command(venue.TypeId, isInstance: true, venue.DetailsGroup) with { RoleIds = [roleId] });

        await using var check = fixture.Services.CreateAsyncScope();
        var member = await check.ServiceProvider.GetRequiredService<XdbDataContext>().Members.AsNoTracking().SingleAsync(m => m.Id == SystemMemberIds.Admin);
        member.LastModified.Should().NotBe(longAgo, "User_Modified must report a change for the UI to rebuild its menu");
        member.LastModifiedBy.Should().Be(SystemMemberIds.Admin);
    }

    [Fact]
    public async Task AdminListedExplicitly_IsRelatedOnce()
    {
        var venue = await CreateVenueAsync();

        var result = await ExecuteAsync(Command(venue.TypeId, isInstance: true) with { RoleIds = [SystemRoleIds.Admin] });

        await using var scope = fixture.Services.CreateAsyncScope();
        (await scope.ServiceProvider.GetRequiredService<XdbDataContext>().RolesViews.AsNoTracking().CountAsync(r => r.ViewId == result.Value))
            .Should().Be(1);
    }

    [Fact]
    public async Task DefinitionDefault_SetsOnlyTheDefinitionColumn()
    {
        var venue = await CreateVenueAsync();

        var result = await ExecuteAsync(Command(venue.TypeId, isInstance: false) with { IsDefault = true });

        await using var scope = fixture.Services.CreateAsyncScope();
        var type = await scope.ServiceProvider.GetRequiredService<XdbDataContext>().AssetTypes.AsNoTracking().SingleAsync(t => t.Id == venue.TypeId);
        type.DefaultViewId.Should().Be(result.Value);
        type.DefaultInstanceViewId.Should().BeNull();
    }

    [Fact]
    public async Task GroupOfAnotherLevelOrType_FailsValidation_AndWritesNothing()
    {
        var venue = await CreateVenueAsync();
        var other = await CreateVenueAsync();
        var command = Command(venue.TypeId, isInstance: false, venue.LocationGroup, other.DetailsGroup) with { IsDefault = true };

        var result = await ExecuteAsync(command);

        result.Status.Should().Be(ResultStatus.Validation);
        ErrorsOf(result).Select(e => e.PropertyName).Should().BeEquivalentTo(["PropertyGroupIds[0]", "PropertyGroupIds[1]"]);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<XdbDataContext>();
        (await db.Views.AsNoTracking().AnyAsync(v => v.Id == command.Id)).Should().BeFalse();
        (await db.AssetTypes.AsNoTracking().SingleAsync(t => t.Id == venue.TypeId)).DefaultViewId.Should().BeNull();
    }

    [Fact]
    public async Task InstanceViewOnATypeWithoutInstances_FailsValidation()
    {
        var typeId = await CreateTypeAsync(instanceLabel: null);

        var result = await ExecuteAsync(Command(typeId, isInstance: true));

        result.Status.Should().Be(ResultStatus.Validation);
        ErrorsOf(result).Should().ContainSingle(e => e.PropertyName == nameof(CreateViewCommand.IsInstance));
    }

    [Fact]
    public async Task NameTakenOnTheSameType_FailsValidation_ButNotOnAnother()
    {
        var venue = await CreateVenueAsync();
        var other = await CreateVenueAsync();
        (await ExecuteAsync(Command(venue.TypeId, isInstance: true) with { Name = "All" })).Status.Should().Be(ResultStatus.Created);

        var sameType = await ExecuteAsync(Command(venue.TypeId, isInstance: false) with { Name = "All" });
        var otherType = await ExecuteAsync(Command(other.TypeId, isInstance: true) with { Name = "All" });

        sameType.Status.Should().Be(ResultStatus.Validation);
        ErrorsOf(sameType).Should().ContainSingle(e => e.PropertyName == nameof(CreateViewCommand.Name));
        otherType.Status.Should().Be(ResultStatus.Created);
    }

    [Fact]
    public async Task MissingOrDeletedRole_FailsValidation()
    {
        var venue = await CreateVenueAsync();
        var deleted = await CreateRoleAsync(deleted: true);

        var result = await ExecuteAsync(Command(venue.TypeId, isInstance: true) with { RoleIds = [Guid.NewGuid(), deleted] });

        result.Status.Should().Be(ResultStatus.Validation);
        ErrorsOf(result).Select(e => e.PropertyName).Should().BeEquivalentTo(["RoleIds[0]", "RoleIds[1]"]);
    }

    [Fact]
    public async Task TakenId_IsAConflict()
    {
        var venue = await CreateVenueAsync();
        var first = await ExecuteAsync(Command(venue.TypeId, isInstance: true));

        var second = await ExecuteAsync(Command(venue.TypeId, isInstance: true) with { Id = first.Value });

        second.Status.Should().Be(ResultStatus.Conflict);
        ErrorsOf(second).Should().ContainSingle(e => e.PropertyName == nameof(CreateViewCommand.Id));
    }

    private static CreateViewCommand Command(Guid typeId, bool isInstance, params Guid[] groups) => new()
    {
        ActorId = SystemMemberIds.Admin,
        Id = Guid.NewGuid(),
        AssetTypeId = typeId,
        IsInstance = isInstance,
        Name = $"View {Guid.NewGuid():N}",
        PropertyGroupIds = groups
    };

    private static List<FluentValidation.Results.ValidationFailure> ErrorsOf(Result<Guid> result) =>
        result.Should().BeOfType<Failure<Guid>>().Subject.Errors;

    private async Task<Result<Guid>> ExecuteAsync(CreateViewCommand command)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ICreateViewCommandHandler>().ExecuteAsync(command);
    }

    /// <summary>A type with City and Capacity on instances, in two instance-level groups.</summary>
    private async Task<(Guid TypeId, Guid DetailsGroup, Guid LocationGroup)> CreateVenueAsync()
    {
        var city = await CreatePropertyAsync("City");
        var capacity = await CreatePropertyAsync("Capacity", DataType.Int);
        var typeId = await CreateTypeAsync("Venue", new CreateAssetTypeProperty(city, IsInstance: true), new CreateAssetTypeProperty(capacity, IsInstance: true));
        var details = await CreateGroupAsync(typeId, "Details", SystemPropertyIds.AssetName, city);
        var location = await CreateGroupAsync(typeId, "Location", capacity);
        return (typeId, details, location);
    }

    private async Task<Guid> CreateGroupAsync(Guid typeId, string name, params Guid[] properties)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<ICreatePropertyGroupCommandHandler>().ExecuteAsync(new CreatePropertyGroupCommand
        {
            ActorId = SystemMemberIds.Admin,
            AssetTypeId = typeId,
            IsInstance = true,
            Name = name,
            Properties = properties.Select(p => new CreatePropertyGroupProperty(p)).ToList()
        });
        result.Status.Should().Be(ResultStatus.Created);
        return result.Value;
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

    private async Task<Guid> CreatePropertyAsync(string name, DataType type = DataType.String)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<ICreatePropertyCommandHandler>()
            .ExecuteAsync(new CreatePropertyCommand { ActorId = SystemMemberIds.Admin, Name = name, DataType = type });
        result.Status.Should().Be(ResultStatus.Created);
        return result.Value;
    }

    // No CreateRole slice yet; a role row is all a view relation needs.
    private async Task<Guid> CreateRoleAsync(bool deleted = false)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<XdbDataContext>();
        var now = fixture.Services.GetRequiredService<IDateTimeProvider>().UtcNow();
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = $"Role {Guid.NewGuid():N}",
            Created = now,
            CreatedBy = SystemMemberIds.Admin,
            IsActive = true,
            Deleted = deleted ? now : null,
            DeletedBy = deleted ? SystemMemberIds.Admin : null
        };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        return role.Id;
    }
}
