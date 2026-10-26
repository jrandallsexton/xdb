using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using FluentValidation;
using FluentValidation.Results;

using Xdb.Api.Application.Assets.Commands.CreateAssets;
using Xdb.Api.Application.GeneratedObjects.Commands.RegenerateAll;
using Xdb.Core.Common;

namespace Xdb.Api.Application.Assets.Commands.CreateAsset;

/// <param name="Id">The new asset's id.</param>
/// <param name="Failures">Gen-table refreshes that failed; the asset itself was still created.</param>
public sealed record CreateAssetResult(Guid Id, IReadOnlyList<GenerationFailure> Failures);

public interface ICreateAssetCommandHandler
{
    /// <summary>The new asset (Created); Conflict when the id is taken; Validation for a bad command.</summary>
    Task<Result<CreateAssetResult>> ExecuteAsync(CreateAssetCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Replaces the legacy Asset_Create / Asset_CreateInstance plus PropertyValues_Save path (AssetWriter and
/// SubmittalEngine over raw SQL). The asset, one reviewed submittal, its name value and its property values are
/// written together, approved by the actor; the original wrote them statement by statement in two passes. The
/// asset is then merged into its type stack's gen tables (the original queued that in the background). Not carried
/// over: legacy narratives (written for fixed legacy types), parent relations and view default values. The work is
/// <see cref="CreateAssetsCommandHandler"/>'s, for a batch of one.
/// </summary>
public sealed class CreateAssetCommandHandler(IValidator<CreateAssetCommand> validator, ICreateAssetsCommandHandler assets) : ICreateAssetCommandHandler
{
    public async Task<Result<CreateAssetResult>> ExecuteAsync(CreateAssetCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Failed(ResultStatus.Validation, validation.Errors);

        var id = command.Id ?? Guid.NewGuid();
        var result = await assets.ExecuteAsync(new CreateAssetsCommand
        {
            ActorId = command.ActorId,
            AssetTypeId = command.AssetTypeId,
            Assets =
            [
                new CreateAssetsItem { Id = id, Name = command.Name, Description = command.Description, InstanceOfId = command.InstanceOfId, Values = command.Values }
            ]
        }, cancellationToken);

        if (result is Failure<CreateAssetsResult> failure)
            return Failed(failure.Status, failure.Errors);
        if (result.Value.Rejected is [var rejected])
            return Failed(rejected.Status, [.. rejected.Errors]);
        return new Success<CreateAssetResult>(new CreateAssetResult(id, result.Value.Failures), ResultStatus.Created);
    }

    private static Failure<CreateAssetResult> Failed(ResultStatus status, List<ValidationFailure> errors) =>
        new(default!, status, errors);
}
