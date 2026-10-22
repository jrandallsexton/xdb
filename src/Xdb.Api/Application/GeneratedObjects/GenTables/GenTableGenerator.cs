using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Xdb.Api.Infrastructure.Data.Procedures;

namespace Xdb.Api.Application.GeneratedObjects.GenTables;

public interface IGenTableGenerator
{
    /// <summary>
    /// (Re)creates an asset type's gen tables and MERGE procedures: definition, then instance when the type has an
    /// instance label. The tables are loaded by their SELECT ... INTO.
    /// </summary>
    /// <returns>The number of tables created (each has one MERGE procedure).</returns>
    Task<int> GenerateAsync(Guid assetTypeId, CancellationToken cancellationToken = default);
}

/// <summary>
/// The original's CreateGeneratedObjects. Runs statement by statement without a transaction: a failure leaves the
/// type with whatever was created before it (a dropped table stays dropped), as in the original.
/// </summary>
public sealed class GenTableGenerator(IGeneratedObjectProcedures procedures, IOptions<GeneratedObjectsOptions> options) : IGenTableGenerator
{
    /// <summary>The original's timeout for gen-table DDL, in seconds.</summary>
    private const int DdlTimeout = 300;

    public async Task<int> GenerateAsync(Guid assetTypeId, CancellationToken cancellationToken = default)
    {
        var assetType = await procedures.GetAssetTypeAsync(assetTypeId, cancellationToken)
            ?? throw new InvalidOperationException($"Asset type {assetTypeId:D} does not exist.");

        var definitionLabel = await procedures.GetReportingLabelAsync(assetTypeId, forInstances: false, cancellationToken);
        var descendantsAndSelf = (await procedures.GetDescendantsAsync(assetTypeId, cancellationToken)).Append(assetTypeId).ToList();
        var hasChildAssetTypes = descendantsAndSelf.Count != 1;
        var loadAssetTypeIds = assetType.AllowAssets ? [assetTypeId] : descendantsAndSelf;

        var tables = 0;
        foreach (var isInstance in new[] { false, true })
        {
            if (isInstance && string.IsNullOrEmpty(assetType.InstanceLabel))
                continue;

            var names = await procedures.GetGenPropertyNamesAsync(assetTypeId, isInstance, hasChildAssetTypes, cancellationToken);
            var properties = (await procedures.GetPropertiesAsync(names.Select(n => n.Id).Distinct().ToList(), cancellationToken))
                .ToDictionary(p => p.Id);
            var propertyIds = GenPropertySet.Order(names, properties,
                assetNameLabel: await procedures.GetReportingLabelAsync(assetTypeId, forInstances: isInstance, cancellationToken),
                instanceOfLabel: await procedures.GetReportingLabelAsync(assetTypeId, forInstances: !isInstance, cancellationToken));

            // As in the original, two live relations for the same property fail the type (Dictionary.Add).
            var relations = new Dictionary<Guid, PropertyRelationRow>();
            foreach (var relation in await procedures.GetRelationsAsync(assetTypeId, propertyIds.ToList(), cancellationToken))
                relations.Add(relation.PropertyId, relation);

            var selects = GenTableSql.BuildSelects(new GenTableInput(assetTypeId, isInstance, propertyIds, properties, relations,
                loadAssetTypeIds, definitionLabel, options.Value.ViewImageUrl));

            var table = GeneratedObjectNames.Table(assetTypeId, isInstance, isRaw: false);
            var rawTable = GeneratedObjectNames.Table(assetTypeId, isInstance, isRaw: true);

            await procedures.ExecuteAsync(GenTableSql.CreateTable(table, selects.Decoded), DdlTimeout, cancellationToken);
            await procedures.ExecuteAsync(GenTableSql.CreateTable(rawTable, selects.Raw), DdlTimeout, cancellationToken);
            await procedures.ExecuteAsync(GenTableSql.AddPrimaryKey(table), DdlTimeout, cancellationToken);
            await procedures.ExecuteAsync(GenTableSql.AddPrimaryKey(rawTable), DdlTimeout, cancellationToken);

            foreach (var (name, select) in new[] { (table, selects.Decoded), (rawTable, selects.Raw) })
            {
                var columns = await procedures.GetTableColumnNamesAsync(name, cancellationToken);
                await procedures.ExecuteAsync(GenTableSql.DropProcedure(GeneratedObjectNames.UpdateProcedure(name)), DdlTimeout, cancellationToken);
                await procedures.ExecuteAsync(GenTableSql.CreateMergeProcedure(name, select, columns), DdlTimeout, cancellationToken);
            }

            tables += 2;
        }

        return tables;
    }
}
