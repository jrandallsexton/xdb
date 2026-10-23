using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Xdb.Api.Application.Activity.Legacy;
using Xdb.Api.Application.Assets;
using Xdb.Api.Application.Common.Identity;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.GeneratedObjects;
using Xdb.Api.Application.Metadata;
using Xdb.Api.Application.Metadata.Queries.AssetTypes;
using Xdb.Api.Application.Metadata.Queries.Lookups;
using Xdb.Api.Infrastructure.Background;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;
using Xdb.Core.LegacyWire;

namespace Xdb.Api.Application.Activity;

/// <summary>The search kinds of Search (UI ESearchType); any other number searches nothing.</summary>
public enum SearchType
{
    Asset = 0,
    PropertyValue = 1,
    PropertyValueAsset = 2,
    User = 3
}

/// <summary>
/// Submittal_GetSummary's steps in the original order (spec M7-S 1.3), shared with the details feed: the first step
/// that fails decides the error.
/// </summary>
public sealed class SubmittalSummaryLoader(IActivityProcedures activity, IMetadataProcedures metadata, IAssetProcedures assets, IUiProcedures ui)
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    public async Task<SubmittalSummaryContract> LoadAsync(string? submittalId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(submittalId, out var id))
            throw new LegacyLogicalException("Invalid submittalId");

        var values = new Dictionary<string, KeyValuePair<string, string>>();
        foreach (var row in await activity.GetSubmittalSummaryAsync(id, cancellationToken))
            values.TryAdd(row.Property, new KeyValuePair<string, string>(row.Previous, row.New));
        var created = await activity.GetSubmittalCreatedAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Nullable object must have a value.");
        var (assetId, assetName) = await activity.GetSubmittalAssetInfoAsync(id, cancellationToken) ?? (Guid.Empty, null);
        var assetTypeId = await metadata.GetAssetTypeIdOfAssetAsync(assetId, cancellationToken);
        var isInstance = await assets.IsInstanceAsync(assetId, cancellationToken);
        var viewId = await ui.GetDefaultViewIdAsync(assetTypeId ?? throw new InvalidOperationException("Nullable object must have a value."), isInstance,
            cancellationToken);
        return new SubmittalSummaryContract
        {
            AssetId = assetId.ToString(), AssetName = assetName, AssetTypeId = assetTypeId.Value.ToString(), ViewId = viewId.ToString(),
            Created = created.ToString("M/d/yyyy h:mm:ss tt", EnUs), Values = values
        };
    }
}

public interface IActivityQueryHandler
{
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> GetUnreviewedSubmittalsAsync(CancellationToken cancellationToken = default);
    Task<Result<SubmittalContract>> GetSubmittalAsync(Guid submittalId, CancellationToken cancellationToken = default);
    Task<Result<SubmittalSummaryContract>> GetSummaryAsync(string? submittalId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> GetTriggerDictionaryAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<TriggerContract?>> GetTriggerAsync(Guid triggerId, CancellationToken cancellationToken = default);
    Task<Result<RssChannel>> GetHomeFeedAsync(string? userId, int maxItems, CancellationToken cancellationToken = default);
    Task<Result<RssChannel>> GetDetailsFeedAsync(string? userId, string? submittalId, CancellationToken cancellationToken = default);
    Task<Result<RssChannel>> GetActivityFeedAsync(string? assetType, CancellationToken cancellationToken = default);
}

/// <summary>Submittal reads, trigger reads and the activity feeds (spec M7-S sections 1-3).</summary>
public sealed class ActivityQueryHandler(
    IActivityProcedures activity, IMetadataProcedures metadata, IRoleProcedures roles, PropertyValueLoader values, SubmittalSummaryLoader summaries,
    CurrentMember currentMember, IOptions<LegacyEnvironmentOptions> environment, IDateTimeProvider clock) : IActivityQueryHandler
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");
    private const string EnUsGeneralPattern = "M/d/yyyy h:mm:ss tt";
    private const int MaxFeedItems = 250;
    private const int TriggerNameLimit = 100;
    private const int RestrictionType = 4;

    private DateTime Now() => clock.UtcNow().ToLocalTime();

    private string FeedDescription => environment.Value.ProductName + " - System Activity Feed";

    private string FeedTitle => environment.Value.ProductName + " System Activity Feed - Role-Based";

