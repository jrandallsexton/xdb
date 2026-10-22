namespace Xdb.Api.Application.Common.Metadata;

/// <summary>Property data types (DataTypes lookup table; values are fixed ids).</summary>
public enum DataType : byte
{
    Date = 0,
    DateTime = 1,
    Document = 2,
    Float = 3,
    Image = 4,
    Int = 5,
    String = 6,
    Url = 7,
    User = 8,
    Asset = 9,
    Memo = 10,
    RelationParentChild = 11,
    RelationChildParent = 12,
    Dependency = 13,
    Currency = 14,
    RelationOther = 15,
    SystemAssetType = 16,
    SystemDescription = 17,
    SystemInstanceOf = 18,
    SystemAssetName = 19,
    IPv4 = 20,
    IPv6 = 21,
    SystemInstanceOfDescription = 22,
    PickList = 23,
    Html = 24
}
