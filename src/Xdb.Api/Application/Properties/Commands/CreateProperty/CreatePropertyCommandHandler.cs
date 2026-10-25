using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using FluentValidation;
using FluentValidation.Results;

using Microsoft.EntityFrameworkCore;

using Xdb.Api.Application.Common.Metadata;
using Xdb.Api.Infrastructure.Data;
using Xdb.Api.Infrastructure.Data.Entities;
using Xdb.Core.Common;

using DataType = Xdb.Api.Application.Common.Metadata.DataType;

namespace Xdb.Api.Application.Properties.Commands.CreateProperty;

public interface ICreatePropertyCommandHandler
{
    /// <summary>The new property's id (Created); Conflict when the id is taken; Validation for a bad command.</summary>
    Task<Result<Guid>> ExecuteAsync(CreatePropertyCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Replaces the create half of the legacy Property_Save (PropertySaver and spr_Property_Save, a plain upsert with no
/// side effects). The property is created approved by its creator, as nearly all legacy properties were; generated
/// objects are untouched until a type links the property.
/// </summary>
public sealed class CreatePropertyCommandHandler(
    XdbDataContext db,
    IValidator<CreatePropertyCommand> validator,
    IDateTimeProvider clock) : ICreatePropertyCommandHandler
{
    public async Task<Result<Guid>> ExecuteAsync(CreatePropertyCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return new Failure<Guid>(Guid.Empty, ResultStatus.Validation, validation.Errors);

        var errors = await ValidateReferencesAsync(command, cancellationToken);
        if (errors.Count > 0)
            return new Failure<Guid>(Guid.Empty, ResultStatus.Validation, errors);

        var id = command.Id ?? Guid.NewGuid();
        if (await db.Properties.AsNoTracking().AnyAsync(p => p.Id == id, cancellationToken))
            return new Failure<Guid>(Guid.Empty, ResultStatus.Conflict,
                [new ValidationFailure(nameof(CreatePropertyCommand.Id), $"Property {id} already exists.")]);

        var isUser = command.DataType == DataType.User;
        // New slices write UTC. Rows written by the legacy paths hold local (Eastern) time.
        var now = clock.UtcNow();

        db.Properties.Add(new Property
        {
            Id = id,
            DataTypeId = (byte)command.DataType,
            Name = command.Name,
            DisplayValue = command.DisplayValue,
            Description = command.Description,
            PickListId = command.PickListId,
            AssetTypeId = isUser ? SystemAssetTypeIds.User : command.AssetTypeId,
            // The User type holds definitions only.
            AssetTypeIsInstance = isUser ? false : command.AssetTypeIsInstance,
            RoleId = command.RoleId,
            AllowMultiValue = command.AllowMultiValue,
            IsOrdered = command.IsOrdered,
            IsSystem = false,
            Created = now,
            CreatedBy = command.ActorId,
            Approved = now,
            ApprovedBy = command.ActorId
        });
        await db.SaveChangesAsync(cancellationToken);

        return new Success<Guid>(id, ResultStatus.Created);
    }

    private async Task<List<ValidationFailure>> ValidateReferencesAsync(CreatePropertyCommand command, CancellationToken cancellationToken)
    {
        var errors = new List<ValidationFailure>();

        if (!await db.Members.AsNoTracking().AnyAsync(m => m.Id == command.ActorId && m.Deleted == null, cancellationToken))
            errors.Add(new ValidationFailure(nameof(CreatePropertyCommand.ActorId), "The actor is not a live member."));

        if (command.PickListId is { } pickListId
            && !await db.PickLists.AsNoTracking().AnyAsync(l => l.Id == pickListId && l.Deleted == null, cancellationToken))
            errors.Add(new ValidationFailure(nameof(CreatePropertyCommand.PickListId), $"Pick list {pickListId} does not exist."));

        if (command.DataType != DataType.User && command.AssetTypeId is { } assetTypeId
            && !await db.AssetTypes.AsNoTracking().AnyAsync(t => t.Id == assetTypeId && t.Deleted == null, cancellationToken))
            errors.Add(new ValidationFailure(nameof(CreatePropertyCommand.AssetTypeId), $"Asset type {assetTypeId} does not exist."));

        if (command.RoleId is { } roleId
            && !await db.Roles.AsNoTracking().AnyAsync(r => r.Id == roleId && r.Deleted == null, cancellationToken))
            errors.Add(new ValidationFailure(nameof(CreatePropertyCommand.RoleId), $"Role {roleId} does not exist."));

        return errors;
    }
}
