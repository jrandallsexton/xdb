using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xdb.Api.Infrastructure.Data.Procedures;

/// <summary>
/// Reads the metadata the generated-object generators need, as the original read it, and runs their DDL.
/// DDL runs without a transaction and with the connection's default SET options (ANSI_NULLS and
/// QUOTED_IDENTIFIER ON), which is what every original generated module was created with.
/// </summary>
public interface IGeneratedObjectProcedures
{
    /// <summary>The asset type whether or not it is deleted; null when absent.</summary>
    Task<GenAssetTypeRow?> GetAssetTypeAsync(Guid assetTypeId, CancellationToken cancellationToken);

    /// <summary>spr_AssetTypes_GetDictionary @IncludeUnused = 1: every non-deleted asset type, by name.</summary>
    Task<IReadOnlyList<Guid>> GetAssetTypeIdsAsync(CancellationToken cancellationToken);

    /// <summary>spr_AssetType_GetAllDescendants: live descendants, ordered by child name.</summary>
    Task<IReadOnlyList<Guid>> GetDescendantsAsync(Guid assetTypeId, CancellationToken cancellationToken);

    /// <summary>The type's InstanceLabel (forInstances) or DefinitionLabel, trimmed; "" when deleted, absent or NULL.</summary>
    Task<string> GetReportingLabelAsync(Guid assetTypeId, bool forInstances, CancellationToken cancellationToken);

    /// <summary>
    /// The property-set query behind a gen table: the type's live properties for the pass, every live system
    /// property (Definition: only AssetTypeIsInstance = 0), and the System_AssetType properties when the type has
    /// descendants, ordered by ISNULL(DisplayValue, Name).
    /// </summary>
    Task<IReadOnlyList<GenPropertyNameRow>> GetGenPropertyNamesAsync(Guid assetTypeId, bool isInstance, bool hasChildAssetTypes, CancellationToken cancellationToken);

    /// <summary>spr_Properties_GetByListOfPropertyIds (deleted properties included).</summary>
    Task<IReadOnlyList<PropertyMetaRow>> GetPropertiesAsync(IReadOnlyCollection<Guid> propertyIds, CancellationToken cancellationToken);

    /// <summary>spr_AssetTypePropertyRelationList_GetByAssetTypeIdAndPropertyIds: live relations with live properties.</summary>
    Task<IReadOnlyList<PropertyRelationRow>> GetRelationsAsync(Guid assetTypeId, IReadOnlyCollection<Guid> propertyIds, CancellationToken cancellationToken);

    /// <summary>The original's AssetTypePropertyRelation_Get(type, property): the first live relation, or null.</summary>
    Task<PropertyRelationRow?> GetRelationAsync(Guid assetTypeId, Guid propertyId, CancellationToken cancellationToken);

    /// <summary>Non-deleted reports.</summary>
    Task<IReadOnlyList<Guid>> GetReportIdsAsync(CancellationToken cancellationToken);

    /// <summary>Active, non-deleted roles.</summary>
    Task<IReadOnlyList<Guid>> GetRoleIdsAsync(CancellationToken cancellationToken);

    /// <summary>spr_Role_Get's asset types, permissions and filters, grouped in the procedure's order.</summary>
    Task<IReadOnlyList<RoleAssetTypeRow>> GetRoleAssetTypesAsync(Guid roleId, CancellationToken cancellationToken);

    /// <summary>Column names of a table, in column order.</summary>
    Task<IReadOnlyList<string>> GetTableColumnNamesAsync(string tableName, CancellationToken cancellationToken);

    /// <summary>Whether any gen table exists (a database that has never been generated has none).</summary>
    Task<bool> AnyGenTablesAsync(CancellationToken cancellationToken);

    Task ExecuteAsync(string sql, int commandTimeoutSeconds, CancellationToken cancellationToken);
}
