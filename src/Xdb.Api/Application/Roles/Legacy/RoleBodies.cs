using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Views.Legacy;

namespace Xdb.Api.Application.Roles.Legacy;

/// <summary>
/// Reads role request bodies as the original's data-contract serializer did with its types (measured with .NET
/// Framework): every member is required at every depth; unknown members are ignored; a bad Guid, a null for a value
/// type, or a date not written as /Date(ms[+-hhmm])/ fails (the Request Error page); null lists stay null. Dates with
/// an offset become the instant in server-local time; dates without one keep the UTC wall time.
/// </summary>
public static partial class RoleBodies
{
    private static readonly string[] RoleMembers =
        ["AssetTypes", "Created", "CreatedBy", "Deleted", "DeletedBy", "Description", "Id", "IsActive", "IsDirty", "IsNew", "IsSystem", "LastModified",
         "LastModifiedBy", "Members", "Name", "Reports", "RoleLead"];
    private static readonly string[] MemberMembers = ["Id", "IsDeleted", "IsDirty", "IsNew", "MemberId", "MemberName"];
    private static readonly string[] ReportMembers = ["Created", "CreatedBy", "Id", "IsDeleted", "IsDirty", "IsNew", "ReportId", "ReportName"];
    private static readonly string[] AssetTypeMembers = ["AssetTypeId", "Id", "InheritFrom", "IsDeleted", "IsDirty", "IsInherited", "IsInstance", "IsNew", "Permissions", "RoleId"];
    private static readonly string[] PermissionMembers = ["FilterExpression", "FilterString", "Filters", "Id", "IsDeleted", "IsDirty", "IsIncludesPermission", "IsNew", "PermissionId"];
    private static readonly string[] FilterMembers = ["DisplayValue", "Id", "Operator", "OperatorId", "Order", "Property", "PropertyId", "Value"];
    private static readonly string[] ViewRelationMembers = ["Created", "CreatedBy", "Deleted", "DeletedBy", "Id", "IsAutoApprove", "IsDirty", "IsNew", "RoleId", "ViewId"];
    private static readonly string[] ProcessRelationMembers = ["AssetTypeId", "Created", "CreatedBy", "Deleted", "DeletedBy", "Id", "IsDirty", "IsNew", "ProcessId", "RoleId"];

    /// <summary>A Role, or null for an empty or null body.</summary>
    public static RoleContract? Role(JsonElement? root)
    {
        if (root is null)
            return null;
        var r = Object(root.Value, RoleMembers);
        return new RoleContract
        {
            AssetTypes = List(r.GetProperty("AssetTypes"), AssetType),
            Created = Date(r.GetProperty("Created")),
            CreatedBy = Guid(r.GetProperty("CreatedBy")),
            Deleted = NullableDate(r.GetProperty("Deleted")),
            DeletedBy = NullableGuid(r.GetProperty("DeletedBy")),
            Description = LegacyJsonBody.Text(r.GetProperty("Description")),
            Id = Guid(r.GetProperty("Id")),
            IsActive = Bool(r.GetProperty("IsActive")),
            IsDirty = Bool(r.GetProperty("IsDirty")),
            IsNew = Bool(r.GetProperty("IsNew")),
            IsSystem = Bool(r.GetProperty("IsSystem")),
            LastModified = NullableDate(r.GetProperty("LastModified")),
            LastModifiedBy = NullableGuid(r.GetProperty("LastModifiedBy")),
            Members = List(r.GetProperty("Members"), Member),
            Name = LegacyJsonBody.Text(r.GetProperty("Name")),
            Reports = List(r.GetProperty("Reports"), Report),
            RoleLead = NullableGuid(r.GetProperty("RoleLead"))
        };
    }

