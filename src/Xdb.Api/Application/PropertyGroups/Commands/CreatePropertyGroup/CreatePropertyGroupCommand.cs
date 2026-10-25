using System;
using System.Collections.Generic;

namespace Xdb.Api.Application.PropertyGroups.Commands.CreatePropertyGroup;

/// <summary>
/// Creates a property group: an ordered set of properties shown together on one level (definition or instance) of an
/// asset type's forms. Views are built from groups; placing the group in a view is not part of creation.
/// </summary>
public sealed record CreatePropertyGroupCommand
{
    /// <summary>The member creating the group; stamped as creator of the group and its members.</summary>
    public required Guid ActorId { get; init; }

    /// <summary>A fixed id (imports re-run against the same ids); null generates one.</summary>
    public Guid? Id { get; init; }

    public required Guid AssetTypeId { get; init; }

    /// <summary>True: the group is for the type's instances; false: for its definitions.</summary>
    public required bool IsInstance { get; init; }

    /// <summary>Unique among the type's live groups at the same level.</summary>
    public required string Name { get; init; }

    /// <summary>The caption shown for the group; null uses the name.</summary>
    public string? DisplayValue { get; init; }

    /// <summary>The group's properties in display order.</summary>
    public IReadOnlyList<CreatePropertyGroupProperty> Properties { get; init; } = [];
}

/// <param name="PropertyId">
/// A property linked to the type at the group's level, or a system property (name, description, instance of, ...),
/// which is never linked.
/// </param>
/// <param name="IsRequired">The form will not save without a value.</param>
/// <param name="IsReadOnly">The form shows the value but does not edit it.</param>
public sealed record CreatePropertyGroupProperty(Guid PropertyId, bool IsRequired = false, bool IsReadOnly = false);
