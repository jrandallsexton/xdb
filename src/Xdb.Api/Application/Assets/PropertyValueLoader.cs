using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Metadata;
using Xdb.Api.Application.Metadata.Legacy;
using Xdb.Api.Infrastructure.Data.Procedures;

namespace Xdb.Api.Application.Assets;

/// <summary>A property value as the original's PropertyValue object held it after loading.</summary>
public sealed class PropertyValueModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PropertyId { get; set; }
    public Guid AssetId { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? DisplayValue { get; set; }
    public string? DisplayValueHtml { get; set; }
    public int? Order { get; set; }
    public DateTime Created { get; set; }
    public Guid CreatedBy { get; set; }
}

/// <summary>
/// The original's PropertyValueLayer.PropertyValues_GetForAnAsset and the data layer's value loading: own values,
/// values read from the parent (or definition) for inherited and definition-level properties, synthesized values for
/// the system properties, then the value formatting and display lookups.
/// </summary>
public sealed class PropertyValueLoader(IAssetProcedures assets, IMetadataProcedures metadata, MetadataLoader metadataLoader)
{
    /// <summary>The original's en-US currency format (negatives in parentheses), independent of ICU data.</summary>
    private static readonly NumberFormatInfo Currency = CreateCurrencyFormat();
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    /// <returns>Null when the asset or its (live) asset type does not exist, as the original returned.</returns>
    public async Task<List<PropertyValueModel>?> ForAssetAsync(Guid assetId, IReadOnlyList<Guid> propertyIds, bool includeUnapproved, bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var asset = await assets.GetAssetAsync(assetId, cancellationToken);
        if (asset is null)
            return null;
        var assetType = await metadataLoader.AssetTypeAsync(asset.AssetTypeId, cancellationToken);
        if (assetType is null)
            return null;

        var own = new List<Guid>();
        var fromParent = new List<Guid>();
        var system = new List<PropertyContract>();
        var properties = (await metadata.GetPropertiesAsync(propertyIds, cancellationToken)).ToDictionary(p => p.Id, MetadataLoader.Property);
        var isInstance = asset.IsInstance ?? false;
        foreach (var propertyId in propertyIds)
        {
            var property = properties[propertyId];
            if (property.IsSystem)
            {
                if (property.SystemType is (int)SystemType.AssetName or (int)SystemType.AssetType or (int)SystemType.Description
                    or (int)SystemType.InstanceOf or (int)SystemType.InstanceOfDescription or (int)SystemType.DisplayValue)
                    system.Add(property);
                continue;
            }
            var relation = assetType.PropertyMembers.FirstOrDefault(r => r.PropertyId == propertyId);
            if (relation is not null && ((relation.IsInheritedValue && isInstance) || (isInstance && !relation.IsInstance)))
            {
                if (!fromParent.Contains(propertyId))
                    fromParent.Add(propertyId);
            }
            else if (!own.Contains(propertyId))
            {
                own.Add(propertyId);
            }
        }

        List<PropertyValueModel>? result;
        if (fromParent.Count > 0)
        {
            result = [.. await LoadAsync(assetId, own, includeUnapproved, includeDeleted, cancellationToken)
                ?? throw new NullReferenceException("Values were not loaded.")];
            Guid? source;
            if (await assets.IsInstanceAsync(assetId, cancellationToken))
                source = await assets.GetInstanceOfIdAsync(assetId, cancellationToken);
            else
                source = await assets.GetParentAssetIdAsync(assetId, cancellationToken);
            if (source is { } sourceId)
                result.AddRange(await LoadAsync(sourceId, fromParent, includeUnapproved, includeDeleted, cancellationToken)
                    ?? throw new NullReferenceException("Values were not loaded."));
        }
        else
        {
            result = await LoadAsync(assetId, own, includeUnapproved, includeDeleted, cancellationToken);
        }

        foreach (var property in system)
        {
            var value = await SystemValueAsync(property, assetId, asset, assetType, cancellationToken);
            if (value is null)
                continue;
            (result ?? throw new NullReferenceException("Values were not loaded.")).Add(value);
        }
        return result;
    }

