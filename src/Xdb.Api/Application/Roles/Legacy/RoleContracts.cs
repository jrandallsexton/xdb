using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Xdb.Api.Application.Views.Legacy;

namespace Xdb.Api.Application.Roles.Legacy;

/// <summary>A role as the original's data-contract serializer wrote it (17 members, ordinal order).</summary>
[DataContract(Name = "Role", Namespace = "")]
public sealed class RoleContract
{
    [DataMember] public List<RoleAssetTypeContract>? AssetTypes { get; set; } = [];
    [DataMember] public DateTime Created { get; set; }
    [DataMember] public Guid CreatedBy { get; set; }
    [DataMember] public DateTime? Deleted { get; set; }
    [DataMember] public Guid? DeletedBy { get; set; }
    [DataMember] public string? Description { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public bool IsActive { get; set; } = true;
    [DataMember] public bool IsDirty { get; set; } = true;
    [DataMember] public bool IsNew { get; set; } = true;
    [DataMember] public bool IsSystem { get; set; }
    [DataMember] public DateTime? LastModified { get; set; }
    [DataMember] public Guid? LastModifiedBy { get; set; }
    [DataMember] public List<RoleMemberContract>? Members { get; set; } = [];
    [DataMember] public string? Name { get; set; }
    [DataMember] public List<RoleReportContract>? Reports { get; set; } = [];
    [DataMember] public Guid? RoleLead { get; set; }
}

[DataContract(Name = "RoleMember", Namespace = "")]
public sealed class RoleMemberContract
{
    [DataMember] public Guid Id { get; set; }
    [DataMember] public bool IsDeleted { get; set; }
    [DataMember] public bool IsDirty { get; set; }
    [DataMember] public bool IsNew { get; set; }
    [DataMember] public Guid MemberId { get; set; }
    [DataMember] public string? MemberName { get; set; }
}

[DataContract(Name = "RoleReport", Namespace = "")]
public sealed class RoleReportContract
{
    [DataMember] public DateTime Created { get; set; }
    [DataMember] public Guid CreatedBy { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public bool IsDeleted { get; set; }
    [DataMember] public bool IsDirty { get; set; }
    [DataMember] public bool IsNew { get; set; }
    [DataMember] public Guid ReportId { get; set; }
    [DataMember] public string? ReportName { get; set; }
}

[DataContract(Name = "RoleAssetType", Namespace = "")]
public sealed class RoleAssetTypeContract
{
    [DataMember] public Guid AssetTypeId { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public Guid? InheritFrom { get; set; }
    [DataMember] public bool IsDeleted { get; set; }
    [DataMember] public bool IsDirty { get; set; }
    [DataMember] public bool IsInherited { get; set; }
    [DataMember] public bool IsInstance { get; set; }
    [DataMember] public bool IsNew { get; set; }
    [DataMember] public List<RolePermissionContract>? Permissions { get; set; } = [];
    [DataMember] public Guid RoleId { get; set; }
}

[DataContract(Name = "RolePermission", Namespace = "")]
public sealed class RolePermissionContract
{
    [DataMember] public string? FilterExpression { get; set; }
    [DataMember] public string? FilterString { get; set; }
    [DataMember] public List<FilterContract>? Filters { get; set; } = [];
    [DataMember] public Guid Id { get; set; }
    [DataMember] public bool IsDeleted { get; set; }
    [DataMember] public bool IsDirty { get; set; }
    [DataMember] public bool IsIncludesPermission { get; set; }
    [DataMember] public bool IsNew { get; set; }
    [DataMember] public int PermissionId { get; set; }
}

[DataContract(Name = "RoleAssetTypeProcessRelation", Namespace = "")]
public sealed class RoleProcessRelationContract
{
    [DataMember] public Guid AssetTypeId { get; set; }
    [DataMember] public DateTime Created { get; set; }
    [DataMember] public Guid CreatedBy { get; set; }
    [DataMember] public DateTime? Deleted { get; set; }
    [DataMember] public Guid? DeletedBy { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public bool IsDirty { get; set; }
    [DataMember] public bool IsNew { get; set; }
    [DataMember] public Guid ProcessId { get; set; }
    [DataMember] public Guid RoleId { get; set; }
}