    /// <summary>
    /// A RoleViewRelationList, or null for an empty or null body. An object body is an empty list; a null element
    /// fails. A relation's IsDirty follows the setters in posted order; a repeated ViewId keeps the first.
    /// </summary>
    public static List<RoleViewRelationContract>? ViewRelations(JsonElement? root)
    {
        if (root is null)
            return null;
        var result = new List<RoleViewRelationContract>();
        foreach (var item in Elements(root.Value))
        {
            var e = Object(item, ViewRelationMembers);
            var relation = new RoleViewRelationContract();
            foreach (var member in e.EnumerateObject())
            {
                switch (member.Name)
                {
                    case "Id": relation.IsDirty |= Set(relation.Id, Guid(member.Value), v => relation.Id = v); break;
                    case "RoleId": relation.IsDirty |= Set(relation.RoleId, Guid(member.Value), v => relation.RoleId = v); break;
                    case "ViewId": relation.IsDirty |= Set(relation.ViewId, Guid(member.Value), v => relation.ViewId = v); break;
                    case "CreatedBy": relation.IsDirty |= Set(relation.CreatedBy, Guid(member.Value), v => relation.CreatedBy = v); break;
                    case "Created": relation.IsDirty |= Set(relation.Created, Date(member.Value), v => relation.Created = v); break;
                    // The nullable setters compared against an unset value, so they always marked the relation dirty.
                    case "Deleted": relation.Deleted = NullableDate(member.Value); relation.IsDirty = true; break;
                    case "DeletedBy": relation.DeletedBy = NullableGuid(member.Value); relation.IsDirty = true; break;
                    case "IsDirty": relation.IsDirty = Bool(member.Value); break;
                    case "IsNew": relation.IsNew = Bool(member.Value); break;
                    case "IsAutoApprove": relation.IsAutoApprove = Bool(member.Value); break;
                }
            }
            if (!result.Exists(r => r.ViewId == relation.ViewId))
                result.Add(relation);
        }
        return result;
    }

    /// <summary>A RoleAssetTypeProcessRelationList (posted IsDirty kept; a repeated ProcessId keeps the first), or null.</summary>
    public static List<RoleProcessRelationContract>? ProcessRelations(JsonElement? root)
    {
        if (root is null)
            return null;
        var result = new List<RoleProcessRelationContract>();
        foreach (var item in Elements(root.Value))
        {
            var e = Object(item, ProcessRelationMembers);
            var relation = new RoleProcessRelationContract
            {
                AssetTypeId = Guid(e.GetProperty("AssetTypeId")),
                Created = Date(e.GetProperty("Created")),
                CreatedBy = Guid(e.GetProperty("CreatedBy")),
                Deleted = NullableDate(e.GetProperty("Deleted")),
                DeletedBy = NullableGuid(e.GetProperty("DeletedBy")),
                Id = Guid(e.GetProperty("Id")),
                IsDirty = Bool(e.GetProperty("IsDirty")),
                IsNew = Bool(e.GetProperty("IsNew")),
                ProcessId = Guid(e.GetProperty("ProcessId")),
                RoleId = Guid(e.GetProperty("RoleId"))
            };
            if (!result.Exists(r => r.ProcessId == relation.ProcessId))
                result.Add(relation);
        }
        return result;
    }

    private static RoleMemberContract? Member(JsonElement element)
    {
        var e = Object(element, MemberMembers);
        return new RoleMemberContract
        {
            Id = Guid(e.GetProperty("Id")), IsDeleted = Bool(e.GetProperty("IsDeleted")), IsDirty = Bool(e.GetProperty("IsDirty")),
            IsNew = Bool(e.GetProperty("IsNew")), MemberId = Guid(e.GetProperty("MemberId")), MemberName = LegacyJsonBody.Text(e.GetProperty("MemberName"))
        };
    }

    private static RoleReportContract? Report(JsonElement element)
    {
        var e = Object(element, ReportMembers);
        return new RoleReportContract
        {
            Created = Date(e.GetProperty("Created")), CreatedBy = Guid(e.GetProperty("CreatedBy")), Id = Guid(e.GetProperty("Id")),
            IsDeleted = Bool(e.GetProperty("IsDeleted")), IsDirty = Bool(e.GetProperty("IsDirty")), IsNew = Bool(e.GetProperty("IsNew")),
            ReportId = Guid(e.GetProperty("ReportId")), ReportName = LegacyJsonBody.Text(e.GetProperty("ReportName"))
        };
    }

    private static RoleAssetTypeContract? AssetType(JsonElement element)
    {
        var e = Object(element, AssetTypeMembers);
        return new RoleAssetTypeContract
        {
            AssetTypeId = Guid(e.GetProperty("AssetTypeId")), Id = Guid(e.GetProperty("Id")), InheritFrom = NullableGuid(e.GetProperty("InheritFrom")),
            IsDeleted = Bool(e.GetProperty("IsDeleted")), IsDirty = Bool(e.GetProperty("IsDirty")), IsInherited = Bool(e.GetProperty("IsInherited")),
            IsInstance = Bool(e.GetProperty("IsInstance")), IsNew = Bool(e.GetProperty("IsNew")),
            Permissions = List(e.GetProperty("Permissions"), Permission), RoleId = Guid(e.GetProperty("RoleId"))
        };
    }

