using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Infrastructure.Data.Procedures;

namespace Xdb.Api.Application.GeneratedObjects.Views;

/// <summary>Loads what the original's GetSqlString read while building a view, so the SQL can be built without I/O.</summary>
public sealed class ViewInputLoader(IGeneratedObjectProcedures procedures)
{
    public async Task<ViewInput> LoadAsync(Guid assetTypeId, bool isInstance, IReadOnlyList<ViewColumn> columns,
        IReadOnlyList<ViewFilter> filters, string? filterLogic, CancellationToken cancellationToken)
    {
        var label = await procedures.GetReportingLabelAsync(assetTypeId, isInstance, cancellationToken);
        var definitionLabel = isInstance ? await procedures.GetReportingLabelAsync(assetTypeId, forInstances: false, cancellationToken) : label;

        var ids = ViewInput.ReferencedPropertyIds(columns, filters);
        var properties = new Dictionary<Guid, PropertyMetaRow>();
        foreach (var property in await procedures.GetPropertiesAsync(ids, cancellationToken))
            properties.TryAdd(property.Id, property);

        // As in the original, two live relations for the same property fail the view (Dictionary.Add).
        var relations = new Dictionary<Guid, PropertyRelationRow>();
        foreach (var relation in await procedures.GetRelationsAsync(assetTypeId, ids, cancellationToken))
            relations.Add(relation.PropertyId, relation);

        var lookup = new Dictionary<(Guid, Guid), PropertyRelationRow>();
        async Task LookUpAsync(Guid typeId, Guid propertyId)
        {
            if (lookup.ContainsKey((typeId, propertyId)))
                return;
            if (await procedures.GetRelationAsync(typeId, propertyId, cancellationToken) is { } found)
                lookup[(typeId, propertyId)] = found;
        }

        foreach (var filter in filters)
            await LookUpAsync(assetTypeId, filter.PropertyId);
        foreach (var column in columns)
        {
            if (column.SubPropertyId is { } sub && sub != Guid.Empty
                && properties.TryGetValue(column.PropertyId, out var parent) && parent.AssetTypeId is { } parentTypeId)
                await LookUpAsync(parentTypeId, sub);
        }

        return new ViewInput(assetTypeId, isInstance, label, definitionLabel, columns, filters, filterLogic, properties, relations, lookup);
    }
}
