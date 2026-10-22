using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Common.Identity;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects.Views;
using Xdb.Api.Application.Reports.Legacy;
using Xdb.Api.Infrastructure.Data.Procedures;

namespace Xdb.Api.Application.Reports;

/// <summary>A loaded report and the property ids the original kept beside it (not part of the wire contract).</summary>
public sealed record LoadedReport(ReportContract Report, IReadOnlyList<Guid> PropertyIds);

/// <summary>
/// ReportLayer.Get: the report as ReportDal.Get read it, then the checks and side effects of its header naming (every
/// property must exist; a stored-procedure report executes its procedure), then the reporting-label overwrite of the
/// last Asset Name, Instance Of and Instance Of Description properties, which the wire shows and a save persists.
/// </summary>
public sealed class ReportLoader(IReportStore store, IMetadataProcedures metadata, IReportProcedures reports)
{
    private const int NotApplicable = 0;

    public async Task<LoadedReport> LoadAsync(Guid reportId, CancellationToken cancellationToken)
    {
        var report = await store.LoadAsync(reportId, cancellationToken);
        var propertyIds = new List<Guid>();
        foreach (var property in report.ReportProperties!)
        {
            if (!propertyIds.Contains(property.PropertyId))
                propertyIds.Add(property.PropertyId);
        }

        var dictionary = propertyIds.Count == 0
            ? []
            : (await metadata.GetPropertiesAsync(propertyIds, cancellationToken)).ToDictionary(p => p.Id);
        int? assetName = null, instanceOf = null, instanceOfDescription = null;
        if (string.IsNullOrEmpty(report.StoredProc))
        {
            for (var i = 0; i < report.ReportProperties!.Count; i++)
            {
                var rp = report.ReportProperties[i];
                var property = dictionary[rp.PropertyId];
                if (rp.CustomReportFieldType != NotApplicable || property.IsSystem != true)
                    continue;
                switch (property.SystemTypeId)
                {
                    case (int)SystemType.AssetName: assetName = i; break;
                    case (int)SystemType.InstanceOf: instanceOf = i; break;
                    case (int)SystemType.InstanceOfDescription: instanceOfDescription = i; break;
                }
            }
        }
        else
        {
            _ = await reports.ExecuteReportProcedureAsync(report.StoredProc, cancellationToken);
        }

        if (assetName is { } a && await metadata.GetReportingLabelAsync(report.AssetTypeId, report.IsInstance, cancellationToken) is { Length: > 0 } nameLabel)
            report.ReportProperties![a].Label = nameLabel;
        if (instanceOf is { } o && await metadata.GetReportingLabelAsync(report.AssetTypeId, forInstances: false, cancellationToken) is { Length: > 0 } ofLabel)
            report.ReportProperties![o].Label = ofLabel;
        if (instanceOfDescription is { } d && await metadata.GetReportingLabelAsync(report.AssetTypeId, forInstances: false, cancellationToken) is { Length: > 0 } descLabel)
            report.ReportProperties![d].Label = descLabel + " Description";
        return new LoadedReport(report, propertyIds);
    }
}

/// <summary>
/// ReportLayer.FilterString_Generate(report, userFriendly: true) over the report's filters as given (loaded, or posted
/// with a save): "{property} {operator} {value}" per filter, values decoded by the property's type, formatted into
/// the report's filter logic; "" without filters.
/// </summary>
public sealed class ReportFilterString(IReportProcedures reports)
{
    private const string EmptyGuid = "00000000-0000-0000-0000-000000000000";