    /// <summary>Submittals_Get: unreviewed submittals with values, "&lt;asset&gt; on &lt;created&gt; by &lt;member&gt;".</summary>
    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> GetUnreviewedSubmittalsAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<KeyValuePair<Guid, string>>();
        var seen = new HashSet<Guid>();
        foreach (var row in await activity.GetUnreviewedSubmittalsAsync(cancellationToken))
        {
            if (!seen.Add(row.Id))
                throw new ArgumentException($"An item with the same key has already been added: {row.Id:D}");
            result.Add(new KeyValuePair<Guid, string>(row.Id, $"{row.AssetName} on {row.Created.ToString(EnUsGeneralPattern, EnUs)} by {row.CreatedByDisplay}"));
        }
        return new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(result);
    }

    /// <summary>Submittal_Get: the live submittal with its live values (decoded as the asset loader does), each with its property's display name.</summary>
    public async Task<Result<SubmittalContract>> GetSubmittalAsync(Guid submittalId, CancellationToken cancellationToken = default)
    {
        var row = await activity.GetSubmittalAsync(submittalId, cancellationToken)
            ?? throw new NullReferenceException("Object reference not set to an instance of an object.");
        var submittal = new SubmittalContract
        {
            Id = row.Id ?? Guid.NewGuid(), AssetId = row.AssetId ?? Guid.Empty, Notes = row.Notes ?? string.Empty, Created = row.Created ?? Now(), Reviewed = row.Reviewed,
            Deleted = row.Deleted, CreatedBy = row.CreatedBy ?? Guid.Empty, ReviewedBy = row.ReviewedBy, DeletedBy = row.DeletedBy
        };

        var rows = await activity.GetSubmittalValuesAsync(submittal.Id, cancellationToken);
        if (rows.Count > 0)
        {
            var properties = (await metadata.GetPropertiesAsync(rows.Select(r => r.PropertyId).Distinct().ToList(), cancellationToken))
                .ToDictionary(p => p.Id, MetadataLoader.Property);
            foreach (var value in rows)
            {
                var property = properties[value.PropertyId];
                var model = new PropertyValueModel
                {
                    Id = value.Id, PropertyId = value.PropertyId, AssetId = value.AssetId, Value = PropertyValueLoader.FormatValue(property, value.Value ?? string.Empty),
                    Order = value.Order, Created = value.Created, CreatedBy = value.CreatedBy
                };
                if (!string.IsNullOrEmpty(model.Value))
                {
                    model.DisplayValue = await values.DisplayValueAsync(model.Value, property, cancellationToken);
                    model.DisplayValueHtml = await values.DisplayValueHtmlAsync(model, property, cancellationToken);
                }
                submittal.PropertyValues.Add(new SubmittalValueContract
                {
                    Id = value.Id, PropertyId = value.PropertyId, AssetId = value.AssetId, SubmittalGroupId = value.SubmittalGroupId, Value = model.Value,
                    Order = value.Order, Created = value.Created, Approved = value.Approved, Rejected = value.Rejected, Deleted = value.Deleted,
                    CreatedBy = value.CreatedBy, ApprovedBy = value.ApprovedBy, RejectedBy = value.RejectedBy, DeletedBy = value.DeletedBy,
                    DisplayValue = model.DisplayValue, DisplayValueHtml = model.DisplayValueHtml
                });
            }
        }
        foreach (var value in submittal.PropertyValues)
            value.Property = await metadata.GetPropertyDisplayValueAsync(value.PropertyId, cancellationToken);
        return new Success<SubmittalContract>(submittal);
    }

    public async Task<Result<SubmittalSummaryContract>> GetSummaryAsync(string? submittalId, CancellationToken cancellationToken = default) =>
        new Success<SubmittalSummaryContract>(await summaries.LoadAsync(submittalId, cancellationToken));

    public async Task<Result<IReadOnlyList<KeyValuePair<Guid, string>>>> GetTriggerDictionaryAsync(Guid userId, CancellationToken cancellationToken = default) =>
        new Success<IReadOnlyList<KeyValuePair<Guid, string>>>(await activity.GetTriggerDictionaryAsync(userId, cancellationToken));

    /// <summary>TriggerDal.Get: a restriction trigger loads only its restrictions; FilterLogic is never read.</summary>
    public async Task<Result<TriggerContract?>> GetTriggerAsync(Guid triggerId, CancellationToken cancellationToken = default)
    {
        var row = await activity.GetTriggerAsync(triggerId, cancellationToken);
        if (row is null)
            return new Success<TriggerContract?>(null);
        var trigger = new TriggerContract
        {
            Id = triggerId, Name = row.Name.Length > TriggerNameLimit ? row.Name[..(TriggerNameLimit - 1)] : row.Name, Description = row.Description,
            IsActive = bool.Parse(row.IsActive), Type = int.Parse(row.TypeId, NumberStyles.Integer, CultureInfo.InvariantCulture), Created = row.Created,
            CreatedBy = row.CreatedBy, Deleted = row.Deleted, DeletedBy = row.DeletedBy
        };
        if (trigger.Type == RestrictionType)
        {
            foreach (var r in await activity.GetTriggerRestrictionsAsync(triggerId, cancellationToken))
            {
                trigger.Restrictions.Add(new TriggerRestrictionContract
                {
                    Id = r.Id, TriggerId = triggerId, PropertyId = r.PropertyId, RestrictedPropertyId = r.RestrictedPropertyId, SourcePropertyId = r.SourcePropertyId ?? Guid.Empty
                });
            }
        }
        else
        {
            foreach (var p in await activity.GetTriggerPropertiesAsync(triggerId, cancellationToken))
            {
                trigger.Properties.Add(new TriggerPropertyContract
                {
                    Id = p.Id, TriggerId = triggerId, PropertyId = p.PropertyId ?? Guid.Empty, Value = p.Value ?? string.Empty, OperatorId = p.OperatorId,
                    Threshold = p.Threshold ?? string.Empty
                });
            }
            for (var i = await activity.CountTriggerAssetTypesAsync(triggerId, cancellationToken); i > 0; i--)
                trigger.AssetTypes.Add(new EmptyContract());
            for (var i = await activity.CountTriggerAssetsAsync(triggerId, cancellationToken); i > 0; i--)
                trigger.Assets.Add(new EmptyContract());
            trigger.MemberIds.AddRange(await activity.GetTriggerMembersAsync(triggerId, cancellationToken));
            trigger.RoleIds.AddRange(await activity.GetTriggerRolesAsync(triggerId, cancellationToken));
            foreach (var t in await activity.GetTriggerTargetPropertiesAsync(triggerId, cancellationToken))
                trigger.TargetProperties.Add(new TriggerTargetPropertyContract { Id = t.Id, TargetPropertyId = t.TargetPropertyId, ChangeToValue = t.ChangeToValue, TriggerId = triggerId });
        }
        return new Success<TriggerContract?>(trigger);
    }

    /// <summary>
    /// SystemActivityHome (spec M7-S 3.1): the latest narrated submittals, all of them for an administrator, otherwise
    /// those of assets in the member's editable role views; at most 250 items.
    /// </summary>
    public async Task<Result<RssChannel>> GetHomeFeedAsync(string? userId, int maxItems, CancellationToken cancellationToken = default)
    {
        if (maxItems == 0)
            maxItems = 25;
        if (!Guid.TryParse(userId, out var memberId))
            throw new LegacyLogicalException("Invalid userId");
        var rest = environment.Value.RestServiceAddress;
        var channelLink = LegacyUri.AbsoluteUri(rest + "/SystemActivity");

        string sql;
        if (await roles.ContainsMemberAsync(memberId, SystemRoleIds.Admin, cancellationToken))
        {
            sql = string.Format(CultureInfo.InvariantCulture,
                "SELECT DISTINCT TOP {0} A.[Id], A.[AssetTypeId], ISNULL(A.[DisplayValue], A.[Name]) AS [Asset Code], 'Admin' AS [Role], PVS.[Id], PVS.[Narrative], PVS.[Created]",
                maxItems) + "\r\n"
                + "FROM [Assets] A\r\n"
                + "INNER JOIN [PropertyValueSubmittals] PVS ON PVS.[AssetId] = A.[Id]\r\n"
                + "WHERE ((PVS.[Narrative] IS NOT NULL) AND (PVS.[CreatedBy] != '6CACA132-7827-4DE6-81E6-2B7D8DB939B9'))\r\n"
                + "ORDER BY PVS.[Created] DESC\r\n";
        }
        else
        {
            var editable = await activity.GetEditableRolesAsync(memberId, cancellationToken);
            // No editable roles: an empty feed. (The original built an empty UNION here and failed with a syntax error.)
            if (editable.Count == 0)
                return new Success<RssChannel>(new RssChannel(FeedTitle, channelLink, FeedDescription, environment.Value.FeedEditor, new List<RssItem>()));
            var inner = new StringBuilder();
            for (var i = 0; i < editable.Count; i++)
            {
                var role = editable[i];
                inner.Append($"SELECT [AssetId], [Asset Code], '{role.RoleName}' AS [Role] FROM [{GeneratedObjectNames.RoleView(role.RoleId, role.AssetTypeId, 1)}]").Append("\r\n");
                if (i < editable.Count - 1)
                    inner.Append("UNION\r\n");
            }
            sql = string.Format(CultureInfo.InvariantCulture,
                "SELECT DISTINCT TOP {0} A.[AssetId], A2.[AssetTypeId], [Asset Code], [Role], PVS.[Id], PVS.[Narrative], PVS.[Created] FROM (", maxItems) + "\r\n"
                + inner + "\r\n"
                + ") A\r\n"
                + "INNER JOIN [PropertyValueSubmittals] PVS ON PVS.[AssetId] = A.[AssetId]\r\n"
                + "INNER JOIN [Assets] A2 ON A2.[Id] = A.[AssetId]\r\n"
                + "WHERE PVS.[Narrative] IS NOT NULL\r\n"
                + "ORDER BY PVS.[Created] DESC\r\n";
        }

        var items = new List<RssItem>();
        foreach (var row in await activity.GetActivityAsync(sql, cancellationToken))
        {
            if (items.Count >= MaxFeedItems)
                break;
            var link = LegacyUri.AbsoluteUri($"{rest}/SystemActivityHomeDetails?userId={userId}&submittalId={row.SubmittalId}");
            var submittalDate = row.Created.ToString("ddd dd MMM yyyy", EnUs) + " " + row.Created.ToString("hh:mm:ss.F", CultureInfo.InvariantCulture);
            items.Add(new RssItem(row.SubmittalId.ToString(), link, row.Narrative, row.Role, Now(), row.Created,
                [new("assetTypeId", row.AssetTypeId.ToString()), new("submittalDate", submittalDate)]));
        }
        return new Success<RssChannel>(new RssChannel(FeedTitle, channelLink, FeedDescription, environment.Value.FeedEditor, items));
    }

    /// <summary>
    /// SystemActivityHomeDetails (spec M7-S 3.2): one item per summary value. The channel link keeps the template's
    /// literal "{0}"; item links put the userId text into submittalId, as the original did.
    /// </summary>
    public async Task<Result<RssChannel>> GetDetailsFeedAsync(string? userId, string? submittalId, CancellationToken cancellationToken = default)
    {
        var template = environment.Value.RestServiceAddress + "/SystemActivityHome?submittalId={0}&maxItems=100";
        var channelLink = LegacyUri.AbsoluteUri(template + "/SystemActivity");
        var summary = await summaries.LoadAsync(submittalId, cancellationToken);
        var items = new List<RssItem>();
        if (summary.Values.Count > 0)
        {
            var created = DateTime.Parse(summary.Created!, EnUs);
            foreach (var (property, change) in summary.Values)
            {
                var link = LegacyUri.AbsoluteUri(string.Format(CultureInfo.InvariantCulture, template, userId));
                items.Add(new RssItem(change.Value, link, property, $"{property} changed from '{change.Key}' to '{change.Value}'.", Now(), created));
            }
        }
        return new Success<RssChannel>(new RssChannel(FeedTitle + " - Submittal Details", channelLink, FeedDescription, environment.Value.FeedEditor, items));
    }

    /// <summary>
    /// SystemActivity (spec M7-S 3.3): the original read the caller's asset types, then ran a statement that never
    /// compiles (ORDER BY in a derived table without TOP, SQL 1033), so every call ends in the Request Error page. The
    /// port does the same two reads and fails the same way without sending the statement.
    /// </summary>
    public async Task<Result<RssChannel>> GetActivityFeedAsync(string? assetType, CancellationToken cancellationToken = default)
    {
        await activity.ReadAssetTypesForMemberAsync(await currentMember.GetIdAsync(cancellationToken), cancellationToken);
        throw new InvalidOperationException("The ORDER BY clause is invalid in views, inline functions, derived tables, subqueries, and common table expressions, unless TOP is also specified.");
    }
}