    /// <summary>The original's PropertyValueLayer.Get(propertyId, assetId): the first approved live value, decoded.</summary>
    public async Task<PropertyValueModel?> GetAsync(Guid propertyId, Guid assetId, CancellationToken cancellationToken)
    {
        var id = await assets.GetApprovedValueIdAsync(propertyId, assetId, cancellationToken);
        if (id == Guid.Empty)
            return null;
        var row = await assets.GetPropertyValueAsync(id, cancellationToken);
        if (row is null)
            return null;
        var value = new PropertyValueModel { Id = id, PropertyId = row.PropertyId, AssetId = row.AssetId, Value = row.Value ?? string.Empty, Order = row.Order,
            Created = row.Created, CreatedBy = row.CreatedBy };
        if (!string.IsNullOrEmpty(value.Value))
        {
            var property = await metadata.GetPropertyAsync(value.PropertyId, cancellationToken) is { } p ? MetadataLoader.Property(p) : null;
            value.Value = FormatValue(property, value.Value);
            value.DisplayValue = await DisplayValueAsync(value.Value, property, cancellationToken);
            value.DisplayValueHtml = await DisplayValueHtmlAsync(value, property, cancellationToken);
        }
        return value;
    }

    /// <summary>PropertyValueDal.PropertyValues_GetForAnAsset: null when unapproved values are requested without history.</summary>
    private async Task<List<PropertyValueModel>?> LoadAsync(Guid assetId, IReadOnlyList<Guid> propertyIds, bool includeUnapproved, bool includeDeleted,
        CancellationToken cancellationToken)
    {
        if (propertyIds.Count == 0)
            return [];
        if (includeUnapproved && !includeDeleted)
            return null;

        var values = (await assets.GetAssetValuesAsync(assetId, propertyIds, includeDeleted, cancellationToken)).Select(row => new PropertyValueModel
        {
            Id = row.Id, PropertyId = row.PropertyId, AssetId = row.AssetId, Value = row.Value ?? string.Empty, Order = row.Order,
            Created = row.Created, CreatedBy = row.CreatedBy, DisplayValue = row.DisplayValue
        }).ToList();
        if (values.Count == 0)
            return values;

        var properties = (await metadata.GetPropertiesAsync(values.Select(v => v.PropertyId).Distinct().ToList(), cancellationToken))
            .ToDictionary(p => p.Id, MetadataLoader.Property);
        foreach (var value in values)
        {
            var property = properties[value.PropertyId];
            value.Value = FormatValue(property, value.Value);
            if (!string.IsNullOrEmpty(value.Value))
            {
                value.DisplayValue = await DisplayValueAsync(value.Value, property, cancellationToken);
                value.DisplayValueHtml = await DisplayValueHtmlAsync(value, property, cancellationToken);
            }
        }
        return values;
    }

