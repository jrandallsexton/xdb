using System;

using Xdb.Api.Application.Common.Metadata;

namespace Xdb.Api.Application.Properties.Commands.CreateProperty;

/// <summary>
/// Creates one property. Properties are global: a type uses one through an AssetTypesProperties row, so a property
/// must exist before any type links it.
/// </summary>
public sealed record CreatePropertyCommand
{
    /// <summary>The member creating the property; stamped as creator and approver.</summary>
    public required Guid ActorId { get; init; }

    /// <summary>A fixed id (imports re-run against the same ids); null generates one.</summary>
    public Guid? Id { get; init; }

    public required string Name { get; init; }

    public required DataType DataType { get; init; }

    public string? DisplayValue { get; init; }

    public string? Description { get; init; }

    /// <summary>The list a PickList property draws its values from. Required for, and only for, PickList.</summary>
    public Guid? PickListId { get; init; }

    /// <summary>The type an Asset property refers to. A User property always targets the system User type.</summary>
    public Guid? AssetTypeId { get; init; }

    /// <summary>Whether the referenced assets are instances (true) or definitions (false); required with AssetTypeId.</summary>
    public bool? AssetTypeIsInstance { get; init; }

    /// <summary>Limits a User property to the members of this role.</summary>
    public Guid? RoleId { get; init; }

    public bool AllowMultiValue { get; init; }

    public bool IsOrdered { get; init; }
}
