using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xdb.Api.Application.AssetTypes.Commands.CreateAssetType;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects;
using Xdb.Api.Application.Properties.Commands.CreateProperty;
using Xdb.Api.Infrastructure.Data;
using Xdb.Api.Tests.Integration.Application.Setup;
using Xdb.Core.Common;

using Xunit;

using DataType = Xdb.Api.Application.Common.Metadata.DataType;

namespace Xdb.Api.Tests.Integration.Application.AssetTypes;

/// <summary>CreateAssetType against a fresh install: real foreign keys, the unique name, and generated gen tables.</summary>
public class CreateAssetTypeCommandHandlerTests(FreshInstallFixture fixture) : IClassFixture<FreshInstallFixture>
{
    // Type names are unique across the database, which every test shares.
    private static CreateAssetTypeCommand Command(params CreateAssetTypeProperty[] properties) => new()
    {
        ActorId = SystemMemberIds.Admin,
        Id = Guid.NewGuid(),
        Name = $"Venue {Guid.NewGuid():N}",
        Pluralization = "Venues",
        DefinitionLabel = "Venue Kind",
        InstanceLabel = "Venue",
        Properties = properties
    };

    [Fact]
    public async Task CreatesTheType_ItsLinks_AndBothGenTables()
    {
        var city = await CreatePropertyAsync("City");
        var capacity = await CreatePropertyAsync("Capacity", DataType.Int);
        var command = Command(new CreateAssetTypeProperty(city, IsInstance: true), new CreateAssetTypeProperty(capacity, IsInstance: true));

        var result = await ExecuteAsync(command);

        result.Should().BeOfType<Success<CreateAssetTypeResult>>();
        result.Status.Should().Be(ResultStatus.Created);
        result.Value.Id.Should().Be(command.Id!.Value);
        result.Value.Failures.Should().BeEmpty();
        result.Value.GenTables.Should().Be(4, "decoded and raw tables at both levels: the type has an instance label");

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<XdbDataContext>();
        var type = await db.AssetTypes.AsNoTracking().SingleAsync(t => t.Id == result.Value.Id);
        type.Name.Should().Be(command.Name);
        type.Pluralization.Should().Be("Venues");
        type.DefinitionLabel.Should().Be("Venue Kind");
        type.InstanceLabel.Should().Be("Venue");
        type.AllowAssets.Should().BeTrue();
        type.IsSystem.Should().BeFalse();
        type.CreatedBy.Should().Be(SystemMemberIds.Admin);
        type.Created.Should().BeCloseTo(fixture.Services.GetRequiredService<IDateTimeProvider>().UtcNow(), TimeSpan.FromMinutes(2));

        var links = await db.AssetTypesProperties.AsNoTracking().Where(l => l.AssetTypeId == result.Value.Id).ToListAsync();
        links.Select(l => l.PropertyId).Should().BeEquivalentTo([city, capacity]);
        links.Should().OnlyContain(l => l.IsInstance && !l.IsInherited && !l.IsInheritedValue && l.CreatedBy == SystemMemberIds.Admin);

        (await GenTableExistsAsync(result.Value.Id, isInstance: false)).Should().BeTrue();
        (await GenTableExistsAsync(result.Value.Id, isInstance: true)).Should().BeTrue();
    }

    [Fact]
    public async Task WithoutAnInstanceLabel_BuildsOnlyTheDefinitionGenTable()
    {
        var result = await ExecuteAsync(Command(new CreateAssetTypeProperty(await CreatePropertyAsync("Code"), IsInstance: false))
            with { InstanceLabel = null });

        result.Value.GenTables.Should().Be(2, "decoded and raw definition tables only");
        (await GenTableExistsAsync(result.Value.Id, isInstance: true)).Should().BeFalse();
    }

