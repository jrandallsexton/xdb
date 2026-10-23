using System;
using System.Runtime.Serialization;

namespace Xdb.Api.Application.Admin.Legacy;

/// <summary>A service module's metadata (Core ServiceModuleMetadata): members in ordinal order, all required in a body.</summary>
[DataContract(Name = "ServiceModuleMetadata", Namespace = "")]
public sealed class ServiceModuleMetadataContract
{
    [DataMember] public byte[]? AssemblyData { get; set; }
    [DataMember] public string? AssemblyName { get; set; } = string.Empty;
    [DataMember] public string? AssemblyNamespace { get; set; } = string.Empty;
    [DataMember] public string? AssemblyVersion { get; set; } = string.Empty;
    [DataMember] public string? ClassName { get; set; } = string.Empty;
    [DataMember] public DateTime Created { get; set; }
    [DataMember] public DateTime? Deleted { get; set; }
    [DataMember] public string? Description { get; set; } = string.Empty;
    [DataMember] public DateTime? Downloaded { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public bool IsActive { get; set; }
    [DataMember] public bool IsDirty { get; set; }
    [DataMember] public bool IsNew { get; set; }
    [DataMember] public DateTime? LastCompleted { get; set; }
    [DataMember] public DateTime? LastRan { get; set; }
    [DataMember] public string? Name { get; set; } = string.Empty;

    /// <summary>The constructor's object: a random id, created now, new and not dirty.</summary>
    public static ServiceModuleMetadataContract Fresh(DateTime now) => new() { Id = Guid.NewGuid(), Created = now, IsNew = true, IsDirty = false };
}
