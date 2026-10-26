using System;
using System.Collections.Generic;

using Xdb.Api.Application.Assets.Commands.CreateAsset;

namespace Xdb.Api.Application.Assets.Commands.CreateAssets;

/// <summary>
/// Creates many assets of one type, definitions and instances, each with its property values approved by the actor as
/// one submittal (as <see cref="CreateAssetCommand"/> does for one). An asset that fails its checks is rejected and the
/// others are still created.
/// </summary>
public sealed record CreateAssetsCommand
{
    /// <summary>The member creating the assets; stamped as creator, submitter and approver.</summary>
    public required Guid ActorId { get; init; }

    public required Guid AssetTypeId { get; init; }

    /// <summary>
    /// The assets, in any order. Definitions are written first, so an instance can name a definition in the same batch;
    /// Asset values must name assets that already exist.
    /// </summary>
    public IReadOnlyList<CreateAssetsItem> Assets { get; init; } = [];
}

/// <summary>One asset; its values follow <see cref="CreateAssetValue"/>.</summary>
public sealed record CreateAssetsItem
{
    /// <summary>The new asset's id (imports re-run against the same ids).</summary>
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    /// <summary>The definition this asset is an instance of; null creates a definition.</summary>
    public Guid? InstanceOfId { get; init; }

    /// <summary>A property appears more than once only when it allows multiple values; their order is kept.</summary>
    public IReadOnlyList<CreateAssetValue> Values { get; init; } = [];
}
