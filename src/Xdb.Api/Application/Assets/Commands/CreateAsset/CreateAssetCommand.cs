using System;
using System.Collections.Generic;

namespace Xdb.Api.Application.Assets.Commands.CreateAsset;

/// <summary>
/// Creates one asset (a definition, or an instance of a definition) with its property values, approved by the actor
/// as one submittal. Every value's property must be linked to the asset's type at the asset's level.
/// </summary>
public sealed record CreateAssetCommand
{
    /// <summary>The member creating the asset; stamped as creator, submitter and approver.</summary>
    public required Guid ActorId { get; init; }

    /// <summary>A fixed id (imports re-run against the same ids); null generates one.</summary>
    public Guid? Id { get; init; }

    public required Guid AssetTypeId { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    /// <summary>The definition this asset is an instance of; null creates a definition.</summary>
    public Guid? InstanceOfId { get; init; }

    /// <summary>
    /// The values. A property appears more than once only when it allows multiple values; their order is kept.
    /// Each value is text in its type's canonical form; see <see cref="CreateAssetValue"/>.
    /// </summary>
    public IReadOnlyList<CreateAssetValue> Values { get; init; } = [];
}

/// <summary>
/// One property value. Forms by data type: String and Memo as given; Int and Float in invariant culture
/// ("12345", "32.4520219"); Date as yyyy-MM-dd; DateTime as ISO 8601 (no offset means UTC; stored in UTC);
/// PickList as the pick list value's id; Asset as the referenced asset's id; User as the member's id.
/// </summary>
public sealed record CreateAssetValue(Guid PropertyId, string Value);
