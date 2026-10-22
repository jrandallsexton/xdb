using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Metadata.Legacy;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Metadata.Queries.AssetTypes;

// Asset type reads. Each handler reproduces the original operation's rules; failures the original reported as the
// host's generic fault are thrown, so the legacy wire returns that fault.

public sealed record GetAssetTypeQuery(Guid AssetTypeId);

public interface IGetAssetTypeQueryHandler
{
    Task<Result<AssetTypeContract?>> ExecuteAsync(GetAssetTypeQuery query, CancellationToken cancellationToken = default);
}

/// <summary>A live asset type with its properties and relations; null for a missing or deleted type.</summary>
public sealed class GetAssetTypeQueryHandler(MetadataLoader loader) : IGetAssetTypeQueryHandler
{
    public async Task<Result<AssetTypeContract?>> ExecuteAsync(GetAssetTypeQuery query, CancellationToken cancellationToken = default) =>
        new Success<AssetTypeContract?>(await loader.AssetTypeAsync(query.AssetTypeId, cancellationToken));
}

public sealed record GetAssetTypeParentQuery(Guid AssetTypeId);

public interface IGetAssetTypeParentQueryHandler
{
    Task<Result<AssetTypeContract?>> ExecuteAsync(GetAssetTypeParentQuery query, CancellationToken cancellationToken = default);
}

/// <summary>The live parent type; null for a root, a missing type, or a deleted parent.</summary>
public sealed class GetAssetTypeParentQueryHandler(IMetadataProcedures procedures, MetadataLoader loader) : IGetAssetTypeParentQueryHandler
{
    public async Task<Result<AssetTypeContract?>> ExecuteAsync(GetAssetTypeParentQuery query, CancellationToken cancellationToken = default)
    {
        var parentId = await procedures.GetParentAssetTypeIdAsync(query.AssetTypeId, cancellationToken);
        return new Success<AssetTypeContract?>(parentId is { } id ? await loader.AssetTypeAsync(id, cancellationToken) : null);
    }
}

public sealed record GetAssetTypeParentIdQuery(Guid AssetTypeId, bool TopMostParent);

public interface IGetAssetTypeParentIdQueryHandler
{
    Task<Result<Guid>> ExecuteAsync(GetAssetTypeParentIdQuery query, CancellationToken cancellationToken = default);
}

/// <summary>The parent (or top-most ancestor) id; the empty Guid for a root or missing type.</summary>
public sealed class GetAssetTypeParentIdQueryHandler(IMetadataProcedures procedures) : IGetAssetTypeParentIdQueryHandler
{
    public async Task<Result<Guid>> ExecuteAsync(GetAssetTypeParentIdQuery query, CancellationToken cancellationToken = default)
    {
        var parent = await procedures.GetParentAssetTypeIdAsync(query.AssetTypeId, cancellationToken);
        if (!query.TopMostParent)
            return new Success<Guid>(parent ?? Guid.Empty);

        var top = Guid.Empty;
        while (parent is { } id)
        {
            top = id;
            parent = await procedures.GetParentAssetTypeIdAsync(id, cancellationToken);
        }
        return new Success<Guid>(top);
    }
}

public sealed record GetAssetTypeNameQuery(Guid AssetTypeId, bool GetPlural);

