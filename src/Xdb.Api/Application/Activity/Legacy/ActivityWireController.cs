using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;

using Xdb.Api.Application.Assets.Legacy;
using Xdb.Core.LegacyWire;

namespace Xdb.Api.Application.Activity.Legacy;

/// <summary>
/// The original service's submittal, trigger, activity-feed and search operations (spec M7-S). The feeds were XML
/// operations: any failure in them is the Request Error page rather than the JSON fault.
/// </summary>
[Route(LegacyWire.BaseRoute)]
public sealed class ActivityWireController : LegacyWireControllerBase
{
    // ---- Submittals ----

    /// <summary>userId is bound and unused.</summary>
    [HttpGet("Submittals_Get")]
    public async Task<IActionResult> SubmittalsGet([FromServices] IActivityQueryHandler handler, [FromQuery] Guid userId, CancellationToken cancellationToken) =>
        (await handler.GetUnreviewedSubmittalsAsync(cancellationToken)).ToLegacyResult(TypedDictionary);

    [HttpGet("Submittal_Get")]
    public async Task<IActionResult> SubmittalGet([FromServices] IActivityQueryHandler handler, [FromQuery] Guid submittalId, CancellationToken cancellationToken) =>
        (await handler.GetSubmittalAsync(submittalId, cancellationToken)).ToLegacyResult(submittal => LegacyWire.Typed(submittal));

    [HttpGet("Submittal_GetSummary")]
    public async Task<IActionResult> SubmittalGetSummary([FromServices] IActivityQueryHandler handler, [FromQuery] string? submittalId, CancellationToken cancellationToken) =>
        (await handler.GetSummaryAsync(submittalId, cancellationToken)).ToLegacyResult(summary => LegacyWire.Typed(summary));

    // ---- Triggers ----

    [HttpGet("Triggers_GetDictionary")]
    public async Task<IActionResult> TriggersGetDictionary([FromServices] IActivityQueryHandler handler, [FromQuery] Guid userId, CancellationToken cancellationToken) =>
        (await handler.GetTriggerDictionaryAsync(userId, cancellationToken)).ToLegacyResult(triggers => LegacyWire.HtmlStream(LegacyFormatters.FormatDictionary(triggers)));

    [HttpGet("Trigger_Get")]
    public async Task<IActionResult> TriggerGet([FromServices] IActivityQueryHandler handler, [FromQuery] Guid triggerId, CancellationToken cancellationToken) =>
        (await handler.GetTriggerAsync(triggerId, cancellationToken)).ToLegacyResult(LegacyWire.TypedOrNull);

    /// <summary>Bare TriggerClient body. Every bound call fails in the database (see <see cref="TriggerCommandHandler"/>).</summary>
    [HttpPost("TriggerCreate")]
    public async Task<IActionResult> TriggerCreate([FromServices] ITriggerCommandHandler handler, CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken);
        if (error is not null)
            return error;
        TriggerBody? body;
        try
        {
            body = ActivityBodies.Trigger(root);
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        return (await handler.CreateAsync(body, cancellationToken)).ToLegacyResult(done => LegacyWire.Typed(done));
    }

    // ---- Activity feeds (XML) ----

    [HttpGet("SystemActivityHome")]
    [LegacyXmlOperation]
    public async Task<IActionResult> SystemActivityHome([FromServices] IActivityQueryHandler handler, [FromQuery] string? userId, [FromQuery] int maxItems,
        CancellationToken cancellationToken) =>
        (await handler.GetHomeFeedAsync(userId, maxItems, cancellationToken)).ToLegacyResult(LegacyWire.Rss);

    [HttpGet("SystemActivityHomeDetails")]
    [LegacyXmlOperation]
    public async Task<IActionResult> SystemActivityHomeDetails([FromServices] IActivityQueryHandler handler, [FromQuery] string? userId, [FromQuery] string? submittalId,
        CancellationToken cancellationToken) =>
        (await handler.GetDetailsFeedAsync(userId, submittalId, cancellationToken)).ToLegacyResult(LegacyWire.Rss);

    [HttpGet("SystemActivity")]
    [LegacyXmlOperation]
    public async Task<IActionResult> SystemActivity([FromServices] IActivityQueryHandler handler, [FromQuery] string? assetType, CancellationToken cancellationToken) =>
        (await handler.GetActivityFeedAsync(assetType, cancellationToken)).ToLegacyResult(LegacyWire.Rss);

