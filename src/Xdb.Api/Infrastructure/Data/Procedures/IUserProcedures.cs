using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xdb.Api.Infrastructure.Data.Procedures;

/// <summary>A member as MemberDal.Get read it (spr_Member_Get; the middle name is not read).</summary>
public sealed record MemberDetailRow(Guid Id, string UserId, string LastName, string FirstName, DateTime Created, Guid CreatedBy, DateTime? LastModified,
    DateTime? Deleted, Guid? DeletedBy, bool IsAdmin, bool AllowBeta);

/// <summary>One spr_MemberVisits_Get row.</summary>
public sealed record MemberVisitRow(DateTime Created, string? BrowserName, string? BrowserVersion);

/// <summary>A report shortcut (spr_Bookmarks_GetShortcuts rows with a report name).</summary>
public sealed record ShortcutRow(Guid ReportId, string? ReportName);

/// <summary>One alert (TriggerLogsMembers joined to TriggerLogs).</summary>
public sealed record AlertRow(Guid Id, Guid TriggerLogId, Guid TriggerId, Guid AssetId, Guid SubmittalId, Guid PropertyValueId, Guid PropertyId, string OldValue,
    string NewValue, string TriggerText, DateTime Viewed, DateTime Pinned);

/// <summary>An open question group (QuestionDal.GetQuestions).</summary>
public sealed record QuestionGroupRow(Guid Id, Guid SurveyId, Guid AssetId, string Caption, DateTime Created);

/// <summary>An open question of a group (QuestionDal.Questions).</summary>
public sealed record QuestionRow(Guid Id, int Index, bool AllowUnknown, bool DisableNext, string? DisableCondition, Guid PropertyValueId, string Message, DateTime Created);

/// <summary>QuestionDal.Get: the question's group, value, member.</summary>
public sealed record QuestionRefRow(Guid QuestionGroupId, Guid PropertyValueId, Guid MemberId);

/// <summary>The original's member, preference, feedback, alert and question data access (spec M7-U), with its SQL text.</summary>
public interface IUserProcedures
{
    // Members
    /// <summary>spr_Member_Get; null when there is no row.</summary>
    Task<MemberDetailRow?> GetMemberAsync(Guid memberId, CancellationToken cancellationToken);
    /// <summary>RoleDal.Roles_Get(memberId, false): the member's live roles in active roles (a repeated role fails).</summary>
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetActiveRolesAsync(Guid memberId, CancellationToken cancellationToken);
    /// <summary>MemberLayer.LastModified: Members.LastModified round-tripped through en-US text; null when absent or NULL.</summary>
    Task<DateTime?> GetLastModifiedAsync(Guid memberId, CancellationToken cancellationToken);
    /// <summary>MemberLayer.GetMemberId: the member with that login (deleted ones too), or the empty Guid; a null login is not supplied (fails).</summary>
    Task<Guid> GetMemberIdByLoginAsync(string? login, CancellationToken cancellationToken);
    Task<IReadOnlyList<MemberVisitRow>> GetVisitsAsync(Guid memberId, CancellationToken cancellationToken);
    /// <summary>spr_Member_Save with the original's parameters (an existing id updates, then the insert always runs).</summary>
    Task SaveMemberAsync(Guid id, string userId, string? lastName, string? firstName, DateTime created, Guid createdBy, CancellationToken cancellationToken);

    // Client model
    Task<IReadOnlyList<KeyValuePair<Guid, string>>> GetProcessesAsync(Guid memberId, bool isAdmin, CancellationToken cancellationToken);
    Task<IReadOnlyList<ShortcutRow>> GetShortcutsAsync(Guid memberId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetDashboardIdsAsync(Guid memberId, CancellationToken cancellationToken);

    // Settings and preferences
    Task<string> GetMemberSettingAsync(Guid memberId, string key, CancellationToken cancellationToken);
    Task InsertMemberSettingAsync(Guid memberId, string key, string value, CancellationToken cancellationToken);
    /// <summary>MemberDal.UserPreference_Save: the original's concatenated count and insert texts.</summary>
    Task<bool> SaveUserPreferenceAsync(Guid memberId, Guid optionTypeId, string? optionValue, CancellationToken cancellationToken);

    // Errors and feedback
    /// <summary>ErrorDal.Error_Get: (Message, Exception); null when there is no row.</summary>
    Task<(string Message, string? Exception)?> GetErrorAsync(Guid errorId, CancellationToken cancellationToken);
    Task InsertBugAsync(int area, int criticality, string? title, string? bug, Guid createdBy, CancellationToken cancellationToken);
    Task InsertSuggestionAsync(string? title, string? suggestion, Guid createdBy, CancellationToken cancellationToken);

    // Alerts
    Task<IReadOnlyList<AlertRow>> GetAlertsAsync(Guid memberId, CancellationToken cancellationToken);
    Task<int> CountUnreadAlertsAsync(Guid memberId, CancellationToken cancellationToken);
    Task PinAlertAsync(Guid alertId, bool pin, CancellationToken cancellationToken);

    // Questions
    Task<IReadOnlyList<QuestionGroupRow>> GetOpenQuestionGroupsAsync(Guid memberId, CancellationToken cancellationToken);
    Task<IReadOnlyList<QuestionRow>> GetOpenQuestionsAsync(Guid groupId, CancellationToken cancellationToken);
    Task<QuestionRefRow?> GetQuestionAsync(Guid questionId, CancellationToken cancellationToken);
    Task SaveResponseAsync(Guid questionId, short response, string responseText, DateTime responded, CancellationToken cancellationToken);
    Task<int> CountOpenQuestionsAsync(Guid groupId, CancellationToken cancellationToken);
    Task CloseQuestionGroupAsync(Guid groupId, CancellationToken cancellationToken);
}
