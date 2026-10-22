using System;

namespace Xdb.Api.Application.Common.Metadata;

/// <summary>System properties whose ids the original hard-coded.</summary>
public static class SystemPropertyIds
{
    public static readonly Guid AssetName = new("8D145EE0-54D2-47DB-8785-5AE086C20DDE");
    public static readonly Guid AssetType = new("48014265-A355-45EC-9878-94C041ABD927");
    public static readonly Guid Description = new("E2A550BC-D54D-47F1-9019-EA8170A8EAFB");
    public static readonly Guid InstanceOf = new("E1AE755A-E987-42F4-8028-BF3C5AAA97DF");
    public static readonly Guid InstanceOfDescription = new("080D9FBF-8EA3-47FE-8340-FB8C12353CF8");
}

/// <summary>System asset types whose ids the original hard-coded.</summary>
public static class SystemAssetTypeIds
{
    /// <summary>Members are assets of this type (same id as the member).</summary>
    public static readonly Guid User = new("6C5DE6FF-1F85-4016-84ED-2F78B7C10517");
}

/// <summary>System roles whose ids the original hard-coded (also in SQL: MemberIsAdmin, spr_Role_Get).</summary>
public static class SystemRoleIds
{
    public static readonly Guid Admin = new("68F8BBAB-C026-4ED4-A875-164C48E8874D");
}

/// <summary>System members whose ids the original hard-coded.</summary>
public static class SystemMemberIds
{
    /// <summary>The built-in system administrator; creator of seeded rows.</summary>
    public static readonly Guid Admin = new("6CACA132-7827-4DE6-81E6-2B7D8DB939B9");
}