    // ---- Search ----

    /// <summary>Json.NET text as ASCII, application/json; an absent or empty phrase is the null stream.</summary>
    [HttpGet("Search")]
    public async Task<IActionResult> Search([FromServices] ISearchQueryHandler handler, [FromQuery] string? phrase, [FromQuery] Guid userId,
        [FromQuery] SearchType searchType, CancellationToken cancellationToken) =>
        (await handler.SearchAsync(phrase, userId, searchType, cancellationToken))
            .ToLegacyResult(results => results is null ? LegacyWire.NullStream() : LegacyWire.JsonStream(JsonConvert.SerializeObject(results)));

    /// <summary>Json.NET text as ASCII, text/html; an absent or empty phrase is the null stream.</summary>
    [HttpGet("SearchTag")]
    public async Task<IActionResult> SearchTag([FromServices] ISearchQueryHandler handler, [FromQuery] string? phrase, CancellationToken cancellationToken) =>
        (await handler.SearchTagAsync(phrase, cancellationToken))
            .ToLegacyResult(tags => tags is null ? LegacyWire.NullStream() : LegacyWire.HtmlStream(JsonConvert.SerializeObject(tags)));

    private static IActionResult TypedDictionary(IReadOnlyList<KeyValuePair<Guid, string>> values)
    {
        var dictionary = new Dictionary<Guid, string>();
        foreach (var (key, value) in values)
            dictionary.Add(key, value);
        return LegacyWire.Typed(dictionary);
    }
}

/// <summary>The TriggerCreate body as the original serializer bound it (spec M7-S 0.4, FX T).</summary>
public static class ActivityBodies
{
    private static readonly string[] TriggerMembers = ["id", "name", "description", "type", "assetTypeIds", "specific", "properties"];
    private static readonly string[] PropertyMembers = ["propertyId", "operatorId", "value"];

    /// <summary>
    /// TriggerClient: the seven members required (case-sensitive), strings from any scalar, type an integer (quoted
    /// digits allowed, names and null rejected), specific a non-null boolean, lists null or arrays (an object reads as
    /// an empty list), property entries null or complete. Null for an empty or null body.
    /// </summary>
    public static TriggerBody? Trigger(JsonElement? root)
    {
        if (root is null)
            return null;
        var r = root.Value;
        if (r.ValueKind != JsonValueKind.Object)
            throw new LegacyBodyException("Missing required members.");
        foreach (var name in TriggerMembers)
        {
            if (!r.TryGetProperty(name, out _))
                throw new LegacyBodyException("Missing a required member.");
        }
        var properties = r.GetProperty("properties");
        if (properties.ValueKind == JsonValueKind.Array)
        {
            foreach (var property in properties.EnumerateArray())
            {
                if (property.ValueKind == JsonValueKind.Null)
                    continue;
                if (property.ValueKind != JsonValueKind.Object)
                    throw new LegacyBodyException("Not a trigger property.");
                foreach (var name in PropertyMembers)
                {
                    if (!property.TryGetProperty(name, out _))
                        throw new LegacyBodyException("Missing a required member.");
                }
            }
        }
        return new TriggerBody(
            LegacyJsonBody.Text(r.GetProperty("id")),
            LegacyJsonBody.Text(r.GetProperty("name")),
            LegacyJsonBody.Text(r.GetProperty("description")),
            Int64(r.GetProperty("type")),
            StringList(r.GetProperty("assetTypeIds")),
            XmlBoolean(r.GetProperty("specific")));
    }

    private static int Int64(JsonElement value)
    {
        var text = value.ValueKind switch
        {
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.String => value.GetString(),
            _ => null
        };
        if (text is not null && long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            return unchecked((int)number);
        throw new LegacyBodyException("Not an Int64.");
    }

    private static bool XmlBoolean(JsonElement value)
    {
        var text = value.ValueKind switch
        {
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
        return text?.Trim(' ', '\t', '\r', '\n') switch
        {
            "true" or "1" => true,
            "false" or "0" => false,
            _ => throw new LegacyBodyException("Not a boolean.")
        };
    }

    private static List<string?>? StringList(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Null:
                return null;
            case JsonValueKind.Object:
                return [];
            case JsonValueKind.Array:
                var list = new List<string?>();
                foreach (var item in value.EnumerateArray())
                    list.Add(LegacyJsonBody.Text(item));
                return list;
            default:
                throw new LegacyBodyException("Not an array.");
        }
    }
}
