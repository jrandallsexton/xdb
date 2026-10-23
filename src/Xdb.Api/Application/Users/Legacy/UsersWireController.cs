using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Users.Commands;
using Xdb.Api.Application.Users.Queries;
using Xdb.Core.LegacyWire;

namespace Xdb.Api.Application.Users.Legacy;

/// <summary>
/// The original service's member, error, feedback, alert and question operations (spec M7-U). None of them caught
/// logical failures, so every failure is the generic fault.
/// </summary>
[Route(LegacyWire.BaseRoute)]
public sealed class UsersWireController : LegacyWireControllerBase
{
    // ---- Members ----

    /// <summary>bName and bVersion are bound and unused.</summary>
    [HttpGet("UserClient_Get")]
    public async Task<IActionResult> UserClientGet([FromServices] IUserQueryHandler handler, [FromQuery] string? userId, [FromQuery] string? bName,
        [FromQuery] string? bVersion, CancellationToken cancellationToken) =>
        (await handler.GetUserClientAsync(userId, cancellationToken)).ToLegacyResult(LegacyWire.TypedOrNull);

    [HttpGet("User_Modified")]
    public async Task<IActionResult> UserModified([FromServices] IUserQueryHandler handler, [FromQuery] Guid userId, [FromQuery] string? checkDateTime,
        CancellationToken cancellationToken) =>
        (await handler.IsModifiedAsync(userId, checkDateTime, cancellationToken)).ToLegacyResult(modified => LegacyWire.Typed(modified));

    [HttpGet("UserId_Get")]
    public async Task<IActionResult> UserIdGet([FromServices] IUserQueryHandler handler, [FromQuery] string? userId, CancellationToken cancellationToken) =>
        (await handler.GetMemberIdAsync(userId, cancellationToken)).ToLegacyResult(id => id is { } memberId ? LegacyWire.Typed(memberId) : LegacyWire.NullTyped());

    [HttpGet("UserId_GetCurrent")]
    public async Task<IActionResult> UserIdGetCurrent([FromServices] IUserQueryHandler handler, CancellationToken cancellationToken) =>
        (await handler.GetCurrentMemberIdAsync(cancellationToken)).ToLegacyResult(id => LegacyWire.Typed(id));

    [HttpGet("User_GetDisplayName")]
    public async Task<IActionResult> UserGetDisplayName([FromServices] IUserQueryHandler handler, [FromQuery] Guid userId, CancellationToken cancellationToken) =>
        (await handler.GetDisplayNameAsync(userId, cancellationToken)).ToLegacyResult(name => LegacyWire.Typed(name));

    [HttpGet("User_GetActivity")]
    public async Task<IActionResult> UserGetActivity([FromServices] IUserQueryHandler handler, [FromQuery] Guid userId, CancellationToken cancellationToken) =>
        (await handler.GetActivityAsync(userId, cancellationToken)).ToLegacyResult(visits => LegacyWire.Typed(visits));

    /// <summary>userId is bound (a bad one is the request-error page) and unused.</summary>
    [HttpGet("Users_GetAll")]
    public async Task<IActionResult> UsersGetAll([FromServices] IUserQueryHandler handler, [FromQuery] Guid userId, CancellationToken cancellationToken) =>
        (await handler.GetMembersAsync(cancellationToken)).ToLegacyResult(members => LegacyWire.HtmlStream(LegacyFormatters.FormatDictionary(members)));

    [HttpGet("Users_GetDictionary")]
    public async Task<IActionResult> UsersGetDictionary([FromServices] IUserQueryHandler handler, [FromQuery] Guid userId, CancellationToken cancellationToken) =>
        (await handler.GetMembersForAsync(userId, cancellationToken)).ToLegacyResult(members => members is null ? LegacyWire.NullTyped() : TypedDictionary(members));

    /// <summary>Wrapped body: id, userName, lName, fName, mName (bound and unused).</summary>
    [HttpPost("User_Create")]
    public async Task<IActionResult> UserCreate([FromServices] IUserCommandHandler handler, CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken, wrapped: true);
        if (error is not null)
            return error;
        if (WrappedRootFails(root))
            return LegacyWire.RequestError();
        Guid id;
        string? userName, lastName, firstName;
        try
        {
            id = LegacyJsonBody.Member(root, "id", LegacyJsonBody.Guid, Guid.Empty);
            userName = LegacyJsonBody.Member(root, "userName", LegacyJsonBody.Text, null);
            lastName = LegacyJsonBody.Member(root, "lName", LegacyJsonBody.Text, null);
            firstName = LegacyJsonBody.Member(root, "fName", LegacyJsonBody.Text, null);
            _ = LegacyJsonBody.Member(root, "mName", LegacyJsonBody.Text, null);
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        return (await handler.CreateMemberAsync(id, userName, lastName, firstName, cancellationToken)).ToLegacyResult(done => LegacyWire.Typed(done));
    }

