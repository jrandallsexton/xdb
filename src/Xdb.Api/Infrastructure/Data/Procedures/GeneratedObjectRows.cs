using System;
using System.Collections.Generic;

namespace Xdb.Api.Infrastructure.Data.Procedures;

/// <summary>The asset type columns the gen-table generator reads. Labels are raw (not trimmed).</summary>
public sealed class GenAssetTypeRow
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? DefinitionLabel { get; init; }
    public string? InstanceLabel { get; init; }
    public bool AllowAssets { get; init; }
}

/// <summary>One row of the original's property-set query for a gen table: id and ISNULL(DisplayValue, Name).</summary>
public sealed record GenPropertyNameRow(Guid Id, string PropertyName);

/// <summary>The columns of spr_Properties_GetByListOfPropertyIds that the generators use. Deleted properties are included.</summary>
public sealed class PropertyMetaRow
{
    public Guid Id { get; init; }
    public byte? DataTypeId { get; init; }
    public bool AllowMultiValue { get; init; }
    public string? Name { get; init; }
    public string? DisplayValue { get; init; }
    public Guid? AssetTypeId { get; init; }
    public bool? AssetTypeIsInstance { get; init; }
    public bool IsSystem { get; init; }
    public int? SystemTypeId { get; init; }
}

/// <summary>An asset type to property relation (AssetTypesProperties).</summary>
public sealed class PropertyRelationRow
{
    public Guid Id { get; init; }
    public Guid AssetTypeId { get; init; }
    public Guid PropertyId { get; init; }
    public bool IsInstance { get; init; }
    public bool IsInherited { get; init; }
    public bool IsInheritedValue { get; init; }
}

/// <summary>A role's asset type with its permissions, grouped from spr_Role_Get's fourth result set in its order.</summary>
public sealed class RoleAssetTypeRow
{
    public Guid Id { get; init; }
    public Guid AssetTypeId { get; init; }
    public bool IsInstance { get; init; }
    public bool IsInherited { get; init; }
    public List<RolePermissionRow> Permissions { get; } = [];
}

public sealed class RolePermissionRow
{
    public Guid Id { get; init; }
    public int PermissionId { get; init; }
    public string FilterExpression { get; init; } = string.Empty;
    public List<RoleFilterRow> Filters { get; } = [];
}

/// <summary>A role filter; Value is "" when NULL, as the original loaded it.</summary>
public sealed class RoleFilterRow
{
    public Guid PropertyId { get; init; }
    public int OperatorId { get; init; }
    public string Operator { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}
