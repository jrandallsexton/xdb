using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Xdb.Api.Application.Assets.Legacy;

[DataContract(Name = "PropertyValueClient", Namespace = "")]
public sealed class PropertyValueClientContract
{
    [DataMember] public string? DisplayValue { get; set; }
    [DataMember] public string? DisplayValueHtml { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public int? Order { get; set; }
    [DataMember] public Guid PropertyId { get; set; }
    [DataMember] public string? Value { get; set; }

    public static PropertyValueClientContract From(PropertyValueModel value) => new()
    {
        Id = value.Id,
        PropertyId = value.PropertyId,
        Value = value.Value,
        DisplayValue = value.DisplayValue,
        DisplayValueHtml = value.DisplayValueHtml,
        Order = value.Order
    };
}

/// <summary>The asset editor's client model.</summary>
[DataContract(Name = "AssetClient", Namespace = "")]
public sealed class AssetClientContract
{
    [DataMember] public bool AllowDelete { get; set; }
    [DataMember] public bool AllowEdit { get; set; }
    [DataMember] public Guid AssetTypeId { get; set; }
    [DataMember] public string? Description { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public Guid? InstanceOfId { get; set; }
    [DataMember] public bool IsInstance { get; set; }
    [DataMember] public string? Name { get; set; }
    [DataMember] public List<PropertyValueClientContract> PropertyValues { get; set; } = [];
    /// <summary>The asset's LastModified (else Created) as en-US general date text.</summary>
    [DataMember(Name = "lastModified")] public string? LastModified { get; set; }
}

/// <summary>A history line; serialized by Json.NET in declaration order, as the original's data contract was.</summary>
public sealed record HistoricalValue(string PropertyName, string? PropertyValue, string CreationInfo);

/// <summary>A chart filter value: its display text (key), raw value (value) and whether the current selection has it.</summary>
[DataContract(Name = "AssetFilterItem", Namespace = "")]
public sealed class AssetFilterItemContract
{
    [DataMember(Name = "enabled")] public bool Enabled { get; set; }
    [DataMember(Name = "key")] public string? Key { get; set; }
    [DataMember(Name = "value")] public string? Value { get; set; }
}

/// <summary>The chart filter panel: property names (culture-sorted) and each property's values.</summary>
[DataContract(Name = "AssetFilterClient", Namespace = "")]
public sealed class AssetFilterClientContract
{
    [DataMember] public SortedDictionary<string, Guid> Properties { get; set; } = [];
    [DataMember] public Dictionary<Guid, List<AssetFilterItemContract>> Values { get; set; } = [];
}

/// <summary>An asset relation graph node (RGraph); <see cref="Data"/> always carries a null relation.</summary>
[DataContract(Name = "RGraphData", Namespace = "")]
public sealed class RGraphDataContract
{
    [DataMember(Name = "children")] public List<RGraphDataContract> Children { get; set; } = [];
    [DataMember(Name = "data")] public RGraphRelationContract Data { get; set; } = new();
    [DataMember(Name = "id")] public string? Id { get; set; }
    [DataMember(Name = "name")] public string? Name { get; set; }
}

[DataContract(Name = "RGraphDataRelation", Namespace = "")]
public sealed class RGraphRelationContract
{
    [DataMember(Name = "relation")] public string? Relation { get; set; }
}

/// <summary>An asset's hit count; freq is the count as text.</summary>
[DataContract(Name = "AssetHit", Namespace = "")]
public sealed class AssetHitContract
{
    [DataMember(Name = "freq")] public string? Freq { get; set; }
    [DataMember(Name = "id")] public Guid Id { get; set; }
    [DataMember(Name = "tag")] public string? Tag { get; set; }
}

/// <summary>A recently viewed asset with its view URL.</summary>
[DataContract(Name = "recentItem", Namespace = "")]
public sealed class RecentItemContract
{
    [DataMember(Name = "assetName")] public string? AssetName { get; set; }
    [DataMember(Name = "assetTypeId")] public string? AssetTypeId { get; set; }
    [DataMember(Name = "viewUrl")] public string? ViewUrl { get; set; }
}
