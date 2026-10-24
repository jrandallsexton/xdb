using System;
using System.Collections.Generic;

namespace Xdb.Api.Application.Views.Commands.CreateView;

/// <summary>
/// Creates a view: the form and list through which members see one level (definition or instance) of an asset type,
/// built from that type's property groups and reached through roles. A view of a type also serves its descendants.
/// </summary>
public sealed record CreateViewCommand
{
    /// <summary>The member creating the view; stamped as creator of the view and its relations.</summary>
    public required Guid ActorId { get; init; }

    /// <summary>A fixed id (imports re-run against the same ids); null generates one.</summary>
    public Guid? Id { get; init; }

    public required Guid AssetTypeId { get; init; }

    /// <summary>True: the view lists and edits the type's instances; false: its definitions.</summary>
    public required bool IsInstance { get; init; }

    /// <summary>Unique among the type's live views.</summary>
    public required string Name { get; init; }

    /// <summary>The name members see; null uses the name.</summary>
    public string? DisplayValue { get; init; }

    public string? Description { get; init; }

    /// <summary>What the view calls one of its items (e.g. "Venue"); it also labels the name field. Null leaves the UI's generic wording.</summary>
    public string? DriverCaption { get; init; }

    /// <summary>The label of the button that creates an item. Null leaves the UI's generic wording.</summary>
    public string? NewItemCaption { get; init; }

    /// <summary>Shows values without editing them.</summary>
    public bool IsReadOnly { get; init; }

    /// <summary>Listed in members' view menus; false makes it reachable only by link.</summary>
    public bool IsStandAlone { get; init; } = true;

    /// <summary>Lets asset pickers in the view create a new item ("[Enter New]") for members allowed to.</summary>
    public bool IsCreation { get; init; }

    /// <summary>The type's property groups at the view's level, in display order.</summary>
    public IReadOnlyList<Guid> PropertyGroupIds { get; init; } = [];

    /// <summary>Roles whose members see the view. The Admin role is always added.</summary>
    public IReadOnlyList<Guid> RoleIds { get; init; } = [];

    /// <summary>Makes the view the type's default for its level: the view an asset of the type opens in.</summary>
    public bool IsDefault { get; init; }
}
