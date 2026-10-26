using System;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Xdb.Api.Application.AssetTypes.Commands.CreateAssetType;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Metadata.Queries.Properties;
using Xdb.Api.Application.PickLists.Commands.CreatePickList;
using Xdb.Api.Application.Properties.Commands.CreateProperty;
using Xdb.Api.Tests.Integration.Application.Setup;
using Xdb.Core.Common;

using Xunit;

using DataType = Xdb.Api.Application.Common.Metadata.DataType;

namespace Xdb.Api.Tests.Integration.Application.Metadata;

/// <summary>Properties_GetNonFreeformByAssetTypeId (the charts page's property list) against a fresh install.</summary>
public class GetNonFreeformPropertiesQueryHandlerTests(FreshInstallFixture fixture) : IClassFixture<FreshInstallFixture>
{
    [Fact]
    public async Task PropertiesWithoutASystemType_AreRead_AndOnlyNonFreeformOnesListed()
    {
        // Properties created by the slices leave SystemTypeId NULL; reading it once failed as "no row".
        var list = await RunAsync<ICreatePickListCommandHandler, CreatePickListCommand, Guid>(new CreatePickListCommand
        {
            ActorId = SystemMemberIds.Admin, Name = $"Surface {Guid.NewGuid():N}", Description = "Surface",
            Values = [new CreatePickListValue { Id = Guid.NewGuid(), Value = "Grass" }]
        }, (h, c) => h.ExecuteAsync(c));
        var surface = await CreatePropertyAsync(new CreatePropertyCommand
        {
            ActorId = SystemMemberIds.Admin, Name = $"Surface {Guid.NewGuid():N}", DataType = DataType.PickList, PickListId = list
        });
        var city = await CreatePropertyAsync(new CreatePropertyCommand { ActorId = SystemMemberIds.Admin, Name = $"City {Guid.NewGuid():N}", DataType = DataType.String });
        var type = await RunAsync<ICreateAssetTypeCommandHandler, CreateAssetTypeCommand, CreateAssetTypeResult>(new CreateAssetTypeCommand
        {
            ActorId = SystemMemberIds.Admin, Name = $"Venue {Guid.NewGuid():N}", Pluralization = "Venues", DefinitionLabel = "Venue",
            Properties = [new CreateAssetTypeProperty(surface, IsInstance: false), new CreateAssetTypeProperty(city, IsInstance: false)]
        }, (h, c) => h.ExecuteAsync(c));

        await using var scope = fixture.Services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<IGetNonFreeformPropertiesQueryHandler>()
            .ExecuteAsync(new GetNonFreeformPropertiesQuery(type.Id, AssetRequestType.Definition, IncludeInherited: true));

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(p => p.Key).Should().Contain(surface).And.NotContain(city);
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
}