public interface ISearchQueryHandler
{
    /// <summary>Search: null for an absent or empty phrase (no search, nothing logged).</summary>
    Task<Result<List<SearchResultContract>?>> SearchAsync(string? phrase, Guid userId, SearchType searchType, CancellationToken cancellationToken = default);
    Task<Result<List<TagContract>?>> SearchTagAsync(string? phrase, CancellationToken cancellationToken = default);
}

/// <summary>Search and SearchTag (spec M7-S section 4).</summary>
public sealed class SearchQueryHandler(
    IActivityProcedures activity, IAssetProcedures assets, IGetDefaultViewsQueryHandler defaultViews, CurrentMember currentMember, IBackgroundWorkQueue background,
    IOptions<LegacyEnvironmentOptions> environment) : ISearchQueryHandler
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");
    private const string Category = "Asset";

    /// <summary>
    /// UILayer.Search2: the search is logged in the background before it runs (with the trimmed phrase), then one
    /// search kind runs. The original treated every caller as an administrator here.
    /// </summary>
    public async Task<Result<List<SearchResultContract>?>> SearchAsync(string? phrase, Guid userId, SearchType searchType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(phrase))
            return new Success<List<SearchResultContract>?>(null);
        var text = phrase.Trim();
        background.Enqueue($"log search {userId}", (services, ct) => services.GetRequiredService<IActivityProcedures>().InsertSearchAsync(text, userId, ct));

        var definitionViews = Views((await defaultViews.ExecuteAsync(new GetDefaultViewsQuery(userId, AssetRequestType.Definition), cancellationToken)).Value);
        var instanceViews = Views((await defaultViews.ExecuteAsync(new GetDefaultViewsQuery(userId, AssetRequestType.Instance), cancellationToken)).Value);
        var baseUrl = environment.Value.ClientUiRoot + "/" + environment.Value.ClientViewPageName;
        string? Url(Guid assetTypeId, bool isInstance, Guid assetId, string? fallback) =>
            (isInstance ? instanceViews : definitionViews).TryGetValue(assetTypeId, out var viewId) ? $"{baseUrl}?viewId={viewId}&assetId={assetId}" : fallback;

        var results = new List<SearchResultContract>();
        switch (searchType)
        {
            case SearchType.Asset:
            case SearchType.User:
                var rows = searchType == SearchType.Asset ? await activity.SearchAssetsAsync(text, cancellationToken) : await activity.SearchUsersAsync(text, cancellationToken);
                foreach (var row in rows)
                {
                    results.Add(new SearchResultContract
                    {
                        AssetId = row.AssetId, AssetTypeId = row.AssetTypeId.ToString(), Title = row.Title, Content = row.Content ?? string.Empty, Category = Category,
                        Index = results.Count, Url = Url(row.AssetTypeId, row.IsInstance, row.AssetId, string.Empty)
                    });
                }
                break;
            case SearchType.PropertyValue:
                foreach (var row in await activity.SearchPropertyValueTextAsync(text, cancellationToken))
                {
                    results.Add(new SearchResultContract
                    {
                        Title = row.Title, Category = Category, AssetTypeId = row.AssetTypeId.ToString(), Content = Highlight(row.Content!, text), Index = results.Count,
                        AssetId = row.AssetId, Url = Url(row.AssetTypeId, row.IsInstance, row.AssetId, null)
                    });
                }
                break;
            case SearchType.PropertyValueAsset:
                var ids = new List<Guid>();
                foreach (var row in await activity.SearchAssetsAsync(text, cancellationToken))
                {
                    if (!ids.Contains(row.AssetId))
                        ids.Add(row.AssetId);
                }
                if (ids.Count == 0)
                    break;
                foreach (var row in await activity.SearchPropertyValueAssetsAsync(userId, string.Join(",", ids.Select(id => id.ToString())), cancellationToken))
                {
                    var content = row.Description ?? string.Empty;
                    if (row.InstanceOfId is { } definitionId)
                        content = (await assets.GetAssetAsync(definitionId, cancellationToken) ?? throw new NullReferenceException("Object reference not set to an instance of an object."))
                            .Description!;
                    results.Add(new SearchResultContract
                    {
                        Title = row.AssetName + " - " + row.Property + " - " + row.AssetValue, Category = Category, Content = Highlight(content, text), Index = results.Count,
                        AssetId = row.AssetId, AssetTypeId = row.AssetTypeId.ToString(), Url = Url(row.AssetTypeId, row.IsInstance, row.AssetId, null)
                    });
                }
                break;
        }
        return new Success<List<SearchResultContract>?>(results);
    }

    /// <summary>
    /// The original bolded the first culture-sensitive (en-US) match of the lower-cased phrase and closed the tag after
    /// the phrase's length. DEVIATION (recorded): .NET 10 compares with ICU, not NLS; they differ only for some
    /// non-ASCII expansions and ignorable characters.
    /// </summary>
    public static string Highlight(string text, string phrase)
    {
        var index = EnUs.CompareInfo.IndexOf(text.ToLower(EnUs), phrase.ToLower(EnUs), CompareOptions.None);
        if (index > -1)
        {
            text = text.Insert(index, "<b>");
            text = text.Insert(index + phrase.Length + 3, "</b>");
        }
        return text;
    }

    private static Dictionary<Guid, Guid> Views(IReadOnlyList<KeyValuePair<Guid, Guid>> pairs)
    {
        var dictionary = new Dictionary<Guid, Guid>();
        foreach (var (key, value) in pairs)
            dictionary.Add(key, value);
        return dictionary;
    }

    /// <summary>
    /// SearchTag: asset names then member names, each name once, numbered from 1. The original ran the two reads on
    /// parallel threads into one unsynchronized dictionary (entries interleaved, and lost on large results); the port
    /// runs them in turn (recorded deviation: one of the original's possible outputs, never a corrupted one).
    /// </summary>
    public async Task<Result<List<TagContract>?>> SearchTagAsync(string? phrase, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(phrase))
            return new Success<List<TagContract>?>(null);
        _ = await currentMember.GetIdAsync(cancellationToken);
        var names = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var procedure in new[] { "spr_SearchTag_Asset", "spr_SearchTag_User" })
        {
            foreach (var name in await activity.SearchTagAsync(procedure, phrase, cancellationToken))
            {
                if (seen.Add(name))
                    names.Add(name);
            }
        }
        var result = new List<TagContract>();
        foreach (var name in names)
            result.Add(new TagContract { Id = result.Count + 1, Name = name, Img = "http://example.com/img1.jpg", Type = "contact" });
        return new Success<List<TagContract>?>(result);
    }
}

