using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;
using Moq.AutoMock;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects.Commands.RegenerateAll;
using Xdb.Api.Application.GeneratedObjects.GenTables;
using Xdb.Api.Application.GeneratedObjects.Views;
using Xdb.Api.Infrastructure.Data.Procedures;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.GeneratedObjects;

public class GeneratorTests
{
    private static readonly Guid RoleId = Guid.NewGuid();
    private static readonly Guid TypeId = Guid.NewGuid();
    private static readonly Guid PropertyId = Guid.NewGuid();

    private readonly AutoMocker _mocker = new();
    private readonly Mock<IGeneratedObjectProcedures> _procedures;
    private readonly List<string> _executed = [];

    public GeneratorTests()
    {
        _procedures = _mocker.GetMock<IGeneratedObjectProcedures>();
        _procedures.Setup(p => p.ExecuteAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<string, int, CancellationToken>((sql, _, _) => _executed.Add(sql))
            .Returns(Task.CompletedTask);
        _procedures.Setup(p => p.GetReportingLabelAsync(TypeId, It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync("Server");
        _procedures.Setup(p => p.GetPropertiesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new PropertyMetaRow { Id = PropertyId, DataTypeId = (byte)DataType.String, Name = "Site" },
                new PropertyMetaRow { Id = SystemPropertyIds.AssetName, DataTypeId = (byte)DataType.String, Name = "AssetName", IsSystem = true, SystemTypeId = 0 }
            ]);
        _procedures.Setup(p => p.GetRelationsAsync(TypeId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _procedures.Setup(p => p.GetRelationAsync(TypeId, PropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyRelationRow { Id = Guid.NewGuid(), AssetTypeId = TypeId, PropertyId = PropertyId, IsInstance = true });
        _mocker.Use(new ViewInputLoader(_procedures.Object));
    }

    private static RoleAssetTypeRow AssetType(bool isInstance, bool inherited, params RolePermissionRow[] permissions)
    {
        var row = new RoleAssetTypeRow { Id = Guid.NewGuid(), AssetTypeId = TypeId, IsInstance = isInstance, IsInherited = inherited };
        row.Permissions.AddRange(permissions);
        return row;
    }

    private static RolePermissionRow Permission(int permissionId, params string[] values)
    {
        var row = new RolePermissionRow { Id = Guid.NewGuid(), PermissionId = permissionId, FilterExpression = string.Join(" AND ", values.Select((_, i) => $"{{{i}}}")) };
        row.Filters.AddRange(values.Select(v => new RoleFilterRow { PropertyId = PropertyId, OperatorId = 0, Operator = "=", Value = v }));
        return row;
    }

    [Fact]
    public async Task RoleViews_SkipInheritedTypes_UnfilteredPermissions_AndCurrentUserFilters()
    {
        _procedures.Setup(p => p.GetRoleAssetTypesAsync(RoleId, It.IsAny<CancellationToken>())).ReturnsAsync([
            AssetType(false, inherited: true, Permission(1, "a")),
            AssetType(false, inherited: false, Permission(1, "a"), Permission(3), Permission(5, "CURRENT_USER", "b"))
        ]);

        var views = await _mocker.CreateInstance<RoleViewGenerator>().GenerateAsync(RoleId);

        views.Should().Be(1);
        _executed.Should().HaveCount(2);
        _executed[0].Should().StartWith("IF EXISTS").And.Contain($"vwRole_{RoleId:D}_{TypeId:D}_1");
        _executed[1].Should().StartWith($"CREATE VIEW [dbo].[vwRole_{RoleId:D}_{TypeId:D}_1]");
    }

    [Fact]
    public async Task RoleViews_DefinitionAndInstanceShareAName_SoTheLaterWins()
    {
        _procedures.Setup(p => p.GetRoleAssetTypesAsync(RoleId, It.IsAny<CancellationToken>())).ReturnsAsync([
            AssetType(false, false, Permission(1, "a")),
            AssetType(true, false, Permission(1, "b"))
        ]);

        await _mocker.CreateInstance<RoleViewGenerator>().GenerateAsync(RoleId);

        var creates = _executed.Where(s => s.StartsWith("CREATE VIEW", StringComparison.Ordinal)).ToList();
        creates.Should().HaveCount(2);
        creates[1].Should().Contain("_I] AS [A]");
    }

    [Fact]
    public async Task RegenerateAll_RecordsFailures_AndCarriesOn()
    {
        var bad = Guid.NewGuid();
        var good = Guid.NewGuid();
        var report = Guid.NewGuid();
        _procedures.Setup(p => p.GetAssetTypeIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([bad, good]);
        _procedures.Setup(p => p.GetReportIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([report]);
        _procedures.Setup(p => p.GetRoleIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([RoleId]);
        _mocker.GetMock<IGenTableGenerator>().Setup(g => g.GenerateAsync(bad, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        _mocker.GetMock<IGenTableGenerator>().Setup(g => g.GenerateAsync(good, It.IsAny<CancellationToken>())).ReturnsAsync(4);
        _mocker.GetMock<IReportViewGenerator>().Setup(g => g.GenerateAsync(report, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mocker.GetMock<IRoleViewGenerator>().Setup(g => g.GenerateAsync(RoleId, It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var result = (await _mocker.CreateInstance<RegenerateAllCommandHandler>().ExecuteAsync(new RegenerateAllCommand())).Value;

        result.Should().BeEquivalentTo(new RegenerateAllResult(4, 1, 2, [new GenerationFailure("AssetType", bad, "boom")]));
    }

    [Fact]
    public async Task RegenerateAll_BuildsTablesBeforeViews()
    {
        var order = new List<string>();
        _procedures.Setup(p => p.GetAssetTypeIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([TypeId]);
        _procedures.Setup(p => p.GetReportIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([Guid.NewGuid()]);
        _procedures.Setup(p => p.GetRoleIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([RoleId]);
        _mocker.GetMock<IGenTableGenerator>().Setup(g => g.GenerateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Callback(() => order.Add("tables")).ReturnsAsync(2);
        _mocker.GetMock<IReportViewGenerator>().Setup(g => g.GenerateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Callback(() => order.Add("report")).ReturnsAsync(true);
        _mocker.GetMock<IRoleViewGenerator>().Setup(g => g.GenerateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Callback(() => order.Add("role")).ReturnsAsync(1);

        await _mocker.CreateInstance<RegenerateAllCommandHandler>().ExecuteAsync(new RegenerateAllCommand());

        order.Should().Equal("tables", "report", "role");
    }
}