public interface IGetAssetTypeNameQueryHandler
{
    Task<Result<string>> ExecuteAsync(GetAssetTypeNameQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Name or pluralization (falling back to the name), trimmed; "" when absent. Deleted types included.</summary>
public sealed class GetAssetTypeNameQueryHandler(IMetadataProcedures procedures) : IGetAssetTypeNameQueryHandler
{
    public async Task<Result<string>> ExecuteAsync(GetAssetTypeNameQuery query, CancellationToken cancellationToken = default) =>
        new Success<string>(query.GetPlural
            ? await procedures.GetAssetTypePluralizationAsync(query.AssetTypeId, cancellationToken)
            : await procedures.GetAssetTypeNameAsync(query.AssetTypeId, cancellationToken));
}

public sealed record GetAssetTypeReportingLabelQuery(Guid AssetTypeId, AssetRequestType RequestType);

public interface IGetAssetTypeReportingLabelQueryHandler
{
    Task<Result<string>> ExecuteAsync(GetAssetTypeReportingLabelQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// The instance label for Instance, otherwise the definition label; without a label, "Definition" for Definition
/// and "Instance" for anything else (Both and out-of-range values included).
/// </summary>
public sealed class GetAssetTypeReportingLabelQueryHandler(IMetadataProcedures procedures) : IGetAssetTypeReportingLabelQueryHandler
{
    public async Task<Result<string>> ExecuteAsync(GetAssetTypeReportingLabelQuery query, CancellationToken cancellationToken = default)
    {
        var label = await procedures.GetReportingLabelAsync(query.AssetTypeId, query.RequestType == AssetRequestType.Instance, cancellationToken);
        if (string.IsNullOrEmpty(label))
            label = query.RequestType == AssetRequestType.Definition ? "Definition" : "Instance";
        return new Success<string>(label);
    }
}

public sealed record AssetTypeHasAssetsQuery(Guid AssetTypeId, AssetRequestType RequestType, bool SearchChildAssetTypes);

public interface IAssetTypeHasAssetsQueryHandler
{
    Task<Result<bool>> ExecuteAsync(AssetTypeHasAssetsQuery query, CancellationToken cancellationToken = default);
}

public sealed class AssetTypeHasAssetsQueryHandler(MetadataRules rules) : IAssetTypeHasAssetsQueryHandler
{
    public async Task<Result<bool>> ExecuteAsync(AssetTypeHasAssetsQuery query, CancellationToken cancellationToken = default) =>
        new Success<bool>(await rules.HasAssetsAsync(query.AssetTypeId, query.RequestType, query.SearchChildAssetTypes, cancellationToken));
}

public sealed record GetAssetTypeDefaultViewIdQuery(Guid AssetTypeId);

public interface IGetAssetTypeDefaultViewIdQueryHandler
{
    Task<Result<Guid>> ExecuteAsync(GetAssetTypeDefaultViewIdQuery query, CancellationToken cancellationToken = default);
}

/// <summary>The definition default view (empty Guid when none). A missing or deleted type fails, as in the original.</summary>
public sealed class GetAssetTypeDefaultViewIdQueryHandler(MetadataLoader loader) : IGetAssetTypeDefaultViewIdQueryHandler
{
    public async Task<Result<Guid>> ExecuteAsync(GetAssetTypeDefaultViewIdQuery query, CancellationToken cancellationToken = default)
    {
        var assetType = await loader.AssetTypeAsync(query.AssetTypeId, cancellationToken)
            ?? throw new NullReferenceException("Asset type not found.");
        return new Success<Guid>(assetType.DefaultViewId ?? Guid.Empty);
    }
}

public sealed record GetAssetTypeDescendantsQuery(Guid ParentAssetTypeId);

public interface IGetAssetTypeDescendantsQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetTypeDescendantsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>AssetType_GetChildren / _GetChildrenOnly: every live descendant (not only direct children), en-US by name.</summary>
public sealed class GetAssetTypeDescendantsQueryHandler(MetadataLoader loader) : IGetAssetTypeDescendantsQueryHandler
{
    public async Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetTypeDescendantsQuery query, CancellationToken cancellationToken = default) =>
        new Success<List<KeyValuePair<Guid, string>>>(await loader.DescendantDictionaryAsync(query.ParentAssetTypeId, cancellationToken));
}

public sealed record GetAssetTypesAvailableForCreationQuery(Guid AssetTypeId);

public interface IGetAssetTypesAvailableForCreationQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetTypesAvailableForCreationQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// AssetType_GetLowestLevelParents (which calls AssetTypes_GetAvailableForCreation): descendants by name, then the
/// type itself, keeping those that allow assets. A missing type fails.
/// </summary>
public sealed class GetAssetTypesAvailableForCreationQueryHandler(IMetadataProcedures procedures, MetadataRules rules) : IGetAssetTypesAvailableForCreationQueryHandler
{
    public async Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetTypesAvailableForCreationQuery query, CancellationToken cancellationToken = default)
    {
        var result = new List<KeyValuePair<Guid, string>>();
        foreach (var id in await rules.ChildrenAsync(query.AssetTypeId, includeAllDescendants: true, cancellationToken))
        {
            if (await procedures.GetAllowAssetsAsync(id, cancellationToken))
                result.Add(new KeyValuePair<Guid, string>(id, await procedures.GetAssetTypeNameAsync(id, cancellationToken)));
        }
        return new Success<List<KeyValuePair<Guid, string>>>(result);
    }
}

public sealed record GetAssetTypeByProcessStepQuery(Guid ProcessId, int ProcessStepIndex);

public interface IGetAssetTypeByProcessStepQueryHandler
{
    Task<Result<Guid>> ExecuteAsync(GetAssetTypeByProcessStepQuery query, CancellationToken cancellationToken = default);
}

/// <summary>The asset type of a process step's view; a missing step fails.</summary>
public sealed class GetAssetTypeByProcessStepQueryHandler(IMetadataProcedures procedures) : IGetAssetTypeByProcessStepQueryHandler
{
    public async Task<Result<Guid>> ExecuteAsync(GetAssetTypeByProcessStepQuery query, CancellationToken cancellationToken = default)
    {
        var viewId = await procedures.GetProcessStepViewIdAsync(query.ProcessId, query.ProcessStepIndex, cancellationToken)
            ?? throw new InvalidOperationException("No ViewId found for this process step.");
        return new Success<Guid>(await procedures.GetViewAssetTypeIdAsync(viewId, cancellationToken));
    }
}

public sealed record GetAssetTypesByProcessStepQuery(Guid ProcessId, int ProcessStepIndex);

public interface IGetAssetTypesByProcessStepQueryHandler
{
    Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetTypesByProcessStepQuery query, CancellationToken cancellationToken = default);
}

/// <summary>The step view's type and its descendants that have live definition assets; empty when there is no step.</summary>
public sealed class GetAssetTypesByProcessStepQueryHandler(IMetadataProcedures procedures, MetadataRules rules) : IGetAssetTypesByProcessStepQueryHandler
{
    public async Task<Result<List<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetTypesByProcessStepQuery query, CancellationToken cancellationToken = default)
    {
        var result = new List<KeyValuePair<Guid, string>>();
        if (await procedures.GetProcessStepViewIdAsync(query.ProcessId, query.ProcessStepIndex, cancellationToken) is { } viewId)
        {
            var assetTypeId = await procedures.GetViewAssetTypeIdAsync(viewId, cancellationToken);
            if (assetTypeId != Guid.Empty)
            {
                foreach (var id in await rules.ChildrenAsync(assetTypeId, includeAllDescendants: true, cancellationToken))
                {
                    if (await rules.HasAssetsAsync(id, AssetRequestType.Both, searchChildAssetTypes: false, cancellationToken))
                        result.Add(new KeyValuePair<Guid, string>(id, await procedures.GetAssetTypeNameAsync(id, cancellationToken)));
                }
            }
        }
        return new Success<List<KeyValuePair<Guid, string>>>(result);
    }
}

