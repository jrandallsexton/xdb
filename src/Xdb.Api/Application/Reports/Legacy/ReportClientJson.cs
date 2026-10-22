using System;
using System.Collections.Generic;

using Newtonsoft.Json;

using Xdb.Api.Application.Reports.Queries.GetReportClient.Dtos;

namespace Xdb.Api.Application.Reports.Legacy;

/// <summary>
/// ReportClient_Get's body: the original serialized this shape with Json.NET (default settings, members in
/// declaration order, nulls written). <c>Id</c> echoes the reportId query string exactly as sent.
/// </summary>
public static class ReportClientJson
{
    private static readonly JsonSerializerSettings Settings = new();

    public static string Serialize(string rawReportId, ReportClientDto dto) =>
        JsonConvert.SerializeObject(new Wire
        {
            Id = rawReportId,
            Name = dto.Name,
            PropertyNames = dto.PropertyNames,
            GroupColumns = dto.GroupColumns,
            GroupColName = dto.GroupColumnName,
            FilterString = dto.FilterString
        }, Formatting.None, Settings);

    /// <summary>Member order is the wire contract.</summary>
    private sealed class Wire
    {
        public string? Id { get; init; }
        public string? Name { get; init; }
        public IReadOnlyList<string?>? PropertyNames { get; init; }
        public string? SubGridId { get; init; }
        public string? SubGridJoinCol { get; init; }
        public IReadOnlyList<Guid>? GroupColumns { get; init; }
        public string? GroupColName { get; init; }
        public string? FilterString { get; init; }
    }
}
