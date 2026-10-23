using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Assets.Queries;
using Xdb.Api.Application.Common.Identity;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Metadata;
using Xdb.Api.Application.Users.Legacy;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Users.Queries;

/// <summary>A member as MemberLayer.Get returned it: the row plus the bulk-upload permission.</summary>
public sealed record MemberModel(MemberDetailRow Row, bool AllowBulkUploads)
{
    /// <summary>The Member.DisplayValue property ("Last, First [login]"; the middle-initial form is unreachable).</summary>
    public string DisplayValue => $"{Row.LastName}, {Row.FirstName} [{Row.UserId}]";
}

/// <summary>MemberLayer.Get (spec M7-U 1.10): the row, then (non-admins) the active-role read that sets AllowBulkUploads.</summary>
public sealed class MemberLoader(IUserProcedures users)
{
    public async Task<MemberModel?> LoadAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var row = await users.GetMemberAsync(memberId, cancellationToken);
        if (row is null)
            return null;
        // RoleLayer.HasPermission(role, BulkUpload) is `return true`: any live role in an active role allows uploads.
        var allowBulkUploads = row.IsAdmin || (await users.GetActiveRolesAsync(memberId, cancellationToken)).Count > 0;
        return new MemberModel(row, allowBulkUploads);
    }
}

public interface IUserQueryHandler
{
    Task<Result<UserClientContract?>> GetUserClientAsync(string? userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> IsModifiedAsync(Guid userId, string? checkDateTime, CancellationToken cancellationToken = default);
    Task<Result<Guid?>> GetMemberIdAsync(string? login, CancellationToken cancellationToken = default);
    Task<Result<Guid>> GetCurrentMemberIdAsync(CancellationToken cancellationToken = default);
    Task<Result<string>> GetDisplayNameAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<List<string>>> GetActivityAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> GetMembersAsync(CancellationToken cancellationToken = default);
    /// <summary>Users_GetDictionary: the members dictionary, or null when the asking member does not exist.</summary>
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>?>> GetMembersForAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<List<string>>> GetDomainsAsync(CancellationToken cancellationToken = default);
    Task<Result<ErrorContract?>> GetErrorAsync(string? id, CancellationToken cancellationToken = default);
    Task<Result<List<AlertClientContract>?>> GetAlertsAsync(string? userId, CancellationToken cancellationToken = default);
    Task<Result<int>> CountAlertsAsync(string? userId, CancellationToken cancellationToken = default);
    Task<Result<List<QuestionGroupContract>>> GetQuestionsAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>The member, error, alert and question reads (spec M7-U section 1).</summary>
public sealed class UserQueryHandler(
    IUserProcedures users, MemberLoader members, CurrentMember currentMember, IViewProcedures views, IReportStore reports, IMetadataProcedures metadata,
    IDashboardProcedures dashboards, IUiProcedures ui, IAssetProcedures assets, IGetRecentItemsQueryHandler recentItems, MetadataLoader metadataLoader)
    : IUserQueryHandler
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    /// <summary>The release-notes setting the client model reads (the deployed release never changed the key).</summary>
    private const string ReleaseNoticeKey = "relNotes1.5.0.5";

    /// <summary>PickListIds.NetworkDomains (absent from the data: Domains faults).</summary>
    private static readonly Guid NetworkDomainsPickList = new("cb3203c5-e1e4-4106-865e-6fe0aec0c1c7");

    /// <summary>
    /// UserClient_Get (spec M7-U 1.1): an unparseable or empty id is the current member. The original ran the five
    /// dictionary reads on pool threads and waited for all of them; any failure faults the call, as here.
    /// </summary>
    public async Task<Result<UserClientContract?>> GetUserClientAsync(string? userId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(userId, out var id))
            id = await currentMember.GetIdAsync(cancellationToken);
        if (id == Guid.Empty)
            id = await currentMember.GetIdAsync(cancellationToken);

        var member = await members.LoadAsync(id, cancellationToken);
        if (member is null || member.Row.Deleted.HasValue)
            return new Success<UserClientContract?>(null);

        var userViews = await views.GetUserViewsAsync(id, cancellationToken);
        var processes = member.Row.Id == Guid.Empty ? [] : await users.GetProcessesAsync(member.Row.Id, member.Row.IsAdmin, cancellationToken);
        var recent = await recentItems.ExecuteAsync(new GetRecentItemsQuery(member.Row.Id), cancellationToken);
        var reportList = await metadata.IsAdminAsync(id, SystemRoleIds.Admin, cancellationToken)
            ? await reports.GetReportsDictionaryAsync(null, cancellationToken)
            : await reports.GetReportsDictionaryAsync(id, cancellationToken);
        var shortcuts = new Dictionary<string, string>();
        foreach (var shortcut in await users.GetShortcutsAsync(member.Row.Id, cancellationToken))
        {
            if (!string.IsNullOrEmpty(shortcut.ReportName))
                shortcuts.Add(shortcut.ReportId.ToString(), shortcut.ReportName);
        }

        var setting = await users.GetMemberSettingAsync(id, ReleaseNoticeKey, cancellationToken);
        var client = new UserClientContract
        {
            Id = member.Row.Id,
            UserId = member.Row.UserId,
            LastName = member.Row.LastName,
            FirstName = member.Row.FirstName,
            DisplayValue = member.DisplayValue,
            HiddenUIElements = null,
            AllowBulkUploads = member.AllowBulkUploads,
            IsAdmin = member.Row.IsAdmin,
            AllowBeta = member.Row.AllowBeta,
            ShowReleaseNotice = string.IsNullOrEmpty(setting),
            Views = Dictionary(userViews),
            Processes = Dictionary(processes),
            Recent = recent.Value,
            Reports = Dictionary(reportList),
            Shortcuts = shortcuts,
            LastModified = member.Row.LastModified?.ToString("M/d/yyyy h:mm:ss tt", EnUs)
        };
        client.Dashboards = await DashboardsAsync(member.Row.Id, cancellationToken);
        return new Success<UserClientContract?>(client);
    }

