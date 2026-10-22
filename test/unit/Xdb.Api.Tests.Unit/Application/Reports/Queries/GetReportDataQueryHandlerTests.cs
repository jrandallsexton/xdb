using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;
using Moq.AutoMock;

using Xdb.Api.Application.Reports.Queries.GetReportData;
using Xdb.Api.Infrastructure.Background;
using Xdb.Api.Infrastructure.Data.Procedures;

using Xunit;

namespace Xdb.Api.Tests.Unit.Application.Reports.Queries;

public class GetReportDataQueryHandlerTests
{
    private static readonly Guid ReportId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly AutoMocker _mocker = new();
    private readonly Mock<IReportProcedures> _procedures;

    public GetReportDataQueryHandlerTests()
    {
        _procedures = _mocker.GetMock<IReportProcedures>();
        _procedures.Setup(p => p.GetReportStoredProcAsync(ReportId, It.IsAny<CancellationToken>())).ReturnsAsync("");
        _procedures.Setup(p => p.GetReportOrderByColumnAsync(ReportId, It.IsAny<CancellationToken>())).ReturnsAsync("");
        _procedures.Setup(p => p.QueryReportViewAsync(ReportId, "", It.IsAny<CancellationToken>())).ReturnsAsync(Rows(3));
    }

    private static DataTable Rows(int count)
    {
        var table = new DataTable();
        table.Columns.Add("A", typeof(int));
        for (var i = 0; i < count; i++)
            table.Rows.Add(i);
        return table;
    }

    private Task<Xdb.Core.Common.Result<DataTable>> Execute(int page, string? sidx = null, string? sord = null) =>
        _mocker.CreateInstance<GetReportDataQueryHandler>().ExecuteAsync(new GetReportDataQuery(ReportId, UserId, page, sidx, sord));

    [Fact]
    public async Task PageOne_ReturnsEveryRow()
    {
        (await Execute(1)).Value.Rows.Count.Should().Be(3);
    }

    [Fact]
    public async Task PageZero_ReturnsOnlyTheFirstRow()
    {
        (await Execute(0)).Value.Rows.Count.Should().Be(1);
    }

    [Fact]
    public async Task PageTwo_IsEmpty()
    {
        (await Execute(2)).Value.Rows.Count.Should().Be(0);
    }

    [Fact]
    public async Task Sort_IsAppliedInMemory()
    {
        var result = (await Execute(1, "A", "desc")).Value;

        result.Rows[0]["A"].Should().Be(2);
    }

    [Fact]
    public async Task Log_IsOpenedSynchronouslyAndClosedInTheBackgroundWithTheRowCount()
    {
        Func<IServiceProvider, CancellationToken, Task>? close = null;
        _mocker.GetMock<IBackgroundWorkQueue>()
            .Setup(q => q.Enqueue(It.IsAny<string>(), It.IsAny<Func<IServiceProvider, CancellationToken, Task>>()))
            .Callback<string, Func<IServiceProvider, CancellationToken, Task>>((_, work) => close = work);

        await Execute(1);

        _procedures.Verify(p => p.SaveReportLogAsync(It.IsAny<Guid>(), ReportId, UserId, 0, It.IsAny<CancellationToken>()), Times.Once);
        close.Should().NotBeNull();

        var services = new Mock<IServiceProvider>();
        services.Setup(s => s.GetService(typeof(IReportProcedures))).Returns(_procedures.Object);
        await close!(services.Object, CancellationToken.None);

        _procedures.Verify(p => p.SaveReportLogAsync(It.IsAny<Guid>(), ReportId, UserId, 3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FailingStoredProcedure_Throws()
    {
        _procedures.Setup(p => p.GetReportStoredProcAsync(ReportId, It.IsAny<CancellationToken>())).ReturnsAsync("rpt_X");
        _procedures.Setup(p => p.ExecuteReportProcedureAsync("rpt_X", It.IsAny<CancellationToken>())).ReturnsAsync((DataTable?)null);

        var act = () => Execute(1);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
