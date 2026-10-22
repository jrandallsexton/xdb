using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects;
using Xdb.Api.Application.Metadata;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Assets.Queries;

/// <summary>DictionaryValues_GetForPropertyRestricted's answer and which writer the original used for it.</summary>
public sealed record RestrictedValues(List<KeyValuePair<Guid, string>> Values, bool AsJson);

public sealed record GetRestrictedValuesQuery(Guid ParentPropertyId, Guid ChildPropertyId, string? ParentPropertyValue, Guid SourcePropertyId);

public interface IGetRestrictedValuesQueryHandler
{
    Task<Result<RestrictedValues>> ExecuteAsync(GetRestrictedValuesQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// DictionaryValues_GetForPropertyRestricted: the values a dependent property may take given its parent's value. A
/// picklist parent restricts the child picklist by its dependencies (text/html); an asset parent offers the source
/// property's values on that asset, culture-sorted by text (application/json); anything else is [] (text/html).
/// </summary>
public sealed class GetRestrictedValuesQueryHandler(IMetadataProcedures metadata, IFilterProcedures filters, PropertyValueLoader values)
    : IGetRestrictedValuesQueryHandler
{
    public async Task<Result<RestrictedValues>> ExecuteAsync(GetRestrictedValuesQuery query, CancellationToken cancellationToken = default)
    {
        var parent = await metadata.GetPropertyAsync(query.ParentPropertyId, cancellationToken) is { } p ? MetadataLoader.Property(p) : null;
        var child = await metadata.GetPropertyAsync(query.ChildPropertyId, cancellationToken) is { } c ? MetadataLoader.Property(c) : null;
        if (parent is null || !Guid.TryParse(query.ParentPropertyValue, out var parentValue))
            return new Success<RestrictedValues>(new RestrictedValues([], AsJson: false));

        switch (parent.DataType)
        {
            case (int)LegacyDataType.PickList:
            {
                var childPickList = (child ?? throw new NullReferenceException("Child property not found.")).PickListId
                    ?? throw new InvalidOperationException("Nullable object must have a value.");
                var parentPickList = parent.PickListId ?? throw new InvalidOperationException("Nullable object must have a value.");
                return new Success<RestrictedValues>(new RestrictedValues(
                    [.. await filters.GetDependentPickListValuesAsync(childPickList, parentPickList, parentValue, cancellationToken)], AsJson: false));
            }
            case (int)LegacyDataType.Asset:
            {
                var loaded = await values.ForAssetAsync(parentValue, [query.SourcePropertyId], includeUnapproved: false, includeDeleted: false, cancellationToken)
                    ?? throw new NullReferenceException("Asset or asset type not found.");
                var sorted = new SortedDictionary<string, Guid>(LegacyCollation.EnUs);
                foreach (var item in loaded)
                    sorted.Add(string.IsNullOrEmpty(item.DisplayValue) ? item.Value : item.DisplayValue, new Guid(item.Value));
                var result = new List<KeyValuePair<Guid, string>>();
                foreach (var (text, id) in sorted)
                {
                    if (result.Exists(r => r.Key == id))
                        throw new ArgumentException($"An item with the same key has already been added: {id:D}");
                    result.Add(new KeyValuePair<Guid, string>(id, text));
                }
                return new Success<RestrictedValues>(new RestrictedValues(result, AsJson: true));
            }
            default:
                return new Success<RestrictedValues>(new RestrictedValues([], AsJson: false));
        }
    }
}

public sealed record GetPotentialParentsQuery(Guid ChildAssetTypeId);

public interface IGetPotentialParentsQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>?>> ExecuteAsync(GetPotentialParentsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Assets_GetPotentialParents: the parent type's definitions (as Assets_GetDictionary), or null without a parent type.</summary>
public sealed class GetPotentialParentsQueryHandler(IMetadataProcedures metadata, AssetDictionaries dictionaries) : IGetPotentialParentsQueryHandler
{
    public async Task<Result<List<KeyValuePair<Guid, string>>?>> ExecuteAsync(GetPotentialParentsQuery query, CancellationToken cancellationToken = default)
    {
        if (await metadata.GetParentAssetTypeIdAsync(query.ChildAssetTypeId, cancellationToken) is not { } parent)
            return new Success<List<KeyValuePair<Guid, string>>?>(null);
        return new Success<List<KeyValuePair<Guid, string>>?>(await dictionaries.DefinitionsAsync(parent, includeDescriptions: false, searchChildren: false, cancellationToken));
    }
}