    private async Task<List<DashboardClientContract>> DashboardsAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var result = new List<DashboardClientContract>();
        foreach (var dashboardId in await users.GetDashboardIdsAsync(memberId, cancellationToken))
        {
            var dashboard = await dashboards.GetDashboardAsync(dashboardId, cancellationToken)
                ?? throw new InvalidOperationException("Dashboard not found.");
            var client = new DashboardClientContract { Id = dashboard.Id.ToString(), Caption = dashboard.Caption, Index = dashboard.Index, MemberId = memberId.ToString() };
            foreach (var item in dashboard.Items)
            {
                client.Items.Add(new DashboardItemClientContract
                {
                    Id = item.Id.ToString(), DashboardId = dashboard.Id.ToString(), Index = item.Index, ItemType = item.ItemTypeId, ChartId = item.ChartId.ToString(),
                    Title = item.Title, Subtitle = item.Subtitle
                });
            }
            result.Add(client);
        }
        return result;
    }

    private static Dictionary<Guid, string> Dictionary(IReadOnlyList<KeyValuePair<Guid, string>> values)
    {
        var dictionary = new Dictionary<Guid, string>();
        foreach (var (key, value) in values)
            dictionary.Add(key, value);
        return dictionary;
    }

    /// <summary>User_Modified: as ViewClientModified, against Members.LastModified.</summary>
    public async Task<Result<bool>> IsModifiedAsync(Guid userId, string? checkDateTime, CancellationToken cancellationToken = default)
    {
        var lastModified = await users.GetLastModifiedAsync(userId, cancellationToken);
        if (DateTime.TryParse(checkDateTime, EnUs, DateTimeStyles.None, out var check))
            return new Success<bool>(lastModified.HasValue && lastModified.Value.CompareTo(check) != 0);
        return new Success<bool>(lastModified.HasValue);
    }

    public async Task<Result<Guid?>> GetMemberIdAsync(string? login, CancellationToken cancellationToken = default)
    {
        var id = await users.GetMemberIdByLoginAsync(login, cancellationToken);
        return new Success<Guid?>(id == Guid.Empty ? null : id);
    }

    public async Task<Result<Guid>> GetCurrentMemberIdAsync(CancellationToken cancellationToken = default) =>
        new Success<Guid>(await currentMember.GetIdAsync(cancellationToken));

    /// <summary>MemberLayer.DisplayValue: "" for no member, the login when both names are empty, else "Last, First [login]".</summary>
    public async Task<Result<string>> GetDisplayNameAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var member = await members.LoadAsync(userId, cancellationToken);
        if (member is null)
            return new Success<string>(string.Empty);
        if (member.Row.LastName.Length == 0 && member.Row.FirstName.Length == 0)
            return new Success<string>(member.Row.UserId);
        return new Success<string>(member.DisplayValue);
    }

    /// <summary>UserVisits_Get: "date\t[browser\tversion]" per distinct visit, newest first (invariant date format).</summary>
    public async Task<Result<List<string>>> GetActivityAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = new List<string>();
        foreach (var visit in await users.GetVisitsAsync(userId, cancellationToken))
            result.Add($"{visit.Created.ToString(CultureInfo.InvariantCulture)}\t[{visit.BrowserName ?? string.Empty}\t{visit.BrowserVersion ?? string.Empty}]");
        return new Success<List<string>>(result);
    }

    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> GetMembersAsync(CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await metadata.GetMembersDictionaryAsync(cancellationToken));

    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>?>> GetMembersForAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (await members.LoadAsync(userId, cancellationToken) is null)
            return new Success<IReadOnlyList<KeyValuePair<Guid, string>>?>(null);
        return new Success<IReadOnlyList<KeyValuePair<Guid, string>>?>(await metadata.GetMembersDictionaryAsync(cancellationToken));
    }

    /// <summary>Domains: the network-domains picklist's values; the picklist does not exist, so this faults as the original did.</summary>
    public async Task<Result<List<string>>> GetDomainsAsync(CancellationToken cancellationToken = default)
    {
        var pickList = await metadataLoader.PickListAsync(NetworkDomainsPickList, cancellationToken)
            ?? throw new NullReferenceException("Object reference not set to an instance of an object.");
        var result = new List<string>();
        foreach (var value in pickList.PickListValues)
            result.Add(value.Value);
        return new Success<List<string>>(result);
    }

    /// <summary>Error_Get: the logged error; Exception falls back to Message when NULL (every row today).</summary>
    public async Task<Result<ErrorContract?>> GetErrorAsync(string? id, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var errorId))
            return new Success<ErrorContract?>(null);
        var row = await users.GetErrorAsync(errorId, cancellationToken);
        if (row is not { } error)
            return new Success<ErrorContract?>(null);
        return new Success<ErrorContract?>(new ErrorContract { Id = errorId.ToString(), Message = error.Message, Exception = error.Exception ?? error.Message });
    }

    public async Task<Result<List<AlertClientContract>?>> GetAlertsAsync(string? userId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(userId, out var memberId))
            return new Success<List<AlertClientContract>?>(null);
        var result = new List<AlertClientContract>();
        foreach (var alert in await users.GetAlertsAsync(memberId, cancellationToken))
        {
            result.Add(new AlertClientContract
            {
                Id = alert.Id.ToString(), TriggerLogId = alert.TriggerLogId.ToString(), TriggerId = alert.TriggerId.ToString(), AssetId = alert.AssetId.ToString(),
                SubmittalId = alert.SubmittalId.ToString(), PropertyValueId = alert.PropertyValueId.ToString(), PropertyId = alert.PropertyId.ToString(),
                OldValue = alert.OldValue, NewValue = alert.NewValue, TriggerText = alert.TriggerText, Pinned = alert.Pinned != default
            });
        }
        return new Success<List<AlertClientContract>?>(result);
    }

    public async Task<Result<int>> CountAlertsAsync(string? userId, CancellationToken cancellationToken = default) =>
        new Success<int>(Guid.TryParse(userId, out var memberId) ? await users.CountUnreadAlertsAsync(memberId, cancellationToken) : 0);

    /// <summary>QuestionLayer.GetQuestions: the open groups with their open questions, then each group's default view id as its Url.</summary>
    public async Task<Result<List<QuestionGroupContract>>> GetQuestionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var groups = new List<QuestionGroupContract>();
        foreach (var row in await users.GetOpenQuestionGroupsAsync(userId, cancellationToken))
            groups.Add(new QuestionGroupContract { Id = row.Id, SurveyId = row.SurveyId, AssetId = row.AssetId, Caption = row.Caption, MemberId = userId, Created = row.Created });
        foreach (var group in groups)
        {
            foreach (var question in await users.GetOpenQuestionsAsync(group.Id, cancellationToken))
            {
                group.Questions.Add(new QuestionContract
                {
                    Id = question.Id, Index = question.Index, AllowUnknown = question.AllowUnknown, DisableNext = question.DisableNext,
                    DisableCondition = question.DisableCondition, PropertyValueId = question.PropertyValueId, Message = question.Message, Created = question.Created,
                    MemberId = userId, QuestionGroupId = group.Id
                });
            }
        }
        foreach (var group in groups)
        {
            var isInstance = await assets.IsInstanceAsync(group.AssetId, cancellationToken);
            var assetTypeId = await metadata.GetAssetTypeIdOfAssetAsync(group.AssetId, cancellationToken)
                ?? throw new InvalidOperationException("Nullable object must have a value.");
            group.Url = (await ui.GetDefaultViewIdAsync(assetTypeId, isInstance, cancellationToken)).ToString();
        }
        return new Success<List<QuestionGroupContract>>(groups);
    }
}
