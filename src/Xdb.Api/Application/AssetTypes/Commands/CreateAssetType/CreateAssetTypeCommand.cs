using System;
using System.Collections.Generic;

namespace Xdb.Api.Application.AssetTypes.Commands.CreateAssetType;

/// <summary>
/// Creates an asset type with its property links, under an optional parent whose links it inherits. The linked
/// properties must already exist. Default views are set once views exist; they are not part of creation.
/// </summary>
public sealed record CreateAssetTypeCommand
{
    /// <summary>The member creating the type; stamped as creator of the type, its relation and its links.</summary>
    public required Guid ActorId { get; init; }

    /// <summary>A fixed id (imports re-run against the same ids); null generates one.</summary>
    public Guid? Id { get; init; }

    /// <summary>Unique across every type, deleted ones included.</summary>
    public required string Name { get; init; }

    public required string Pluralization { get; init; }

    /// <summary>What a definition of this type is called (e.g. "Venue").</summary>
    public string? DefinitionLabel { get; init; }

    /// <summary>What an instance is called; null means the type has no instances (and no instance gen table).</summary>
    public string? InstanceLabel { get; init; }

    /// <summary>False for a grouping type that holds no assets of its own; its gen tables load its descendants'.</summary>
    public bool AllowAssets { get; init; } = true;

    /// <summary>The parent type; its live property links are copied to this type as inherited links.</summary>
    public Guid? ParentId { get; init; }

    /// <summary>The type's own property links.</summary>
    public IReadOnlyList<CreateAssetTypeProperty> Properties { get; init; } = [];
}

/// <param name="PropertyId">An existing, non-system property.</param>
/// <param name="IsInstance">True: the property belongs to instances; false: to definitions.</param>
public sealed record CreateAssetTypeProperty(Guid PropertyId, bool IsInstance);