    [Fact]
    public async Task Child_InheritsItsParentsLinks()
    {
        var city = await CreatePropertyAsync("City");
        var code = await CreatePropertyAsync("Code");
        var parent = await ExecuteAsync(Command(new CreateAssetTypeProperty(city, true), new CreateAssetTypeProperty(code, false))
            with { AllowAssets = false });
        var roof = await CreatePropertyAsync("Roof");

        var child = await ExecuteAsync(Command(new CreateAssetTypeProperty(roof, true)) with { ParentId = parent.Value.Id });

        child.Status.Should().Be(ResultStatus.Created);
        child.Value.Failures.Should().BeEmpty();
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<XdbDataContext>();
        (await db.AssetTypeRelations.AsNoTracking()
            .CountAsync(r => r.ParentAssetTypeId == parent.Value.Id && r.ChildAssetTypeId == child.Value.Id && r.Deleted == null)).Should().Be(1);
        var links = await db.AssetTypesProperties.AsNoTracking().Where(l => l.AssetTypeId == child.Value.Id)
            .Select(l => new { l.PropertyId, l.IsInstance, l.IsInherited }).ToListAsync();
        links.Should().BeEquivalentTo(new[]
        {
            new { PropertyId = city, IsInstance = true, IsInherited = true },
            new { PropertyId = code, IsInstance = false, IsInherited = true },
            new { PropertyId = roof, IsInstance = true, IsInherited = false }
        });
    }

    [Fact]
    public async Task LinkAlreadyInheritedFromTheParent_FailsValidation()
    {
        var city = await CreatePropertyAsync("City");
        var parent = await ExecuteAsync(Command(new CreateAssetTypeProperty(city, true)));

        var child = await ExecuteAsync(Command(new CreateAssetTypeProperty(city, true)) with { ParentId = parent.Value.Id });

        child.Status.Should().Be(ResultStatus.Validation);
        ErrorsOf(child).Should().ContainSingle(e => e.PropertyName == nameof(CreateAssetTypeCommand.Properties));
    }

    [Fact]
    public async Task NameOfAnyType_FailsValidation()
    {
        var result = await ExecuteAsync(Command() with { Name = "user" });

        result.Status.Should().Be(ResultStatus.Validation);
        ErrorsOf(result).Should().ContainSingle(e => e.PropertyName == nameof(CreateAssetTypeCommand.Name));
    }

    [Fact]
    public async Task MissingOrSystemProperty_FailsValidation_AndWritesNothing()
    {
        var command = Command(new CreateAssetTypeProperty(Guid.NewGuid(), true), new CreateAssetTypeProperty(SystemPropertyIds.Description, false));

        var result = await ExecuteAsync(command);

        result.Status.Should().Be(ResultStatus.Validation);
        ErrorsOf(result).Should().HaveCount(2).And.OnlyContain(e => e.PropertyName == nameof(CreateAssetTypeCommand.Properties));
        await using var scope = fixture.Services.CreateAsyncScope();
        (await scope.ServiceProvider.GetRequiredService<XdbDataContext>().AssetTypes.AsNoTracking().AnyAsync(t => t.Id == command.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task MissingParent_FailsValidation()
    {
        var result = await ExecuteAsync(Command() with { ParentId = Guid.NewGuid() });

        ErrorsOf(result).Should().ContainSingle(e => e.PropertyName == nameof(CreateAssetTypeCommand.ParentId));
    }

    [Fact]
    public async Task TakenId_IsAConflict()
    {
        var command = Command();
        await ExecuteAsync(command);

        var again = await ExecuteAsync(command with { Name = $"Other {Guid.NewGuid():N}" });

        again.Status.Should().Be(ResultStatus.Conflict);
    }

    private static List<FluentValidation.Results.ValidationFailure> ErrorsOf(Result<CreateAssetTypeResult> result) =>
        ((Failure<CreateAssetTypeResult>)result).Errors;

    private async Task<Result<CreateAssetTypeResult>> ExecuteAsync(CreateAssetTypeCommand command)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ICreateAssetTypeCommandHandler>().ExecuteAsync(command);
    }

    private async Task<Guid> CreatePropertyAsync(string name, DataType type = DataType.String)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<ICreatePropertyCommandHandler>()
            .ExecuteAsync(new CreatePropertyCommand { ActorId = SystemMemberIds.Admin, Name = name, DataType = type });
        result.Status.Should().Be(ResultStatus.Created);
        return result.Value;
    }

    private async Task<bool> GenTableExistsAsync(Guid assetTypeId, bool isInstance)
    {
        await using var connection = new SqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("SELECT COUNT(*) FROM sys.tables WHERE [name] = @Name", connection);
        command.Parameters.AddWithValue("@Name", GeneratedObjectNames.Table(assetTypeId, isInstance, isRaw: false));
        return (int)(await command.ExecuteScalarAsync())! == 1;
    }
}