    public async Task<string> BuildAsync(ReportContract report, CancellationToken cancellationToken)
    {
        var filters = report.Filters;
        if (filters is null || filters.Count == 0)
            return string.Empty;

        var ids = filters.Select(f => f.PropertyId).Distinct().ToList();
        var dictionary = (await reports.GetPropertiesAsync(string.Join(",", ids.Select(id => id.ToString("D"))), cancellationToken)).ToDictionary(p => p.Id);
        var items = new List<object>();
        foreach (var filter in filters)
        {
            var property = dictionary[filter.PropertyId];
            var propertyName = await reports.GetPropertyDisplayValueAsync(filter.PropertyId, cancellationToken);
            var value = filter.Value;
            switch ((DataType)property.DataTypeId)
            {
                case DataType.Date:
                    if (filter.Value != "NULL" && !property.IsSystem && Guid.TryParse(filter.Value, out var datePartId)
                        && await reports.GetDatePartDisplayValueAsync(datePartId, cancellationToken) is { } datePart)
                        value = datePart;
                    break;
                case DataType.Asset:
                case DataType.RelationParentChild:
                case DataType.RelationChildParent:
                case DataType.RelationOther:
                    if (property.IsSystem && property.SystemTypeId is (int)SystemType.AssetName or (int)SystemType.InstanceOf)
                        propertyName = report.ReportingLabel!;
                    value = await reports.GetAssetNameAsync(new Guid(filter.Value!), cancellationToken);
                    break;
                case DataType.PickList:
                    var pickListValue = await reports.GetPickListValueAsync(new Guid(filter.Value!), cancellationToken)
                        ?? throw new NullReferenceException($"Pick list value {filter.Value} not found.");
                    value = string.IsNullOrEmpty(pickListValue.DisplayValue) ? pickListValue.Value : pickListValue.DisplayValue;
                    break;
                case DataType.User:
                    value = await MemberDisplayValueAsync(new Guid(filter.Value!), cancellationToken);
                    break;
            }
            if (filter.Value == EmptyGuid)
                value = "null";
            items.Add($"{propertyName} {filter.Operator} {value}");
        }
        return string.Format(CultureInfo.InvariantCulture, report.FilterLogic!, [.. items]);
    }

    private async Task<string> MemberDisplayValueAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var member = await reports.GetMemberAsync(memberId, cancellationToken);
        if (member is null)
            return string.Empty;
        if (string.IsNullOrEmpty(member.FName) && string.IsNullOrEmpty(member.LName))
            return member.UserId;
        return $"{member.LName}, {member.FName} [{member.UserId}]";
    }
}

/// <summary>
/// ReportLayer.Save: validation (logical failures), the filter string, the rows in the original's order without a
/// transaction (a failure leaves the earlier writes), a full reload, then the view: dropped, then recreated; if the
/// CREATE fails a report posted as new is killed (even one that existed) and the failure becomes a logical one.
/// </summary>
public sealed class ReportSaver(IReportStore store, ReportLoader loader, ReportFilterString filterString, IReportViewGenerator views)
{
    private const int NotApplicable = 0;

    public async Task<bool> SaveAsync(ReportContract report, CancellationToken cancellationToken)
    {
        var isNew = report.IsNew;
        if (string.IsNullOrEmpty(report.ReportName))
            throw new LegacyLogicalException("Report Name must be defined.");
        if ((report.Filters is null || report.Filters.Count == 0) && !string.IsNullOrEmpty(report.FilterLogic))
            report.FilterLogic = string.Empty;
        if (report.Filters is { Count: > 0 } && string.IsNullOrEmpty(report.FilterLogic))
            throw new LegacyLogicalException("Filter Logic not provided and the report has filters defined.");
        report.FilterString = await filterString.BuildAsync(report, cancellationToken);

        await store.SaveReportAsync(report, cancellationToken);
        await store.DeletePropertiesAsync(report.Id, cancellationToken);
        foreach (var property in report.ReportProperties ?? throw new NullReferenceException("ReportProperties is null."))
        {
            var isGrouping = (report.GroupColumns ?? throw new NullReferenceException("GroupColumns is null.")).Contains(property.PropertyId);
            await store.InsertPropertyAsync(report.Id, property, isGrouping, cancellationToken);
            if (property.CustomReportFieldType != NotApplicable && property.PropertyIds is { Count: > 0 } subIds)
            {
                for (var n = 0; n < subIds.Count; n++)
                    await store.InsertPropertyPropertyAsync(property.Id, subIds[n], n, cancellationToken);
            }
        }
        await store.DeleteFiltersAsync(report.Id, cancellationToken);
        foreach (var filter in report.Filters ?? throw new NullReferenceException("Filters is null."))
            await store.InsertFilterAsync(report.Id, filter, cancellationToken);

        var loaded = await loader.LoadAsync(report.Id, cancellationToken);
        if (!string.IsNullOrEmpty(loaded.Report.StoredProc))
            return true;
        var select = await views.BuildSelectAsync(report.Id, cancellationToken) ?? throw new InvalidOperationException("Report view could not be built.");
        await views.ReplaceAsync(report.Id, select, async failure =>
        {
            if (isNew)
                await store.KillAsync(report.Id, cancellationToken);
            throw new LegacyLogicalException(failure.ToString());
        }, cancellationToken);
        return true;
    }
}