public interface ITriggerCommandHandler
{
    Task<Result<bool>> CreateAsync(TriggerBody? trigger, CancellationToken cancellationToken = default);
}

/// <summary>
/// TriggerCreate (spec M7-S 2.3). The original's save sends a parameter the procedure does not declare and omits a
/// required one, so the database rejects every call before writing anything. The port sends the same parameters and
/// fails the same way; it must not correct them (that would start saving triggers the original never saved).
/// </summary>
public sealed class TriggerCommandHandler(IActivityProcedures activity, CurrentMember currentMember, IDateTimeProvider clock) : ITriggerCommandHandler
{
    private const int TriggerNameLimit = 100;

    public async Task<Result<bool>> CreateAsync(TriggerBody? trigger, CancellationToken cancellationToken = default)
    {
        var body = trigger ?? throw new NullReferenceException("Object reference not set to an instance of an object.");
        var id = new Guid(body.Id ?? throw new ArgumentNullException("g"));
        var name = body.Name is { Length: > TriggerNameLimit } ? body.Name[..(TriggerNameLimit - 1)] : body.Name;
        foreach (var assetTypeId in body.AssetTypeIds ?? throw new NullReferenceException("Object reference not set to an instance of an object."))
            _ = new Guid(assetTypeId ?? throw new ArgumentNullException("g"));
        var created = clock.UtcNow().ToLocalTime();
        var createdBy = await currentMember.GetIdAsync(cancellationToken);
        await activity.SaveTriggerAsync(id, name, name, isActive: true, body.Type, created, createdBy, cancellationToken);
        return new Success<bool>(true);
    }
}