    private static RolePermissionContract? Permission(JsonElement element)
    {
        var e = Object(element, PermissionMembers);
        return new RolePermissionContract
        {
            FilterExpression = LegacyJsonBody.Text(e.GetProperty("FilterExpression")), FilterString = LegacyJsonBody.Text(e.GetProperty("FilterString")),
            Filters = List(e.GetProperty("Filters"), Filter), Id = Guid(e.GetProperty("Id")), IsDeleted = Bool(e.GetProperty("IsDeleted")),
            IsDirty = Bool(e.GetProperty("IsDirty")), IsIncludesPermission = Bool(e.GetProperty("IsIncludesPermission")), IsNew = Bool(e.GetProperty("IsNew")),
            PermissionId = LegacyJsonBody.Int(e.GetProperty("PermissionId"))
        };
    }

    private static FilterContract? Filter(JsonElement element)
    {
        var e = Object(element, FilterMembers);
        return new FilterContract
        {
            DisplayValue = LegacyJsonBody.Text(e.GetProperty("DisplayValue")), Id = Guid(e.GetProperty("Id")), Operator = LegacyJsonBody.Text(e.GetProperty("Operator"))!,
            OperatorId = LegacyJsonBody.Int(e.GetProperty("OperatorId")), Order = LegacyJsonBody.Int(e.GetProperty("Order")),
            Property = LegacyJsonBody.Text(e.GetProperty("Property"))!, PropertyId = Guid(e.GetProperty("PropertyId")), Value = LegacyJsonBody.Text(e.GetProperty("Value"))!
        };
    }

    /// <summary>A list member: null stays null; an array is read item by item (a null item stays null); anything else fails.</summary>
    internal static List<T>? List<T>(JsonElement element, Func<JsonElement, T?> read) where T : class => element.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.Array => [.. element.EnumerateArray().Select(item => item.ValueKind == JsonValueKind.Null ? null! : read(item)!)],
        _ => throw new LegacyBodyException("Not an array.")
    };

    /// <summary>A top-level relation list: an array's items (a null item fails), an object as an empty list, a scalar fails.</summary>
    private static IEnumerable<JsonElement> Elements(JsonElement root) => root.ValueKind switch
    {
        JsonValueKind.Array => root.EnumerateArray().Select(item => item.ValueKind == JsonValueKind.Null ? throw new LegacyBodyException("Null item.") : item),
        JsonValueKind.Object => [],
        _ => throw new LegacyBodyException("Not a list.")
    };

    internal static JsonElement Object(JsonElement element, string[] required) =>
        element.ValueKind == JsonValueKind.Object && required.All(name => element.TryGetProperty(name, out _))
            ? element
            : throw new LegacyBodyException("Missing a required member.");

    private static bool Set<T>(T current, T value, Action<T> store)
    {
        if (EqualityComparer<T>.Default.Equals(current, value))
            return false;
        store(value);
        return true;
    }

    internal static Guid Guid(JsonElement element) =>
        element.ValueKind == JsonValueKind.String && System.Guid.TryParse(element.GetString(), out var id) ? id : throw new LegacyBodyException("Not a Guid.");

    internal static Guid? NullableGuid(JsonElement element) => element.ValueKind == JsonValueKind.Null ? null : Guid(element);

    internal static bool Bool(JsonElement element) => element.ValueKind == JsonValueKind.Null ? throw new LegacyBodyException("Null boolean.") : LegacyJsonBody.Bool(element);

    internal static DateTime? NullableDate(JsonElement element) => element.ValueKind == JsonValueKind.Null ? null : Date(element);

    /// <summary>A DCJS date: "/Date(ms)/" keeps the UTC wall time; with an offset it is the instant in server-local time.</summary>
    public static DateTime Date(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.String || DcjsDate().Match(element.GetString()!) is not { Success: true } match)
            throw new LegacyBodyException("Not a date.");
        var instant = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(match.Groups["ms"].Value, CultureInfo.InvariantCulture)).UtcDateTime;
        return match.Groups["offset"].Success ? instant.ToLocalTime() : instant;
    }

    [GeneratedRegex(@"^/Date\((?<ms>-?\d+)(?<offset>[+-]\d{4})?\)/$")]
    private static partial Regex DcjsDate();
}
