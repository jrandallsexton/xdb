using System;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Properties.Commands.CreateProperty;
using Xdb.Api.Infrastructure.Data;
using Xdb.Api.Infrastructure.Data.Entities;
using Xdb.Api.Tests.Integration.Application.Setup;
using Xdb.Core.Common;

using Xunit;

using DataType = Xdb.Api.Application.Common.Metadata.DataType;

namespace Xdb.Api.Tests.Integration.Application.Properties;

/// <summary>CreateProperty against a fresh install: real foreign keys, column types and seeded system rows.</summary>
public class CreatePropertyCommandHandlerTests(FreshInstallFixture fixture) : IClassFixture<FreshInstallFixture>
{
    private static CreatePropertyCommand Command(DataType type = DataType.String) =>
        new() { ActorId = SystemMemberIds.Admin, Id = Guid.NewGuid(), Name = "City", DataType = type };

    [Fact]
    public async Task CreatesAnApprovedProperty_WithTheFixedId()
    {
        var command = Command() with { DisplayValue = "City", Description = "Where it is" };

        var result = await ExecuteAsync(command);

        result.Should().BeOfType<Success<Guid>>();
        result.Status.Should().Be(ResultStatus.Created);
        result.Value.Should().Be(command.Id!.Value);

        var row = await LoadAsync(result.Value);
        row.DataTypeId.Should().Be((byte)DataType.String);
        row.Name.Should().Be("City");
        row.DisplayValue.Should().Be("City");
        row.Description.Should().Be("Where it is");
        row.IsSystem.Should().BeFalse();
        row.CreatedBy.Should().Be(SystemMemberIds.Admin);
        row.ApprovedBy.Should().Be(SystemMemberIds.Admin);
        row.Approved.Should().Be(row.Created);
        // UTC, not the host's local time (smalldatetime rounds to the minute).
        row.Created.Should().BeCloseTo(fixture.Services.GetRequiredService<IDateTimeProvider>().UtcNow(), TimeSpan.FromMinutes(2));
        row.LastModified.Should().BeNull();
        row.LastModifiedBy.Should().BeNull();
    }

    [Fact]
    public async Task WithoutAnId_GeneratesOne()
    {
        var result = await ExecuteAsync(Command() with { Id = null });

        result.Value.Should().NotBe(Guid.Empty);
        (await LoadAsync(result.Value)).Name.Should().Be("City");
    }

    [Fact]
    public async Task TakenId_IsAConflict()
    {
        var command = Command();
        await ExecuteAsync(command);

        var again = await ExecuteAsync(command with { Name = "Other" });

        again.Status.Should().Be(ResultStatus.Conflict);
        (await LoadAsync(command.Id!.Value)).Name.Should().Be("City");
    }

    [Fact]
    public async Task ActorThatIsNotALiveMember_FailsValidation()
    {
        var command = Command() with { ActorId = Guid.NewGuid() };

        var result = await ExecuteAsync(command);

        result.Status.Should().Be(ResultStatus.Validation);
        ((Failure<Guid>)result).Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreatePropertyCommand.ActorId));
        (await ExistsAsync(command.Id!.Value)).Should().BeFalse();
    }

    [Fact]
    public async Task PickListProperty_LinksAnExistingList()
    {
        var pickListId = Guid.NewGuid();
        await SqlAsync($"""
            INSERT INTO [dbo].[PickLists] ([Id], [Name], [Description], [CreatedBy])
            VALUES ('{pickListId:D}', 'Surface', 'Playing surface', '{SystemMemberIds.Admin:D}')
            """);

        var result = await ExecuteAsync(Command(DataType.PickList) with { Name = "Surface", PickListId = pickListId });

        result.Status.Should().Be(ResultStatus.Created);
        (await LoadAsync(result.Value)).PickListId.Should().Be(pickListId);
    }

    [Fact]
    public async Task PickListProperty_WithAMissingList_FailsValidation()
    {
        var result = await ExecuteAsync(Command(DataType.PickList) with { PickListId = Guid.NewGuid() });

        result.Status.Should().Be(ResultStatus.Validation);
        ((Failure<Guid>)result).Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreatePropertyCommand.PickListId));
    }

    [Fact]
    public async Task AssetProperty_WithAMissingType_FailsValidation()
    {
        var result = await ExecuteAsync(Command(DataType.Asset) with { AssetTypeId = Guid.NewGuid(), AssetTypeIsInstance = true });

        result.Status.Should().Be(ResultStatus.Validation);
        ((Failure<Guid>)result).Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreatePropertyCommand.AssetTypeId));
    }

    [Fact]
    public async Task AssetProperty_RefersToAnExistingType()
    {
        var result = await ExecuteAsync(Command(DataType.Asset) with
        {
            Name = "Owner", AssetTypeId = SystemAssetTypeIds.User, AssetTypeIsInstance = false
        });

        var row = await LoadAsync(result.Value);
        row.AssetTypeId.Should().Be(SystemAssetTypeIds.User);
        row.AssetTypeIsInstance.Should().BeFalse();
    }

    [Fact]
    public async Task UserProperty_TargetsTheUserType_AndKeepsItsRole()
    {
        var roleId = Guid.NewGuid();
        await SqlAsync($"""
            INSERT INTO [dbo].[Roles] ([Id], [Name], [CreatedBy], [IsSystem], [IsActive])
            VALUES ('{roleId:D}', 'Venue Editors', '{SystemMemberIds.Admin:D}', 0, 1)
            """);

        var result = await ExecuteAsync(Command(DataType.User) with { Name = "Steward", RoleId = roleId });

        var row = await LoadAsync(result.Value);
        row.AssetTypeId.Should().Be(SystemAssetTypeIds.User);
        row.AssetTypeIsInstance.Should().BeFalse();
        row.RoleId.Should().Be(roleId);
    }

    [Fact]
    public async Task InvalidShape_FailsBeforeTouchingTheDatabase()
    {
        var command = Command(DataType.SystemAssetName);

        var result = await ExecuteAsync(command);

        result.Status.Should().Be(ResultStatus.Validation);
        (await ExistsAsync(command.Id!.Value)).Should().BeFalse();
    }

    private async Task<Result<Guid>> ExecuteAsync(CreatePropertyCommand command)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ICreatePropertyCommandHandler>().ExecuteAsync(command);
    }

    private async Task<Property> LoadAsync(Guid id)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<XdbDataContext>().Properties.AsNoTracking().SingleAsync(p => p.Id == id);
    }

    private async Task<bool> ExistsAsync(Guid id)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<XdbDataContext>().Properties.AsNoTracking().AnyAsync(p => p.Id == id);
    }

    private async Task SqlAsync(string sql)
    {
        await using var connection = new SqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
