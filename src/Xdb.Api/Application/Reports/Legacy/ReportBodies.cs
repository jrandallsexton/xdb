using System.Text.Json;

using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Roles.Legacy;
using Xdb.Api.Application.Views.Legacy;

namespace Xdb.Api.Application.Reports.Legacy;

/// <summary>
/// Reads a posted Report as the original's data-contract serializer did: every member but StoredProc required at every
/// depth, no constructors run (a null list stays null), typed values strict (see <see cref="RoleBodies"/>), numbers
/// and enums may be quoted and any integer is accepted.
/// </summary>
public static class ReportBodies
{
    private static readonly string[] ReportMembers =
        ["AssetTypeId", "Created", "CreatedBy", "Deleted", "DeletedBy", "FilterLogic", "FilterString", "Filters", "GroupColumns", "Id", "IsInstance", "IsNew",
         "ReportName", "ReportProperties", "ReportingLabel"];
    private static readonly string[] PropertyMembers =
        ["CustomReportFieldType", "Id", "Index", "IsGrouping", "Label", "Logic", "PropertyId", "PropertyIds", "PropertyName", "SubPropertyId", "SubPropertyName",
         "TimeElapsedFormat", "TimeElapsedSpecificDate", "TimeElapsedType"];
    private static readonly string[] FilterMembers = ["DisplayValue", "Id", "Operator", "OperatorId", "Order", "Property", "PropertyId", "Value"];

    /// <summary>A Report, or null for an empty or null body.</summary>
    public static ReportContract? Report(JsonElement? root)
    {
        if (root is null)
            return null;
        var r = RoleBodies.Object(root.Value, ReportMembers);
        return new ReportContract
        {
            AssetTypeId = RoleBodies.Guid(r.GetProperty("AssetTypeId")),
            Created = RoleBodies.Date(r.GetProperty("Created")),
            CreatedBy = RoleBodies.Guid(r.GetProperty("CreatedBy")),
            Deleted = RoleBodies.NullableDate(r.GetProperty("Deleted")),
            DeletedBy = RoleBodies.NullableGuid(r.GetProperty("DeletedBy")),
            FilterLogic = LegacyJsonBody.Text(r.GetProperty("FilterLogic")),
            FilterString = LegacyJsonBody.Text(r.GetProperty("FilterString")),
            Filters = RoleBodies.List(r.GetProperty("Filters"), Filter),
            GroupColumns = GuidList(r.GetProperty("GroupColumns")),
            Id = RoleBodies.Guid(r.GetProperty("Id")),
            IsInstance = RoleBodies.Bool(r.GetProperty("IsInstance")),
            IsNew = RoleBodies.Bool(r.GetProperty("IsNew")),
            ReportName = LegacyJsonBody.Text(r.GetProperty("ReportName")),
            ReportProperties = RoleBodies.List(r.GetProperty("ReportProperties"), Property),
            ReportingLabel = LegacyJsonBody.Text(r.GetProperty("ReportingLabel")),
            StoredProc = r.TryGetProperty("StoredProc", out var storedProc) ? LegacyJsonBody.Text(storedProc) : null
        };
    }

    private static ReportPropertyContract? Property(JsonElement element)
    {
        var e = RoleBodies.Object(element, PropertyMembers);
        return new ReportPropertyContract
        {
            CustomReportFieldType = Int(e.GetProperty("CustomReportFieldType")),
            Id = RoleBodies.Guid(e.GetProperty("Id")),
            Index = Int(e.GetProperty("Index")),
            IsGrouping = RoleBodies.Bool(e.GetProperty("IsGrouping")),
            Label = LegacyJsonBody.Text(e.GetProperty("Label")),
            Logic = LegacyJsonBody.Text(e.GetProperty("Logic")),
            PropertyId = RoleBodies.Guid(e.GetProperty("PropertyId")),
            PropertyIds = GuidList(e.GetProperty("PropertyIds")),
            PropertyName = LegacyJsonBody.Text(e.GetProperty("PropertyName")),
            SubPropertyId = RoleBodies.NullableGuid(e.GetProperty("SubPropertyId")),
            SubPropertyName = LegacyJsonBody.Text(e.GetProperty("SubPropertyName")),
            TimeElapsedFormat = Int(e.GetProperty("TimeElapsedFormat")),
            TimeElapsedSpecificDate = RoleBodies.NullableDate(e.GetProperty("TimeElapsedSpecificDate")),
            TimeElapsedType = Int(e.GetProperty("TimeElapsedType"))
        };
    }

    private static FilterContract? Filter(JsonElement element)
    {
        var e = RoleBodies.Object(element, FilterMembers);
        return new FilterContract
        {
            DisplayValue = LegacyJsonBody.Text(e.GetProperty("DisplayValue")),
            Id = RoleBodies.Guid(e.GetProperty("Id")),
            Operator = LegacyJsonBody.Text(e.GetProperty("Operator"))!,
            OperatorId = Int(e.GetProperty("OperatorId")),
            Order = Int(e.GetProperty("Order")),
            Property = LegacyJsonBody.Text(e.GetProperty("Property"))!,
            PropertyId = RoleBodies.Guid(e.GetProperty("PropertyId")),
            Value = LegacyJsonBody.Text(e.GetProperty("Value"))!
        };
    }

    /// <summary>A Guid list: null stays null; each item must be a Guid.</summary>
    private static System.Collections.Generic.List<System.Guid>? GuidList(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.Array => [.. System.Linq.Enumerable.Select(element.EnumerateArray(), RoleBodies.Guid)],
        _ => throw new LegacyBodyException("Not an array.")
    };

    private static int Int(JsonElement element) => element.ValueKind == JsonValueKind.Null ? throw new LegacyBodyException("Null number.") : LegacyJsonBody.Int(element);
}
