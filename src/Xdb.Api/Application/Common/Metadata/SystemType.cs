namespace Xdb.Api.Application.Common.Metadata;

/// <summary>Roles of system properties (SystemTypes lookup table; values are fixed ids).</summary>
public enum SystemType
{
    AssetName = 0,
    AssetType = 1,
    Created = 2,
    CreatedBy = 3,
    Deleted = 4,
    DeletedBy = 5,
    Description = 6,
    InstanceOf = 7,
    InstanceOfDescription = 8,
    LastModified = 9,
    LastModifiedBy = 10,
    NotApplicable = 11,
    DisplayValue = 12
}