    private async Task<PropertyValueModel?> SystemValueAsync(PropertyContract property, Guid assetId, AssetRow asset, AssetTypeContract assetType,
        CancellationToken cancellationToken)
    {
        var name = Truncate(asset.Name ?? string.Empty, 255, 254);
        var displayValue = Truncate(asset.DisplayValue ?? string.Empty, 100, 99);
        switch (property.SystemType)
        {
            case (int)SystemType.AssetName:
                return Synthesized(property, assetId, string.IsNullOrEmpty(displayValue) ? name : displayValue, asset.CreatedBy, asset.Created);
            case (int)SystemType.AssetType:
                var type = Synthesized(property, assetId, asset.AssetTypeId.ToString("D"), asset.CreatedBy, asset.Created);
                type.DisplayValue = assetType.Name;
                return type;
            case (int)SystemType.Description:
                return string.IsNullOrEmpty(asset.Description) ? null : Synthesized(property, assetId, asset.Description, asset.CreatedBy, asset.Created);
            case (int)SystemType.DisplayValue:
                return Synthesized(property, assetId, await assets.GetAssetDisplayValueTextAsync(assetId, cancellationToken), asset.CreatedBy, asset.Created);
            case (int)SystemType.InstanceOf:
            {
                var instanceOf = await assets.GetInstanceOfIdAsync(assetId, cancellationToken)
                    ?? throw new InvalidOperationException("Nullable object must have a value.");
                var text = await assets.GetAssetNameTextAsync(instanceOf, cancellationToken);
                var value = Synthesized(property, assetId, instanceOf.ToString("D"), asset.CreatedBy, asset.Created);
                value.DisplayValue = text;
                return value;
            }
            case (int)SystemType.InstanceOfDescription:
            {
                var instanceOf = await assets.GetInstanceOfIdAsync(assetId, cancellationToken)
                    ?? throw new InvalidOperationException("Nullable object must have a value.");
                var description = await GetAsync(SystemPropertyIds.Description, instanceOf, cancellationToken);
                if (description is null)
                    return null;
                var text = string.IsNullOrEmpty(description.DisplayValue) ? description.Value : description.DisplayValue;
                var createdBy = await assets.GetAssetCreatedByAsync(instanceOf, cancellationToken)
                    ?? throw new InvalidOperationException("Nullable object must have a value.");
                var value = Synthesized(property, assetId, text, createdBy, description.Created);
                value.CreatedBy = description.CreatedBy;
                value.DisplayValue = text;
                return value;
            }
            default:
                return null;
        }
    }

    private static PropertyValueModel Synthesized(PropertyContract property, Guid assetId, string value, Guid createdBy, DateTime created) =>
        new() { PropertyId = property.Id, AssetId = assetId, Value = value, CreatedBy = createdBy, Created = created };

