using System;
using System.Runtime.Serialization;

namespace Xdb.Api.Application.BulkUploads.Legacy;

/// <summary>A bulk upload's status (Core BulkUpload), polled by the uploader page.</summary>
[DataContract(Name = "BulkUpload", Namespace = "")]
public sealed class BulkUploadContract
{
    [DataMember] public DateTime? Completed { get; set; }
    [DataMember] public DateTime Created { get; set; }
    [DataMember] public Guid CreatedBy { get; set; }
    [DataMember] public Guid FileId { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public int RecordCount { get; set; }
}

/// <summary>One import log line (Core BulkUploadLog), serialized by Json.NET in declaration order.</summary>
public sealed class BulkUploadLogContract
{
    public int Id { get; set; }
    public string? Category { get; set; }
    public Guid? AssetId { get; set; }
    public string? AssetName { get; set; }
    public Guid? AssetTypeId { get; set; }
    public bool IsInstance { get; set; }
    public string? Message { get; set; }
    public int Order { get; set; }
}
