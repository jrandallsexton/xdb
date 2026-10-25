using System;
using System.Collections.Generic;

namespace Xdb.Api.Application.PickLists.Commands.CreatePickList;

/// <summary>
/// Creates a pick list with its values in one write. A PickList property draws its values from a list, so the list
/// must exist before the property. Service-backed lists (values read from an external service) are not created here.
/// </summary>
public sealed record CreatePickListCommand
{
    /// <summary>The member creating the list; stamped as creator and approver of the list and its values.</summary>
    public required Guid ActorId { get; init; }

    /// <summary>A fixed id (imports re-run against the same ids); null generates one.</summary>
    public Guid? Id { get; init; }

    public required string Name { get; init; }

    public string? DisplayValue { get; init; }

    public required string Description { get; init; }

    /// <summary>Whether editors may add values while entering data.</summary>
    public bool AllowNewValues { get; init; }

    /// <summary>The values in display order; a value's position is its order.</summary>
    public IReadOnlyList<CreatePickListValue> Values { get; init; } = [];
}

public sealed record CreatePickListValue
{
    /// <summary>A fixed id; null generates one.</summary>
    public Guid? Id { get; init; }

    public required string Value { get; init; }

    public string? DisplayValue { get; init; }
}