    [HttpGet("UserPreference_Save")]
    public async Task<IActionResult> UserPreferenceSave([FromServices] IUserCommandHandler handler, [FromQuery] Guid userId, [FromQuery] Guid optionTypeId,
        [FromQuery] string? optionValue, CancellationToken cancellationToken) =>
        (await handler.SavePreferenceAsync(userId, optionTypeId, optionValue, cancellationToken)).ToLegacyResult(done => LegacyWire.Typed(done));

    [HttpGet("DisableReleaseNotice")]
    public async Task<IActionResult> DisableReleaseNotice([FromServices] IUserCommandHandler handler, [FromQuery] Guid userId, [FromQuery] string? version,
        CancellationToken cancellationToken) =>
        (await handler.DisableReleaseNoticeAsync(userId, version, cancellationToken)).ToLegacyResult(done => LegacyWire.Typed(done));

    [HttpGet("Domains")]
    public async Task<IActionResult> Domains([FromServices] IUserQueryHandler handler, CancellationToken cancellationToken) =>
        (await handler.GetDomainsAsync(cancellationToken)).ToLegacyResult(domains => LegacyWire.Typed(domains));

    /// <summary>
    /// Environment-bound: the original asked a directory service that does not exist in this environment and always
    /// faulted. The port faults without any network call.
    /// </summary>
    [HttpGet("UserData_Get")]
    public IActionResult UserDataGet([FromQuery] string? userId) =>
        throw new InvalidOperationException("The directory service is not available.");

    // ---- Errors and feedback ----

    [HttpGet("Error_Get")]
    public async Task<IActionResult> ErrorGet([FromServices] IUserQueryHandler handler, [FromQuery] string? id, CancellationToken cancellationToken) =>
        (await handler.GetErrorAsync(id, cancellationToken)).ToLegacyResult(LegacyWire.TypedOrNull);

    /// <summary>Wrapped body: area, criticality (Int32), title, bugText.</summary>
    [HttpPost("Bug_Create")]
    public async Task<IActionResult> BugCreate([FromServices] IUserCommandHandler handler, CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken, wrapped: true);
        if (error is not null)
            return error;
        // Unlike the string-only Suggestion_Create, an empty body here was the request-error page (measured).
        if (root is null || WrappedRootFails(root))
            return LegacyWire.RequestError();
        int area, criticality;
        string? title, bugText;
        try
        {
            area = LegacyJsonBody.Member(root, "area", LegacyJsonBody.Int, 0);
            criticality = LegacyJsonBody.Member(root, "criticality", LegacyJsonBody.Int, 0);
            title = LegacyJsonBody.Member(root, "title", LegacyJsonBody.Text, null);
            bugText = LegacyJsonBody.Member(root, "bugText", LegacyJsonBody.Text, null);
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        return (await handler.CreateBugAsync(area, criticality, title, bugText, cancellationToken)).ToLegacyResult(done => LegacyWire.Typed(done));
    }

    /// <summary>Wrapped body: title, suggestion, memberId (bound and unused).</summary>
    [HttpPost("Suggestion_Create")]
    public async Task<IActionResult> SuggestionCreate([FromServices] IUserCommandHandler handler, CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken, wrapped: true);
        if (error is not null)
            return error;
        if (WrappedRootFails(root))
            return LegacyWire.RequestError();
        string? title, suggestion;
        try
        {
            title = LegacyJsonBody.Member(root, "title", LegacyJsonBody.Text, null);
            suggestion = LegacyJsonBody.Member(root, "suggestion", LegacyJsonBody.Text, null);
            _ = LegacyJsonBody.Member(root, "memberId", LegacyJsonBody.Guid, Guid.Empty);
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        return (await handler.CreateSuggestionAsync(title, suggestion, cancellationToken)).ToLegacyResult(done => LegacyWire.Typed(done));
    }

    // ---- Alerts and questions ----

    [HttpGet("Alerts")]
    public async Task<IActionResult> Alerts([FromServices] IUserQueryHandler handler, [FromQuery] string? userId, CancellationToken cancellationToken) =>
        (await handler.GetAlertsAsync(userId, cancellationToken)).ToLegacyResult(LegacyWire.TypedOrNull);

