using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;
using Moq.AutoMock;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Reports.Queries.GetReportClient;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.Reports.Queries;

public class GetReportClientQueryHandlerTests
{
    private static readonly Guid ReportId = Guid.NewGuid();
    private static readonly Guid AssetTypeId = Guid.NewGuid();

    private readonly AutoMocker _mocker = new();
    private readonly Mock<IReportProcedures> _procedures;

    public GetReportClientQueryHandlerTests()
    {
        _procedures = _mocker.GetMock<IReportProcedures>();
        _procedures.Setup(p => p.GetReportFiltersAsync(ReportId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
    }

    private void Report(ReportRow report, params (ReportPropertyRow Row, PropertyRow Property)[] properties)
    {
        var rows = new List<ReportPropertyRow>();
        var props = new List<PropertyRow>();
        foreach (var (row, property) in properties)
        {
            rows.Add(row);
            props.Add(property);
        }
        _procedures.Setup(p => p.GetReportAsync(ReportId, It.IsAny<CancellationToken>())).ReturnsAsync((report, rows));
        _procedures.Setup(p => p.GetPropertiesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(props);
    }

    private static (ReportPropertyRow, PropertyRow) Prop(string name, string? display = null, bool isSystem = false, SystemType? system = null,
        int? custom = null, string? label = null, Guid? subPropertyId = null, string? property = null, string? subProperty = null, bool grouping = false)
    {
        var id = Guid.NewGuid();
        return (new ReportPropertyRow { Id = Guid.NewGuid(), PropertyId = id, SubPropertyId = subPropertyId, CustomReportFieldTypeId = custom, Label = label,
                    Property = property, SubProperty = subProperty, IsGrouping = grouping },
                new PropertyRow { Id = id, Name = name, DisplayValue = display, IsSystem = isSystem, SystemTypeId = (int?)system, DataTypeId = (byte)DataType.String });
    }

    private Task<Result<Xdb.Api.Application.Reports.Queries.GetReportClient.Dtos.ReportClientDto>> Execute() =>
        _mocker.CreateInstance<GetReportClientQueryHandler>().ExecuteAsync(new GetReportClientQuery(ReportId));

    [Fact]
    public async Task MissingReport_IsNotFound()
    {
        _procedures.Setup(p => p.GetReportAsync(ReportId, It.IsAny<CancellationToken>())).ReturnsAsync(((ReportRow?)null, (IReadOnlyList<ReportPropertyRow>)[]));

        (await Execute()).Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task NoProperties_GivesEmptyNamesWithoutAPropertyLookup()
    {
        Report(new ReportRow { ReportName = "R" });

        (await Execute()).Value.PropertyNames.Should().BeEmpty();
        _procedures.Verify(p => p.GetPropertiesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StoredProcedureReport_UsesTheProceduresColumnsWithoutRowNumberAndAssetId()
    {
        Report(new ReportRow { ReportName = "R", StoredProc = "rpt_X" });
        var table = new System.Data.DataTable();
        foreach (var column in new[] { "rn", "AssetId", "Asset", "Response Date" })
            table.Columns.Add(column);
        _procedures.Setup(p => p.ExecuteReportProcedureAsync("rpt_X", It.IsAny<CancellationToken>())).ReturnsAsync(table);

        (await Execute()).Value.PropertyNames.Should().Equal("Asset", "Response Date");
    }

    [Fact]
    public async Task StoredProcedureReport_WhoseProcedureFails_HasNullNames()
    {
        Report(new ReportRow { ReportName = "R", StoredProc = "rpt_X" });
        _procedures.Setup(p => p.ExecuteReportProcedureAsync("rpt_X", It.IsAny<CancellationToken>())).ReturnsAsync((System.Data.DataTable?)null);

        (await Execute()).Value.PropertyNames.Should().BeNull();
    }

    [Fact]
    public async Task PropertyNames_FollowTheOriginalRules()
    {
        _procedures.Setup(p => p.GetReportingLabelAsync(AssetTypeId, true, It.IsAny<CancellationToken>())).ReturnsAsync("Server");
        _procedures.Setup(p => p.GetReportingLabelAsync(AssetTypeId, false, It.IsAny<CancellationToken>())).ReturnsAsync("Model");
        Report(new ReportRow { ReportName = "R", AssetTypeId = AssetTypeId, IsInstance = true },
            Prop("Name", isSystem: true, system: SystemType.AssetName),
            Prop("InstanceOf", isSystem: true, system: SystemType.InstanceOf),
            Prop("InstanceOfDescription", isSystem: true, system: SystemType.InstanceOfDescription),
            Prop("raw", display: "Display"),
            Prop("raw", display: ""),
            Prop("ignored", custom: 2, label: "Custom"),
            Prop("ignored", custom: 2, label: null),
            Prop("ignored", subPropertyId: Guid.NewGuid(), property: "Owner", subProperty: "Email"),
            Prop("ignored", label: "Label is ignored for plain properties"));

        var dto = (await Execute()).Value;

        dto.PropertyNames.Should().Equal("Server", "Model", "Model Description", "Display", "raw", "Custom", null, "Owner.Email", "ignored");
        dto.GroupColumns.Should().BeNull();
        dto.FilterString.Should().BeEmpty();
    }

    [Fact]
    public async Task AssetName_FallsBackWhenTheTypeHasNoLabel()
    {
        _procedures.Setup(p => p.GetReportingLabelAsync(It.IsAny<Guid?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync("");
        Report(new ReportRow { ReportName = "R" }, Prop("Name", isSystem: true, system: SystemType.AssetName));

        (await Execute()).Value.PropertyNames.Should().Equal("Asset Name");
    }

    [Fact]
    public async Task Grouping_UsesTheLastGroupColumnsDisplayValue()
    {
        var first = Prop("A", grouping: true);
        var second = Prop("B", grouping: true);
        Report(new ReportRow { ReportName = "R" }, first, second);
        _procedures.Setup(p => p.GetPropertyDisplayValueAsync(second.Item2.Id, It.IsAny<CancellationToken>())).ReturnsAsync("Bee");

        var dto = (await Execute()).Value;

        dto.GroupColumns.Should().Equal(first.Item2.Id, second.Item2.Id);
        dto.GroupColumnName.Should().Be("Bee");
    }

    [Fact]
    public async Task FilterString_RendersUsersAndEmptyGuids()
    {
        var owner = Guid.NewGuid();
        var member = Guid.NewGuid();
        var column = Prop("x");
        Report(new ReportRow { ReportName = "R", FilterExpression = "(({0}) AND ({1}))" }, column);
        _procedures.Setup(p => p.GetReportFiltersAsync(ReportId, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new ReportFilterRow { PropertyId = owner, Operator = "=", Value = member.ToString() },
            new ReportFilterRow { PropertyId = owner, Operator = "<>", Value = "00000000-0000-0000-0000-000000000000" }
        ]);
        _procedures.SetupSequence(p => p.GetPropertiesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([column.Item2])
            .ReturnsAsync([new PropertyRow { Id = owner, Name = "Owner", DataTypeId = (byte)DataType.User }]);
        _procedures.Setup(p => p.GetPropertyDisplayValueAsync(owner, It.IsAny<CancellationToken>())).ReturnsAsync("Owner");
        _procedures.Setup(p => p.GetMemberAsync(member, It.IsAny<CancellationToken>())).ReturnsAsync(new MemberNameRow { UserId = "jdoe", LName = "Doe", FName = "Jane" });

        (await Execute()).Value.FilterString.Should().Be("((Owner = Doe, Jane [jdoe]) AND (Owner <> null))");
    }
}