    /// <summary>FormatPropertyValue: only currency values change (numbers formatted as en-US currency).</summary>
    public static string FormatValue(PropertyContract? property, string value)
    {
        if (property?.DataType != (int)LegacyDataType.Currency || string.IsNullOrEmpty(value))
            return value;
        if (value.Length == 1 && !int.TryParse(value, NumberStyles.Integer, EnUs, out _))
            return string.Empty;
        var first = value[..1];
        if (int.TryParse(first, NumberStyles.Integer, EnUs, out _))
            return double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, EnUs, out var number) ? number.ToString("C", Currency) : value;
        if (first == "$")
            return double.TryParse(value[1..], NumberStyles.Float | NumberStyles.AllowThousands, EnUs, out var amount) ? amount.ToString("C", Currency) : value;
        return value;
    }

    /// <summary>PopulateDisplayValue: the readable text of a stored value, by the property's system type or data type.</summary>
    public async Task<string?> DisplayValueAsync(string value, PropertyContract? property, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        Guid.TryParse(value, out var id);
        if (property is null)
            return string.Empty;

        if (property.IsSystem)
        {
            switch (property.SystemType)
            {
                case (int)SystemType.AssetType:
                    return await metadata.GetAssetTypeNameAsync(id, cancellationToken);
                case (int)SystemType.CreatedBy:
                case (int)SystemType.DeletedBy:
                    return await assets.GetMemberAsync(id, cancellationToken) is { } member ? $"{member.LastName}, {member.FirstName} [{member.UserId}]" : string.Empty;
                default:
                    return value;
            }
        }

        switch (property.DataType)
        {
            case (int)LegacyDataType.Asset:
            case (int)LegacyDataType.Relation_ParentChild:
            case (int)LegacyDataType.Relation_ChildParent:
            case (int)LegacyDataType.Dependency:
            case (int)LegacyDataType.Relation_Other:
                if (await assets.GetAssetAsync(id, cancellationToken) is { } asset)
                {
                    var display = Truncate(asset.DisplayValue ?? string.Empty, 100, 99);
                    return string.IsNullOrEmpty(display) ? Truncate(asset.Name ?? string.Empty, 255, 254) : display;
                }
                return string.Empty;
            case (int)LegacyDataType.User:
                return await assets.GetMemberAsync(id, cancellationToken) is { } user ? $"{user.LastName}, {user.FirstName} [{user.UserId}]" : string.Empty;
            case (int)LegacyDataType.Document:
                return await assets.GetDocumentAsync(id, cancellationToken) is { } document ? document.Title : string.Empty;
            case (int)LegacyDataType.Image:
                return await metadata.GetImageAsync(id, cancellationToken) is { } image ? image.Name ?? string.Empty : string.Empty;
            case (int)LegacyDataType.PickList:
                return await assets.GetPickListValueTextAsync(property.PickListId!.Value, value, cancellationToken);
            case (int)LegacyDataType.Currency:
                if (await metadata.GetCurrencyValueAsync(id, cancellationToken) is { } currency)
                {
                    var amount = currency.Value.ToString(CultureInfo.InvariantCulture);
                    return currency.SymbolDisplay.Length > 1 ? $"{currency.Symbol} {amount}" : $"{currency.Symbol}{amount}";
                }
                return value;
            case (int)LegacyDataType.System_AssetType:
                return await metadata.GetAssetTypeNameAsync(id, cancellationToken);
            case (int)LegacyDataType.URL:
                if (await assets.GetUrlAsync(id, cancellationToken) is { } url)
                    return string.IsNullOrEmpty(url.Name) ? url.Url : $"{url.Name}|{url.Url}";
                return string.Empty;
            default:
                return value;
        }
    }

    /// <summary>PopulateDisplayValueHtml: for Guid-valued values, a currency or link rendering, else "" or the value.</summary>
    public async Task<string> DisplayValueHtmlAsync(PropertyValueModel value, PropertyContract? property, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(value.Value) || !Guid.TryParse(value.Value, out var id) || property is null)
            return string.Empty;
        switch (property.DataType)
        {
            case (int)LegacyDataType.Asset:
            case (int)LegacyDataType.Relation_ParentChild:
            case (int)LegacyDataType.Relation_ChildParent:
            case (int)LegacyDataType.Dependency:
            case (int)LegacyDataType.Relation_Other:
            case (int)LegacyDataType.User:
            case (int)LegacyDataType.Document:
            case (int)LegacyDataType.Image:
            case (int)LegacyDataType.String:
            case (int)LegacyDataType.System_AssetType:
                return string.Empty;
            case (int)LegacyDataType.Currency:
                return await metadata.GetCurrencyValueAsync(id, cancellationToken) is { } currency
                    ? $"{currency.Symbol}^{currency.Value.ToString(CultureInfo.InvariantCulture)}"
                    : string.Empty;
            case (int)LegacyDataType.URL:
                if (await assets.GetUrlAsync(id, cancellationToken) is { } url)
                    return string.IsNullOrEmpty(url.Name) ? $"<a href='{url.Url}' target='_new'>{url.Url}</a>" : $"<a href='{url.Url}' target='_new'>{url.Name}</a>";
                return value.Value;
            default:
                return value.Value;
        }
    }

    /// <summary>The original's setters truncated over-long names and display values.</summary>
    public static string Truncate(string text, int limit, int keep) => text.Length > limit ? text[..keep] : text;

    private static NumberFormatInfo CreateCurrencyFormat()
    {
        var format = (NumberFormatInfo)CultureInfo.GetCultureInfo("en-US").NumberFormat.Clone();
        format.CurrencySymbol = "$";
        format.CurrencyDecimalDigits = 2;
        format.CurrencyDecimalSeparator = ".";
        format.CurrencyGroupSeparator = ",";
        format.CurrencyGroupSizes = [3];
        format.CurrencyPositivePattern = 0;
        format.CurrencyNegativePattern = 0;
        return format;
    }
}
