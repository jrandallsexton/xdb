namespace Xdb.Api.Application.Common.Metadata;

// The original's enums with their exact member names, for binding legacy query parameters: the original accepted a
// member name in any case or any integer. Spellings are the original's.

public enum LegacyDataType
{
    Undefined = -1,
    Date = 0,
    DateTime = 1,
    Document = 2,
    Float = 3,
    Image = 4,
    Int = 5,
    String = 6,
    URL = 7,
    User = 8,
    Asset = 9,
    Memo = 10,
    Relation_ParentChild = 11,
    Relation_ChildParent = 12,
    Dependency = 13,
    Currency = 14,
    Relation_Other = 15,
    System_AssetType = 16,
    System_Description = 17,
    IPv4 = 20,
    IPv6 = 21,
    System_InstanceOfDesc = 22,
    PickList = 23,
    HTML = 24,
    Geo = 25
}

public enum LegacyCustomReportFieldType
{
    NotApplicable = 0,
    Calculated = 1,
    Concatentation = 2,
    TimeElapsed = 3,
    RangeLabel = 4,
    ValueLabel = 5
}
