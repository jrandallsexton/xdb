using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Xdb.Api.Application.Assets.Legacy;

namespace Xdb.Api.Application.Users.Legacy;

/// <summary>The current-user bootstrap model (UI UserClient); members serialize in ordinal name order.</summary>
[DataContract(Name = "UserClient", Namespace = "")]
public sealed class UserClientContract
{
    [DataMember] public bool AllowBulkUploads { get; set; }
    [DataMember] public string? DisplayValue { get; set; }
    [DataMember] public string? FirstName { get; set; }
    /// <summary>Never loaded by the original: always null.</summary>
    [DataMember] public List<string>? HiddenUIElements { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public bool IsAdmin { get; set; }
    /// <summary>The member's LastModified as en-US text, or null.</summary>
    [DataMember] public string? LastModified { get; set; }
    [DataMember] public string? LastName { get; set; }
    [DataMember] public Dictionary<Guid, string> Processes { get; set; } = [];
    [DataMember] public string? UserId { get; set; }
    [DataMember] public Dictionary<Guid, string> Views { get; set; } = [];
    [DataMember(Name = "allowBeta")] public bool AllowBeta { get; set; }
    [DataMember(Name = "dashboards")] public List<DashboardClientContract> Dashboards { get; set; } = [];
    [DataMember(Name = "recent")] public List<RecentItemContract> Recent { get; set; } = [];
    [DataMember(Name = "reports")] public Dictionary<Guid, string> Reports { get; set; } = [];
    [DataMember(Name = "shortcuts")] public Dictionary<string, string> Shortcuts { get; set; } = [];
    [DataMember(Name = "showReleaseNotice")] public bool ShowReleaseNotice { get; set; }
}

[DataContract(Name = "DashboardClient", Namespace = "")]
public sealed class DashboardClientContract
{
    [DataMember(Name = "caption")] public string? Caption { get; set; }
    [DataMember(Name = "id")] public string? Id { get; set; }
    [DataMember(Name = "index")] public int Index { get; set; }
    [DataMember(Name = "items")] public List<DashboardItemClientContract> Items { get; set; } = [];
    [DataMember(Name = "memberId")] public string? MemberId { get; set; }
}

[DataContract(Name = "DashboardItemClient", Namespace = "")]
public sealed class DashboardItemClientContract
{
    [DataMember(Name = "chartId")] public string? ChartId { get; set; }
    [DataMember(Name = "dashboardId")] public string? DashboardId { get; set; }
    [DataMember(Name = "id")] public string? Id { get; set; }
    [DataMember(Name = "index")] public int Index { get; set; }
    [DataMember(Name = "itemType")] public int ItemType { get; set; }
    [DataMember(Name = "subtitle")] public string? Subtitle { get; set; }
    [DataMember(Name = "title")] public string? Title { get; set; }
}

/// <summary>A logged error as Error_Get returns it.</summary>
[DataContract(Name = "Error", Namespace = "")]
public sealed class ErrorContract
{
    [DataMember] public string? Exception { get; set; }
    [DataMember] public string? Id { get; set; }
    [DataMember] public string? Message { get; set; }
}

/// <summary>An alert (UI AlertClient). The original's constructor never set viewed or created (both DateTime.MinValue).</summary>
[DataContract(Name = "AlertClient", Namespace = "")]
public sealed class AlertClientContract
{
    [DataMember(Name = "assetId")] public string? AssetId { get; set; }
    [DataMember(Name = "created")] public DateTime Created { get; set; }
    [DataMember(Name = "id")] public string? Id { get; set; }
    [DataMember(Name = "newValue")] public string? NewValue { get; set; }
    [DataMember(Name = "oldValue")] public string? OldValue { get; set; }
    [DataMember(Name = "pinned")] public bool Pinned { get; set; }
    [DataMember(Name = "propertyId")] public string? PropertyId { get; set; }
    [DataMember(Name = "propertyValueId")] public string? PropertyValueId { get; set; }
    [DataMember(Name = "submittalId")] public string? SubmittalId { get; set; }
    [DataMember(Name = "triggerId")] public string? TriggerId { get; set; }
    [DataMember(Name = "triggerLogId")] public string? TriggerLogId { get; set; }
    [DataMember(Name = "triggerText")] public string? TriggerText { get; set; }
    [DataMember(Name = "viewed")] public DateTime Viewed { get; set; }
}

/// <summary>
/// The original's QuestionGroup: a plain class (no data contract), so every public property is serialized in ordinal
/// order. Closed is never set (DateTime.MinValue).
/// </summary>
[DataContract(Name = "QuestionGroup", Namespace = "")]
public sealed class QuestionGroupContract
{
    [DataMember] public Guid AssetId { get; set; }
    [DataMember] public string? Caption { get; set; }
    [DataMember] public DateTime Closed { get; set; }
    [DataMember] public DateTime Created { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public Guid MemberId { get; set; }
    [DataMember] public List<QuestionContract> Questions { get; set; } = [];
    [DataMember] public Guid SurveyId { get; set; }
    [DataMember] public string? Url { get; set; }
}

/// <summary>The original's Question (plain class). AssetId is never set by the reader.</summary>
[DataContract(Name = "Question", Namespace = "")]
public sealed class QuestionContract
{
    [DataMember] public bool AllowUnknown { get; set; }
    [DataMember] public Guid AssetId { get; set; }
    [DataMember] public DateTime Created { get; set; }
    [DataMember] public string? DisableCondition { get; set; }
    [DataMember] public bool DisableNext { get; set; }
    [DataMember] public Guid Id { get; set; }
    [DataMember] public int Index { get; set; }
    [DataMember] public Guid MemberId { get; set; }
    [DataMember] public string? Message { get; set; }
    [DataMember] public Guid PropertyValueId { get; set; }
    [DataMember] public Guid QuestionGroupId { get; set; }
}

/// <summary>One QuestionsRespond entry (UI response): questionId as posted, responseValue an Int16, responseText optional.</summary>
public sealed record QuestionResponse(string? QuestionId, short ResponseValue, string? ResponseText);
