using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xdb.Api.Application.Assets.Legacy;
using Xdb.Api.Application.Assets.Writes;
using Xdb.Api.Application.Common.Identity;
using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Application.Users.Legacy;
using Xdb.Api.Infrastructure.Data.Procedures;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Users.Commands;

public interface IUserCommandHandler
{
    Task<Result<bool>> CreateMemberAsync(Guid id, string? userName, string? lastName, string? firstName, CancellationToken cancellationToken = default);
    Task<Result<bool>> DisableReleaseNoticeAsync(Guid userId, string? version, CancellationToken cancellationToken = default);
    Task<Result<bool>> SavePreferenceAsync(Guid userId, Guid optionTypeId, string? optionValue, CancellationToken cancellationToken = default);
    Task<Result<bool>> CreateBugAsync(int area, int criticality, string? title, string? bugText, CancellationToken cancellationToken = default);
    Task<Result<bool>> CreateSuggestionAsync(string? title, string? suggestion, CancellationToken cancellationToken = default);
    Task<Result<bool>> PinAlertAsync(string? alertId, bool pin, CancellationToken cancellationToken = default);
    Task<Result<bool>> RespondAsync(List<QuestionResponse?>? responses, CancellationToken cancellationToken = default);
}

/// <summary>
/// The member, preference, feedback, alert and question writes (spec M7-U section 2). None of the original operations
/// caught a LogicalException, so every failure, logical ones included, is the generic fault. No transactions.
/// </summary>
public sealed class UserCommandHandler(
    IUserProcedures users, IMetadataProcedures metadata, IAssetWriteProcedures writes, IAssetProcedures assets, AssetWriter assetWriter, SubmittalEngine engine,
    CurrentMember currentMember, IDateTimeProvider clock) : IUserCommandHandler
{
    private const int MemberTextLimit = 50;

    private DateTime Now() => clock.UtcNow().ToLocalTime();

    /// <summary>The Member setters: longer than 50 characters keeps 49.</summary>
    private static string? MemberText(string? text) => text is { Length: > MemberTextLimit } ? text[..(MemberTextLimit - 1)] : text;

    /// <summary>
    /// User_Create: MemberLayer.Save (validate, spr_Member_Save), then the member's User asset (created by the system
    /// administrator). spr_Member_Save runs its insert even after updating an existing id, so that case faults with
    /// the update kept; an empty id inserts the member and then fails the asset validation.
    /// </summary>
    public async Task<Result<bool>> CreateMemberAsync(Guid id, string? userName, string? lastName, string? firstName, CancellationToken cancellationToken = default)
    {
        var login = MemberText(userName);
        var last = MemberText(lastName);
        var first = MemberText(firstName);
        var created = Now();
        var createdBy = await currentMember.GetIdAsync(cancellationToken);

        if (string.IsNullOrEmpty(login))
            throw new LegacyLogicalException("UserId cannot be null");
        if (!await metadata.IsLiveMemberIdAsync(createdBy, cancellationToken))
            throw new LegacyLogicalException("Invalid user id");
        await users.SaveMemberAsync(id, login, last, first, created, createdBy, cancellationToken);

        var assetCreated = Now();
        var asset = new AssetContract
        {
            Id = id, Name = AssetWriter.Name(login), AssetTypeId = SystemAssetTypeIds.User, Description = null, CreatedBy = SystemMemberIds.Admin, Created = assetCreated,
            Approved = assetCreated, ApprovedBy = SystemMemberIds.Admin, DisplayValue = AssetWriter.DisplayValue($"{last}, {first} [{login}]"), IsNew = true, IsDirty = true
        };
        await assetWriter.SaveAsync(asset, [], cancellationToken);
        return new Success<bool>(true);
    }

    /// <summary>DisableReleaseNotice: one MemberSettings row "relNotes" + version = "true" (no duplicate check).</summary>
    public async Task<Result<bool>> DisableReleaseNoticeAsync(Guid userId, string? version, CancellationToken cancellationToken = default)
    {
        await users.InsertMemberSettingAsync(userId, "relNotes" + version, "true", cancellationToken);
        return new Success<bool>(true);
    }

    public async Task<Result<bool>> SavePreferenceAsync(Guid userId, Guid optionTypeId, string? optionValue, CancellationToken cancellationToken = default) =>
        new Success<bool>(await users.SaveUserPreferenceAsync(userId, optionTypeId, optionValue, cancellationToken));

    public async Task<Result<bool>> CreateBugAsync(int area, int criticality, string? title, string? bugText, CancellationToken cancellationToken = default)
    {
        await users.InsertBugAsync(area, criticality, title, bugText, await currentMember.GetIdAsync(cancellationToken), cancellationToken);
        return new Success<bool>(true);
    }

    /// <summary>Suggestion_Create: the posted member id is ignored; the current member is recorded.</summary>
    public async Task<Result<bool>> CreateSuggestionAsync(string? title, string? suggestion, CancellationToken cancellationToken = default)
    {
        await users.InsertSuggestionAsync(title, suggestion, await currentMember.GetIdAsync(cancellationToken), cancellationToken);
        return new Success<bool>(true);
    }

    public async Task<Result<bool>> PinAlertAsync(string? alertId, bool pin, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(alertId, out var id))
            return new Success<bool>(false);
        await users.PinAlertAsync(id, pin, cancellationToken);
        return new Success<bool>(true);
    }

    /// <summary>
    /// QuestionsRespond (spec M7-U 2.6, DLL): every response is converted first (a bad question id fails before any
    /// SQL), then all are saved and finished groups closed, then each "No" deletes the question's value or, with an
    /// alternate text, replaces it through an auto-approved submittal by the question's member.
    /// </summary>
    public async Task<Result<bool>> RespondAsync(List<QuestionResponse?>? responses, CancellationToken cancellationToken = default)
    {
        if (responses is null)
            throw new NullReferenceException("Object reference not set to an instance of an object.");
        var values = new List<(Guid QuestionId, short Value, string? Text, DateTime Created)>();
        foreach (var response in responses)
        {
            if (response is null)
                throw new NullReferenceException("Object reference not set to an instance of an object.");
            values.Add((new Guid(response.QuestionId ?? throw new ArgumentNullException("g")), response.ResponseValue, response.ResponseText, Now()));
        }

        var groups = new List<Guid>();
        foreach (var value in values)
        {
            await users.SaveResponseAsync(value.QuestionId, value.Value, string.IsNullOrEmpty(value.Text) ? string.Empty : value.Text, value.Created, cancellationToken);
            var question = await users.GetQuestionAsync(value.QuestionId, cancellationToken)
                ?? throw new NullReferenceException("Object reference not set to an instance of an object.");
            if (!groups.Contains(question.QuestionGroupId))
                groups.Add(question.QuestionGroupId);
        }
        foreach (var group in groups)
        {
            if (await users.CountOpenQuestionsAsync(group, cancellationToken) == 0)
                await users.CloseQuestionGroupAsync(group, cancellationToken);
        }

        foreach (var value in values)
        {
            if (value.Value != 0)
                continue;
            if (await users.GetQuestionAsync(value.QuestionId, cancellationToken) is not { } question)
                continue;
            if (string.IsNullOrEmpty(value.Text))
            {
                await writes.DeleteValueAsync(question.PropertyValueId, question.MemberId, cancellationToken);
                continue;
            }
            var previous = await assets.GetPropertyValueAsync(question.PropertyValueId, cancellationToken);
            await writes.DeleteValueAsync(question.PropertyValueId, question.MemberId, cancellationToken);
            if (previous is null)
                throw new NullReferenceException("Object reference not set to an instance of an object.");
            _ = await assets.GetAssetNameTextAsync(previous.AssetId, cancellationToken);
            var submittal = new Submittal(Guid.NewGuid(), previous.AssetId, Now(), question.MemberId);
            submittal.Values.Add(new SubmittalValue
            {
                Id = Guid.NewGuid(), PropertyId = previous.PropertyId, AssetId = previous.AssetId, Value = value.Text, CreatedBy = question.MemberId, Created = Now()
            });
            await engine.SaveAsync(submittal, isAutoApprove: true, question.MemberId, cancellationToken);
        }
        return new Success<bool>(true);
    }
}