    [HttpGet("AlertCount")]
    public async Task<IActionResult> AlertCount([FromServices] IUserQueryHandler handler, [FromQuery] string? userId, CancellationToken cancellationToken) =>
        (await handler.CountAlertsAsync(userId, cancellationToken)).ToLegacyResult(count => LegacyWire.Typed(count));

    /// <summary>Bare KeyValuePair&lt;string,bool&gt; body.</summary>
    [HttpPost("AlertPin")]
    public async Task<IActionResult> AlertPin([FromServices] IUserCommandHandler handler, CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken);
        if (error is not null)
            return error;
        (string? Key, bool Value) pair;
        try
        {
            pair = UserBodies.Pin(root);
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        return (await handler.PinAlertAsync(pair.Key, pair.Value, cancellationToken)).ToLegacyResult(done => LegacyWire.Typed(done));
    }

    [HttpGet("Questions")]
    public async Task<IActionResult> Questions([FromServices] IUserQueryHandler handler, [FromQuery] Guid userId, CancellationToken cancellationToken) =>
        (await handler.GetQuestionsAsync(userId, cancellationToken)).ToLegacyResult(groups => LegacyWire.Typed(groups));

    /// <summary>Bare List&lt;response&gt; body.</summary>
    [HttpPost("QuestionsRespond")]
    public async Task<IActionResult> QuestionsRespond([FromServices] IUserCommandHandler handler, CancellationToken cancellationToken)
    {
        var (root, error) = await LegacyJsonBody.ReadAsync(Request, cancellationToken);
        if (error is not null)
            return error;
        List<QuestionResponse?>? responses;
        try
        {
            responses = UserBodies.Responses(root);
        }
        catch (LegacyBodyException e)
        {
            return e.ToResult();
        }
        return (await handler.RespondAsync(responses, cancellationToken)).ToLegacyResult(done => LegacyWire.Typed(done));
    }

    private bool WrappedRootFails(JsonElement? root) =>
        root is null ? Request.ContentLength is > 0 : root.Value.ValueKind != JsonValueKind.Object;

    private static IActionResult TypedDictionary(IReadOnlyList<KeyValuePair<Guid, string>> values)
    {
        var dictionary = new Dictionary<Guid, string>();
        foreach (var (key, value) in values)
            dictionary.Add(key, value);
        return LegacyWire.Typed(dictionary);
    }
}

/// <summary>The bare bodies of AlertPin and QuestionsRespond as the original serializer bound them (spec M7-U 0.3, FX P and R).</summary>
public static class UserBodies
{
    /// <summary>
    /// KeyValuePair&lt;string,bool&gt;: lowercase key and value both required; the key any scalar (as text), the value
    /// true/false or "true"/"false"/1/0. An empty or null body is the default pair (null, false).
    /// </summary>
    public static (string? Key, bool Value) Pin(JsonElement? root)
    {
        if (root is null)
            return (null, false);
        var r = root.Value;
        if (r.ValueKind != JsonValueKind.Object || !r.TryGetProperty("key", out var key) || !r.TryGetProperty("value", out var value))
            throw new LegacyBodyException("Missing a required member.");
        return (LegacyJsonBody.Text(key), XmlBoolean(value));
    }

    /// <summary>
    /// List&lt;response&gt;: an array of objects with questionId (any scalar) and responseValue (Int16) required and
    /// responseText optional; an object root binds an empty list, a null element stays null, a null body is null.
    /// </summary>
    public static List<QuestionResponse?>? Responses(JsonElement? root)
    {
        if (root is null)
            return null;
        var r = root.Value;
        if (r.ValueKind == JsonValueKind.Object)
            return [];
        if (r.ValueKind != JsonValueKind.Array)
            throw new LegacyBodyException("Not an array.");
        var result = new List<QuestionResponse?>();
        foreach (var item in r.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Null)
            {
                result.Add(null);
                continue;
            }
            if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("questionId", out var questionId) || !item.TryGetProperty("responseValue", out var responseValue))
                throw new LegacyBodyException("Missing a required member.");
            var text = item.TryGetProperty("responseText", out var responseText) ? LegacyJsonBody.Text(responseText) : null;
            result.Add(new QuestionResponse(LegacyJsonBody.Text(questionId), Int16(responseValue), text));
        }
        return result;
    }

    /// <summary>XmlConvert.ToBoolean over the member's text: "true", "false", "1" or "0" (surrounding white space allowed).</summary>
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

    private static short Int16(JsonElement value)
    {
        var text = value.ValueKind switch
        {
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.String => value.GetString(),
            _ => null
        };
        if (text is not null && short.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            return number;
        throw new LegacyBodyException("Not an Int16.");
    }
}