public sealed record GetAssetTypesDictionaryQuery(bool IncludeUnused);

public interface IGetAssetTypesDictionaryQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetTypesDictionaryQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Live asset types by name; when not including unused ones, only types that allow assets and have any.</summary>
public sealed class GetAssetTypesDictionaryQueryHandler(IMetadataProcedures procedures) : IGetAssetTypesDictionaryQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetAssetTypesDictionaryQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await procedures.GetAssetTypesDictionaryAsync(query.IncludeUnused, cancellationToken));
}

public sealed record GetRootAssetTypesQuery;

public interface IGetRootAssetTypesQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetRootAssetTypesQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Types that never had a parent (no Deleted filter on either table), by name.</summary>
public sealed class GetRootAssetTypesQueryHandler(IMetadataProcedures procedures) : IGetRootAssetTypesQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> ExecuteAsync(GetRootAssetTypesQuery query, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await procedures.GetRootAssetTypesAsync(cancellationToken));
}

/// <summary>A node of the asset type tree: roots carry their id line, children do not.</summary>
public sealed record AssetTypeTreeNode(Guid Id, string Name, IReadOnlyList<AssetTypeTreeNode> Children);

public sealed record GetAssetTypeTreeQuery;

public interface IGetAssetTypeTreeQueryHandler
{
    Task<Result<IReadOnlyList<AssetTypeTreeNode>>> ExecuteAsync(GetAssetTypeTreeQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Roots (untrimmed names) with their direct live children (trimmed names) by name, recursively.</summary>
public sealed class GetAssetTypeTreeQueryHandler(IMetadataProcedures procedures, MetadataRules rules) : IGetAssetTypeTreeQueryHandler
{
    public async Task<Result<IReadOnlyList<AssetTypeTreeNode>>> ExecuteAsync(GetAssetTypeTreeQuery query, CancellationToken cancellationToken = default)
    {
        var roots = new List<AssetTypeTreeNode>();
        foreach (var (id, name) in await procedures.GetRootAssetTypesAsync(cancellationToken))
            roots.Add(new AssetTypeTreeNode(id, name, await ChildrenAsync(id, cancellationToken)));
        return new Success<IReadOnlyList<AssetTypeTreeNode>>(roots);
    }

    private async Task<IReadOnlyList<AssetTypeTreeNode>> ChildrenAsync(Guid parentId, CancellationToken cancellationToken)
    {
        var nodes = new List<AssetTypeTreeNode>();
        foreach (var id in await rules.ChildrenAsync(parentId, includeAllDescendants: false, cancellationToken))
            nodes.Add(new AssetTypeTreeNode(id, await procedures.GetAssetTypeNameAsync(id, cancellationToken), await ChildrenAsync(id, cancellationToken)));
        return nodes;
    }
}

public sealed record GetDefaultViewsQuery(Guid UserId, AssetRequestType RequestType);

public interface IGetDefaultViewsQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, Guid>>>> ExecuteAsync(GetDefaultViewsQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default (instance) views per type: all for an administrator, otherwise through the member's roles. "Both" fails;
/// an out-of-range request type produces invalid SQL and fails, as in the original.
/// </summary>
public sealed class GetDefaultViewsQueryHandler(IMetadataProcedures procedures) : IGetDefaultViewsQueryHandler
{
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, Guid>>>> ExecuteAsync(GetDefaultViewsQuery query, CancellationToken cancellationToken = default)
    {
        var isAdmin = await procedures.IsAdminAsync(query.UserId, SystemRoleIds.Admin, cancellationToken);
        var column = query.RequestType switch
        {
            AssetRequestType.Both => throw new InvalidOperationException("Can't request for both"),
            AssetRequestType.Definition => "AT.[DefaultViewId]",
            AssetRequestType.Instance => "AT.[DefaultInstanceViewId]",
            _ => string.Empty
        };
        return new Success<IReadOnlyList<KeyValuePair<Guid, Guid>>>(
            await procedures.GetDefaultViewsAsync(isAdmin ? null : query.UserId, column, cancellationToken));
    }
}

public sealed record GetAssetTypeIdOfAssetQuery(Guid AssetId);

public interface IGetAssetTypeIdOfAssetQueryHandler
{
    Task<Result<Guid?>> ExecuteAsync(GetAssetTypeIdOfAssetQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetAssetTypeIdOfAssetQueryHandler(IMetadataProcedures procedures) : IGetAssetTypeIdOfAssetQueryHandler
{
    public async Task<Result<Guid?>> ExecuteAsync(GetAssetTypeIdOfAssetQuery query, CancellationToken cancellationToken = default) =>
        new Success<Guid?>(await procedures.GetAssetTypeIdOfAssetAsync(query.AssetId, cancellationToken));
}
